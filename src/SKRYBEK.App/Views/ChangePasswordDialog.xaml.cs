using System.Windows;
using SKRYBEK.App.Helpers;

namespace SKRYBEK.App.Views;

public partial class ChangePasswordDialog : Window
{
    public string? NoweHaslo { get; private set; }

    public ChangePasswordDialog()
    {
        InitializeComponent();
    }

    private void Zmien_Click(object sender, RoutedEventArgs e)
    {
        if (Haslo1.Password != Haslo2.Password)
        {
            SkrybekMessageBox.ShowWarning("Hasła nie są identyczne.", "Błąd", this);
            return;
        }
        if (Haslo1.Password.Length < 4)
        {
            SkrybekMessageBox.ShowWarning("Hasło musi mieć co najmniej 4 znaki.", "Błąd", this);
            return;
        }
        NoweHaslo    = Haslo1.Password;
        DialogResult = true;
        Close();
    }

    private void Anuluj_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
