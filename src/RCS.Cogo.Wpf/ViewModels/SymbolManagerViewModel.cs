using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using RCS.Data;
using RCS.Data.Entities;
using System.Runtime.CompilerServices;

namespace RCS.Cogo.Wpf.ViewModels;

public class SymbolManagerViewModel : INotifyPropertyChanged
{
    public ObservableCollection<SymbolManagerEntity> Symbols { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }

    private SymbolManagerEntity? _selectedSymbol;
    public SymbolManagerEntity? SelectedSymbol
    {
        get => _selectedSymbol;
        set { _selectedSymbol = value; OnPropertyChanged(); }
    }

    // Dropdown source lists based on requirements
    public ObservableCollection<string> AvailableDisciplines { get; } = new(new[] 
    { 
        "gas", "electric", "water", "sewer", "waste water", "reclaimed", "chilled",
        "g", "st", "d", "e", "el"
    });

    public ObservableCollection<string> AvailableTypes { get; } = new(new[] 
    { 
        "fitting", "pipe", "manhole", "valve" 
    });

    public SymbolManagerViewModel()
    {
        SaveCommand = new RCS.Cogo.Wpf.Commands.RelayCommand(_ => Save());
        AddCommand = new RCS.Cogo.Wpf.Commands.RelayCommand(_ => AddNew());
        DeleteCommand = new RCS.Cogo.Wpf.Commands.RelayCommand(_ => DeleteSelected());
        ImportCommand = new RCS.Cogo.Wpf.Commands.RelayCommand(_ => ImportCsv());
        ExportCommand = new RCS.Cogo.Wpf.Commands.RelayCommand(_ => ExportCsv());

        LoadData();
    }

    private void LoadData()
    {
        Symbols.Clear();
        using var db = new AppDbContext();
        var items = db.SymbolManagers.ToList();
        foreach (var item in items)
        {
            Symbols.Add(item);
        }
    }

    private void AddNew()
    {
        var newItem = new SymbolManagerEntity();
        Symbols.Add(newItem);
        SelectedSymbol = newItem;
    }

    private void DeleteSelected()
    {
        if (SelectedSymbol != null)
        {
            Symbols.Remove(SelectedSymbol);
            SelectedSymbol = null;
        }
    }

    private void Save()
    {
        using var db = new AppDbContext();
        
        // Very simple sync for now: truncate and insert, or track changes properly.
        // It's safer to clear and add since we are editing in memory without attaching contexts.
        db.SymbolManagers.RemoveRange(db.SymbolManagers);
        
        // Rebuild elements to lose tracking
        var entities = Symbols.Select(s => new SymbolManagerEntity 
        {
            ClientCode = s.ClientCode,
            SystemCode = s.SystemCode,
            Symbol = s.Symbol,
            Type = s.Type,
            Discipline = s.Discipline
        }).ToList();

        db.SymbolManagers.AddRange(entities);
        db.SaveChanges();

        // Refresh Memory to get actual DB IDs back
        LoadData();
        System.Windows.MessageBox.Show("Symbols Saved Successfully!", "Symbol Manager", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void ImportCsv()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(dialog.FileName);
                
                // Track existing in memory so we don't clobber configurations if we re-import master code lists
                var existingSymbols = Symbols.ToList(); 
                Symbols.Clear();
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        string clientCode = parts[0].Trim();
                        string systemCode = parts[1].Trim();
                        
                        var existing = existingSymbols.FirstOrDefault(s => s.ClientCode == clientCode && s.SystemCode == systemCode);

                        // If CSV provides additional columns, extract them
                        string symbol = (parts.Length >= 3) ? parts[2].Trim() : "";
                        string type = (parts.Length >= 4) ? parts[3].Trim() : "";
                        string discipline = (parts.Length >= 5) ? parts[4].Trim() : "";

                        // If they are missing in the import, fall back gracefully to the existing configured memory state
                        if (existing != null)
                        {
                            if (string.IsNullOrEmpty(symbol)) symbol = existing.Symbol ?? "";
                            if (string.IsNullOrEmpty(type)) type = existing.Type ?? "";
                            if (string.IsNullOrEmpty(discipline)) discipline = existing.Discipline ?? "";
                        }

                        // Validate! If the data supplied in the CSV doesn't exist in our Combobox ItemsSources, null it.
                        // This gracefully skips importing purely descriptive 3rd-columns from Standard Cogo Codes exports.
                        if (!string.IsNullOrEmpty(symbol) && !SymbolManagerStaticSource.AvailableSymbols.Contains(symbol)) symbol = "";
                        if (!string.IsNullOrEmpty(type) && !SymbolManagerStaticSource.Types.Contains(type.ToLowerInvariant())) type = "";
                        if (!string.IsNullOrEmpty(discipline) && !SymbolManagerStaticSource.Disciplines.Contains(discipline.ToLowerInvariant())) discipline = "";

                        Symbols.Add(new SymbolManagerEntity
                        {
                            ClientCode = clientCode,
                            SystemCode = systemCode,
                            Symbol = symbol,
                            Type = type,
                            Discipline = discipline
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing: {ex.Message}");
            }
        }
    }

    private void ExportCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "Symbols.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (var s in Symbols)
                {
                    sb.AppendLine($"{s.ClientCode},{s.SystemCode},{s.Symbol},{s.Type},{s.Discipline}");
                }
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString());
                System.Windows.MessageBox.Show($"Exported {Symbols.Count} symbols to {dialog.FileName}", "Export Successful");
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting: {ex.Message}");
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
