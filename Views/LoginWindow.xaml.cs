using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PasswordManager.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            PasswordInput.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredPassword = PasswordInput.Password;

            if (string.IsNullOrEmpty(enteredPassword))
            {
                ShowError("Введите пароль!");
                return;
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string masterPasswordPath = Path.Combine(appDir, "data", "master.dat");

            if(!File.Exists(masterPasswordPath))
            {
                ShowError("Файл мастер-пароля не найден!");
                return;
            }

            // Парсинг master.dat
            byte[] fileBytes = File.ReadAllBytes(masterPasswordPath);
            int iterations = BitConverter.ToInt32(fileBytes, 0);
            byte[] salt = new byte[32];
            byte[] storedVerifier = new byte[32];

            Buffer.BlockCopy(fileBytes, 4, salt, 0, 32);
            Buffer.BlockCopy(fileBytes, 36, storedVerifier, 0, 32);

            // Проверка ключа и верефикатора
            byte[] key = DeriveKey(enteredPassword, salt, iterations);
            byte[] computedVerifier = SHA256.HashData(key);

            // Сравнение (с защитой от timing-атак)
            if(CryptographicOperations.FixedTimeEquals(computedVerifier, storedVerifier))
            {
                var mainWindow = new MainWindow(key);
                mainWindow.Show();
                this.Close();
            } else
            {
                ShowError("Неверный мастер-пароль!");
                PasswordInput.Password = string.Empty;
                PasswordInput.Focus();
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private byte[] DeriveKey(string password, byte[] salt, int iterations)
        {
            using var pdkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);

            return pdkdf2.GetBytes(32);
        }
    }
}