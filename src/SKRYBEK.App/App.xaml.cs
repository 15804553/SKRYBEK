using System.IO;
using System.Windows;
using SKRYBEK.Services;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        var login = new Views.LoginWindow();
        if (login.ShowDialog() != true)
        {
            Shutdown(0);
            return;
        }

        var main = new Views.MainWindow();
        MainWindow = main;
        main.Show();
        await main.InitializeAsync(login.Session!);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SkrybekLog.Info("=== SKRYBEK STOP ===");
        SkrybekLog.Close();
        base.OnExit(e);
    }
}
