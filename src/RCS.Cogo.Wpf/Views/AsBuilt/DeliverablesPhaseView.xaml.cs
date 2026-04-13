using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;
using Microsoft.Win32;
using RCS.Piping.Core.Delivery;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class DeliverablesPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;

    public DeliverablesPhaseView() => InitializeComponent();

    private void BtnBrowseFolder_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Select export output folder",
            FileName = "Select Folder"
        };
        if (dlg.ShowDialog() == true)
            TxtOutputFolder.Text = System.IO.Path.GetDirectoryName(dlg.FileName) ?? TxtOutputFolder.Text;
    }

    private void BtnBuildPackage_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;

        var outputDir = TxtOutputFolder.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputDir) || !System.IO.Directory.Exists(outputDir))
        {
            MessageBox.Show("Please select a valid output folder before building the package.",
                            "Build Package", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var builder = new PackageBuilder(outputDir);
            var dir     = builder.Build(Vm.Job);

            // Refresh the export history list
            ExportHistoryList.ItemsSource = Vm.Job.ExportHistory;

            MessageBox.Show($"Package built successfully:\n{dir}",
                            "Build Package", MessageBoxButton.OK, MessageBoxImage.Information);

            // Open the folder in Explorer
            System.Diagnostics.Process.Start("explorer.exe", $"\"{dir}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Package build failed:\n{ex.Message}",
                            "Build Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPrintReport_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        
        // Show a prompt asking whether to Print or just Save XPS
        MessageBoxResult res = MessageBox.Show(
            "Do you want to send this report directly to a printer/print-to-pdf dialog?\n\nSelect 'No' to save it immediately as an XPS document.",
            "Print Output Destination", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            
        if (res == MessageBoxResult.Yes)
            AsBuiltReportPrinter.Print(Vm.Job);
        else if (res == MessageBoxResult.No)
            AsBuiltReportPrinter.SaveAsXps(Vm.Job, Window.GetWindow(this));
    }

    public void Load(AsBuiltJob job)
    {
        // Deliverables ItemsControl binds via DataContext -> no code-behind assignment needed
        ExportHistoryList.ItemsSource        = job.ExportHistory;

        // Default output folder to Desktop if not set
        if (string.IsNullOrWhiteSpace(TxtOutputFolder.Text))
            TxtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // Update build button state
        BtnBuildPackage.IsEnabled = !job.Deliverables.Any(c => c.IsBlocked && c.IsEnabled);
    }
}
