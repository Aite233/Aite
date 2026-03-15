using System.Windows;
using System.IO;
using System.Reflection;
namespace Aite.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache"));
        string? exePath = Assembly.GetExecutingAssembly().Location;
        string? exeDir = Path.GetDirectoryName(exePath);
        if (!string.IsNullOrEmpty(exeDir))
        {
            Directory.SetCurrentDirectory(exeDir);
        }
        
        MainWindow mainWindow = new MainWindow();
        this.MainWindow = mainWindow;
        mainWindow.Visibility = Visibility.Hidden;
        
        ActivationWindow activationWindow = new ActivationWindow();
        activationWindow.ShowDialog();
        
        if (!activationWindow.IsActivated)
        {
            System.Console.WriteLine("激活失败，关闭应用程序");
            Shutdown();
        }
        else
        {
            mainWindow.Visibility = Visibility.Visible;
            mainWindow.Closed += (sender, e) => Shutdown();
        }

    }
}
