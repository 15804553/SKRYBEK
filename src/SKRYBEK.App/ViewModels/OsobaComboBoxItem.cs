using System.Windows.Media;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

/// <summary>
/// Wrapper Funkcjonariusz do comboboxa z podziałem na sugerowanych i pozostałych.
/// Sugerowani (spełniający wymagania pojazdu/pozycji) są wyświetlani u góry.
/// </summary>
public sealed class OsobaComboBoxItem
{
    public Funkcjonariusz Osoba { get; }
    public bool CzySugerowana { get; }

    public string StopienINazwisko => Osoba.StopienINazwisko;
    public string NazwaGrupy => CzySugerowana ? "Zalecani" : "Pozostali";

    /// <summary>
    /// Kolor tekstu zgodny z grafikiem BOBER — kontrast do tła roli, czerwony dla nurka.
    /// </summary>
    public Brush KolorBoberaForeground => BoberKolorHelper.WyznaczKolorForeground(Osoba);

    public string TooltipText
    {
        get
        {
            var czesci = new List<string>();
            if (Osoba.NazwyUprawnien.Count > 0)
                czesci.Add("Uprawnienia: " + string.Join(", ", Osoba.NazwyUprawnien));
            if (Osoba.NazwyFunkcjiDodatkowych.Count > 0)
                czesci.Add("Funkcje: " + string.Join(", ", Osoba.NazwyFunkcjiDodatkowych));
            return czesci.Count > 0 ? string.Join("\n", czesci) : string.Empty;
        }
    }

    public OsobaComboBoxItem(Funkcjonariusz osoba, bool czySugerowana)
    {
        Osoba = osoba;
        CzySugerowana = czySugerowana;
    }

    // WPF editable ComboBox używa ToString() do wyświetlenia tekstu w polu edycji
    // po wybraniu pozycji z listy. Bez tego override'u WPF pokazuje nazwę klasy.
    public override string ToString() => StopienINazwisko;

    public override bool Equals(object? obj) =>
        obj is OsobaComboBoxItem other && other.Osoba.Id == Osoba.Id;

    public override int GetHashCode() => Osoba.Id.GetHashCode();
}
