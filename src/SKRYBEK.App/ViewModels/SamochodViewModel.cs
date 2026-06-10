using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
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

    public ObservableCollection<Funkcjonariusz> DostepneOsoby { get; } = [];

    public Funkcjonariusz? WybranaOsoba
    {
        get => _wybranaOsoba;
        set => PrzypiszOsobeZListy(value, aktualizujTekst: true);
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
            : null;
        _tekstOsoby = !string.IsNullOrWhiteSpace(model.Nazwisko)
            ? model.Nazwisko
            : _wybranaOsoba?.StopienINazwisko ?? string.Empty;
        OdswiezDostepneOsoby();
    }

    private void PrzypiszOsobeZListy(Funkcjonariusz? value, bool aktualizujTekst)
    {
        if (_wybranaOsoba?.Id == value?.Id && !aktualizujTekst) return;

        if (value is not null)
        {
            if (!PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(value, Pozycja))
            {
                SkrybekMessageBox.ShowWarning(
                    PozycjaSamochoduRules.OpisWymagania(Pozycja),
                    "Niedozwolone przypisanie");
                OnPropertyChanged(nameof(WybranaOsoba));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }

            if (_editor.CzyKonfliktPodstawowy(value.Id, _samochod.Samochod.Id))
            {
                SkrybekMessageBox.ShowWarning(
                    $"{value.StopienINazwisko} jest już przypisany/a do innego pojazdu podstawowego.\n" +
                    "Ta sama osoba nie może siedzieć na dwóch pojazdach podstawowych.",
                    "Konflikt pojazdów podstawowych");
                OnPropertyChanged(nameof(WybranaOsoba));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }
        }

        SetProperty(ref _wybranaOsoba, value);
        OnPropertyChanged(nameof(WybranaOsoba));
        _model.FunkcjonariuszId = value?.Id;
        _model.Nazwisko = value is not null
            ? $"{value.Stopien} {value.Nazwisko}"
            : _tekstOsoby;

        if (aktualizujTekst)
            UstawTekstProgramowo(_model.Nazwisko);
    }

    private void UstawBezOsobyZListy()
    {
        if (_wybranaOsoba is null) return;
        SetProperty(ref _wybranaOsoba, null);
        OnPropertyChanged(nameof(WybranaOsoba));
        _model.FunkcjonariuszId = null;
    }

    private void UstawTekstProgramowo(string tekst)
    {
        _tekstUstawianyProgramowo = true;
        SetProperty(ref _tekstOsoby, tekst, nameof(TekstOsoby));
        _tekstUstawianyProgramowo = false;
    }

    public void OdswiezDostepneOsoby()
    {
        DostepneOsoby.Clear();
        foreach (var osoba in _editor.WszystkieOsoby)
        {
            if (PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(osoba, Pozycja))
                DostepneOsoby.Add(osoba);
        }

        if (_wybranaOsoba is not null && DostepneOsoby.All(o => o.Id != _wybranaOsoba.Id))
            DostepneOsoby.Insert(0, _wybranaOsoba);
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

    public IEnumerable<PozycjaSamochodu> GetModele()
        => Pozycje.Select(p => p.ToModel());
}
