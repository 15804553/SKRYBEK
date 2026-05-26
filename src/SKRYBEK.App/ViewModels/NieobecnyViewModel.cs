using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class NieobecnyViewModel : ObservableObject
{
    [ObservableProperty] private string _nazwisko = string.Empty;
    [ObservableProperty] private TypNieobecnosci _typNieobecnosci;
    [ObservableProperty] private Funkcjonariusz? _wybranaOsoba;

    public TypNieobecnosci Typ => TypNieobecnosci;

    public NieobecnyViewModel(NieobecnyWSluzbie model)
    {
        _nazwisko        = model.Nazwisko;
        _typNieobecnosci = model.TypNieobecnosci;
    }

    public NieobecnyWSluzbie ToModel()
        => new()
        {
            FunkcjonariuszId = WybranaOsoba?.Id,
            Nazwisko         = !string.IsNullOrWhiteSpace(Nazwisko) ? Nazwisko
                               : WybranaOsoba is not null ? $"{WybranaOsoba.Stopien} {WybranaOsoba.Nazwisko}"
                               : string.Empty,
            TypNieobecnosci  = TypNieobecnosci
        };
}

public sealed partial class NieobecniGroupViewModel : ObservableObject
{
    public TypNieobecnosci Typ { get; }
    public string Tytul { get; }
    public ObservableCollection<NieobecnyViewModel> Items { get; } = [];

    public NieobecniGroupViewModel(TypNieobecnosci typ, IEnumerable<NieobecnyWSluzbie> initial)
    {
        Typ   = typ;
        Tytul = typ switch
        {
            TypNieobecnosci.Urlop       => "URLOPY",
            TypNieobecnosci.CzasWolny   => "CZAS WOLNY",
            TypNieobecnosci.Chory       => "CHORZY",
            TypNieobecnosci.Delegowany  => "DELEGOWANI",
            TypNieobecnosci.DyzurDomowy => "DYŻUR DOMOWY",
            _                           => typ.ToString()
        };
        foreach (var n in initial)
            Items.Add(new NieobecnyViewModel(n));
    }

    [RelayCommand]
    private void DodajNieobecnego()
        => Items.Add(new NieobecnyViewModel(new NieobecnyWSluzbie { TypNieobecnosci = Typ }));

    [RelayCommand]
    private void UsunNieobecnego(NieobecnyViewModel vm)
        => Items.Remove(vm);

    public IEnumerable<NieobecnyWSluzbie> GetModele()
        => Items.Select(i => i.ToModel()).Where(m => !string.IsNullOrWhiteSpace(m.Nazwisko));
}
