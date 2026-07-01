namespace SKRYBEK.App.Helpers;

/// <summary>Reguły widoczności edytora rozkazu wg zalogowanego użytkownika.</summary>
public static class SkrybekLoginAccess
{
    public static bool IsPaAccount(string? login) =>
        string.Equals(login?.Trim(), "PA", StringComparison.OrdinalIgnoreCase);

    public static bool ShowPersonnelPanel(string? login) => !IsPaAccount(login);

    public static bool ShowSaveButton(string? login) => !IsPaAccount(login);

    public static bool ShowApproveButton(string? login, bool canEditAll, bool mozeAkceptowac) =>
        !IsPaAccount(login) && canEditAll && mozeAkceptowac;

    public static bool ShowUnlockButton(string? login, bool canEditAll, bool mozeOdblokowac) =>
        !IsPaAccount(login) && canEditAll && mozeOdblokowac;
}
