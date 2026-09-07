using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PasswordManager.Services;
using PasswordManager.Models;

namespace PasswordManager.Views
{
    public partial class PasswordCreation : Window
    {
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(0xD3, 0x2F, 0x2F));
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));        
        private byte[] _vaultKey;

        #region Инициализация и загрузка ключа
        public PasswordCreation(byte[] vaultKey)
        {
            
            InitializeComponent();
            _vaultKey = vaultKey;
            PasswordInput.Focus();
        }
        #endregion

        #region Кнопка "Назад"
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Show();
            }
            this.Close();   
        }
        #endregion

        #region Провека критериев
        private void PasswordInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckCriteria(); // Проверка наличия пароля
        }

        private void LoginInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckCriteria(); // Проверка наличия логина
        }
        private void CheckCriteria()
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

            // Кнопка активна только если все критерии выполнены И заполнен логин
            string login = LoginInput.Text.Trim();
            SaveButton.IsEnabled = lengthOK && digitOK && specialOK && upperOK && lowerOK && !string.IsNullOrEmpty(login);
        }

        private void UpdateCriterion(Border border, bool isMet)
        {
            var textBlock = (TextBlock)border.Child;

            if (isMet)
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

        #endregion

        #region Кнопка "Сгенерировать"
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string generatedPassword = GenerateSecurePassword();
            PasswordInput.Text = generatedPassword;
            CheckCriteria(); // Проверяем критерии для сгенерированного пароля
        }

        private string GenerateSecurePassword()
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var allChars = upperCase + lowerCase + digits + special;

            int passwordLength = RandomNumberGenerator.GetInt32(12, 26); // Случайная длинна от 12 до 25 символов

            var password = new StringBuilder();

            // Гарант хотя бы одиного символа каждого типа
            password.Append(upperCase[RandomNumberGenerator.GetInt32(upperCase.Length)]);
            password.Append(lowerCase[RandomNumberGenerator.GetInt32(lowerCase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            // Заполнение остальных символов случайным образом
            int remainingLength = passwordLength - 4; // Уже добавили 4 символа
            for (int i = 0; i < remainingLength; i++)
            {
                password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }

            // Алгоритм Фишера-Йейтса для перемешивания пароля
            var passwordArray = password.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(0, i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }

            return new string(passwordArray);
        }
        #endregion

        #region Кнопка "Сохранить пароль"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {   
            string login = LoginInput.Text.Trim();
            string password = PasswordInput.Text;

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_vaultKey == null)
            {
                MessageBox.Show("Не удалось получить ключ шифрования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Читаем мастер-пароль для расшифровки vault
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string masterPath = Path.Combine(appDir, "data", "master.dat");
                string vaultPath = Path.Combine(appDir, "data", "vault.enc");

                // Читаем существующие записи
                var credentials = LoadCredentials(vaultPath);

                // Создаём новую запись
                var newCredential = new Credential
                {
                    Id = Guid.NewGuid().ToString(),
                    ServiceName = ServiceNameInput.Text.Trim(),
                    Login = login,
                    Password = password,
                    Note = "",
                    CreatedAt = DateTime.UtcNow
                };

                credentials.Add(newCredential);

                // Сохраняем обратно
                SaveCredentials(vaultPath, credentials, _vaultKey);

                MessageBox.Show("Пароль успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                // Возвращаемся в главное окно
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.Show();
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Credential> LoadCredentials(string vaultPath)
        {
            if (!File.Exists(vaultPath))
                return new List<Credential>();

            byte[] encryptedData = File.ReadAllBytes(vaultPath);
            
            try
            {
                string json = VaultCrypto.Decrypt(encryptedData, _vaultKey);
                return JsonSerializer.Deserialize<List<Credential>>(json) ?? new List<Credential>();
            }
            catch
            {
                // Если не удалось расшифровать - возвращаем пустой список
                return new List<Credential>();
            }
        }

        private void SaveCredentials(string vaultPath, List<Credential> credentials, byte[] key)
        {
            string json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            byte[] encryptedData = VaultCrypto.Encrypt(json, key);
            File.WriteAllBytes(vaultPath, encryptedData);
        }
        #endregion
    }
}