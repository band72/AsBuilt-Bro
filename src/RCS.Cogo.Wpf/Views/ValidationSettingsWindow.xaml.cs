using System.Windows;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

public partial class ValidationSettingsWindow : Window
{
    private readonly ValidationSettingsViewModel _vm;

    public ValidationSettingsWindow()
    {
        InitializeComponent();
        _vm = new ValidationSettingsViewModel();
        DataContext = _vm;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveSettings();
        DialogResult = true;
        Close();
    }
}
