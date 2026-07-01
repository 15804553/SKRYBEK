using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanUsunRozkaz))]
    [NotifyPropertyChangedFor(nameof(CanOpenSettings))]
    private SessionInfo? _session;
    [ObservableProperty] private List<RozkazDzienny> _rozkazy = [];
    [ObservableProperty] private RozkazDzienny? _wybranyRozkaz;
    [ObservableProperty] private RozkazEditorViewModel? _editorVm;
    [ObservableProperty] private int _aktualnyRok = DateTime.Today.Year;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool CanEdit => Session is not null && !Session.IsReadOnly;

    /// <summary>Usuwanie rozkazu — tylko DCA JRG (wymaganie 4).</summary>
    public bool CanUsunRozkaz => Session is not null && Session.CanEditAll;

    /// <summary>Dostęp do ustawień — tylko DCA JRG.</summary>
    public bool CanOpenSettings => Session?.CanEditAll == true;

    public MainViewModel(SessionInfo session)
    {
        session.NormalizePaFlags();
        _session = session;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Rozkazy = await ServiceProvider.Services.Rozkaz.GetByRokAsync(AktualnyRok);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NowyRozkazAsync()
    {
        if (Session is null) return;

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var nrZmiany = Session.NumerZmiany > 0 ? Session.NumerZmiany : 1;
            var dzisiaj  = DateOnly.FromDateTime(DateTime.Today);
            var data     = await ServiceProvider.Services.Personnel.GetNastepnyDzienSluzbyPoAsync(nrZmiany, dzisiaj);

            SkrybekLog.Info(
                $"Nowy rozkaz: zmiana {nrZmiany}, dzisiaj {dzisiaj:yyyy-MM-dd}, data rozkazu {data:yyyy-MM-dd}");

            var rozkaz    = await ServiceProvider.Services.Rozkaz.NowyRozkazAsync(data, nrZmiany);
            var samochody = await ServiceProvider.Services.SamochodyRepo.GetAktywneAsync();
            var wszyscy   = await ServiceProvider.Services.Personnel.GetWszyscyZmianaAsync(nrZmiany);
            var personel  = await ServiceProvider.Services.Personnel.GetDostepniAsync(data, nrZmiany);
            var nrJrg     = await ServiceProvider.Services.UstawieniaRepo.GetAsync(Core.Models.UstawieniaKlucze.NrJRG, "4");

            // Wstępne wypełnienie nieobecnych z BOBER (wymaganie 6)
            rozkaz.Nieobecni = await ServiceProvider.Services.Personnel.GetNieobecniWDniuAsync(data, nrZmiany, wszyscy);

            WybranyRozkaz = null;
            EditorVm = null;
            EditorVm = new RozkazEditorViewModel(rozkaz, samochody, personel, wszyscy, nrJrg, Session, isNew: true);
            EditorVm.Saved += OnRozkazSaved;

            StatusMessage = personel.Count == 0
                ? $"Brak personelu na {data:dd.MM.yyyy}. Sprawdź grafik BOBER i BoberDatabase w DatabasePatch.txt."
                : $"Nowy rozkaz na {data:dd.MM.yyyy} — dostępnych: {personel.Count}";
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd podczas tworzenia nowego rozkazu", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            SkrybekMessageBox.ShowError(
                $"Nie można utworzyć nowego rozkazu:\n{ex.Message}",
                "SKRYBEK — Błąd");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OtworzRozkazAsync(RozkazDzienny rozkaz)
    {
        if (Session is null) return;

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var pelny = await ServiceProvider.Services.Rozkaz.GetByIdAsync(rozkaz.Id);
            if (pelny is null) return;

            var samochody = await ServiceProvider.Services.SamochodyRepo.GetAktywneAsync();
            var nrZmiany  = Session.CanEditAll ? pelny.ZmianaId : Session.NumerZmiany;
            var personel  = await ServiceProvider.Services.Personnel.GetDostepniAsync(pelny.Data, nrZmiany);
            var wszyscy   = await ServiceProvider.Services.Personnel.GetWszyscyZmianaAsync(nrZmiany);
            var nrJrg     = await ServiceProvider.Services.UstawieniaRepo.GetAsync(Core.Models.UstawieniaKlucze.NrJRG, "4");

            WybranyRozkaz = pelny;
            EditorVm = new RozkazEditorViewModel(pelny, samochody, personel, wszyscy, nrJrg, Session, isNew: false);
            EditorVm.Saved += OnRozkazSaved;
        }
        catch (Exception ex)
        {
            SkrybekLog.Error($"Błąd podczas otwierania rozkazu Id={rozkaz.Id}", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            SkrybekMessageBox.ShowError(
                $"Nie można otworzyć rozkazu:\n{ex.Message}",
                "SKRYBEK — Błąd");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UsunRozkazAsync(RozkazDzienny rozkaz)
    {
        // Wymaganie 4: usuwanie tylko DCA JRG
        if (Session is null || !Session.CanEditAll)
        {
            SkrybekMessageBox.ShowWarning("Tylko DCA JRG może usunąć rozkaz.", "Brak uprawnień");
            return;
        }

        await ServiceProvider.Services.Rozkaz.UsunAsync(rozkaz.Id);
        await LoadAsync();
        if (WybranyRozkaz?.Id == rozkaz.Id)
        {
            WybranyRozkaz = null;
            EditorVm = null;
        }
    }

    [RelayCommand]
    private async Task ZmienRokAsync(int rok)
    {
        AktualnyRok = rok;
        await LoadAsync();
    }

    private async void OnRozkazSaved(object? sender, int id)
    {
        await LoadAsync();
        StatusMessage = $"Rozkaz zapisany — Id {id}";
    }

    /// <summary>Odświeża listę rozkazów i otwarty edytor po zamknięciu ustawień.</summary>
    public async Task OdswiezPoUstawieniachAsync()
    {
        await LoadAsync();
        if (Session is null || EditorVm is null) return;

        try
        {
            var data = EditorVm.Data;
            var nrZmiany = WybranyRozkaz?.ZmianaId
                           ?? (Session.NumerZmiany > 0 ? Session.NumerZmiany : 1);

            var samochody = await ServiceProvider.Services.SamochodyRepo.GetAktywneAsync();
            var personel  = await ServiceProvider.Services.Personnel.GetDostepniAsync(data, nrZmiany);
            var wszyscy   = await ServiceProvider.Services.Personnel.GetWszyscyZmianaAsync(nrZmiany);
            var nrJrg     = await ServiceProvider.Services.UstawieniaRepo.GetAsync(
                Core.Models.UstawieniaKlucze.NrJRG, "4");

            EditorVm.OdswiezPoZamknieciuUstawien(samochody, personel, wszyscy, nrJrg);
            StatusMessage = $"Odświeżono widok — dostępnych: {personel.Count}";
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd odświeżania po ustawieniach", ex);
            StatusMessage = $"Błąd odświeżania: {ex.Message}";
        }
    }

    public void Logout()
    {
        Session = null;
        Rozkazy = [];
        EditorVm = null;
    }
}
