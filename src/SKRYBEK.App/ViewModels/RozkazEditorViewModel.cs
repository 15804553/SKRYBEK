using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class RozkazEditorViewModel : ObservableObject
{
    private readonly RozkazDzienny _rozkaz;
    private readonly List<Samochod> _samochody;
    private readonly SessionInfo _session;
    private readonly bool _isNew;

    public event EventHandler<int>? Saved;

    // ── Nagłówek ──────────────────────────────────────────────────────────────
    [ObservableProperty] private int _numerRozkazu;
    [ObservableProperty] private DateOnly _data;
    [ObservableProperty] private string _zajecia = string.Empty;
    [ObservableProperty] private string _uwagi = string.Empty;
    [ObservableProperty] private string _nrJrg = "4";
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // ── Personel ──────────────────────────────────────────────────────────────
    [ObservableProperty] private List<Funkcjonariusz> _wszystkieOsoby = [];
    [ObservableProperty] private List<Funkcjonariusz> _przefiltrowane = [];
    [ObservableProperty] private bool _filterKierowcaC;
    [ObservableProperty] private bool _filterKierowcaCE;
    [ObservableProperty] private bool _filterNurek;
    [ObservableProperty] private bool _filterKPP;
    [ObservableProperty] private string _filterFunkcja = string.Empty;
    [ObservableProperty] private List<string> _dostepneFunkcje = [];

    // ── Sekcje ────────────────────────────────────────────────────────────────
    public ObservableCollection<PozycjaSluzbyViewModel> Sluzba { get; } = [];
    public ObservableCollection<SamochodViewModel> PodzialBojowy { get; } = [];
    public ObservableCollection<NieobecniGroupViewModel> NieobecniGrupy { get; } = [];

    [ObservableProperty] private string _ratownikMedyczny1 = string.Empty;
    [ObservableProperty] private string _ratownikMedyczny2 = string.Empty;

    public RozkazEditorViewModel(
        RozkazDzienny rozkaz,
        List<Samochod> samochody,
        List<Funkcjonariusz> personel,
        string nrJrg,
        SessionInfo session,
        bool isNew)
    {
        _rozkaz   = rozkaz;
        _samochody = samochody;
        _session   = session;
        _isNew     = isNew;

        NumerRozkazu = rozkaz.NumerRozkazu;
        Data         = rozkaz.Data;
        Zajecia      = rozkaz.Zajecia;
        Uwagi        = rozkaz.Uwagi;
        NrJrg        = nrJrg;
        IsReadOnly   = session.IsReadOnly;

        WszystkieOsoby    = personel;
        Przefiltrowane    = personel;
        DostepneFunkcje   = App.Services.Personnel.GetDostepneFunkcje(personel);

        // SŁUŻBA
        foreach (var p in rozkaz.Sluzba)
            Sluzba.Add(new PozycjaSluzbyViewModel(p, personel));

        // PODZIAŁ BOJOWY
        foreach (var sam in samochody)
        {
            var pozycjeModelu = rozkaz.PodzialBojowy.Where(p => p.SamochodId == sam.Id).ToList();
            PodzialBojowy.Add(new SamochodViewModel(sam, pozycjeModelu, personel));
        }

        // RATOWNICY MEDYCZNI
        var r1 = rozkaz.RatwnicyMedyczni.FirstOrDefault(r => r.Pozycja == 1);
        var r2 = rozkaz.RatwnicyMedyczni.FirstOrDefault(r => r.Pozycja == 2);
        RatownikMedyczny1 = r1?.Nazwisko ?? string.Empty;
        RatownikMedyczny2 = r2?.Nazwisko ?? string.Empty;

        // NIEOBECNI
        foreach (TypNieobecnosci typ in Enum.GetValues<TypNieobecnosci>())
        {
            var group = new NieobecniGroupViewModel(typ,
                rozkaz.Nieobecni.Where(n => n.TypNieobecnosci == typ));
            NieobecniGrupy.Add(group);
        }
    }

    // ── Filtrowanie personelu ─────────────────────────────────────────────────

    partial void OnFilterKierowcaCChanged(bool value) => ApplyFilter();
    partial void OnFilterKierowcaCEChanged(bool value) => ApplyFilter();
    partial void OnFilterNurekChanged(bool value) => ApplyFilter();
    partial void OnFilterKPPChanged(bool value) => ApplyFilter();
    partial void OnFilterFunkcjaChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Przefiltrowane = App.Services.Personnel.FiltrujWgKryteriow(
            WszystkieOsoby,
            FilterKierowcaC,
            FilterKierowcaCE,
            FilterNurek,
            FilterKPP,
            string.IsNullOrEmpty(FilterFunkcja) ? null : FilterFunkcja);
    }

    // ── Zapis ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ZapiszAsync()
    {
        IsSaving = true;
        StatusMessage = string.Empty;
        try
        {
            BuildModelFromViewModels();
            var id = await App.Services.Rozkaz.ZapiszAsync(_rozkaz);
            StatusMessage = $"Zapisano rozkaz Nr {_rozkaz.NumerFormatowany}";
            Saved?.Invoke(this, id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd zapisu: {ex.Message}";
            MessageBox.Show(ex.Message, "Błąd zapisu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task EksportujDoWordAsync()
    {
        BuildModelFromViewModels();
        try
        {
            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Eksport");
            var path = App.Services.WordExport.ExportRozkaz(_rozkaz, _samochody, NrJrg, outputDir);
            StatusMessage = $"Wyeksportowano: {System.IO.Path.GetFileName(path)}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd eksportu:\n{ex.Message}", "Błąd eksportu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ResetujFiltr()
    {
        FilterKierowcaC  = false;
        FilterKierowcaCE = false;
        FilterNurek      = false;
        FilterKPP        = false;
        FilterFunkcja    = string.Empty;
        Przefiltrowane   = WszystkieOsoby;
    }

    // ── Walidacja konfliktu pojazd ─────────────────────────────────────────────

    public bool SprawdzKonfliktzPodstawowym(int funkcjonariuszId, int samochodId)
    {
        var samochod = _samochody.FirstOrDefault(s => s.Id == samochodId);
        if (samochod?.CzyPodstawowy != true) return false;

        var tymczasowy = BuildModelFromViewModels();
        return !Services.Rozkaz.RozkazService.MoznaAssignowacDoPodstawowego(
            tymczasowy, funkcjonariuszId, samochodId, _samochody);
    }

    private RozkazDzienny BuildModelFromViewModels()
    {
        _rozkaz.NumerRozkazu = NumerRozkazu;
        _rozkaz.Data         = Data;
        _rozkaz.Zajecia      = Zajecia;
        _rozkaz.Uwagi        = Uwagi;

        _rozkaz.Sluzba.Clear();
        foreach (var vm in Sluzba)
            _rozkaz.Sluzba.Add(vm.ToModel());

        _rozkaz.PodzialBojowy.Clear();
        foreach (var samVm in PodzialBojowy)
            _rozkaz.PodzialBojowy.AddRange(samVm.GetModele());

        _rozkaz.RatwnicyMedyczni.Clear();
        _rozkaz.RatwnicyMedyczni.Add(new RatownikMedyczny { Pozycja = 1, Nazwisko = RatownikMedyczny1 });
        _rozkaz.RatwnicyMedyczni.Add(new RatownikMedyczny { Pozycja = 2, Nazwisko = RatownikMedyczny2 });

        _rozkaz.Nieobecni.Clear();
        foreach (var grp in NieobecniGrupy)
            _rozkaz.Nieobecni.AddRange(grp.GetModele());

        return _rozkaz;
    }
}
