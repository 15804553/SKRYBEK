using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Backup;

public sealed class BackupService
{
    private readonly SkrybekConnectionFactory _factory;
    private readonly UstawieniaRepository _ustawienia;

    public BackupService(SkrybekConnectionFactory factory, UstawieniaRepository ustawienia)
    {
        _factory    = factory;
        _ustawienia = ustawienia;
    }

    public async Task<bool> SprawdzIWykonajBackupAsync()
    {
        var ostatni = await _ustawienia.GetAsync(UstawieniaKlucze.OstatniBackup);

        var teraz = DateTime.Now;
        var obecnyMiesiac = new DateTime(teraz.Year, teraz.Month, 1);

        if (!string.IsNullOrEmpty(ostatni) &&
            DateTime.TryParse(ostatni, out var ostatniDt) &&
            ostatniDt >= obecnyMiesiac)
        {
            return false;
        }

        await WykonajBackupAsync();
        await _ustawienia.SetAsync(UstawieniaKlucze.OstatniBackup, teraz.ToString("yyyy-MM-dd HH:mm:ss"));
        return true;
    }

    public async Task WykonajBackupAsync()
    {
        var srcPath = _factory.DatabasePath;
        if (!File.Exists(srcPath))
            throw new FileNotFoundException("Baza danych SKRYBEK nie znaleziona.", srcPath);

        var backupDir = Path.Combine(
            Path.GetDirectoryName(srcPath) ?? AppContext.BaseDirectory,
            "BACKUP");

        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyy-MM");
        var dstPath   = Path.Combine(backupDir, $"SkrybekDatabase_{timestamp}.bck");

        File.Copy(srcPath, dstPath, overwrite: true);
        SkrybekLog.Info($"Backup bazy danych: {dstPath}");
    }
}
