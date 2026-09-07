using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PasswordManager.Views
{
    public partial class MasterPasswordWindow : Window
    {
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(0x23, 0x7D, 0x32));
        
        public MasterPasswordWindow()
        {
            InitializeComponent();
            PasswordInput.Focus();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }   

        private void PasswordInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string password = PasswordInput.Text;

            bool lengthOK = password.Length >= 12;
            bool digitOK = password.Any(char.IsDigit);
            bool specialOK = password.Any(ch => !char.IsLetterOrDigit(ch));
            bool upperOK = password.Any(char.IsUpper);
            bool lowerOK = password.Any(char.IsLower);

            UpdateCriterion(CriterionLength, lengthOK);
            UpdateCriterion(CriterionDigit, digitOK);
            UpdateCriterion(CriterionSpecial, specialOK);
            UpdateCriterion(CriterionUpper, upperOK);
            UpdateCriterion(CriterionLower, lowerOK);

            CreateButton.IsEnabled = lengthOK && digitOK && specialOK && upperOK && lowerOK;
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string masterPassword = PasswordInput.Text;
            
            // Блокируем кнопку, чтобы не нажали дважды
            CreateButton.IsEnabled = false;
            CreateButton.Content = "Создание...";

            try {
                byte[] key = await Task.Run(() => {
                    byte[] salt = RandomNumberGenerator.GetBytes(32);
                    byte[] derivedKey = DeriveKey(masterPassword, salt);
                    byte[] verifier = SHA256.HashData(derivedKey);

                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    string dataDir = System.IO.Path.Combine(appDir, "data");
                    System.IO.Directory.CreateDirectory(dataDir);
                    string masterPasswordPath = System.IO.Path.Combine(dataDir, "master.dat");

                    using var fileStream = new System.IO.FileStream(masterPasswordPath, System.IO.FileMode.Create);
                    using var binaryWriter = new System.IO.BinaryWriter(fileStream);

                    binaryWriter.Write(600_000);
                    binaryWriter.Write(salt);
                    binaryWriter.Write(verifier);

                    string vaultPath = System.IO.Path.Combine(dataDir, "vault.enc");
                    byte[] emptyVault = EncryptVault("[]", derivedKey);
                    System.IO.File.WriteAllBytes(vaultPath, emptyVault);

                    return derivedKey;
                });

                var mainWindow = new MainWindow(key);
                mainWindow.Show();
                this.Close();
            } catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                CreateButton.IsEnabled = true;
                CreateButton.Content = "Создать мастер-пароль";
            }
        }

        private void UpdateCriterion(Border border, bool isOk)
        {
            var textBlock = (TextBlock)border.Child;

            if(isOk)
            {
                border.BorderBrush = GreenBrush;
                textBlock.Foreground = GreenBrush;
            }
            else
            {
                border.BorderBrush = RedBrush;
                textBlock.Foreground = RedBrush;
            }
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var pdkdf2 = new Rfc2898DeriveBytes(password, salt, 600_000, HashAlgorithmName.SHA256);
            return pdkdf2.GetBytes(32);
        }

        private byte[] EncryptVault(string json, byte[] key)
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] cipherText = new byte[plainBytes.Length];
            byte[] tag = new byte[32];

            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plainBytes, cipherText, tag);

            byte[] result = new byte[12 + cipherText.Length + 16];
            Buffer.BlockCopy(nonce, 0, result, 0, 12);
            Buffer.BlockCopy(cipherText, 0, result, 12, cipherText.Length);
            Buffer.BlockCopy(tag, 0, result, 12 + cipherText.Length, 16);

            return result;
        }
    }
}