using CommunityToolkit.Mvvm.ComponentModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class PozycjaSluzbyViewModel : ObservableObject
{
    private readonly PozycjaSluzby _model;
    private readonly List<Funkcjonariusz> _personel;
    private string _tekstOsoby;
    private Funkcjonariusz? _wybranaOsoba;
    private bool _tekstUstawianyProgramowo;

    public StanowiskoSluzby Stanowisko => _model.Stanowisko;
    public string NazwaStanowiska => _model.NazwaStanowiska;

    public Funkcjonariusz? WybranaOsoba
    {
        get => _wybranaOsoba;
        set => UstawWybranaOsobe(value, aktualizujTekst: true);
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
            var match = PersonelSuggestFilter.ZnajdzDokladnie(_personel, text);
            if (match is not null)
                UstawWybranaOsobe(match, aktualizujTekst: false);
            else
                UstawWybranaOsobe(null, aktualizujTekst: false);
        }
    }

    public PozycjaSluzbyViewModel(PozycjaSluzby model, List<Funkcjonariusz> personel)
    {
        _model    = model;
        _personel = personel;
        _wybranaOsoba = model.FunkcjonariuszId.HasValue
            ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
            : null;
        _tekstOsoby = !string.IsNullOrWhiteSpace(model.Nazwisko)
            ? model.Nazwisko
            : _wybranaOsoba?.StopienINazwisko ?? string.Empty;
    }

    private void UstawWybranaOsobe(Funkcjonariusz? osoba, bool aktualizujTekst)
    {
        if (_wybranaOsoba?.Id == osoba?.Id && !aktualizujTekst) return;

        SetProperty(ref _wybranaOsoba, osoba);
        OnPropertyChanged(nameof(WybranaOsoba));

        _model.FunkcjonariuszId = osoba?.Id;
        _model.Nazwisko = osoba is not null
            ? $"{osoba.Stopien} {osoba.Nazwisko}"
            : _tekstOsoby;

        if (!aktualizujTekst) return;

        _tekstUstawianyProgramowo = true;
        SetProperty(ref _tekstOsoby, _model.Nazwisko, nameof(TekstOsoby));
        _tekstUstawianyProgramowo = false;
    }

    public PozycjaSluzby ToModel() => _model;
}
