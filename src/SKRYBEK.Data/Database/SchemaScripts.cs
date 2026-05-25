namespace SKRYBEK.Data.Database;

internal static class SchemaScripts
{
    public static IReadOnlyList<string> CreateTables =>
    [
        """
        CREATE TABLE Uzytkownicy (
            Id AUTOINCREMENT PRIMARY KEY,
            Login TEXT(50) NOT NULL,
            Rola SHORT NOT NULL,
            NumerZmiany SHORT NOT NULL,
            HasloHash TEXT(128) NOT NULL,
            HasloSol TEXT(64) NOT NULL
        )
        """,
        """
        CREATE TABLE Samochody (
            Id AUTOINCREMENT PRIMARY KEY,
            Nazwa TEXT(100) NOT NULL,
            LiczbaPozycji SHORT NOT NULL,
            Typ SHORT NOT NULL,
            Kolejnosc SHORT NOT NULL,
            CzyAktywny BIT NOT NULL
        )
        """,
        """
        CREATE TABLE Rozkazy (
            Id AUTOINCREMENT PRIMARY KEY,
            NumerRozkazu INTEGER NOT NULL,
            Rok INTEGER NOT NULL,
            Data DATETIME NOT NULL,
            ZmianaId SHORT NOT NULL,
            Zajecia TEXT(500),
            Uwagi TEXT(1000),
            DataUtworzenia DATETIME NOT NULL,
            Status SHORT NOT NULL DEFAULT 0
        )
        """,
        """
        CREATE TABLE RozkazSluzba (
            Id AUTOINCREMENT PRIMARY KEY,
            RozkazId LONG NOT NULL,
            Stanowisko SHORT NOT NULL,
            FunkcjonariuszId LONG,
            Nazwisko TEXT(150)
        )
        """,
        """
        CREATE TABLE RozkazPodzialBojowy (
            Id AUTOINCREMENT PRIMARY KEY,
            RozkazId LONG NOT NULL,
            SamochodId LONG NOT NULL,
            Pozycja SHORT NOT NULL,
            FunkcjonariuszId LONG,
            Nazwisko TEXT(150)
        )
        """,
        """
        CREATE TABLE RozkazNieobecni (
            Id AUTOINCREMENT PRIMARY KEY,
            RozkazId LONG NOT NULL,
            FunkcjonariuszId LONG,
            Nazwisko TEXT(150) NOT NULL,
            TypNieobecnosci SHORT NOT NULL
        )
        """,
        """
        CREATE TABLE RozkazRatwnicyMedyczni (
            Id AUTOINCREMENT PRIMARY KEY,
            RozkazId LONG NOT NULL,
            Pozycja SHORT NOT NULL,
            FunkcjonariuszId LONG,
            Nazwisko TEXT(150)
        )
        """,
        """
        CREATE TABLE Ustawienia (
            Klucz TEXT(100) NOT NULL PRIMARY KEY,
            Wartosc TEXT(500) NOT NULL
        )
        """
    ];
}
