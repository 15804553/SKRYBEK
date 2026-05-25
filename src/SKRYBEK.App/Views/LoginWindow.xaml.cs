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
        LoginBox.Focus();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.LoginCommand.CanExecute(PasswordBox.Password))
            _vm.LoginCommand.Execute(PasswordBox.Password);
    }
}
