using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;

namespace PasswordManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string masterPasswordPath = Path.Combine(appDir, "data", "master.dat");

        if(!File.Exists(masterPasswordPath))
        {
            // Первый запуск и создание мастер-пароля
            var createWindow = new Views.MasterPasswordWindow();
            createWindow.Show();
        } else
        {
            // Последующий запуск с запросом мастер-пароля
            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();
        }
    }
}

