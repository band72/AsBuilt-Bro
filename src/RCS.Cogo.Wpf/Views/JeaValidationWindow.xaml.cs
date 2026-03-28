using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.Services;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

/// <summary>View-model wrapper for JeaIssue — adds icon + filter helpers.</summary>
public class JeaIssueRow
{
    public string Severity      { get; init; } = "";
    public string SeverityIcon  { get; init; } = "";
    public string Sheet         { get; init; } = "";
    public string AssetId       { get; init; } = "";
    public string Field         { get; init; } = "";
    public string Message       { get; init; } = "";

    public static JeaIssueRow From(JeaIssue i) => new()
    {
        Severity     = i.Severity.ToString(),
        SeverityIcon = i.Severity switch
        {
            JeaSeverity.Error   => "⛔",
            JeaSeverity.Warning => "⚠",
            _                   => "ℹ"
        },
        Sheet    = i.Sheet,
        AssetId  = i.AssetId,
        Field    = i.Field,
        Message  = i.Message
    };
}

public partial class JeaValidationWindow : Window
{
    private JeaValidationReport? _report;
    private List<JeaIssueRow>    _allRows   = new();
    private readonly string      _projectId;
    private Action?              _onProceedExport;

    public JeaValidationWindow(string projectId, Action? onProceedExport = null)
    {
        InitializeComponent();
        _projectId       = projectId;
        _onProceedExport = onProceedExport;

        SheetFilter.Items.Add("(All Sheets)");
        SheetFilter.SelectedIndex = 0;
    }

    // ── Run Validation ─────────────────────────────────────────────────
    private void OnValidate(object sender, RoutedEventArgs e)
    {
        RunBtn.IsEnabled = false;
        RunBtn.Content   = "Running...";

        try
        {
            _report = JeaValidationService.Validate(_projectId);
            _allRows = _report.Issues.Select(JeaIssueRow.From).ToList();

            // Rebuild sheet filter
            SheetFilter.Items.Clear();
            SheetFilter.Items.Add("(All Sheets)");
            foreach (var s in _allRows.Select(r => r.Sheet).Distinct().OrderBy(x => x))
                SheetFilter.Items.Add(s);
            SheetFilter.SelectedIndex = 0;

            UpdateBadges();
            ApplyFilter();
            UpdateStatus();

            ExportBtn.IsEnabled    = true;
            ExportJeaBtn.IsEnabled = _report.IsValid || _report.WarningCount > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Validation error:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RunBtn.IsEnabled = true;
            RunBtn.Content   = "▶  Run Validation";
        }
    }

    // ── Filter ──────────────────────────────────────────────────────────
    private void OnFilterChanged(object sender, EventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (IssueGrid == null || CountLabel == null || SheetFilter == null || 
            ShowErrors == null || ShowWarnings == null || ShowInfo == null) 
            return;

        bool showErr  = ShowErrors.IsChecked   == true;
        bool showWarn = ShowWarnings.IsChecked == true;
        bool showInfo = ShowInfo.IsChecked     == true;
        string sheet  = SheetFilter.SelectedItem?.ToString() ?? "(All Sheets)";

        var filtered = _allRows.Where(r =>
            (r.Severity == "Error"   && showErr)  ||
            (r.Severity == "Warning" && showWarn) ||
            (r.Severity == "Info"    && showInfo))
            .Where(r => sheet == "(All Sheets)" || r.Sheet == sheet)
            .ToList();

        IssueGrid.ItemsSource = filtered;
        CountLabel.Text = $"{filtered.Count} issue{(filtered.Count == 1 ? "" : "s")} shown";
    }

    // ── Badges & Status ─────────────────────────────────────────────────
    private void UpdateBadges()
    {
        if (_report == null) return;
        ErrorBadge.Text = $"{_report.ErrorCount} Error{(_report.ErrorCount == 1 ? "" : "s")}";
        WarnBadge.Text  = $"{_report.WarningCount} Warning{(_report.WarningCount == 1 ? "" : "s")}";
        InfoBadge.Text  = $"{_report.InfoCount} Info";
    }

    private void UpdateStatus()
    {
        if (_report == null) return;

        if (_report.IsValid && _report.WarningCount == 0)
        {
            StatusLabel.Text      = "✔ All checks passed — ready to export!";
            StatusLabel.Foreground = System.Windows.Media.Brushes.LightGreen;
            SummaryLabel.Text     = "No issues found. Your data meets all JEA Validation Rules.";
        }
        else if (_report.ErrorCount > 0)
        {
            StatusLabel.Text      = $"⛔ {_report.ErrorCount} error{(_report.ErrorCount == 1 ? "" : "s")} must be fixed before export.";
            StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 82, 82));
            SummaryLabel.Text = $"{_report.WarningCount} additional warnings. Address errors first.";
        }
        else
        {
            StatusLabel.Text      = $"⚠ {_report.WarningCount} warning{(_report.WarningCount == 1 ? "" : "s")} — export may proceed.";
            StatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(255, 210, 50));
            SummaryLabel.Text = "Review warnings before submitting to JEA. Export is allowed.";
        }
    }

    // ── Export Report ────────────────────────────────────────────────────
    private void OnExportReport(object sender, RoutedEventArgs e)
    {
        if (_report == null) return;

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter   = "Text Report|*.txt|CSV|*.csv",
            FileName = $"JEA_Validation_Report_{DateTime.Now:yyyyMMdd}.txt"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            bool isCsv = dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            var sb = new StringBuilder();

            if (isCsv)
            {
                sb.AppendLine("Severity,Sheet,AssetId,Field,Message");
                foreach (var r in _allRows)
                    sb.AppendLine($"{r.Severity},{Q(r.Sheet)},{Q(r.AssetId)},{Q(r.Field)},{Q(r.Message)}");
            }
            else
            {
                sb.AppendLine($"JEA AS-BUILT VALIDATION REPORT  —  {DateTime.Now:yyyy-MM-dd HH:mm}");
                sb.AppendLine(new string('═', 70));
                sb.AppendLine($"  ERRORS   : {_report.ErrorCount}");
                sb.AppendLine($"  WARNINGS : {_report.WarningCount}");
                sb.AppendLine($"  INFO     : {_report.InfoCount}");
                sb.AppendLine(new string('─', 70));
                foreach (var grp in _allRows.GroupBy(r => r.Sheet))
                {
                    sb.AppendLine($"\n  [{grp.Key}]");
                    foreach (var r in grp)
                        sb.AppendLine($"    [{r.Severity.ToUpper(),7}]  {r.AssetId,-20}  {r.Field,-30}  {r.Message}");
                }
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Report saved:\n{dlg.FileName}", "Report Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save report:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string Q(string s) =>
        s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    // ── Proceed to Export JEA ────────────────────────────────────────────
    private void OnProceedExport(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        _onProceedExport?.Invoke();
        Close();
    }
}
