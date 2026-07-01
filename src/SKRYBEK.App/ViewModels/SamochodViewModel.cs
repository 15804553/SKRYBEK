using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.ViewModels;

public sealed class PozycjaSamochoduViewModel : ObservableObject
{
    private readonly PozycjaSamochodu _model;
    private readonly SamochodViewModel _samochod;
    private readonly RozkazEditorViewModel _editor;
    private Funkcjonariusz? _wybranaOsoba;
    private string _tekstOsoby;
    private bool _tekstUstawianyProgramowo;

    public int Pozycja => _model.Pozycja;
    public string NumerPozycji => $"{Pozycja}.";
    public string OznaczeniePozycji => PozycjaSamochoduRules.OznaczeniePozycji(Pozycja);
    public bool MaOznaczenie => !string.IsNullOrEmpty(OznaczeniePozycji);

    public Brush OznaczenieBrush => Pozycja switch
    {
        1 => (Brush)Application.Current.FindResource("SamochodOznaczenieDBrush"),
        2 => (Brush)Application.Current.FindResource("SamochodOznaczenieKBrush"),
        _ => Brushes.Transparent
    };

    private readonly ObservableCollection<OsobaComboBoxItem> _dostepneOsobyRaw = [];
    private ListCollectionView? _dostepneOsobyView;
    private bool _odswiezanieListy;

