namespace SKRYBEK.Core.Models;

public sealed class SessionInfo
{
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string NazwaZmiany { get; set; } = string.Empty;
    public int NumerZmiany { get; set; }
    public bool IsReadOnly { get; set; }
    public bool CanEditAll { get; set; }
    public bool IsPaAccount { get; set; }

    /// <summary>Ujednolica flagi konta PA (login lub nazwa roli).</summary>
    public void NormalizePaFlags()
    {
        if (!IsPaLogin(Login) && !IsPaLogin(NazwaZmiany))
        {
            return;
        }

        IsPaAccount = true;
        IsReadOnly = true;
    }

    public bool IsPaUser => IsPaAccount || IsPaLogin(Login) || IsPaLogin(NazwaZmiany);

    private static bool IsPaLogin(string value) =>
        value.Equals("PA", StringComparison.OrdinalIgnoreCase);
}
