using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

public partial class EditPointsWindow : Window
{
    public ObservableCollection<PointViewModel> Points { get; }
    public System.Collections.Generic.IEnumerable<string> LocalCodes { get; }
    
    public ICommand EditDescriptionCommand { get; }

    public EditPointsWindow(ObservableCollection<PointViewModel> points, System.Collections.Generic.IEnumerable<string> localCodes)
    {
        InitializeComponent();
        Points = points;
        LocalCodes = localCodes;

        EditDescriptionCommand = new EditDescriptionRelayCommand(ExecuteEditDescription);

        // Bind the window's DataContext to itself so we can use its properties directly in XAML
        DataContext = this;
    }

    private void ExecuteEditDescription(object parameter)
    {
        if (parameter is PointViewModel pt)
        {
            var dialog = new EditDescriptionWindow(pt.Description, LocalCodes);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                pt.Description = dialog.SelectedDescription;
            }
        }
    }
}

// Simple internal relay command to avoid relying on a specific project version of RelayCommand
internal class EditDescriptionRelayCommand : ICommand
{
    private readonly Action<object> _execute;

    public EditDescriptionRelayCommand(Action<object> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged { add { } remove { } } // Suppress warnings

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter != null) _execute(parameter);
    }
}
