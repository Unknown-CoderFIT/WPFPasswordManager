using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PasswordManager.Services;
using PasswordManager.Models;

namespace PasswordManager.Views
{
    public partial class PasswordList : Window
    {   
        private readonly byte[] _vaultKey;
        private ObservableCollection<Credential> _credentials;
        public PasswordList(byte[] vaultKey)
        {
            
            InitializeComponent();
            _vaultKey = vaultKey;

            LoadCredentials();
        }

        #region Загрузка данных
        private void LoadCredentials()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string vaultPath = Path.Combine(appDir, "data", "vault.enc");

            if (!File.Exists(vaultPath))
            {
                _credentials = new ObservableCollection<Credential>();
                CredentialsList.ItemsSource = _credentials;
                return;
            }

            try
            {
                byte[] encryptedData = File.ReadAllBytes(vaultPath);
                string json = VaultCrypto.Decrypt(encryptedData, _vaultKey);
                var credentials = JsonSerializer.Deserialize<ObservableCollection<Credential>>(json);

                _credentials = credentials ?? new ObservableCollection<Credential>();
                CredentialsList.ItemsSource = _credentials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _credentials = new ObservableCollection<Credential>();
                CredentialsList.ItemsSource = _credentials;
            }
        }
        #endregion

        #region Кнопка назад
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Show();
            }
            this.Close(); 
        }
        #endregion

        #region Кнопка "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            string id = button.Tag?.ToString();

            if (string.IsNullOrEmpty(id))
                return;

            var result = MessageBox.Show("Удалить эту запись?", "Подтверждение", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var credential = _credentials.FirstOrDefault(c => c.Id == id);
                if (credential != null)
                {
                    _credentials.Remove(credential);
                    SaveCredentials();
                }
            }
        }
        #endregion

        #region Кнопка "Скопировать логин"
        private void CopyLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            string login = button.Tag?.ToString();

            if (!string.IsNullOrEmpty(login))
            {
                Clipboard.SetText(login);
                ShowNotification($"Скопирован логин: {login}");
            }
        }
        #endregion

        #region Кнопка "Скопировать пароль"
        private void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            string password = button.Tag?.ToString();

            if (!string.IsNullOrEmpty(password))
            {
                Clipboard.SetText(password);
                ShowNotification("Скопирован пароль");
            }
        }
        #endregion

        #region Показ уведомлений
        private void ShowNotification(string message)
        {
            CopyNotification.Text = message;
            CopyNotification.Visibility = Visibility.Visible;

            // Скрыть через 3 секунды
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                CopyNotification.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }
        #endregion
        
        #region Обновление данных
        private void SaveCredentials()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string vaultPath = Path.Combine(appDir, "data", "vault.enc");

            string json = JsonSerializer.Serialize(_credentials, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            byte[] encryptedData = VaultCrypto.Encrypt(json, _vaultKey);
            File.WriteAllBytes(vaultPath, encryptedData);
        }
        #endregion
    }
}