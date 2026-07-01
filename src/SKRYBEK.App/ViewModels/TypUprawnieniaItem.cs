using CommunityToolkit.Mvvm.ComponentModel;

namespace SKRYBEK.App.ViewModels;

/// <summary>Element listy uprawnień w ustawieniach pojazdu.</summary>
public sealed partial class TypUprawnieniaItem : ObservableObject
{
    public int Id { get; }
    public string Nazwa { get; }

    [ObservableProperty]
    private bool _czyWybrane;

    public TypUprawnieniaItem(int id, string nazwa, bool czyWybrane = false)
    {
        Id = id;
        Nazwa = nazwa;
        _czyWybrane = czyWybrane;
    }
}
