using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private SessionInfo? _session;
    [ObservableProperty] private List<RozkazDzienny> _rozkazy = [];
    [ObservableProperty] private RozkazDzienny? _wybranyRozkaz;
    [ObservableProperty] private RozkazEditorViewModel? _editorVm;
    [ObservableProperty] private int _aktualnyRok = DateTime.Today.Year;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool CanEdit => Session is not null && !Session.IsReadOnly;

    public MainViewModel(SessionInfo session)
    {
        _session = session;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Rozkazy = await App.Services.Rozkaz.GetByRokAsync(AktualnyRok);
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
            var data     = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var nrZmiany = Session.NumerZmiany > 0 ? Session.NumerZmiany : 1;

            var rozkaz    = await App.Services.Rozkaz.NowyRozkazAsync(data, nrZmiany);
            var samochody = await App.Services.SamochodyRepo.GetAktywneAsync();
            var personel  = await App.Services.Personnel.GetDostepniAsync(data, nrZmiany);
            var nrJrg     = await App.Services.UstawieniaRepo.GetAsync(Core.Models.UstawieniaKlucze.NrJRG, "4");

            EditorVm = new RozkazEditorViewModel(rozkaz, samochody, personel, nrJrg, Session, isNew: true);
            EditorVm.Saved += OnRozkazSaved;
            WybranyRozkaz = null;
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd podczas tworzenia nowego rozkazu", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show(
                $"Nie można utworzyć nowego rozkazu:\n{ex.Message}",
                "SKRYBEK — Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
            var pelny = await App.Services.Rozkaz.GetByIdAsync(rozkaz.Id);
            if (pelny is null) return;

            var samochody = await App.Services.SamochodyRepo.GetAktywneAsync();
            var nrZmiany  = Session.CanEditAll ? pelny.ZmianaId : Session.NumerZmiany;
            var personel  = await App.Services.Personnel.GetDostepniAsync(pelny.Data, nrZmiany);
            var nrJrg     = await App.Services.UstawieniaRepo.GetAsync(Core.Models.UstawieniaKlucze.NrJRG, "4");

            WybranyRozkaz = pelny;
            EditorVm = new RozkazEditorViewModel(pelny, samochody, personel, nrJrg, Session, isNew: false);
            EditorVm.Saved += OnRozkazSaved;
        }
        catch (Exception ex)
        {
            SkrybekLog.Error($"Błąd podczas otwierania rozkazu Id={rozkaz.Id}", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            MessageBox.Show(
                $"Nie można otworzyć rozkazu:\n{ex.Message}",
                "SKRYBEK — Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UsunRozkazAsync(RozkazDzienny rozkaz)
    {
        await App.Services.Rozkaz.UsunAsync(rozkaz.Id);
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

    public void Logout()
    {
        Session = null;
        Rozkazy = [];
        EditorVm = null;
    }
}
