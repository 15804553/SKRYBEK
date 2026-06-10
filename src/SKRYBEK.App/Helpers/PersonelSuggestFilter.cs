using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Helpers;

public static class PersonelSuggestFilter
{
    public static IEnumerable<Funkcjonariusz> Szukaj(IEnumerable<Funkcjonariusz>? personel, string? query, int maxWynikow = 12)
    {
        if (personel is null || string.IsNullOrWhiteSpace(query))
            return [];

        var q = query.Trim();
        return personel
            .Where(p =>
                p.Nazwisko.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Imie.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Stopien.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.StopienINazwisko.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maxWynikow);
    }

    public static Funkcjonariusz? ZnajdzDokladnie(IEnumerable<Funkcjonariusz> personel, string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst)) return null;
        var t = tekst.Trim();
        return personel.FirstOrDefault(p =>
            string.Equals(p.StopienINazwisko, t, StringComparison.OrdinalIgnoreCase)
            || string.Equals($"{p.Stopien} {p.Nazwisko}".Trim(), t, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Nazwisko, t, StringComparison.OrdinalIgnoreCase));
    }
}
