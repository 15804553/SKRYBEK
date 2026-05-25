namespace SKRYBEK.Core.Models;

public sealed class SessionInfo
{
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string NazwaZmiany { get; set; } = string.Empty;
    public int NumerZmiany { get; set; }
    public bool IsReadOnly { get; set; }
    public bool CanEditAll { get; set; }
}
