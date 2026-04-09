using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace RCS.Data.Entities;

[Table("SymbolManager")]
public class SymbolManagerEntity : INotifyPropertyChanged
{
    private int _id;
    private string? _clientCode;
    private string? _systemCode;
    private string? _symbol;
    private string? _type;
    private string? _discipline;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id 
    { 
        get => _id; 
        set { _id = value; OnPropertyChanged(); } 
    }

    [Column("ClientCode")]
    public string? ClientCode 
    { 
        get => _clientCode; 
        set { _clientCode = value; OnPropertyChanged(); } 
    }

    [Column("SystemCode")]
    public string? SystemCode 
    { 
        get => _systemCode; 
        set { _systemCode = value; OnPropertyChanged(); } 
    }

    [Column("Symbol")]
    public string? Symbol 
    { 
        get => _symbol; 
        set { _symbol = value; OnPropertyChanged(); } 
    }

    [Column("Type")]
    public string? Type 
    { 
        get => _type; 
        set { _type = value; OnPropertyChanged(); } 
    }

    [Column("Discipline")]
    public string? Discipline 
    { 
        get => _discipline; 
        set 
        { 
            _discipline = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(Fill)); 
        } 
    }

    private string? _dxfBlock;

    [Column("DxfBlock")]
    public string? DxfBlock 
    { 
        get => _dxfBlock; 
        set { _dxfBlock = value; OnPropertyChanged(); } 
    }


    [NotMapped]
    public string Fill
    {
        get
        {
            if (string.IsNullOrEmpty(Discipline)) return "White";
            string d = Discipline.ToLowerInvariant();
            if (d.Contains("water") && !d.Contains("waste")) return "Blue";
            if (d.Contains("sewer") || d.Contains("waste water")) return "Green";
            if (d.Contains("reclaimed")) return "Purple";
            if (d.Contains("gas")) return "Yellow";
            if (d.Contains("electric")) return "Red";
            if (d.Contains("chilled")) return "LightBlue";
            return "Gray";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
