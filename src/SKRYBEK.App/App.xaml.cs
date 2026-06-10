using System.IO;
using System.Windows;
using System.Windows.Threading;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Configuration;
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
            SkrybekMessageBox.ShowError(
                $"Błąd inicjalizacji:\n{ex.Message}\n\n" +
                $"Ścieżki baz CHOMIK i BOBER ustaw w pliku:\n{DatabasePatch.GetFilePath()}\n\n" +
                "Sprawdź też czy zainstalowany jest Microsoft Access Database Engine (x64).",
                "SKRYBEK — Błąd startu");
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
            SkrybekMessageBox.ShowError(
                $"Błąd podczas otwierania okna głównego:\n{ex.Message}",
                "SKRYBEK — Błąd");
            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SkrybekLog.Error("Nieobsłużony wyjątek w wątku UI", e.Exception);
        SkrybekMessageBox.ShowError(
            $"Nieoczekiwany błąd:\n{e.Exception.Message}",
            "SKRYBEK — Błąd krytyczny");
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SkrybekLog.Info("=== SKRYBEK STOP ===");
        SkrybekLog.Close();
        base.OnExit(e);
    }
}
