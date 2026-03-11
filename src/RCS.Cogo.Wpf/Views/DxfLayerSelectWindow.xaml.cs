using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RCS.Cogo.Wpf.Views;

public partial class DxfLayerSelectWindow : Window
{
    public List<string> SelectedLayers { get; private set; } = new();

    public DxfLayerSelectWindow(IEnumerable<string> layers)
    {
        InitializeComponent();
        foreach (var l in layers)
            lstLayers.Items.Add(l);
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        SelectedLayers = lstLayers.SelectedItems.Cast<string>().ToList();
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
