using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SKRYBEK.App.Helpers;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.Views;

public partial class RozkazEditorView : UserControl
{
    private string _loggedInLogin = string.Empty;
    private bool _canEditAll;
    private INotifyPropertyChanged? _subscribedVm;

    public RozkazEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, DataContext as RozkazEditorViewModel);
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is RozkazEditorViewModel vm)
            {
                SubscribeEditorVm(vm);
                ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, vm);
            }
        };
    }

    private void SubscribeEditorVm(RozkazEditorViewModel vm)
    {
        if (_subscribedVm is not null)
        {
            _subscribedVm.PropertyChanged -= OnEditorVmChanged;
        }

        _subscribedVm = vm;
        vm.PropertyChanged += OnEditorVmChanged;
    }

    private void OnEditorVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is RozkazEditorViewModel vm)
        {
            ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, vm);
        }
    }

    /// <summary>Ustawia widoczność panelu personelu i przycisków wg loginu z sesji.</summary>
    public void ApplyLoggedInUserAccess(string loggedInLogin, bool canEditAll, RozkazEditorViewModel? editorVm)
    {
        _loggedInLogin = loggedInLogin ?? string.Empty;
        _canEditAll = canEditAll;

        if (editorVm is not null)
        {
            SubscribeEditorVm(editorVm);
        }

        var showPersonnel = SkrybekLoginAccess.ShowPersonnelPanel(_loggedInLogin);
        var showSave = SkrybekLoginAccess.ShowSaveButton(_loggedInLogin);
        var showApprove = SkrybekLoginAccess.ShowApproveButton(
            _loggedInLogin, _canEditAll, editorVm?.MozeAkceptowac ?? false);
        var showUnlock = SkrybekLoginAccess.ShowUnlockButton(
            _loggedInLogin, _canEditAll, editorVm?.MozeOdblokować ?? false);

        PersonelPanelHost.Visibility = showPersonnel ? Visibility.Visible : Visibility.Collapsed;
        PersonelColumn.Width = showPersonnel ? GridLength.Auto : new GridLength(0);
        BtnZapiszRozkaz.Visibility = showSave ? Visibility.Visible : Visibility.Collapsed;
        BtnZatwierdzRozkaz.Visibility = showApprove ? Visibility.Visible : Visibility.Collapsed;
        BtnOdblokujRozkaz.Visibility = showUnlock ? Visibility.Visible : Visibility.Collapsed;

        SkrybekLog.Info(
            $"RozkazEditorView.ApplyLoggedInUserAccess: login='{_loggedInLogin}', " +
            $"personel={showPersonnel}, zapisz={showSave}, zatwierdz={showApprove}, odblokuj={showUnlock}");
    }
}
