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
    private List<Funkcjonariusz> _wszyscyZmiany;
    private readonly SessionInfo _session;
    private readonly bool _isNew;

    public event EventHandler<int>? Saved;

    // ── Nagłówek ──────────────────────────────────────────────────────────────
    [ObservableProperty] private int _numerRozkazu;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataDateTime))]
    private DateOnly _data;

    [ObservableProperty] private string _zajecia = string.Empty;
    [ObservableProperty] private string _uwagi = string.Empty;
    [ObservableProperty] private string _nrJrg = "4";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MozeAkceptowac))]
    [NotifyPropertyChangedFor(nameof(MozeOdblokować))]
    [NotifyPropertyChangedFor(nameof(CzyZatwierdzony))]
    private bool _isReadOnly;

    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool IsNew => _isNew;

    // ── Akceptacja rozkazu (wymaganie 3) ──────────────────────────────────────
    public bool CzyZatwierdzony => _rozkaz.Status == StatusRozkazu.Zatwierdzony;

    public bool MozeAkceptowac =>
        _session.CanEditAll && _rozkaz.Status == StatusRozkazu.Roboczy && _rozkaz.Id > 0;

    public bool MozeOdblokować =>
        _session.CanEditAll && _rozkaz.Status == StatusRozkazu.Zatwierdzony;

    public bool CzyKontoPa { get; }

    public string LoginSesji { get; } = string.Empty;

    public bool PokazPanelPersonelu => !CzyKontoPa;

    public bool PokazPrzyciskZatwierdz => MozeAkceptowac && !CzyKontoPa;

    public bool PokazPrzyciskOdblokuj => MozeOdblokować && !CzyKontoPa;

    public bool PokazPrzyciskZapisz => !CzyKontoPa;

    // ── Wybór daty (wymaganie 5) — DatePicker binduje się przez DateTime? ─────
    public DateTime? DataDateTime
    {
        get => Data.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value.HasValue)
                Data = DateOnly.FromDateTime(value.Value);
        }
    }

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
        List<Funkcjonariusz> wszyscyZmiany,
        string nrJrg,
        SessionInfo session,
        bool isNew)
    {
        _rozkaz        = rozkaz;
        _samochody     = samochody;
        _wszyscyZmiany = wszyscyZmiany;
        _session       = session;
        _isNew         = isNew;

        session.NormalizePaFlags();
        CzyKontoPa = session.IsPaUser;
        LoginSesji = session.Login;

        SkrybekLog.Info($"RozkazEditorViewModel: login={LoginSesji}, CzyKontoPa={CzyKontoPa}");

        NumerRozkazu = rozkaz.NumerRozkazu;
        _data        = rozkaz.Data;
        Zajecia      = rozkaz.Zajecia;
        Uwagi        = rozkaz.Uwagi;
        NrJrg        = nrJrg;
        IsReadOnly   = session.IsReadOnly || rozkaz.Status == StatusRozkazu.Zatwierdzony;

        foreach (var osoba in personel)
        {
            WszystkieOsoby.Add(osoba);
            Przefiltrowane.Add(osoba);
        }

        DostepneFunkcje = ServiceProvider.Services.Personnel.GetDostepneFunkcje(personel);
        LiczbaDostepnych = personel.Count;
        PersonelInfo = personel.Count == 0
            ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
            : $"{personel.Count} os. dostępnych na {rozkaz.Data:dd.MM.yyyy}";

        // SŁUŻBA
        foreach (var p in rozkaz.Sluzba)
            Sluzba.Add(new PozycjaSluzbyViewModel(p, personel, this));

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

    // ── Zmiana daty (wymaganie 5) — odświeża personel ────────────────────────
    // NumerRozkazu NIE jest aktualizowany automatycznie przy zmianie daty,
    // gdyż numer wskazuje dzień roku kiedy rozkaz był pisany (dziś), nie dzień służby.
    partial void OnDataChanged(DateOnly value)
    {
        _ = OdswiezPersonelNaDateAsync(value);
    }

    private async Task OdswiezPersonelNaDateAsync(DateOnly data)
    {
        try
        {
            var nrZmiany = _rozkaz.ZmianaId > 0 ? _rozkaz.ZmianaId
                : (_session.NumerZmiany > 0 ? _session.NumerZmiany : 1);

            var nowyPersonel = await ServiceProvider.Services.Personnel.GetDostepniAsync(data, nrZmiany);

            WszystkieOsoby.Clear();
            foreach (var osoba in nowyPersonel)
                WszystkieOsoby.Add(osoba);

            ApplyFilter();
            DostepneFunkcje = ServiceProvider.Services.Personnel.GetDostepneFunkcje(nowyPersonel);
            LiczbaDostepnych = Przefiltrowane.Count;
            PersonelInfo = nowyPersonel.Count == 0
                ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
                : $"{nowyPersonel.Count} os. dostępnych na {data:dd.MM.yyyy}";

            foreach (var samVm in PodzialBojowy)
                samVm.OdswiezWszystkiePozycje();

            // Stanowiska używają własnej listy _personel — musi być zaktualizowana
            // (inaczej po zmianie daty stanowiska widziałyby personel ze starej daty).
            foreach (var pozVm in Sluzba)
                pozVm.OdswiezPersonel(nowyPersonel);

            // Przeładuj sekcje nieobecnych z BOBER na podstawie GrafikWpisy
            await OdswiezNieobecnychZBoberaAsync(data, nrZmiany);
        }
        catch (Exception ex)
        {
            SkrybekLog.Error($"Błąd odświeżania personelu na {data}", ex);
        }
    }

    private async Task OdswiezNieobecnychZBoberaAsync(DateOnly data, int nrZmiany)
    {
        try
        {
            var nieobecni = await ServiceProvider.Services.Personnel.GetNieobecniWDniuAsync(
                data, nrZmiany, _wszyscyZmiany);

            foreach (var grp in NieobecniGrupy)
            {
                var dlaTypu = nieobecni.Where(n => n.TypNieobecnosci == grp.Typ).ToList();
                grp.ZaladujZBobera(dlaTypu);
            }
        }
        catch (Exception ex)
        {
            SkrybekLog.Error($"Błąd przeładowania nieobecnych z BOBER na {data}", ex);
        }
    }

    // ── Akceptacja / odblokowanie (wymaganie 3) ───────────────────────────────

    [RelayCommand]
    private async Task AkceptujRozkazAsync()
    {
        if (!_session.CanEditAll || _rozkaz.Id == 0) return;

        BuildModelFromViewModels();
        await ServiceProvider.Services.Rozkaz.ZapiszAsync(_rozkaz, WszystkieOsoby.ToList());
        await ServiceProvider.Services.Rozkaz.UpdateStatusAsync(_rozkaz.Id, StatusRozkazu.Zatwierdzony);

        _rozkaz.Status = StatusRozkazu.Zatwierdzony;
        IsReadOnly = true;
        OnPropertyChanged(nameof(CzyZatwierdzony));
        OnPropertyChanged(nameof(MozeAkceptowac));
        OnPropertyChanged(nameof(MozeOdblokować));
        OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
        OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));
        StatusMessage = "Rozkaz zatwierdzony — edycja zablokowana.";
    }

    [RelayCommand]
    private async Task OdblokujRozkazAsync()
    {
        if (!_session.CanEditAll) return;

        await ServiceProvider.Services.Rozkaz.UpdateStatusAsync(_rozkaz.Id, StatusRozkazu.Roboczy);

        _rozkaz.Status = StatusRozkazu.Roboczy;
        IsReadOnly = _session.IsReadOnly;
        OnPropertyChanged(nameof(CzyZatwierdzony));
        OnPropertyChanged(nameof(MozeAkceptowac));
        OnPropertyChanged(nameof(MozeOdblokować));
        OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
        OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));
        StatusMessage = "Rozkaz odblokowany — można edytować.";
    }

    /// <summary>Odświeża personel, pojazdy i listy po zamknięciu okna ustawień.</summary>
    public void OdswiezPoZamknieciuUstawien(
        List<Samochod> samochody,
        List<Funkcjonariusz> personel,
        List<Funkcjonariusz> wszyscyZmiany,
        string nrJrg)
    {
        BuildModelFromViewModels();

        _samochody     = samochody;
        _wszyscyZmiany = wszyscyZmiany;

        WszystkieOsoby.Clear();
        foreach (var osoba in personel)
            WszystkieOsoby.Add(osoba);

        ApplyFilter();
        DostepneFunkcje = ServiceProvider.Services.Personnel.GetDostepneFunkcje(personel);
        LiczbaDostepnych = Przefiltrowane.Count;
        PersonelInfo = personel.Count == 0
            ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
            : $"{personel.Count} os. dostępnych na {Data:dd.MM.yyyy}";
        NrJrg = nrJrg;

        // Zaktualizuj _personel w stanowiskach i przebuduj listy z nową datą/personelem.
        foreach (var pozycja in Sluzba)
        {
            var tekst = pozycja.TekstOsoby;
            var match = Helpers.PersonelSuggestFilter.ZnajdzDokladnie(personel, tekst);
            pozycja.OdswiezPersonel(personel);
            if (match is not null)
                pozycja.TekstOsoby = match.StopienINazwisko;
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
        var filtered = ServiceProvider.Services.Personnel.FiltrujWgKryteriow(
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
            var id = await ServiceProvider.Services.Rozkaz.ZapiszAsync(_rozkaz, WszystkieOsoby.ToList());

            // Po pierwszym zapisie nowego rozkazu odblokuj przycisk Akceptuj
            OnPropertyChanged(nameof(MozeAkceptowac));
            OnPropertyChanged(nameof(MozeOdblokować));
            OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
            OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));

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
            var path = ServiceProvider.Services.WordExport.ExportRozkaz(_rozkaz, _samochody, NrJrg, outputDir);
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

    // ── Walidacja konfliktu pojazd podstawowy ────────────────────────────────

    /// <summary>
    /// Zwraca true, gdy osoba jest już przypisana na innym miejscu pojazdu podstawowego
    /// (w tym na innym pojeździe podstawowym). Jedna osoba może siedzieć tylko na jednym
    /// samochodzie oznaczonym jako podstawowy.
    /// </summary>
    public bool CzyKonfliktPodstawowy(int funkcjonariuszId, int docelowySamochodId, int docelowaPozycja)
    {
        var docelowy = _samochody.FirstOrDefault(s => s.Id == docelowySamochodId);
        if (docelowy?.CzyPodstawowy != true) return false;

        foreach (var samVm in PodzialBojowy.Where(s => s.CzyPodstawowy))
        {
            foreach (var poz in samVm.Pozycje)
            {
                if (poz.WybranaOsoba?.Id != funkcjonariuszId) continue;
                if (samVm.Samochod.Id == docelowySamochodId && poz.Pozycja == docelowaPozycja)
                    continue;

                return true;
            }
        }

        return false;
    }

    /// <summary>Odświeża listy comboboxów na wszystkich pojazdach podstawowych.</summary>
    public void OdswiezPozycjePodstawowe()
    {
        foreach (var samVm in PodzialBojowy.Where(s => s.CzyPodstawowy))
            samVm.OdswiezWszystkiePozycje();
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
