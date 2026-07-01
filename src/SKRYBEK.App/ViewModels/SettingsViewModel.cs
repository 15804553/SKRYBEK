using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SessionInfo _session;

    [ObservableProperty] private string _sciezkaBoberBazy = string.Empty;
    [ObservableProperty] private string _sciezkaChomikBazy = string.Empty;
    [ObservableProperty] private string _nrJrg = "4";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<Samochod> Samochody { get; } = [];
    public Array TypySamochodow { get; } = Enum.GetValues<TypSamochodu>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WymaganiaPojazdu))]
    private Samochod? _wybranysamochod;

    public ObservableCollection<TypUprawnieniaItem> WymaganiaPojazdu { get; } = [];

    public bool CanEditAll => _session.CanEditAll;

    /// <summary>Edycja pojazdów i grup — tylko DCA JRG (wymaganie 1).</summary>
    public bool CanEditPojazdy => _session.CanEditAll;

    public SettingsViewModel(SessionInfo session)
    {
        _session = session;
    }


    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            SciezkaBoberBazy  = ServiceProvider.Services.BoberDb.DatabasePath;
            SciezkaChomikBazy = ServiceProvider.Services.ChomikDb.DatabasePath;
            NrJrg             = await ServiceProvider.Services.UstawieniaRepo.GetAsync(UstawieniaKlucze.NrJRG, "4");

            Samochody.Clear();
            foreach (var s in await ServiceProvider.Services.SamochodyRepo.GetAllAsync())
                Samochody.Add(s);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnWybranysamochodChanged(Samochod? value) => _ = ZaladujWymaganiaPojazdAsync(value);

    private async Task ZaladujWymaganiaPojazdAsync(Samochod? samochod)
    {
        WymaganiaPojazdu.Clear();
        if (samochod is null || !CanEditPojazdy) return;

        var wszystkieTypy = await ServiceProvider.Services.Personnel.GetTypyUprawnienAsync();
        foreach (var (id, nazwa) in wszystkieTypy)
        {
            var item = new TypUprawnieniaItem(id, nazwa,
                czyWybrane: samochod.WymaganeUprawnieniaIds.Contains(id));
            item.PropertyChanged += (_, _) => AktualizujWymaganiaWModelu(samochod);
            WymaganiaPojazdu.Add(item);
        }
    }

    private void AktualizujWymaganiaWModelu(Samochod samochod)
    {
        samochod.WymaganeUprawnieniaIds.Clear();
        foreach (var item in WymaganiaPojazdu.Where(i => i.CzyWybrane))
            samochod.WymaganeUprawnieniaIds.Add(item.Id);
    }

    [RelayCommand]
    private async Task ZapiszUstawieniaAsync()
    {
        try
        {
            await ServiceProvider.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.NrJRG, NrJrg);

            StatusMessage = "Ustawienia zapisane. Ścieżki baz edytuj w DatabasePatch.txt i uruchom program ponownie.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TestBoberConnectionAsync()
    {
        var ok = await ServiceProvider.Services.BoberDb.TestConnectionAsync();
        StatusMessage = ok ? "✔ Połączenie z bazą BOBER udane." : "✘ Brak połączenia z bazą BOBER.";
    }

    [RelayCommand]
    private async Task TestChomikConnectionAsync()
    {
        var ok = await ServiceProvider.Services.ChomikDb.TestConnectionAsync();
        StatusMessage = ok ? "✔ Połączenie z bazą CHOMIK udane." : "✘ Brak połączenia z bazą CHOMIK.";
    }

    // ── Samochody ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DodajSamochodAsync()
    {
        var s = new Samochod
        {
            Nazwa         = "Nowy pojazd",
            LiczbaPozycji = 4,
            Typ           = TypSamochodu.Dodatkowy,
            Kolejnosc     = Samochody.Count + 1,
            CzyAktywny    = true
        };
        await ServiceProvider.Services.SamochodyRepo.UpsertAsync(s);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ZapiszSamochodAsync(Samochod s)
    {
        await ServiceProvider.Services.SamochodyRepo.UpsertAsync(s);
        StatusMessage = $"Zapisano pojazd: {s.Nazwa}";
    }

    [RelayCommand]
    private async Task UsunSamochodAsync(Samochod s)
    {
        if (!SkrybekMessageBox.Confirm(
            $"Czy usunąć pojazd '{s.Nazwa}'?",
            "Potwierdź usunięcie",
            SkrybekMessageKind.Warning)) return;
        await ServiceProvider.Services.SamochodyRepo.DeleteAsync(s.Id);
        await LoadAsync();
    }

    // ── Backup ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task WykonajBackupAsync()
    {
        try
        {
            await ServiceProvider.Services.Backup.WykonajBackupAsync();
            StatusMessage = "Backup wykonany pomyślnie.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd backupu: {ex.Message}";
        }
    }
}
