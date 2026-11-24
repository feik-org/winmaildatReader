using System.Windows;

namespace WinmailDatReader.App;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        // „Öffnen mit…“ bzw. Start mit Dateipfad als erstem Argument.
        if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
            window.ViewModel.Open(e.Args[0]);
    }
}
