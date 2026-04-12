using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Startup splash/welcome screen. Shown automatically when the app opens with no
/// active project. The caller (ShellWindow) inspects the Action property to route
/// the user's choice back to the right command.
/// </summary>
public partial class WelcomeWindow : Window
{
    public enum WelcomeAction { None, New, Open, ImportData, OpenRecent, NewAsBuilt }

    /// <summary>Action the user elected. Read by ShellWindow after ShowDialog().</summary>
    public WelcomeAction SelectedAction { get; private set; } = WelcomeAction.None;

    /// <summary>When SelectedAction == OpenRecent, holds the chosen recent file.</summary>
    public RecentFileEntry? SelectedRecentFile { get; private set; }

    private readonly ShellViewModel _vm;

    public WelcomeWindow(ShellViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        // Version badge
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.0";

        // Populate recent list
        RecentList.ItemsSource = _vm.RecentFiles;
        EmptyLabel.Visibility  = _vm.RecentFiles.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Custom chrome drag ──────────────────────────────────────────────────────
    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    // ── Buttons ─────────────────────────────────────────────────────────────────
    private void OnNewProject(object sender, RoutedEventArgs e)
    {
        SelectedAction = WelcomeAction.New;
        DialogResult = true;
    }

    private void OnOpenProject(object sender, RoutedEventArgs e)
    {
        SelectedAction = WelcomeAction.Open;
        DialogResult = true;
    }

    private void OnImport(object sender, RoutedEventArgs e)
    {
        SelectedAction = WelcomeAction.ImportData;
        DialogResult = true;
    }

    private void OnNewAsBuilt(object sender, RoutedEventArgs e)
    {
        SelectedAction = WelcomeAction.NewAsBuilt;
        DialogResult   = true;
    }

    private void OnSkip(object sender, RoutedEventArgs e)
    {
        SelectedAction = WelcomeAction.None;
        DialogResult   = false;
        Close();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Recent file double-click ─────────────────────────────────────────────────
    private void RecentList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentList.SelectedItem is RecentFileEntry entry && File.Exists(entry.FilePath))
        {
            SelectedAction     = WelcomeAction.OpenRecent;
            SelectedRecentFile = entry;
            DialogResult       = true;
        }
        else if (RecentList.SelectedItem is RecentFileEntry missing)
        {
            MessageBox.Show(
                $"File not found:\n{missing.FilePath}",
                "File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
