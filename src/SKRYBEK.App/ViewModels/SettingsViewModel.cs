using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
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

    [ObservableProperty] private Samochod? _wybranysamochod;
    [ObservableProperty] private UserAccount? _wybranyUzytkownik;

    public bool CanEditAll => _session.CanEditAll;

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
            SciezkaBoberBazy  = await App.Services.UstawieniaRepo.GetAsync(UstawieniaKlucze.SciezkaBoberBazy);
            SciezkaChomikBazy = await App.Services.UstawieniaRepo.GetAsync(UstawieniaKlucze.SciezkaChomikBazy);
            NrJrg             = await App.Services.UstawieniaRepo.GetAsync(UstawieniaKlucze.NrJRG, "4");

            Samochody.Clear();
            foreach (var s in await App.Services.SamochodyRepo.GetAllAsync())
                Samochody.Add(s);

            if (IsAdmin)
            {
                Uzytkownicy.Clear();
                foreach (var u in await App.Services.AuthRepo.GetAllAsync())
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
            await App.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.SciezkaBoberBazy, SciezkaBoberBazy);
            await App.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.SciezkaChomikBazy, SciezkaChomikBazy);
            await App.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.NrJRG, NrJrg);

            App.Services.UpdateBoberPath(SciezkaBoberBazy);
            App.Services.UpdateChomikPath(SciezkaChomikBazy);

            StatusMessage = "Ustawienia zapisane.";
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
        var result = MessageBox.Show(
            $"Czy usunąć pojazd '{s.Nazwa}'?",
            "Potwierdź usunięcie",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        await App.Services.SamochodyRepo.DeleteAsync(s.Id);
        await LoadAsync();
    }

    // ── Użytkownicy ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DodajUzytkownikaAsync()
    {
        var u = new UserAccount { Login = "nowy", Role = UserRole.Zmiana1, NumerZmiany = 1 };
        u.HasloSol  = App.Services.Auth.GenerateSalt();
        u.HasloHash = App.Services.Auth.HashPassword("skrybek", u.HasloSol);
        await App.Services.AuthRepo.UpsertAsync(u);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ZapiszUzytkownikaAsync(UserAccount u)
    {
        await App.Services.AuthRepo.UpsertAsync(u);
        StatusMessage = $"Zapisano użytkownika: {u.Login}";
    }

    [RelayCommand]
    private async Task UsunUzytkownikaAsync(UserAccount u)
    {
        if (u.Id == _session.UserId)
        {
            MessageBox.Show("Nie możesz usunąć własnego konta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var result = MessageBox.Show(
            $"Czy usunąć użytkownika '{u.Login}'?",
            "Potwierdź usunięcie",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        await App.Services.AuthRepo.DeleteAsync(u.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ZmienHasloAsync(UserAccount u)
    {
        var dialog = new Views.ChangePasswordDialog();
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.NoweHaslo))
        {
            await App.Services.Auth.ChangePasswordAsync(u.Id, dialog.NoweHaslo);
            StatusMessage = $"Zmieniono hasło dla: {u.Login}";
        }
    }

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