    /// <summary>
    /// Zgrupowana lista dostępnych osób z podziałem na Zalecani/Pozostali (wymaganie 8 + 10).
    /// </summary>
    public ListCollectionView DostepneOsoby
    {
        get
        {
            if (_dostepneOsobyView is null)
            {
                _dostepneOsobyView = new ListCollectionView(_dostepneOsobyRaw);
                _dostepneOsobyView.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(OsobaComboBoxItem.NazwaGrupy)));
            }
            return _dostepneOsobyView;
        }
    }

    public Funkcjonariusz? WybranaOsoba
    {
        get => _wybranaOsoba;
        set => PrzypiszOsobeZListy(value, aktualizujTekst: true);
    }

    public OsobaComboBoxItem? WybranyItem
    {
        get => _wybranaOsoba is null ? null
            : _dostepneOsobyRaw.FirstOrDefault(i => i.Osoba.Id == _wybranaOsoba.Id);
        set
        {
            if (_odswiezanieListy) return;
            PrzypiszOsobeZListy(value?.Osoba, aktualizujTekst: true);
        }
    }

    public string TekstOsoby
    {
        get => _tekstOsoby;
        set
        {
            if (_tekstUstawianyProgramowo)
            {
                SetProperty(ref _tekstOsoby, value ?? string.Empty);
                return;
            }

            // Ignoruj zmiany tekstu wysyłane przez WPF podczas Clear() w OdswiezDostepneOsoby.
            // WPF ustawia Text="" gdy SelectedItem znika z kolekcji po Clear(), co skutkowało
            // niezamierzonym wyczyszczeniem _wybranaOsoba i _model.Nazwisko.
            if (_odswiezanieListy) return;

            var text = value ?? string.Empty;
            if (!SetProperty(ref _tekstOsoby, text)) return;

            _model.Nazwisko = text;
            var match = PersonelSuggestFilter.ZnajdzDokladnie(_editor.WszystkieOsoby, text);
            if (match is not null)
                PrzypiszOsobeZListy(match, aktualizujTekst: false);
            else
                UstawBezOsobyZListy();
        }
    }

    internal PozycjaSamochoduViewModel(
        PozycjaSamochodu model,
        SamochodViewModel samochod,
        RozkazEditorViewModel editor,
        List<Funkcjonariusz> personel)
    {
        _model    = model;
        _samochod = samochod;
        _editor   = editor;
        _wybranaOsoba = model.FunkcjonariuszId.HasValue
            ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
            : PersonelSuggestFilter.ZnajdzDokladnie(personel, model.Nazwisko);

        // Jeśli osoba rozpoznana po nazwisku (brak FunkcjonariuszId w modelu) — napraw FK,
        // aby kolejny zapis zapisał poprawne powiązanie z bazą danych.
        if (_wybranaOsoba is not null && !model.FunkcjonariuszId.HasValue)
            model.FunkcjonariuszId = _wybranaOsoba.Id;

        _tekstOsoby = !string.IsNullOrWhiteSpace(model.Nazwisko)
            ? model.Nazwisko
            : _wybranaOsoba?.StopienINazwisko ?? string.Empty;

        OdswiezDostepneOsoby();
    }

    private void PrzypiszOsobeZListy(Funkcjonariusz? value, bool aktualizujTekst)
    {
        // Jeśli to ta sama osoba — tylko ewentualnie odśwież tekst i wyjdź.
        // Zapobiega pętli: set → ZarejestrujZmianeOsoby → event → rebuild → set ponownie.
        if (_wybranaOsoba?.Id == value?.Id)
        {
            if (aktualizujTekst && value is not null)
                UstawTekstProgramowo(_model.Nazwisko);
            return;
        }

        if (value is not null)
        {
            if (!PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(value, Pozycja))
            {
                SkrybekMessageBox.ShowWarning(
                    PozycjaSamochoduRules.OpisWymagania(Pozycja),
                    "Niedozwolone przypisanie");
                OnPropertyChanged(nameof(WybranaOsoba));
                OnPropertyChanged(nameof(WybranyItem));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }

            if (_editor.CzyKonfliktPodstawowy(value.Id, _samochod.Samochod.Id, Pozycja))
            {
                SkrybekMessageBox.ShowWarning(
                    $"{value.StopienINazwisko} jest już przypisany/a do innego pojazdu podstawowego.\n" +
                    "Ta sama osoba nie może siedzieć na dwóch pojazdach podstawowych.",
                    "Konflikt pojazdów podstawowych");
                OnPropertyChanged(nameof(WybranaOsoba));
                OnPropertyChanged(nameof(WybranyItem));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }
        }

        SetProperty(ref _wybranaOsoba, value);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));
        _model.FunkcjonariuszId = value?.Id;
        _model.Nazwisko = value is not null
            ? $"{value.Stopien} {value.Nazwisko}"
            : _tekstOsoby;

        if (aktualizujTekst)
            UstawTekstProgramowo(_model.Nazwisko);

        if (_samochod.CzyPodstawowy)
            _editor.OdswiezPozycjePodstawowe();
    }

    private void UstawBezOsobyZListy()
    {
        if (_wybranaOsoba is null) return;
        SetProperty(ref _wybranaOsoba, null);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));
        _model.FunkcjonariuszId = null;

        if (_samochod.CzyPodstawowy)
            _editor.OdswiezPozycjePodstawowe();
    }

    private void UstawTekstProgramowo(string tekst)
    {
        _tekstUstawianyProgramowo = true;
        // Jeśli wartość nie zmieniła się, SetProperty nie wyśle PropertyChanged — wymuszamy je
        // ręcznie, żeby WPF zaktualizował ComboBox.Text (który mógł być wyzerowany przez Clear()).
        // Flaga _tekstUstawianyProgramowo chroni re-entrantne TekstOsoby.set przed logiką wyszukiwania.
        if (!SetProperty(ref _tekstOsoby, tekst, nameof(TekstOsoby)))
            OnPropertyChanged(nameof(TekstOsoby));
        _tekstUstawianyProgramowo = false;
    }

    /// <summary>
    /// Odświeża listę dostępnych osób (wymaganie 8 + 10):
    /// sugerowane (spełniają wymagania pojazdu) u góry, pozostałe poniżej;
    /// na pojazdach podstawowych ukrywa osoby już przypisane do innego pojazdu podstawowego.
    /// </summary>
    public void OdswiezDostepneOsoby()
    {
        _odswiezanieListy = true;
        try
        {
            _dostepneOsobyRaw.Clear();
            var sugerowane = new List<Funkcjonariusz>();
            var pozostale  = new List<Funkcjonariusz>();

        foreach (var osoba in _editor.WszystkieOsoby)
        {
            bool dozwolona = PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(osoba, Pozycja);
                if (!dozwolona) continue;

                if (_samochod.CzyPodstawowy &&
                    _editor.CzyKonfliktPodstawowy(osoba.Id, _samochod.Samochod.Id, Pozycja))
                    continue;

                bool sugerowana = PozycjaSamochoduRules.CzyOsobaMaSugerowaneKwalifikacje(
                    osoba, Pozycja, _samochod.Samochod);

                if (sugerowana)
                    sugerowane.Add(osoba);
                else
                    pozostale.Add(osoba);
            }

            foreach (var o in sugerowane)
                _dostepneOsobyRaw.Add(new OsobaComboBoxItem(o, czySugerowana: true));
            foreach (var o in pozostale)
                _dostepneOsobyRaw.Add(new OsobaComboBoxItem(o, czySugerowana: false));

            if (_wybranaOsoba is not null && _dostepneOsobyRaw.All(i => i.Osoba.Id != _wybranaOsoba.Id))
                _dostepneOsobyRaw.Insert(0, new OsobaComboBoxItem(_wybranaOsoba, czySugerowana: false));
        }
        finally
        {
            _odswiezanieListy = false;
        }

        OnPropertyChanged(nameof(WybranyItem));
        // Przywróć tekst jeśli WPF wyczyścił go podczas Clear().
        // UstawTekstProgramowo wymusza PropertyChanged z flagą ochronną — re-entrantne
        // TekstOsoby.set nie wykona wyszukiwania w liście personelu, więc zmiana daty
        // (nowy personel) nie skasuje wcześniej wybranej osoby.
        if (_wybranaOsoba is not null)
            UstawTekstProgramowo(_model.Nazwisko);
    }

    public PozycjaSamochodu ToModel() => _model;
}

public sealed class SamochodViewModel : ObservableObject
{
    public Samochod Samochod { get; }
    public ObservableCollection<PozycjaSamochoduViewModel> Pozycje { get; } = [];
    public string Nazwa => Samochod.Nazwa;
    public bool CzyPodstawowy => Samochod.CzyPodstawowy;

    public SamochodViewModel(
        Samochod samochod,
        IEnumerable<PozycjaSamochodu> modele,
        List<Funkcjonariusz> personel,
        RozkazEditorViewModel editor)
    {
        Samochod = samochod;
        foreach (var m in modele.OrderBy(m => m.Pozycja))
            Pozycje.Add(new PozycjaSamochoduViewModel(m, this, editor, personel));
    }

    public void OdswiezWszystkiePozycje()
    {
        foreach (var p in Pozycje)
            p.OdswiezDostepneOsoby();
    }

    public IEnumerable<PozycjaSamochodu> GetModele()
        => Pozycje.Select(p => p.ToModel());
}
