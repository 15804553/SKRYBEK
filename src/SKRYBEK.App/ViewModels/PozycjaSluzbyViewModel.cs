using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.ViewModels;

public sealed partial class PozycjaSluzbyViewModel : ObservableObject
{
    private readonly PozycjaSluzby _model;
    private List<Funkcjonariusz> _personel;
    private readonly RozkazEditorViewModel _editor;
    private string _tekstOsoby;
    private Funkcjonariusz? _wybranaOsoba;
    private bool _tekstUstawianyProgramowo;

    public StanowiskoSluzby Stanowisko => _model.Stanowisko;
    public string NazwaStanowiska => _model.NazwaStanowiska;

    private readonly ObservableCollection<OsobaComboBoxItem> _dostepneOsobyRaw = [];
    private ListCollectionView? _dostepneOsobyView;
    // Flaga ustawiana podczas przebudowy listy — blokuje WybranyItem.set(null),
    // które WPF wysyła automatycznie gdy kolekcja jest czyszczona (Clear()).
    private bool _odswiezanieListy;

    /// <summary>Lista dostępnych osób z podziałem na grupy (wymaganie 8 + 10).</summary>
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
        set => UstawWybranaOsobe(value, aktualizujTekst: true);
    }

    public OsobaComboBoxItem? WybranyItem
    {
        get => _wybranaOsoba is null ? null
            : _dostepneOsobyRaw.FirstOrDefault(i => i.Osoba.Id == _wybranaOsoba.Id);
        set
        {
            // Ignoruj null-sety wysyłane przez WPF gdy Clear() opróżnia kolekcję źródłową.
            if (_odswiezanieListy) return;
            UstawWybranaOsobe(value?.Osoba, aktualizujTekst: true);
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
            var match = PersonelSuggestFilter.ZnajdzDokladnie(_personel, text);
            if (match is not null)
                UstawWybranaOsobe(match, aktualizujTekst: false);
            else
                UstawWybranaOsobe(null, aktualizujTekst: false);
        }
    }

    public PozycjaSluzbyViewModel(
        PozycjaSluzby model,
        List<Funkcjonariusz> personel,
        RozkazEditorViewModel editor)
    {
        _model    = model;
        _personel = personel;
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

    public void OdswiezDostepneOsoby()
    {
        _odswiezanieListy = true;
        try
        {
            _dostepneOsobyRaw.Clear();
            var sugerowane = new List<Funkcjonariusz>();
            var pozostale  = new List<Funkcjonariusz>();

        foreach (var osoba in _personel)
        {
            bool dozwolona = StanowiskoSluzbyRules.CzyOsobaDozwolonaNaStanowisko(osoba, Stanowisko);
                if (!dozwolona && _wybranaOsoba?.Id != osoba.Id)
                    continue;

                if (dozwolona)
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

        // Powiadom WPF że WybranyItem mógł się zmienić (nowy obiekt wrappera dla tej samej osoby).
        OnPropertyChanged(nameof(WybranyItem));
        // Przywróć tekst jeśli WPF wyczyścił go podczas Clear().
        // UstawTekstProgramowo wymusza PropertyChanged z flagą ochronną — re-entrantne
        // TekstOsoby.set nie wykona wyszukiwania w liście personelu, więc zmiana daty
        // (nowy personel) nie skasuje wcześniej wybranej osoby.
        if (_wybranaOsoba is not null)
            UstawTekstProgramowo(_model.Nazwisko);
    }

    private void UstawWybranaOsobe(Funkcjonariusz? osoba, bool aktualizujTekst)
    {
        // Jeśli to ta sama osoba — tylko ewentualnie odśwież tekst i wyjdź.
        // Bez tej kontroli przy rebuildzie listy (OsobyJuzUzyteZmienione) można
        // wejść w pętlę: set → ZarejestrujZmianeOsoby → event → rebuild → OnPropertyChanged
        // → WPF set SelectedItem → WybranyItem.set → UstawWybranaOsobe z tą samą osobą.
        if (_wybranaOsoba?.Id == osoba?.Id)
        {
            if (aktualizujTekst && osoba is not null)
                UstawTekstProgramowo(_model.Nazwisko);
            return;
        }

        if (osoba is not null && StanowiskoSluzbyRules.MaWymagania(Stanowisko))
        {
            if (!StanowiskoSluzbyRules.CzyOsobaDozwolonaNaStanowisko(osoba, Stanowisko))
            {
                SkrybekMessageBox.ShowWarning(
                    StanowiskoSluzbyRules.OpisWymagania(Stanowisko),
                    "Niedozwolone przypisanie");
                OnPropertyChanged(nameof(WybranaOsoba));
                OnPropertyChanged(nameof(WybranyItem));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }
        }

        SetProperty(ref _wybranaOsoba, osoba);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));

        _model.FunkcjonariuszId = osoba?.Id;
        _model.Nazwisko = osoba is not null
            ? $"{osoba.Stopien} {osoba.Nazwisko}"
            : _tekstOsoby;

        if (!aktualizujTekst) return;
        UstawTekstProgramowo(_model.Nazwisko);
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
    /// Aktualizuje listę personelu po zmianie daty rozkazu i przebudowuje dostępne opcje.
    /// Konieczne, bo _personel jest inicjalizowany raz w konstruktorze.
    /// </summary>
    public void OdswiezPersonel(List<Funkcjonariusz> nowyPersonel)
    {
        _personel = nowyPersonel;
        OdswiezDostepneOsoby();
    }

    public PozycjaSluzby ToModel() => _model;
}
