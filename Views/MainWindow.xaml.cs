using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PasswordManager.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static byte[] _vaultKey;
    public MainWindow(byte[] vaultKey)
    {
        InitializeComponent();
        _vaultKey = vaultKey;
    }

    private void CreatePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var createWindow = new PasswordCreation(_vaultKey);
        createWindow.Closed += (s, args) => this.Show(); // Показываем главное окно обратно
        this.Hide(); // Скрываем, но не закрываем
        createWindow.Show();
    }

    private void ShowPasswordsButton_Click(object sender, RoutedEventArgs e)
    {
        var listWindow = new PasswordList(_vaultKey);
        listWindow.Closed += (s, args) => this.Show(); 
        this.Hide();
        listWindow.Show();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        // Очищаем ключ из памяти при закрытии
        if (_vaultKey != null)
        {
            Array.Clear(_vaultKey, 0, _vaultKey.Length);
        }
        Application.Current.Shutdown();
    }
}