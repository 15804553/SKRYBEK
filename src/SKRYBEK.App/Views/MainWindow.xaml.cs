using System.Windows;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainView.LogoutRequested += OnLogoutRequested;
    }

    public async Task InitializeAsync(SessionInfo session)
    {
        SkrybekLog.Info($"Otwieranie głównego okna dla: {session.Login}");
        await MainView.InitializeAsync(session);
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var login = new LoginWindow();
        var ok = login.ShowDialog();
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

        if (ok == true && login.Session is not null)
        {
            _ = MainView.InitializeAsync(login.Session);
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
}
