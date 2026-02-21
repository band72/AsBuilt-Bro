using System.Windows;

namespace RCS.Cogo.Wpf.Views;

public partial class GeneralSettingsWindow : Window
{
    public GeneralSettingsWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
