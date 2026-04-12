using System.IO;
using System.Windows;

namespace RCS.Cogo.Wpf.Views;

public partial class GeneralSettingsWindow : Window
{
    public GeneralSettingsWindow()
    {
        InitializeComponent();
        Loaded += (_, __) => RefreshBlocksPathStatus();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ShellViewModel vm)
        {
            RCS.Services.GlobalSettingsService.SaveSetting("SymbolScaleMultiplier",     vm.SymbolScaleMultiplier.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("ShowViewportLegend",         vm.ShowViewportLegend.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("IsOutputLogDescending",      vm.IsOutputLogDescending.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("PointNumberSize",            vm.PointNumberSize.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("PointMarkerSize",            vm.PointMarkerSize.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("FigureLineWidth",            vm.FigureLineWidth.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("MapCheckClosureTolerance",   vm.MapCheckClosureTolerance.ToString());
            RCS.Services.GlobalSettingsService.SaveSetting("MinimumBoundaryArea",        vm.MinimumBoundaryArea.ToString());
            // ── JEA settings ──────────────────────────────────────────────
            RCS.Services.GlobalSettingsService.SaveSetting("JeaTemplatePath",            vm.JeaTemplatePath);
            RCS.Services.GlobalSettingsService.SaveSetting("JeaStatePlaneZone",          vm.JeaStatePlaneZone);
            // ── Script Auto-Save ────────────────────────────────────────
            RCS.Services.GlobalSettingsService.SaveSetting("CogoScriptDefaultSavePath",  vm.CogoScriptDefaultSavePath);
            // ── DXF Blocks Library ──────────────────────────────────────
            RCS.Services.GlobalSettingsService.SaveSetting("RcsBlocksPath",              vm.RcsBlocksPath);
        // ── GPS coordinate display format ─────────────────────────────────
        RCS.Services.GlobalSettingsService.SaveSetting("GpsCoordinateFormat",    vm.GpsCoordinateFormat.ToString());
        RCS.Services.GlobalSettingsService.SaveSetting("ShowGpsColumnsInGrid",   vm.ShowGpsColumnsInGrid.ToString());
        // ── GPS Transform session state ──────────────────────────────────
        RCS.Services.GlobalSettingsService.SaveSetting("GpsTransformDirection",  vm.CoordinateTransformVm.Direction.ToString());
        RCS.Services.GlobalSettingsService.SaveSetting("GpsTransformCrsId",      vm.CoordinateTransformVm.SelectedSourceCrs?.CrsId ?? "EPSG:6438");
        }
        Close();
    }

    private void BrowseJeaTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select Blank JEA As-Built Template (.xlsx)",
            Filter = "Excel Workbook|*.xlsx",
        };
        if (dlg.ShowDialog() == true && DataContext is ViewModels.ShellViewModel vm)
            vm.JeaTemplatePath = dlg.FileName;
    }

    private void BrowseCogoScriptPath_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Default Cogo Script Save Folder",
            Multiselect = false,
            InitialDirectory = (DataContext as ViewModels.ShellViewModel)?.CogoScriptDefaultSavePath ?? string.Empty
        };
        if (dlg.ShowDialog() == true && DataContext is ViewModels.ShellViewModel vm)
        {
            vm.CogoScriptDefaultSavePath = dlg.FolderName;
        }
    }

    private void BrowseBlocksPath_Click(object sender, RoutedEventArgs e)
    {
        // .NET 8 WPF native folder picker — no WinForms dependency needed
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title         = "Select the RCS_Blocks folder containing *.dwg block files",
            Multiselect   = false,
            InitialDirectory = (DataContext as ViewModels.ShellViewModel)?.RcsBlocksPath
                              ?? string.Empty
        };
        if (dlg.ShowDialog() == true && DataContext is ViewModels.ShellViewModel vm)
        {
            vm.RcsBlocksPath = dlg.FolderName;
            RefreshBlocksPathStatus();
        }
    }

    private void RefreshBlocksPathStatus()
    {
        if (DataContext is not ViewModels.ShellViewModel vm) return;
        string path = vm.RcsBlocksPath;

        if (string.IsNullOrWhiteSpace(path))
        {
            // Show where Auto-Detect will land
            string autoPath = Views.EditCogoCodeWindow.BlocksDirectory;
            BlocksPathStatus.Text = Directory.Exists(autoPath)
                ? $"✓ Auto-detect: {autoPath}  ({CountDwg(autoPath)} blocks found)"
                : $"⚠ Auto-detect failed. Override the path above.";
            BlocksPathStatus.Foreground = Directory.Exists(autoPath)
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.Orange;
        }
        else
        {
            bool exists = Directory.Exists(path);
            int count   = exists ? CountDwg(path) : 0;
            BlocksPathStatus.Text = exists
                ? $"✓ {count} block file(s) found"
                : "⛔ Directory not found — check the path";
            BlocksPathStatus.Foreground = exists
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.OrangeRed;
        }
    }

    private static int CountDwg(string dir) =>
        Directory.Exists(dir) ? Directory.GetFiles(dir, "*.dwg").Length : 0;
}
