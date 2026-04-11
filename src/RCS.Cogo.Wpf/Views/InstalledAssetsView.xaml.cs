using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

public partial class InstalledAssetsView : UserControl
{
    private ShellViewModel? _shellVm;

    public InstalledAssetsView()
    {
        InitializeComponent();
        // Hook up after the visual tree is fully loaded so GetWindow() works
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _shellVm = Window.GetWindow(this)?.DataContext as ShellViewModel;
        if (_shellVm != null)
            _shellVm.AssetsFilterChanged += OnAssetsFilterChanged;
    }

    // ── Filter box wiring: push text into ShellViewModel ─────────────────────
    private void AssetsFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var shellVm = Window.GetWindow(this)?.DataContext as ShellViewModel;
        if (shellVm != null)
            shellVm.AssetsFilter = AssetsFilterBox.Text;
    }

    private void ClearAssetsFilter_Click(object sender, RoutedEventArgs e)
    {
        AssetsFilterBox.Text = string.Empty;
        var shellVm = Window.GetWindow(this)?.DataContext as ShellViewModel;
        if (shellVm != null)
            shellVm.AssetsFilter = string.Empty;
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);
        // Unsubscribe if we're being removed from the tree
        if (_shellVm != null && oldParent != null && Window.GetWindow(this) == null)
        {
            _shellVm.AssetsFilterChanged -= OnAssetsFilterChanged;
            _shellVm = null;
        }
    }

    /// <summary>
    /// Collapses Expanders whose Header does not contain the current filter text.
    /// An empty filter restores all Expanders.
    /// </summary>
    private void OnAssetsFilterChanged(object? sender, string filter)
    {
        foreach (var expander in FindVisualChildren<Expander>(this))
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                expander.Visibility = Visibility.Visible;
            }
            else
            {
                string header = expander.Header?.ToString() ?? string.Empty;
                expander.Visibility = header.Contains(filter, System.StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) yield return t;
            foreach (var sub in FindVisualChildren<T>(child)) yield return sub;
        }
    }

    // ── Existing event handlers (unchanged) ──────────────────────────────────

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        var vm = DataContext as InstalledAssetsViewModel;
        if (vm == null || !vm.HasActiveProject)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                e.Cancel = true;
                MessageBox.Show("You must have an open active project to import, edit, or delete information.", "Active Project Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        if (e.EditAction == DataGridEditAction.Commit)
        {
            if (vm != null)
            {
                var item = e.Row.Item;
                Dispatcher.InvokeAsync(async () =>
                {
                   try
                   {
                       await vm.SaveItemAsync(item);
                   }
                   catch (System.Exception ex)
                   {
                       var msg = $"SQLite Error details: {ex.Message}\nInner: {ex.InnerException?.Message}";
                       vm.LogAction?.Invoke($"[DB_ERROR_GRID] {msg}");
                       MessageBox.Show(msg + $"\n\nStack: {ex.StackTrace}", "Grid Auto-Save DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
                   }
                });
            }
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is RCS.Data.Entities.InstalledAsset asset)
        {
            // Cancel any inline edits triggered by the double click so it doesn't collide when we close the window
            grid.CancelEdit();
            grid.CancelEdit();
            e.Handled = true;

            var vm = DataContext as InstalledAssetsViewModel;
            if (vm != null)
            {
                if (!vm.HasActiveProject)
                {
                    MessageBox.Show("You must have an open active project to import, edit, or delete information.", "Active Project Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (asset is RCS.Data.Entities.Figure figure) { var window = new FiguresWindow(figure.ProjectId, async () => { await vm.ReloadAsync(); }); window.Owner = Window.GetWindow(this); window.ShowDialog(); } else { var window = new EditAssetWindow(asset, vm); window.Owner = Window.GetWindow(this); window.ShowDialog(); }
            }
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is RCS.Data.Entities.InstalledAsset asset)
        {
            if (DataContext is InstalledAssetsViewModel vm)
            {
                vm.NotifyAssetSelected(asset);
            }
        }
    }
}
