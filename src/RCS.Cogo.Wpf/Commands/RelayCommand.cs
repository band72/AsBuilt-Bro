using System;
using System.Windows.Input;

namespace RCS.Cogo.Wpf.Commands;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter)
    {
        try { _execute(parameter); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Command failed:\n{ex.Message}", "Application Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
