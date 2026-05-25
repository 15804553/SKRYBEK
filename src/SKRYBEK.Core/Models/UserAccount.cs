using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class UserAccount
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int NumerZmiany { get; set; }
    public string HasloHash { get; set; } = string.Empty;
    public string HasloSol { get; set; } = string.Empty;

    public bool IsReadOnly => Role == UserRole.PA;

    public string NazwaZmiany => Role switch
    {
        UserRole.Zmiana1 => "Zmiana 1",
        UserRole.Zmiana2 => "Zmiana 2",
        UserRole.Zmiana3 => "Zmiana 3",
        UserRole.DCAJRG  => "D-ca JRG",
        UserRole.PA      => "PA",
        _                => "Nieznana"
    };
}
