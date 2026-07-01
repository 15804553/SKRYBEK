using System.Windows.Controls;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class SkrybekSettingsView : UserControl
{
    public SettingsViewModel ViewModel { get; }

    public SkrybekSettingsView(SessionInfo session)
    {
        InitializeComponent();
        ViewModel = new SettingsViewModel(session);
        DataContext = ViewModel;
        _ = ViewModel.LoadAsync();
    }
}
