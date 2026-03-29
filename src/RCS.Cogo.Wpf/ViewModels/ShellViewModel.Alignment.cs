using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;
using RCS.Cogo.Core.Primitives;
using RCS.Cogo.Wpf.Commands;
using RCS.Cogo.App.Models;
using RCS.Cogo.App.Persistence;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RCS.Geo.Core;
using RCS.Geo.ProjNet;
using RCS.Geo.Abstractions;
using GeoWpf = RCS.Geo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.ViewModels;

public partial class ShellViewModel
{
    private void ExportScript()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "Script.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                System.IO.File.WriteAllText(dialog.FileName, BatchScriptContent);
                CommandLog.Add($"Script exported to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting script: {ex.Message}");
            }
        }
    }

    private void SaveHorizontalAlignmentFromMenu()
    {
        // Execute SAVE-HALN with default name if no args passed, or prompt
        // Alternatively, since they just want to save the active script...
        // The script itself might be empty, but we'll try to save it.
        string name = "Menu_HALN_" + DateTime.Now.ToString("HHmmss");
        _engine.ExecuteAsync($"SAVE-HALN \"{name}\" \"Saved from Menu\"", _context).Wait();
        // Force refresh
        RefreshData(false);
    }

    private void SaveVerticalAlignmentFromMenu()
    {
        string name = "Menu_PFL_" + DateTime.Now.ToString("HHmmss");
        _engine.ExecuteAsync($"SAVE-PFL \"{name}\" \"Saved from Menu\"", _context).Wait();
        // Force refresh
        RefreshData(false);
    }

    private async void DeleteHorizontalAlignmentFromMenu()
    {
        var win = new RCS.Cogo.Wpf.Views.DeleteAlignmentWindow("Delete Figure", InstalledAssets.FigureAssets) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true && win.SelectedItem is RCS.Data.Entities.Figure ha)
        {
            await InstalledAssets.DeleteAssetAsync(ha);
            InstalledAssets.FigureAssets.Remove(ha);
            RefreshData(false);
        }
    }

    private async void DeleteVerticalAlignmentFromMenu()
    {
        var win = new RCS.Cogo.Wpf.Views.DeleteAlignmentWindow("Delete Figure", InstalledAssets.FigureAssets) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true && win.SelectedItem is RCS.Data.Entities.Figure pa)
        {
            await InstalledAssets.DeleteAssetAsync(pa);
            InstalledAssets.FigureAssets.Remove(pa);
            RefreshData(false);
        }
    }

    private void ExportOutputLog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "OutputLog.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                System.IO.File.WriteAllText(dialog.FileName, ResultLogText);
                CommandLog.Add($"Output log exported to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting output log: {ex.Message}");
            }
        }
    }

    private void ExportPointsTxt()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "Points.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                var sb = new System.Text.StringBuilder();
                // Header (Optional, user didn't ask but good practice, user specified format: Number,N,E,Z,Desc)
                // sb.AppendLine("Number,Northing,Easting,Elevation,Description");
                
                foreach (var p in _context.GetAllPoints())
                {
                    sb.AppendLine($"{p.Id},{p.Point.Northing:F4},{p.Point.Easting:F4},{p.Point.Elevation:F4},{p.Description}");
                }
                
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString());
                CommandLog.Add($"Points exported (TXT) to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting points: {ex.Message}");
            }
        }
    }

    public System.Windows.Input.ICommand ExportDxfCommand { get; }
    public System.Windows.Input.ICommand ImportDxfCommand { get; }
    
    // Report Commands
    public System.Windows.Input.ICommand ReportWaterCommand { get; }
    public System.Windows.Input.ICommand ReportSewerCommand { get; }
    public System.Windows.Input.ICommand ReportGasCommand { get; }
    public System.Windows.Input.ICommand ReportElectricCommand { get; }
    public System.Windows.Input.ICommand ReportDrainageCommand { get; }
    public System.Windows.Input.ICommand ReportAllAssetsCsvCommand { get; }
    public System.Windows.Input.ICommand ReportAllAssetsTxtCommand { get; }
    public System.Windows.Input.ICommand ReportAllAssetsXlsCommand { get; }

    private void ExportAllAssets(string format)
    {
        string filter = format switch {
            "csv" => "CSV Files (*.csv)|*.csv",
            "txt" => "Text Files (*.txt)|*.txt",
            _ => "Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx"
        };
        string ext = format switch {
            "csv" => ".csv",
            "txt" => ".txt",
            _ => ".xlsx"
        };
        string title = "Report All Assets - " + format.ToUpper();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = $"AllAssets_Report{ext}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                InstalledAssets.ExportAllToSingleFile(dialog.FileName, format);
                _context.Log($"[AUDIT] Exported All Assets ({format.ToUpper()}) to {dialog.FileName}");
                System.Windows.MessageBox.Show($"Exported successfully to:\n{dialog.FileName}", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch(Exception ex)
            {
                _context.Log($"[ERROR] Export All Assets Error: {ex.Message}");
                System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error");
            }
        }
    }

    /// <summary>
    /// Generic Save dialog + call into <see cref="RCS.Cogo.Wpf.Services.DisciplineReportService"/>
    /// for any of the five discipline report exports.
    /// </summary>
    private void ExportDisciplineReport(
        string disciplineLabel,
        string defaultFileName,
        Func<string, string, string, RCS.Cogo.Wpf.Services.DisciplineReportResult> exportFunc)
    {
        if (!EnsureActiveProject()) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title    = $"Export {disciplineLabel} Report",
            Filter   = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = defaultFileName
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            string pid   = CurrentProject.Id.ToString();
            string pName = CurrentProject.ProjectName ?? "Unknown Project";

            var result = exportFunc(dialog.FileName, pid, pName);

            if (result.Success)
            {
                _context.Log($"[AUDIT] {disciplineLabel} Report exported → {dialog.FileName}");
                _context.Log($"[AUDIT] {result.Summary()}");

                System.Windows.MessageBox.Show(
                    $"{disciplineLabel} Report saved to:\n{dialog.FileName}" +
                    $"\n\nTotal rows: {result.TotalRows}\n\n" +
                    string.Join("\n", result.SheetCounts
                        .Where(s => s.Count > 0)
                        .Select(s => $"  {s.Sheet}: {s.Count}")),
                    "Report Exported",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                _context.Log($"[ERROR] {disciplineLabel} Report failed: {result.ErrorMessage}");
                System.Windows.MessageBox.Show(
                    $"Export failed:\n{result.ErrorMessage}",
                    "Report Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _context.Log($"[ERROR] {disciplineLabel} Report error: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Unexpected error:\n{ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ExecuteTestNativeSecurity()
    {
        try
        {
            // Pick a seed, maybe the day of the year, or just a constant. 
            // In a real app this would be part of your hardware ID.
            int seed = DateTime.Now.DayOfYear;
            
            // Call into Machine Code (Native C++)!
            string secureData = RCS.Cogo.Wpf.Services.NativeSecurityWrapper.GetSecureData(seed);
            string mId = RCS.Cogo.Wpf.Services.NativeSecurityWrapper.GetHardwareFingerprint();

            System.Windows.MessageBox.Show(
                $"Unmanaged C++ DLL executed successfully!\n\nExtracted Native Machine ID:\n{mId}\n\nSeed Requested: {seed}\nEncrypted C++ Response: {secureData}", 
                "Hardware Level Security Achieved", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to call C++ DLL: {ex.Message}", "Error");
        }
    }

    private void OpenLicensingAgentWindow()
    {
        var win = new RCS.Cogo.Wpf.Views.LicenseAgentWindow
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    private class DxfEntity
    {
        public string Type { get; set; } = "";
        public string Layer { get; set; } = "";
        public System.Collections.Generic.List<RCS.Cogo.Core.Primitives.Point3D> Points { get; set; } = new();
    }

}
