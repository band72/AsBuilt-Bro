using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RCS.Cogo.Wpf.Views;

public partial class EditDescriptionWindow : Window, INotifyPropertyChanged
{
    private string _selectedDescription = "";
    public string SelectedDescription
    {
        get => _selectedDescription;
        set
        {
            if (_selectedDescription != value)
            {
                _selectedDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public System.Collections.Generic.IEnumerable<string> AvailableCodes { get; }

    public EditDescriptionWindow(string initialDescription, System.Collections.Generic.IEnumerable<string> availableCodes)
    {
        InitializeComponent();
        
        SelectedDescription = initialDescription ?? "";
        AvailableCodes = availableCodes;
        
        DataContext = this;
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        // SelectedDescription contains the typed or selected value
        DialogResult = true;
        Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
