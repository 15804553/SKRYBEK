using System.Windows;

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
            MessageBox.Show("Hasła nie są identyczne.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Haslo1.Password.Length < 4)
        {
            MessageBox.Show("Hasło musi mieć co najmniej 4 znaki.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
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
