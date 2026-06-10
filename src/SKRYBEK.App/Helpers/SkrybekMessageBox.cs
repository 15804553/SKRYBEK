using System.Windows;
using SKRYBEK.App.Views;

namespace SKRYBEK.App.Helpers;

public enum SkrybekMessageKind
{
    Information,
    Warning,
    Error,
    Question
}

public enum SkrybekMessageButtons
{
    OK,
    YesNo
}

public enum SkrybekMessageResult
{
    None,
    OK,
    Yes,
    No
}

/// <summary>Okna komunikatów w stylistyce SKRYBEK (zamiast systemowego MessageBox).</summary>
public static class SkrybekMessageBox
{
    public static SkrybekMessageResult Show(
        string message,
        string title,
        SkrybekMessageButtons buttons = SkrybekMessageButtons.OK,
        SkrybekMessageKind kind = SkrybekMessageKind.Information,
        Window? owner = null)
    {
        var dlg = new SkrybekMessageDialog(message, title, buttons, kind);
        UstawWlasciciela(dlg, owner);
        dlg.ShowDialog();
        return dlg.Result;
    }

    public static void ShowError(string message, string title = "SKRYBEK — Błąd", Window? owner = null) =>
        Show(message, title, SkrybekMessageButtons.OK, SkrybekMessageKind.Error, owner);

    public static void ShowWarning(string message, string title, Window? owner = null) =>
        Show(message, title, SkrybekMessageButtons.OK, SkrybekMessageKind.Warning, owner);

    public static void ShowInfo(string message, string title, Window? owner = null) =>
        Show(message, title, SkrybekMessageButtons.OK, SkrybekMessageKind.Information, owner);

    public static bool Confirm(string message, string title, SkrybekMessageKind kind = SkrybekMessageKind.Question, Window? owner = null) =>
        Show(message, title, SkrybekMessageButtons.YesNo, kind, owner) == SkrybekMessageResult.Yes;

    private static void UstawWlasciciela(Window dlg, Window? owner)
    {
        owner ??= Application.Current?.MainWindow;
        if (owner is { IsLoaded: true, IsVisible: true })
        {
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
