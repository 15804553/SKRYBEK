using System.Windows;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SessionInfo session)
    {
        InitializeComponent();
        var vm = new SettingsViewModel(session);
        DataContext = vm;
        _ = vm.LoadAsync();
    }
}
