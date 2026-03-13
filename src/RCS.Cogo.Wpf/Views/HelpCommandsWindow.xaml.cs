using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.Wpf.Views;

public partial class HelpCommandsWindow : Window
{
    private readonly IEnumerable<ICommand> _allCommands;
    
    public HelpCommandsWindow(IEnumerable<ICommand> commands)
    {
        InitializeComponent();
        _allCommands = commands.OrderBy(c => c.Name).ToList();
        CommandsGrid.ItemsSource = _allCommands;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchBox.Text.ToLower();
        if (string.IsNullOrWhiteSpace(query))
        {
            CommandsGrid.ItemsSource = _allCommands;
        }
        else
        {
            CommandsGrid.ItemsSource = _allCommands.Where(c => 
                c.Name.ToLower().Contains(query) || 
                c.Description.ToLower().Contains(query)
            ).ToList();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
