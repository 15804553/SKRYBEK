using System.Windows;
using System.Windows.Input;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public SessionInfo? Session => _vm.Session;

    public LoginWindow()
    {
        InitializeComponent();
        _vm = new LoginViewModel();
        DataContext = _vm;
        _vm.LoginCompleted += (_, ok) =>
        {
            if (ok)
            {
                DialogResult = true;
                Close();
            }
        };

        Loaded += async (_, _) =>
        {
            await _vm.ZaladujUzytkownikowAsync();
            // Ustaw fokus na ComboBox lub PasswordBox po załadowaniu
            if (_vm.HasloWymagane)
                PasswordBox.Focus();
            else
                UzytkownikCombo.Focus();
        };
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var haslo = _vm.HasloWymagane ? PasswordBox.Password : string.Empty;
            if (_vm.LoginCommand.CanExecute(haslo))
                _vm.LoginCommand.Execute(haslo);
        }
    }
}
