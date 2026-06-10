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
    [ObservableProperty] private bool _isAdmin;

    public ObservableCollection<Samochod> Samochody { get; } = [];
    public ObservableCollection<UserAccount> Uzytkownicy { get; } = [];
    public Array TypySamochodow { get; } = Enum.GetValues<TypSamochodu>();

    [ObservableProperty] private Samochod? _wybranysamochod;
    [ObservableProperty] private UserAccount? _wybranyUzytkownik;

    public bool CanEditAll => _session.CanEditAll;

    /// <summary>Edycja listy pojazdów — dostępna dla zmian i DCA; wyłączona tylko dla PA (podgląd).</summary>
    public bool CanEditPojazdy => !_session.IsReadOnly;

    public SettingsViewModel(SessionInfo session)
    {
        _session = session;
        IsAdmin  = session.CanEditAll;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            SciezkaBoberBazy  = App.Services.BoberDb.DatabasePath;
            SciezkaChomikBazy = App.Services.ChomikDb.DatabasePath;
            NrJrg             = await App.Services.UstawieniaRepo.GetAsync(UstawieniaKlucze.NrJRG, "4");

            Samochody.Clear();
            foreach (var s in await App.Services.SamochodyRepo.GetAllAsync())
                Samochody.Add(s);

            if (IsAdmin)
            {
                Uzytkownicy.Clear();
                foreach (var u in await App.Services.ChomikAuthRepo.GetAllAsync())
                    Uzytkownicy.Add(u);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ZapiszUstawieniaAsync()
    {
        try
        {
            await App.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.NrJRG, NrJrg);

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
        var ok = await App.Services.BoberDb.TestConnectionAsync();
        StatusMessage = ok ? "✔ Połączenie z bazą BOBER udane." : "✘ Brak połączenia z bazą BOBER.";
    }

    [RelayCommand]
    private async Task TestChomikConnectionAsync()
    {
        var ok = await App.Services.ChomikDb.TestConnectionAsync();
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
        await App.Services.SamochodyRepo.UpsertAsync(s);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ZapiszSamochodAsync(Samochod s)
    {
        await App.Services.SamochodyRepo.UpsertAsync(s);
        StatusMessage = $"Zapisano pojazd: {s.Nazwa}";
    }

    [RelayCommand]
    private async Task UsunSamochodAsync(Samochod s)
    {
        if (!SkrybekMessageBox.Confirm(
            $"Czy usunąć pojazd '{s.Nazwa}'?",
            "Potwierdź usunięcie",
            SkrybekMessageKind.Warning)) return;
        await App.Services.SamochodyRepo.DeleteAsync(s.Id);
        await LoadAsync();
    }

    // ── Użytkownicy ───────────────────────────────────────────────────────────
    // Użytkownicy i hasła zarządzane są wyłącznie przez CHOMIK.

    [RelayCommand]
    private Task DodajUzytkownikaAsync()
    {
        ShowChomikInfo();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ZapiszUzytkownikaAsync(UserAccount u)
    {
        ShowChomikInfo();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task UsunUzytkownikaAsync(UserAccount u)
    {
        ShowChomikInfo();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ZmienHasloAsync(UserAccount u)
    {
        ShowChomikInfo();
        return Task.CompletedTask;
    }

    private static void ShowChomikInfo() =>
        SkrybekMessageBox.ShowInfo(
            "Użytkownicy i hasła zarządzane są przez aplikację CHOMIK.\nWprowadź zmiany w CHOMIK i uruchom SKRYBEK ponownie.",
            "Zarządzanie użytkownikami");

    // ── Backup ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task WykonajBackupAsync()
    {
        try
        {
            await App.Services.Backup.WykonajBackupAsync();
            StatusMessage = "Backup wykonany pomyślnie.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd backupu: {ex.Message}";
        }
    }
}
