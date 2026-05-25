using CommunityToolkit.Mvvm.ComponentModel;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class PozycjaSluzbyViewModel : ObservableObject
{
    private readonly PozycjaSluzby _model;

    [ObservableProperty] private Funkcjonariusz? _wybranaOsoba;

    public StanowiskoSluzby Stanowisko => _model.Stanowisko;
    public string NazwaStanowiska => _model.NazwaStanowiska;

    public PozycjaSluzbyViewModel(PozycjaSluzby model, List<Funkcjonariusz> personel)
    {
        _model = model;
        WybranaOsoba = model.FunkcjonariuszId.HasValue
            ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
            : null;
    }

    partial void OnWybranaOsobaChanged(Funkcjonariusz? value)
    {
        _model.FunkcjonariuszId = value?.Id;
        _model.Nazwisko = value is not null ? $"{value.Stopien} {value.Nazwisko}" : string.Empty;
    }

    public PozycjaSluzby ToModel() => _model;
}
