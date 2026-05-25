using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed class PozycjaSamochoduViewModel : ObservableObject
{
    private readonly PozycjaSamochodu _model;
    private Funkcjonariusz? _wybranaOsoba;

    public int Pozycja => _model.Pozycja;

    public Funkcjonariusz? WybranaOsoba
    {
        get => _wybranaOsoba;
        set
        {
            if (SetProperty(ref _wybranaOsoba, value))
            {
                _model.FunkcjonariuszId = value?.Id;
                _model.Nazwisko = value is not null ? $"{value.Stopien} {value.Nazwisko}" : string.Empty;
            }
        }
    }

    public PozycjaSamochoduViewModel(PozycjaSamochodu model, List<Funkcjonariusz> personel)
    {
        _model = model;
        _wybranaOsoba = model.FunkcjonariuszId.HasValue
            ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
            : null;
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
        List<Funkcjonariusz> personel)
    {
        Samochod = samochod;
        foreach (var m in modele.OrderBy(m => m.Pozycja))
            Pozycje.Add(new PozycjaSamochoduViewModel(m, personel));
    }

    public IEnumerable<PozycjaSamochodu> GetModele()
        => Pozycje.Select(p => p.ToModel());
}
