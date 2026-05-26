using System.IO;
using System.Windows;
using System.Windows.Threading;
using SKRYBEK.Services;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var dbPath  = Path.Combine(AppContext.BaseDirectory, "SkrybekDatabase.accdb");
        var logPath = Path.Combine(AppContext.BaseDirectory, "SKRYBEK.log");
        SkrybekLog.Initialize(logPath);
        SkrybekLog.Info("=== SKRYBEK START ===");

        try
        {
            Services = await AppServices.CreateAsync(dbPath);
            await Services.Backup.SprawdzIWykonajBackupAsync();
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd inicjalizacji bazy danych", ex);
            MessageBox.Show(
                $"Błąd inicjalizacji bazy danych:\n{ex.Message}\n\nSprawdź czy zainstalowany jest Microsoft Access Database Engine (x64).",
                "SKRYBEK — Błąd startu",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        ShowLogin();
    }

    private void ShowLogin()
    {
        // OnExplicitShutdown — LoginWindow może się zamknąć bez zamykania aplikacji
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var login = new Views.LoginWindow();
        var ok = login.ShowDialog();

        if (ok != true || login.Session is null)
        {
            Shutdown(0);
            return;
        }

        OpenMainWindow(login.Session);
    }

    private async void OpenMainWindow(Core.Models.SessionInfo session)
    {
        try
        {
            SkrybekLog.Info($"Otwieranie głównego okna dla: {session.Login}");
            var main = new Views.MainWindow();
            MainWindow = main;
            // Przywróć normalny tryb — zamknięcie MainWindow = zamknięcie aplikacji
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
            await main.InitializeAsync(session);
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd inicjalizacji okna głównego", ex);
            MessageBox.Show(
                $"Błąd podczas otwierania okna głównego:\n{ex.Message}",
                "SKRYBEK — Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SkrybekLog.Error("Nieobsłużony wyjątek w wątku UI", e.Exception);
        MessageBox.Show(
            $"Nieoczekiwany błąd:\n{e.Exception.Message}",
            "SKRYBEK — Błąd krytyczny",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SkrybekLog.Info("=== SKRYBEK STOP ===");
        SkrybekLog.Close();
        base.OnExit(e);
    }
}
