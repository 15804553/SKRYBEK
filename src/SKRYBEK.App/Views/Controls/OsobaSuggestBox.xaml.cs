using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views.Controls;

public partial class OsobaSuggestBox : UserControl
{
    private bool _suppressSearch;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(OsobaSuggestBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PersonelProperty =
        DependencyProperty.Register(
            nameof(Personel),
            typeof(IEnumerable),
            typeof(OsobaSuggestBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(OsobaSuggestBox),
            new PropertyMetadata(false));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IEnumerable? Personel
    {
        get => (IEnumerable?)GetValue(PersonelProperty);
        set => SetValue(PersonelProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public OsobaSuggestBox()
    {
        InitializeComponent();
    }

    private IEnumerable<Funkcjonariusz> PersonelLista =>
        Personel?.OfType<Funkcjonariusz>() ?? [];

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSearch || IsReadOnly) return;

        var filtered = PersonelSuggestFilter.Szukaj(PersonelLista, InputBox.Text).ToList();
        SuggestionsList.ItemsSource = filtered;
        SuggestionsPopup.IsOpen = filtered.Count > 0 && InputBox.IsKeyboardFocused;
    }

    private void InputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!SuggestionsList.IsKeyboardFocusWithin)
            SuggestionsPopup.IsOpen = false;
    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            SuggestionsPopup.IsOpen = false;

        if (e.Key == Key.Down && SuggestionsPopup.IsOpen && SuggestionsList.Items.Count > 0)
        {
            SuggestionsList.Focus();
            SuggestionsList.SelectedIndex = 0;
            e.Handled = true;
        }
    }

    private void SuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuggestionsList.SelectedItem is Funkcjonariusz osoba)
            UstawZNadpisaniem(osoba.StopienINazwisko);
    }

    private void SuggestionsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is Funkcjonariusz osoba)
            UstawZNadpisaniem(osoba.StopienINazwisko);
    }

    private void UstawZNadpisaniem(string wartosc)
    {
        _suppressSearch = true;
        Text = wartosc;
        InputBox.Text = wartosc;
        _suppressSearch = false;
        SuggestionsPopup.IsOpen = false;
        InputBox.Focus();
    }
}
