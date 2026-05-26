using SKRYBEK.Data.Connections;
using SKRYBEK.Data.Database;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Auth;
using SKRYBEK.Services.Backup;
using SKRYBEK.Services.Export;
using SKRYBEK.Services.Personnel;
using SKRYBEK.Services.Rozkaz;

namespace SKRYBEK.Services;

/// <summary>Kompozycja serwisów — prosta alternatywa dla DI container.</summary>
public sealed class AppServices
{
    public SkrybekConnectionFactory SkrybekDb { get; }
    public BoberConnectionFactory   BoberDb   { get; }
    public ChomikConnectionFactory  ChomikDb  { get; }

    public UstawieniaRepository  UstawieniaRepo  { get; }
    public AuthRepository        AuthRepo        { get; }
    public ChomikAuthRepository  ChomikAuthRepo  { get; }
    public RozkazRepository      RozkazRepo      { get; }
    public SamochodyRepository   SamochodyRepo   { get; }
    public PersonnelRepository   PersonnelRepo   { get; }

    public AuthService      Auth      { get; }
    public RozkazService    Rozkaz    { get; }
    public PersonnelService Personnel { get; }
    public BackupService    Backup    { get; }
    public WordExportService WordExport { get; }

    public DatabaseBootstrapper Bootstrapper { get; }

    private AppServices(
        SkrybekConnectionFactory skrybek,
        BoberConnectionFactory bober,
        ChomikConnectionFactory chomik)
    {
        SkrybekDb = skrybek;
        BoberDb   = bober;
        ChomikDb  = chomik;

        UstawieniaRepo = new UstawieniaRepository(skrybek);
        AuthRepo       = new AuthRepository(skrybek);
        ChomikAuthRepo = new ChomikAuthRepository(chomik);
        RozkazRepo     = new RozkazRepository(skrybek);
        SamochodyRepo  = new SamochodyRepository(skrybek);
        PersonnelRepo  = new PersonnelRepository(bober, chomik);

        Auth       = new AuthService(ChomikAuthRepo);
        Rozkaz     = new RozkazService(RozkazRepo, SamochodyRepo);
        Personnel  = new PersonnelService(PersonnelRepo);
        Backup     = new BackupService(skrybek, UstawieniaRepo);
        WordExport = new WordExportService();

        Bootstrapper = new DatabaseBootstrapper(skrybek);
    }

    public static async Task<AppServices> CreateAsync(string dbPath)
    {
        var skrybek = new SkrybekConnectionFactory(dbPath);
        var bootstrapper = new DatabaseBootstrapper(skrybek);
        await bootstrapper.EnsureCreatedAsync();

        var ustawienia = new UstawieniaRepository(skrybek);
        var boberPath  = await ustawienia.GetAsync(Core.Models.UstawieniaKlucze.SciezkaBoberBazy);
        var chomikPath = await ustawienia.GetAsync(Core.Models.UstawieniaKlucze.SciezkaChomikBazy);

        var bober  = new BoberConnectionFactory(boberPath);
        var chomik = new ChomikConnectionFactory(chomikPath);

        return new AppServices(skrybek, bober, chomik);
    }

    public void UpdateBoberPath(string path)  => BoberDb.UpdatePath(path);
    public void UpdateChomikPath(string path) => ChomikDb.UpdatePath(path);
}
