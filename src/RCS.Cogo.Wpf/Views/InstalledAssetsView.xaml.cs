using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

public partial class InstalledAssetsView : UserControl
{
    public InstalledAssetsView()
    {
        InitializeComponent();
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
        {
            var vm = DataContext as InstalledAssetsViewModel;
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
                var window = new EditAssetWindow(asset, vm);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
        }
    }
}
