namespace SKRYBEK.Core.Models;

public sealed class Ustawienie
{
    public string Klucz { get; set; } = string.Empty;
    public string Wartosc { get; set; } = string.Empty;
}

public static class UstawieniaKlucze
{
    public const string SciezkaBoberBazy = "SciezkaBoberBazy";
    public const string SciezkaChomikBazy = "SciezkaChomikBazy";
    public const string NrJRG = "NrJRG";
    public const string OstatniBackup = "OstatniBackup";
}
