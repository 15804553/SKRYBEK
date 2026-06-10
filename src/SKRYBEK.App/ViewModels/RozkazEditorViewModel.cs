using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.ViewModels;

public sealed partial class RozkazEditorViewModel : ObservableObject
{
    private readonly RozkazDzienny _rozkaz;
    private List<Samochod> _samochody;
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
    public ObservableCollection<Funkcjonariusz> WszystkieOsoby { get; } = [];
    public ObservableCollection<Funkcjonariusz> Przefiltrowane { get; } = [];
    [ObservableProperty] private bool _filterKierowcaC;
    [ObservableProperty] private bool _filterKierowcaCE;
    [ObservableProperty] private bool _filterNurek;
    [ObservableProperty] private bool _filterKPP;
    [ObservableProperty] private string _filterFunkcja = string.Empty;
    [ObservableProperty] private List<string> _dostepneFunkcje = [];
    [ObservableProperty] private int _liczbaDostepnych;
    [ObservableProperty] private string _personelInfo = string.Empty;

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

        foreach (var osoba in personel)
        {
            WszystkieOsoby.Add(osoba);
            Przefiltrowane.Add(osoba);
        }

        DostepneFunkcje = App.Services.Personnel.GetDostepneFunkcje(personel);
        LiczbaDostepnych = personel.Count;
        PersonelInfo = personel.Count == 0
            ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
            : $"{personel.Count} os. dostępnych na {rozkaz.Data:dd.MM.yyyy}";

        // SŁUŻBA
        foreach (var p in rozkaz.Sluzba)
            Sluzba.Add(new PozycjaSluzbyViewModel(p, personel));

        // PODZIAŁ BOJOWY
        foreach (var sam in samochody)
        {
            var pozycjeModelu = rozkaz.PodzialBojowy.Where(p => p.SamochodId == sam.Id).ToList();
            PodzialBojowy.Add(new SamochodViewModel(sam, pozycjeModelu, personel, this));
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

    /// <summary>Odświeża personel, pojazdy i listy po zamknięciu okna ustawień.</summary>
    public void OdswiezPoZamknieciuUstawien(List<Samochod> samochody, List<Funkcjonariusz> personel, string nrJrg)
    {
        BuildModelFromViewModels();

        _samochody = samochody;

        WszystkieOsoby.Clear();
        foreach (var osoba in personel)
            WszystkieOsoby.Add(osoba);

        ApplyFilter();
        DostepneFunkcje = App.Services.Personnel.GetDostepneFunkcje(personel);
        LiczbaDostepnych = Przefiltrowane.Count;
        PersonelInfo = personel.Count == 0
            ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
            : $"{personel.Count} os. dostępnych na {Data:dd.MM.yyyy}";
        NrJrg = nrJrg;

        foreach (var pozycja in Sluzba)
        {
            var tekst = pozycja.TekstOsoby;
            var match = Helpers.PersonelSuggestFilter.ZnajdzDokladnie(personel, tekst);
            pozycja.TekstOsoby = match?.StopienINazwisko ?? tekst;
        }

        var istniejace = _rozkaz.PodzialBojowy.ToDictionary(p => (p.SamochodId, p.Pozycja));
        _rozkaz.PodzialBojowy.Clear();
        foreach (var sam in samochody)
        {
            for (int poz = 1; poz <= sam.LiczbaPozycji; poz++)
            {
                if (istniejace.TryGetValue((sam.Id, poz), out var stara))
                {
                    _rozkaz.PodzialBojowy.Add(stara);
                    continue;
                }

                _rozkaz.PodzialBojowy.Add(new PozycjaSamochodu
                {
                    SamochodId = sam.Id,
                    Pozycja    = poz
                });
            }
        }

        PodzialBojowy.Clear();
        foreach (var sam in samochody)
        {
            var pozycjeModelu = _rozkaz.PodzialBojowy.Where(p => p.SamochodId == sam.Id).ToList();
            PodzialBojowy.Add(new SamochodViewModel(sam, pozycjeModelu, personel, this));
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
        var filtered = App.Services.Personnel.FiltrujWgKryteriow(
            WszystkieOsoby,
            FilterKierowcaC,
            FilterKierowcaCE,
            FilterNurek,
            FilterKPP,
            string.IsNullOrEmpty(FilterFunkcja) ? null : FilterFunkcja);

        Przefiltrowane.Clear();
        foreach (var osoba in filtered)
            Przefiltrowane.Add(osoba);

        LiczbaDostepnych = Przefiltrowane.Count;
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
            var id = await App.Services.Rozkaz.ZapiszAsync(_rozkaz, WszystkieOsoby.ToList());
            StatusMessage = $"Zapisano rozkaz Nr {_rozkaz.NumerFormatowany}";
            Saved?.Invoke(this, id);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            SkrybekLog.Error("Błąd zapisu rozkazu", ex);
            StatusMessage = $"Błąd zapisu: {msg}";
            SkrybekMessageBox.ShowError(msg, "Błąd zapisu");
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
            SkrybekMessageBox.ShowError($"Błąd eksportu:\n{ex.Message}", "Błąd eksportu");
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
        Przefiltrowane.Clear();
        foreach (var osoba in WszystkieOsoby)
            Przefiltrowane.Add(osoba);
    }

    // ── Walidacja konfliktu pojazd ─────────────────────────────────────────────

    /// <summary>
    /// Zwraca true, gdy osoba jest już na innym pojeździe podstawowym
    /// (przypisanie do docelowego pojazdu podstawowego byłoby konfliktem).
    /// </summary>
    public bool CzyKonfliktPodstawowy(int funkcjonariuszId, int docelowySamochodId)
    {
        var docelowy = _samochody.FirstOrDefault(s => s.Id == docelowySamochodId);
        if (docelowy?.CzyPodstawowy != true) return false;

        var innePodstawowe = _samochody
            .Where(s => s.CzyPodstawowy && s.Id != docelowySamochodId)
            .Select(s => s.Id)
            .ToHashSet();

        return PodzialBojowy
            .Where(s => innePodstawowe.Contains(s.Samochod.Id))
            .SelectMany(s => s.Pozycje)
            .Any(p => p.WybranaOsoba?.Id == funkcjonariuszId);
    }

    private RozkazDzienny BuildModelFromViewModels()
    {
        _rozkaz.NumerRozkazu = NumerRozkazu;
        _rozkaz.Data         = Data;
        _rozkaz.Rok          = Data.Year;
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
