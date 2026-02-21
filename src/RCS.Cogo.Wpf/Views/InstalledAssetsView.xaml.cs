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
                   await vm.SaveItemAsync(item);
                });
            }
        }
    }
}
