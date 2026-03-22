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
        if (DataContext is ViewModels.ShellViewModel vm)
        {
            RCS.Services.GlobalSettingsService.SaveSetting("SymbolScaleMultiplier", vm.SymbolScaleMultiplier.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("ShowViewportLegend", vm.ShowViewportLegend.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("IsOutputLogDescending", vm.IsOutputLogDescending.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("PointNumberSize", vm.PointNumberSize.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("PointMarkerSize", vm.PointMarkerSize.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("FigureLineWidth", vm.FigureLineWidth.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("MapCheckClosureTolerance", vm.MapCheckClosureTolerance.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("MinimumBoundaryArea", vm.MinimumBoundaryArea.ToString());
        }
        Close();
    }
}
