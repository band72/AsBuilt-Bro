using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using RCS.Cogo.App;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;
using RCS.Cogo.Core.Primitives;
using RCS.Cogo.Wpf.Commands;

    
using RCS.Cogo.App.Models;
using RCS.Cogo.App.Persistence;
using System.IO;
using System.Text.Json;

namespace RCS.Cogo.Wpf.ViewModels;

public class PointViewModel : ViewModelBase
{
    private string _id;
    public string Id { get => _id; set => SetField(ref _id, value); }
    
    private double _northing;
    public double Northing { get => _northing; set => SetField(ref _northing, value); }
    
    private double _easting;
    public double Easting { get => _easting; set => SetField(ref _easting, value); }
    
    private double _elevation;
    public double Elevation { get => _elevation; set => SetField(ref _elevation, value); }
    
    private string _description;
    public string Description 
    { 
        get => _description; 
        set 
        {
            if (SetField(ref _description, value))
            {
                Validate();
            }
        } 
    }

    private bool _isValidCode;
    public bool IsValidCode { get => _isValidCode; set => SetField(ref _isValidCode, value); }

    private readonly System.Collections.Generic.HashSet<string> _validCodes;

    public PointViewModel(string id, Point3D p, string desc, System.Collections.Generic.IEnumerable<string> validCodes)
    {
        _id = id;
        _northing = p.Northing;
        _easting = p.Easting;
        _elevation = p.Elevation;
        _description = desc;
        _validCodes = new System.Collections.Generic.HashSet<string>(validCodes, StringComparer.OrdinalIgnoreCase);
        Validate();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            IsValidCode = false;
            return;
        }
        
        // Check first word against valid codes
        var parts = Description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            IsValidCode = _validCodes.Contains(parts[0]);
        }
        else
        {
            IsValidCode = false;
        }
    }
}

public class ShellViewModel : ViewModelBase
{
    private readonly ScriptEngine _engine;
    private readonly CogoContext _context;

    private string _commandInput = "";
    public string CommandInput
    {
        get => _commandInput;
        set => SetField(ref _commandInput, value);
    }

    private string _batchScriptContent = "// Enter batch commands here...";
    public string BatchScriptContent
    {
        get => _batchScriptContent;
        set => SetField(ref _batchScriptContent, value);
    }

    public ObservableCollection<string> CommandLog { get; } = new();
    
    // Changed to string for TextBox binding (Select All/Copy support)
    private string _resultLogText = "";
    public string ResultLogText
    {
        get => _resultLogText;
        set => SetField(ref _resultLogText, value);
    }

    public ObservableCollection<PointViewModel> Points { get; } = new();
    public ObservableCollection<FigureViewModel> Figures { get; } = new();
    public ObservableCollection<StructureViewModel> StructureGraphics { get; } = new();

    private Project _currentProject = new Project();
    public Project CurrentProject
    {
        get => _currentProject;
        set
        {
            if (SetField(ref _currentProject, value))
            {
                _ = LoadInstalledAssetsAsync();
            }
        }
    }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetField(ref _selectedTabIndex, value))
            {
                var tabName = value switch 
                {
                    0 => "Points",
                    1 => "Cogo", 
                    2 => "Cogo Script",
                    3 => "Curve Solver",
                    4 => "Piping",
                    5 => "Piping Script",
                    6 => "Codes",
                    7 => "Materials",
                    _ => $"Tab {value}"
                };
                _context.Log($"[AUDIT] Switched to tab: {tabName}");
            }
        }
    }

    private double _currentViewScale = 1.0;
    public double CurrentViewScale
    {
        get => _currentViewScale;
        set
        {
            if (SetField(ref _currentViewScale, value))
            {
                OnPropertyChanged(nameof(MarkerScale));
            }
        }
    }

    private double _symbolScaleMultiplier = 1.0;
    public double SymbolScaleMultiplier
    {
        get => _symbolScaleMultiplier;
        set
        {
            if (SetField(ref _symbolScaleMultiplier, value))
            {
                OnPropertyChanged(nameof(MarkerScale));
            }
        }
    }

    public ObservableCollection<double> AvailableSymbolScales { get; } = new(new[] { 0.5, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0, 10.0 });

    // Marker scale prevents points from becoming microscopic when zoomed out.
    // Adjusted constant (e.g. 5.0) to make them visible "dots".
    public double MarkerScale => (1.0 / Math.Abs(_currentViewScale)) * 2.5 * SymbolScaleMultiplier;

    public System.Windows.Input.ICommand SubmitCommand { get; }
    public System.Windows.Input.ICommand ImportBatchCommand { get; }
    public System.Windows.Input.ICommand RunBatchCommand { get; }

    public System.Windows.Input.ICommand ZoomInCommand { get; }
    public System.Windows.Input.ICommand ZoomOutCommand { get; }
    public System.Windows.Input.ICommand ZoomExtentsCommand { get; }

    public event EventHandler? ZoomExtentsRequested;
    public event EventHandler? ZoomInRequested;
    public event EventHandler? ZoomOutRequested;

    public ShellViewModel()
    {
        var registry = AppInitializer.InitializeRegistry();
        
        // Context logs to our ResultLogText
        _context = new CogoContext(log => 
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => 
            {
                if (log == "[CLEAR]")
                {
                    ResultLogText = "";
                }
                else
                {
                    // Append line
                    ResultLogText += log + Environment.NewLine;
                }
            });
        });

        _engine = new ScriptEngine(registry);

        // Ensure Global DB exists - Load Codes & Materials
        try
        {
            using (var db = new RCS.Data.AppDbContext())
            {
                RCS.Data.DbInitializer.Initialize(db);
                
                // Load Codes
                var codes = db.CogoCodes.ToList();
                foreach (var c in codes)
                {
                    CogoCodes.Add(new CogoCode(c.LocalCode, c.SystemCode, c.Description));
                }
                
                // Load Materials
                var mats = db.Materials.ToList();
                foreach (var m in mats)
                {
                    MasterCatalog.Add(new RCS.Piping.Core.Models.MaterialItem
                    {
                        PartKey = m.PartKey, Discipline = m.Discipline, FeatureType = m.FeatureType,
                        Size = m.Size, Material = m.Material,
                        Manufacturer = m.Manufacturer, Model = m.Model, Year = m.Year, Notes = m.Notes
                    });
                }
                
                _context.Log($"[AUDIT] Loaded {codes.Count} codes and {mats.Count} materials from Master Database.");
            }
        }
        catch (Exception ex)
        {
             _context.Log($"[ERROR] Failed to load Master Database: {ex.Message}");
             CommandLog.Add($"[ERROR] DB Load Failed: {ex.Message}");
             // System.Windows.MessageBox.Show($"Error loading Master Database (Codes/Materials): {ex.Message}", "Database Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        PopulateDropdowns();

        SubmitCommand = new RelayCommand(async _ => await ExecuteCommandAsync());
        ImportBatchCommand = new RelayCommand(_ => ImportBatchScript());
        RunBatchCommand = new RelayCommand(async _ => await RunBatchScriptAsync());

        ZoomInCommand = new RelayCommand(_ => ZoomInRequested?.Invoke(this, EventArgs.Empty));
        ZoomOutCommand = new RelayCommand(_ => ZoomOutRequested?.Invoke(this, EventArgs.Empty));
        ZoomExtentsCommand = new RelayCommand(_ => ZoomExtentsRequested?.Invoke(this, EventArgs.Empty));
        ExportDxfCommand = new RelayCommand(_ => ExportDxf());
        ExportBomCommand = new RelayCommand(_ => ExportBom());
        
        ExportScriptCommand = new RelayCommand(_ => ExportScript());
        ExportPointsTxtCommand = new RelayCommand(_ => ExportPointsTxt());
        ExportPointsXmlCommand = new RelayCommand(_ => ExportPointsXml());
        // ExportDxfCommand = new RelayCommand(_ => ExportDxf()); // This line was moved up
        SyncToAssetsCommand = new RelayCommand(_ => SyncAssets());
        
        SolveCurveCommand = new RelayCommand(_ => SolveCurve());
        UtilConvertCommand = new RelayCommand(_ => ExecuteUtilConvert());
        UtilSupplementCommand = new RelayCommand(_ => ExecuteUtilSupplement());
        ClearCurveSolverCommand = new RelayCommand(_ => ClearCurveSolver());

        _pipeNetwork = new RCS.Piping.Core.Models.PipeNetwork();
        _pipelineRunner = new RCS.Piping.Core.Runner.PipelineRunner(_context, _pipeNetwork);
        CalculateSlopeCommand = new RelayCommand(_ => CalculateSlope());
        AddPipeCommand = new RelayCommand(_ => AddPipe());
        AddStructureCommand = new RelayCommand(_ => AddStructure());
        ValidateNetworkCommand = new RelayCommand(_ => ValidateNetwork());
        
        SavePipingCommand = new RelayCommand(_ => SavePiping());
        LoadPipingCommand = new RelayCommand(_ => LoadPiping());
        
        ImportCodesCommand = new RelayCommand(_ => ImportCodesCsv());
        ExportCodesCommand = new RelayCommand(_ => ExportCodesCsv());
        ClearCodesCommand = new RelayCommand(_ => ClearCodes());
        
        ImportCatalogCommand = new RelayCommand(_ => ImportCatalog());
        AddMaterialToProjectCommand = new RelayCommand(_ => AddMaterialToProject());
        ExportMaterialsCommand = new RelayCommand(_ => ExportMaterials());
        
        ProcessPipingScriptCommand = new RelayCommand(_ => ProcessPipingScript());
        ImportPipingScriptCommand = new RelayCommand(_ => ImportPipingScript());
        ExportPipingScriptCommand = new RelayCommand(_ => ExportPipingScript());
        
        ImportPointsListCommand = new RelayCommand(_ => ImportPointsList());

        NewProjectCommand = new RelayCommand(_ => NewProject());
        EditProjectCommand = new RelayCommand(_ => EditProject());
        SaveProjectCommand = new RelayCommand(_ => SaveProject());
        OpenProjectCommand = new RelayCommand(_ => OpenProject());
        CloseProjectCommand = new RelayCommand(_ => CloseProject());
        OpenReportSettingsCommand = new RelayCommand(_ => OpenReportSettings());
        
        CompactDbCommand = new RelayCommand(_ => CompactDatabase());
        VerifyDbCommand = new RelayCommand(_ => VerifyDatabase());
        RepairDbCommand = new RelayCommand(_ => RepairDatabase());
        ExportDbCsvCommand = new RelayCommand(_ => ExportDatabaseCsv());
        ExportInstalledAssetsCommand = new RelayCommand(_ => ExportInstalledAssets());

        CloseCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());

        CloseCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());
        CloseCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());
        AboutCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("RCS COGO Enterprise\nVersion 2.0\n\nAdvanced Agentic Coding Demo", "About"));
        
        InstalledAssets = new InstalledAssetsViewModel();
        // Load default/empty project
        _ = LoadInstalledAssetsAsync();
    }

    private InstalledAssetsViewModel _installedAssets = null!;
    public InstalledAssetsViewModel InstalledAssets
    {
        get => _installedAssets;
        set => SetField(ref _installedAssets, value);
    }

    private async Task LoadInstalledAssetsAsync()
    {
        if (_currentProject != null)
        {
             // Use ID from current project. If generic "0000" fallback.
             string pNum = "0000"; 
             // Try to extract number from name if possible or just use Name
             if (!string.IsNullOrEmpty(_currentProject.ProjectName)) pNum = _currentProject.ProjectName;
             
             await InstalledAssets.LoadProjectAsync(_currentProject.Id.ToString(), pNum);
        }
    }

    public System.Windows.Input.ICommand NewProjectCommand { get; }
    public System.Windows.Input.ICommand EditProjectCommand { get; }
    public System.Windows.Input.ICommand SaveProjectCommand { get; }
    public System.Windows.Input.ICommand OpenProjectCommand { get; }
    public System.Windows.Input.ICommand CloseProjectCommand { get; }
    public System.Windows.Input.ICommand ImportPointsListCommand { get; }

    public System.Windows.Input.ICommand CloseCommand { get; }
    public System.Windows.Input.ICommand AboutCommand { get; }

    public System.Windows.Input.ICommand ExportScriptCommand { get; }
    public System.Windows.Input.ICommand ExportPointsTxtCommand { get; }
    public System.Windows.Input.ICommand ExportPointsXmlCommand { get; }

    // Curve Solver Properties
    private string _curveRadius = "";
    public string CurveRadius { get => _curveRadius; set => SetField(ref _curveRadius, value); }

    private string _curveTangent = "";
    public string CurveTangent { get => _curveTangent; set => SetField(ref _curveTangent, value); }
    
    private string _curveChord = "";
    public string CurveChord { get => _curveChord; set => SetField(ref _curveChord, value); }

    private string _curveArc = "";
    public string CurveArc { get => _curveArc; set => SetField(ref _curveArc, value); }
    
    // Delta in Decimal Degrees
    private string _curveDelta = "";
    public string CurveDelta { get => _curveDelta; set => SetField(ref _curveDelta, value); }
    
    // Delta in DMS (Read-only for display)
    private string _curveDeltaDms = "";
    public string CurveDeltaDms { get => _curveDeltaDms; set => SetField(ref _curveDeltaDms, value); }

    // --- Utilities Section ---
    
    // DMS Converter
    private string _utilDecInput = "";
    public string UtilDecInput { get => _utilDecInput; set => SetField(ref _utilDecInput, value); }

    private string _utilDmsOutput = "";
    public string UtilDmsOutput { get => _utilDmsOutput; set => SetField(ref _utilDmsOutput, value); }

    public System.Windows.Input.ICommand UtilConvertCommand { get; }

    // Supplement Finder
    private string _utilSuppInput = ""; // Input in Decimal
    public string UtilSuppInput { get => _utilSuppInput; set => SetField(ref _utilSuppInput, value); }

    private string _utilSuppOutput = ""; // Output in DMS
    public string UtilSuppOutput { get => _utilSuppOutput; set => SetField(ref _utilSuppOutput, value); }

    public System.Windows.Input.ICommand UtilSupplementCommand { get; }
    public System.Windows.Input.ICommand ClearCurveSolverCommand { get; } // New Command

    public System.Windows.Input.ICommand SolveCurveCommand { get; }

    // --- Piping Section ---
    private RCS.Piping.Core.Models.PipeNetwork _pipeNetwork;
    private RCS.Piping.Core.Runner.PipelineRunner _pipelineRunner;

    public ObservableCollection<RCS.Piping.Core.Models.PipeRun> PipeRuns { get; } = new();

    // Inputs
    private string _pipeFromId = "";
    public string PipeFromId { get => _pipeFromId; set => SetField(ref _pipeFromId, value); }

    private string _pipeToId = "";
    public string PipeToId { get => _pipeToId; set => SetField(ref _pipeToId, value); }
    
    private string _pipeDiameter = "8.0";
    public string PipeDiameter { get => _pipeDiameter; set => SetField(ref _pipeDiameter, value); }
    
    // Inverts & Slope
    private string _pipeInvStart = "";
    public string PipeInvStart { get => _pipeInvStart; set => SetField(ref _pipeInvStart, value); }

    private string _pipeInvEnd = "";
    public string PipeInvEnd { get => _pipeInvEnd; set => SetField(ref _pipeInvEnd, value); }

    private string _pipeSlope = ""; // Display only for now
    public string PipeSlope { get => _pipeSlope; set => SetField(ref _pipeSlope, value); }

    public System.Windows.Input.ICommand CalculateSlopeCommand { get; }
    public System.Windows.Input.ICommand AddPipeCommand { get; }
    public System.Windows.Input.ICommand ValidateNetworkCommand { get; }
    
    public System.Windows.Input.ICommand SavePipingCommand { get; }
    public System.Windows.Input.ICommand LoadPipingCommand { get; }

    private void CalculateSlope()
    {
        if (!_context.PointExists(PipeFromId) || !_context.PointExists(PipeToId))
        {
            PipeSlope = "Err: Pts";
            return;
        }

        if (double.TryParse(PipeInvStart, out double start) && double.TryParse(PipeInvEnd, out double end))
        {
            var p1 = _context.GetPoint(PipeFromId);
            var p2 = _context.GetPoint(PipeToId);
            
            if (p1 != null && p2 != null)
            {
                double dist = Math.Sqrt(Math.Pow(p2.Northing - p1.Northing, 2) + Math.Pow(p2.Easting - p1.Easting, 2));
                if (dist > 0.001)
                {
                    double slope = ((start - end) / dist) * 100.0;
                    PipeSlope = $"{slope:F2}%";
                }
            }
        }
    }

    private void SavePiping()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Piping Network (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = "PipingNetwork.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                RCS.Piping.Core.Serialization.PipingSerializer.SaveToFile(_pipeNetwork, dialog.FileName);
                CommandLog.Add($"Piping network saved to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error saving execution: {ex.Message}");
            }
        }
    }

    private void LoadPiping()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Piping Network (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // Clear existing ViewModels first? No, we sync from model.
                // But Model doesn't notify. So we must manually refresh.
                // Or clear both if we want a fresh start.
                // For now, let's just append.
                RCS.Piping.Core.Serialization.PipingSerializer.LoadFromFile(_pipeNetwork, dialog.FileName);
                
                // Sync ViewModels
                PipeRuns.Clear();
                foreach(var run in _pipeNetwork.GetAllRuns()) PipeRuns.Add(run);
                
                Structures.Clear();
                foreach(var str in _pipeNetwork.GetAllStructures()) Structures.Add(str);

                CommandLog.Add($"Piping network loaded from: {dialog.FileName}");
                RefreshData();
            }
            catch (Exception ex)
            {
                 CommandLog.Add($"Error loading execution: {ex.Message}");
            }
        }
    }

    private void AddPipe()
    {
        if (string.IsNullOrWhiteSpace(PipeFromId) || string.IsNullOrWhiteSpace(PipeToId))
        {
            CommandLog.Add("Error: Must specify From and To Point IDs.");
            return;
        }

        if (!_context.PointExists(PipeFromId))
        {
             CommandLog.Add($"Error: Point {PipeFromId} does not exist.");
             return;
        }
        if (!_context.PointExists(PipeToId))
        {
             CommandLog.Add($"Error: Point {PipeToId} does not exist.");
             return;
        }

        if (!double.TryParse(PipeDiameter, out double diam))
        {
             CommandLog.Add("Error: Invalid Diameter.");
             return;
        }

        double? startInv = double.TryParse(PipeInvStart, out double si) ? si : null;
        double? endInv = double.TryParse(PipeInvEnd, out double ei) ? ei : null;

        var newRun = new RCS.Piping.Core.Models.PipeRun
        {
            FromPointId = PipeFromId,
            ToPointId = PipeToId,
            Diameter = diam,
            InvertStart = startInv,
            InvertEnd = endInv
        };

        _pipeNetwork.AddRun(newRun);
        PipeRuns.Add(newRun); // Keep observable synced
        CommandLog.Add($"Added Pipe {newRun.Id} ({PipeFromId}->{PipeToId}, D={diam})");
        
        // Advance for Continuous Run
        PipeFromId = PipeToId;
        PipeToId = "";
        
        // Advance Invert if valid
        if (endInv.HasValue)
        {
            PipeInvStart = endInv.Value.ToString();
        }
        else
        {
             PipeInvStart = "";
        }
        PipeInvEnd = "";
        PipeSlope = "";

        // Refresh?
        RefreshData();
    }

    // Structures Section
    public ObservableCollection<RCS.Piping.Core.Models.PipeStructure> Structures { get; } = new();

    private string _structPointId = "";
    public string StructPointId { get => _structPointId; set => SetField(ref _structPointId, value); }
    
    private string _structType = "Manhole"; // Default
    public string StructType { get => _structType; set => SetField(ref _structType, value); }

    public System.Windows.Input.ICommand AddStructureCommand { get; }

    private void AddStructure()
    {
        if (string.IsNullOrWhiteSpace(StructPointId))
        {
             CommandLog.Add("Error: Must specify Point ID.");
             return;
        }

        if (!_context.PointExists(StructPointId))
        {
             CommandLog.Add($"Error: Point {StructPointId} does not exist.");
             return;
        }

        var newStruct = new RCS.Piping.Core.Models.PipeStructure
        {
            PointId = StructPointId,
            Type = StructType
        };

        _pipeNetwork.AddStructure(newStruct);
        Structures.Add(newStruct);
        CommandLog.Add($"Added Structure {newStruct.Id} at {StructPointId} ({StructType})");
        
        RefreshData();
    }


    // --- Codes Section ---
    public ObservableCollection<CogoCode> CogoCodes { get; } = new();
    public System.Windows.Input.ICommand ImportCodesCommand { get; }
    public System.Windows.Input.ICommand ExportCodesCommand { get; }

    private void ImportCodesCsv()
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
                
                // Clear existing in memory and DB
                CogoCodes.Clear();
                using (var db = new RCS.Data.AppDbContext())
                {
                    db.CogoCodes.RemoveRange(db.CogoCodes);
                    
                    var entities = new List<RCS.Data.Entities.CogoCodeEntity>();
                    foreach(var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',');
                        
                        string local = "", sys = "", desc = "";
                        
                        if (parts.Length >= 3) { local = parts[0].Trim(); sys = parts[1].Trim(); desc = parts[2].Trim(); }
                        else if (parts.Length == 2) { local = parts[0].Trim(); sys = parts[1].Trim(); }
                        else if (parts.Length == 1) { local = parts[0].Trim(); }
                        
                        CogoCodes.Add(new CogoCode(local, sys, desc));
                        entities.Add(new RCS.Data.Entities.CogoCodeEntity { LocalCode = local, SystemCode = sys, Description = desc });
                    }
                    
                    db.CogoCodes.AddRange(entities);
                    db.SaveChanges();
                }
                
                CommandLog.Add($"Imported {CogoCodes.Count} codes from {dialog.FileName} to Database");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error importing codes: {ex.Message}");
                System.Windows.MessageBox.Show($"Error importing codes: {ex.Message}");
            }
        }
    }

    public System.Windows.Input.ICommand ClearCodesCommand { get; }

    private void ClearCodes()
    {
        if (System.Windows.MessageBox.Show("Are you sure you want to clear all codes from the Master Database?", "Confirm Clear", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
        {
            try 
            {
                CogoCodes.Clear();
                using (var db = new RCS.Data.AppDbContext())
                {
                    db.CogoCodes.RemoveRange(db.CogoCodes);
                    db.SaveChanges();
                }
                CommandLog.Add("Cleared all Cogo Codes from Database.");
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show($"Error clearing codes: {ex.Message}");
            }
        }
    }

    private void ExportCodesCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
             Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
             DefaultExt = ".csv",
             FileName = "Codes.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach(var code in CogoCodes)
                {
                    sb.AppendLine($"{code.LocalCode},{code.SystemCode},{code.Description}");
                }
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString());
                CommandLog.Add($"Exported {CogoCodes.Count} codes to {dialog.FileName}");
            }
            catch (Exception ex)
            {
                 CommandLog.Add($"Error exporting codes: {ex.Message}");
            }
        }
    }

    // --- Materials Section ---
    public ObservableCollection<RCS.Piping.Core.Models.MaterialItem> MasterCatalog { get; } = new();
    public ObservableCollection<RCS.Piping.Core.Models.MaterialItem> ProjectMaterials { get; } = new();
    
    private RCS.Piping.Core.Models.MaterialItem? _selectedCatalogItem;
    public RCS.Piping.Core.Models.MaterialItem? SelectedCatalogItem { get => _selectedCatalogItem; set => SetField(ref _selectedCatalogItem, value); }

    public System.Windows.Input.ICommand ImportCatalogCommand { get; }
    public System.Windows.Input.ICommand AddMaterialToProjectCommand { get; }
    public System.Windows.Input.ICommand ExportMaterialsCommand { get; }
    
    private void ImportCatalog()
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
                MasterCatalog.Clear();
                
                // Save to DB
                using (var db = new RCS.Data.AppDbContext())
                {
                    db.Materials.RemoveRange(db.Materials);
                    db.SaveChanges(); // Clear first
                    
                    var entities = new List<RCS.Data.Entities.MaterialEntity>();

                    // Expecting Header: PartKey,Discipline,FeatureType,Size,Material,Manufacturer,ManufacturerPartNo,YearManufactured,Notes
                    bool first = true;
                    foreach(var line_raw in lines)
                    {
                        var line = line_raw;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        
                        // Parse CSV line (simple split, assumes no commas in values or quotes)
                        var p = line.Split(',');
                        
                        // Header Check
                        if (first) 
                        {
                            if (line.ToLower().Contains("partkey")) { first = false; continue; }
                            first = false;
                        }

                        if (p.Length < 1) continue;
                        
                        var item = new RCS.Piping.Core.Models.MaterialItem
                        {
                            PartKey = p.Length > 0 ? p[0].Trim() : "",
                            Discipline = p.Length > 1 ? p[1].Trim() : "",
                            FeatureType = p.Length > 2 ? p[2].Trim() : "",
                            Size = p.Length > 3 ? p[3].Trim() : "",
                            Material = p.Length > 4 ? p[4].Trim() : "",
                            Quantity = p.Length > 5 && int.TryParse(p[5], out int q) ? q : 1, 
                            Manufacturer = p.Length > 6 ? p[6].Trim() : "",
                            Model = p.Length > 7 ? p[7].Trim() : "",
                            Year = p.Length > 8 ? p[8].Trim() : "",
                            Notes = p.Length > 12 ? p[12].Trim() : ""
                        };
                        
                        MasterCatalog.Add(item);
                        
                        entities.Add(new RCS.Data.Entities.MaterialEntity
                        {
                            PartKey = item.PartKey, Discipline = item.Discipline, FeatureType = item.FeatureType,
                            Size = item.Size, Material = item.Material,
                            Manufacturer = item.Manufacturer, Model = item.Model, Year = item.Year, Notes = item.Notes
                        });
                    }
                    
                    db.Materials.AddRange(entities);
                    db.SaveChanges();
                }

                PopulateDropdowns();
                CommandLog.Add($"Imported {MasterCatalog.Count} materials from {dialog.FileName} to Database");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error importing catalog: {ex.Message}");
            }
        }
    }

    private void AddMaterialToProject()
    {
        if (SelectedCatalogItem == null) return;
        
        var existing = ProjectMaterials.FirstOrDefault(m => m.PartKey == SelectedCatalogItem.PartKey && m.Manufacturer == SelectedCatalogItem.Manufacturer);
        if (existing != null)
        {
            existing.Quantity += 1;
            // Hack to refresh/notify
             ProjectMaterials.Remove(existing);
             ProjectMaterials.Add(existing);
        }
        else
        {
            var newItem = new RCS.Piping.Core.Models.MaterialItem();
            newItem.CopyFrom(SelectedCatalogItem);
            newItem.Quantity = 1;
            ProjectMaterials.Add(newItem);
        }
        CommandLog.Add($"Added {SelectedCatalogItem.DisplayName} to Project.");
    }

    private void ExportMaterials()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "ProjectSchedule.csv"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var sw = new System.IO.StreamWriter(dialog.FileName);
                sw.WriteLine("PartKey,Discipline,FeatureType,Size,Material,Quantity,Manufacturer,ManufacturerPartNo,YearManufactured,Confidence,Source,Warning,Notes");
                
                foreach(var i in ProjectMaterials)
                {
                    // Escape quotes
                    static string C(string s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";
                    
                    var line = string.Join(",", new[]
                    {
                        C(i.PartKey),
                        C(i.Discipline),
                        C(i.FeatureType),
                        C(i.Size),
                        C(i.Material),
                        i.Quantity.ToString(),
                        C(i.Manufacturer),
                        C(i.Model),
                        C(i.Year),
                        C("Manual"), // Confidence
                        C("Manual"), // Source
                        C(""),       // Warning
                        C(i.Notes)
                    });
                    sw.WriteLine(line);
                }
                CommandLog.Add($"Exported Schedule to {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting schedule: {ex.Message}");
            }
        }
    }

    // --- Piping Script ---
    private string _pipingScriptText = "";
    public string PipingScriptText { get => _pipingScriptText; set => SetField(ref _pipingScriptText, value); }
    public System.Windows.Input.ICommand ProcessPipingScriptCommand { get; }
    public System.Windows.Input.ICommand ImportPipingScriptCommand { get; }
    public System.Windows.Input.ICommand ExportPipingScriptCommand { get; }

    private void ImportPipingScript()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                PipingScriptText = System.IO.File.ReadAllText(dialog.FileName);
                CommandLog.Add($"Loaded piping script: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error reading file: {ex.Message}");
            }
        }
    }

    private void ExportPipingScript()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "PipingScript.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                System.IO.File.WriteAllText(dialog.FileName, PipingScriptText);
                CommandLog.Add($"Piping script exported to: {dialog.FileName}");
            }
            catch (Exception)
            {
            }
        }
    }

    public System.Windows.Input.ICommand ExportBomCommand { get; }

    private void ExportBom()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "Project_BOM.csv"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("Type,Discipline,Part/Material,Quantity,Length/Count");

                // Calculate Pipes
                var pipeGroups = PipeRuns.GroupBy(r => new { r.Type, r.Material, r.Diameter });
                foreach(var pg in pipeGroups)
                {
                    double totalLen = 0;
                    foreach(var run in pg)
                    {
                        var p1 = _context.GetPoint(run.FromPointId);
                        var p2 = _context.GetPoint(run.ToPointId);
                        if (p1 != null && p2 != null)
                        {
                            double dx = p2.Easting - p1.Easting;
                            double dy = p2.Northing - p1.Northing;
                            totalLen += Math.Sqrt(dx * dx + dy * dy);
                        }
                    }
                    writer.WriteLine($"Pipe,{pg.Key.Type},{pg.Key.Material} Dia:{pg.Key.Diameter},LF,{totalLen:F2}");
                }

                // Calculate Structures
                var structGroups = Structures.GroupBy(s => new { s.Type });
                foreach(var sg in structGroups)
                {
                    writer.WriteLine($"Structure,Mixed,{sg.Key.Type},EA,{sg.Count()}");
                }

                CommandLog.Add($"BOM exported to: {dialog.FileName}");
                _context.Log($"[AUDIT] BOM Exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting BOM: {ex.Message}");
            }
        }
    }

    private async void ProcessPipingScript()
    {
        if (string.IsNullOrWhiteSpace(PipingScriptText)) { CommandLog.Add("Script is empty."); return; }

        _context.Log("--- Processing Unified Cogo Context ---");
        var lines = PipingScriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                continue;
            await _engine.ExecuteAsync(trimmed, _context);
        }

        _context.Log("--- Compiling Piping Script ---");
        
        var compiler = new RCS.Piping.Core.Scripting.PipeScriptCompiler();
        
        var validMaterials = new HashSet<string>(MasterCatalog.Select(m => m.Material), StringComparer.OrdinalIgnoreCase);
        // Sometimes codes are in CogoCodes, or we use FeatureType from MasterCatalog. 
        // We will pass the standard feature types and local codes to allow broad combinations. 
        var validCodes = new HashSet<string>(CogoCodes.Select(c => c.LocalCode), StringComparer.OrdinalIgnoreCase);
        foreach (var mat in MasterCatalog)
        {
            if (!string.IsNullOrWhiteSpace(mat.FeatureType)) validCodes.Add(mat.FeatureType);
        }

        // Include valid Utility Disciplines explicitly mapped from PRUN defaults
        validCodes.Add("W");
        validCodes.Add("WW");
        validCodes.Add("S");
        validCodes.Add("R");

        var result = compiler.Compile(PipingScriptText, (id) => _context.GetPoint(id), validMaterials, validCodes);

        // Clear existing states so double-execution actually replaces the networks instead of incrementally stacking items
        _pipeNetwork.Clear();
        PipeRuns.Clear();
        Structures.Clear();

        // Process Diagnostics
        foreach (var diag in result.Diagnostics)
        {
            _context.Log(diag.ToString());
        }

        if (result.Diagnostics.Any(d => d.Severity == "ERROR"))
        {
            _context.Log("Compilation failed with errors. No changes made.");
            return;
        }

        // Apply Results
        int runsAdded = 0;
        int structsAdded = 0;

        foreach (var run in result.Runs)
        {
            _pipeNetwork.AddRun(run);
            PipeRuns.Add(run);
            runsAdded++;
        }

        foreach (var str in result.Structures)
        {
            // Only add if not already in UI collection (check by PointId)
            if (!Structures.Any(s => s.PointId == str.PointId))
            {
                _pipeNetwork.AddStructure(str);
                Structures.Add(str);
                structsAdded++;
            }
        }
        
        _context.Log($"Compilation Success: Added {runsAdded} runs and {structsAdded} structures.");
        _context.Log("--- Parsing Complete ---");
        
        RefreshData();
        // Auto-Sync disabled per user request. Use manual sync button.
        // _ = SyncToAssetsAsync(result);
    }

    public System.Windows.Input.ICommand SyncToAssetsCommand { get; }

    private void SyncAssets()
    {
        // Re-use existing logic, but from current state
        // We need to re-compile or just iterate existing in-memory structures?
        // Existing logic takes ScriptCompileResult. We can probably just iterate PipeRuns and Structures in memory.
        
        // Let's create a dummy result from current state
        var result = new RCS.Piping.Core.Scripting.ScriptCompileResult();
        result.Runs.AddRange(PipeRuns);
        result.Structures.AddRange(Structures);
        
        _ = SyncToAssetsAsync(result);
    }

    private bool HasScriptKey(RCS.Data.Entities.InstalledAsset asset, string key) 
    {
        return asset.Notes?.Contains($"[ScriptID:{key}]") == true;
    }

    private string AddScriptKey(string? notes, string key)
    {
        string tag = $"[ScriptID:{key}]";
        if (notes?.Contains(tag) == true) return notes;
        return (notes + " " + tag).Trim();
    }

    private async Task SyncToAssetsAsync(RCS.Piping.Core.Scripting.ScriptCompileResult result)
    {
        _context.Log("--- Syncing to Installed Assets ---");
        int count = 0;
        int updated = 0;

        foreach (var run in result.Runs)
        {
            var pStart = _context.GetPoint(run.FromPointId);
            var pEnd = _context.GetPoint(run.ToPointId);
            double n1 = pStart?.Northing ?? 0;
            double e1 = pStart?.Easting ?? 0;
            double n2 = pEnd?.Northing ?? 0;
            double e2 = pEnd?.Easting ?? 0;

            string type = (run.Type ?? "").ToUpper();
            string key = $"Run-{run.Id}"; 
            
            // Checks
            RCS.Data.Entities.InstalledAsset? existing = null;
            RCS.Data.Entities.InstalledAsset? assetToSave = null;

            if (type == "W") 
            {
                 var specific = InstalledAssets.WaterPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.WaterPipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material;
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);
                 
                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "WW")
            {
                 var specific = InstalledAssets.WWGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.WWGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);
                 
                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "R")
            {
                 var specific = InstalledAssets.ReclaimedPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.ReclaimedPipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);

                 assetToSave = item;
                 existing = specific;
            }

            if (assetToSave != null)
            {
                assetToSave.UpdatedUtc = DateTime.UtcNow;
                if (existing == null)
                {
                    assetToSave.CreatedUtc = DateTime.UtcNow;
                    await InstalledAssets.AddItemAsync(assetToSave);
                    count++;
                }
                else
                {
                    await InstalledAssets.SaveItemAsync(assetToSave);
                    updated++;
                }
            }
        }

        foreach (var s in result.Structures)
        {
            var p = _context.GetPoint(s.PointId);
            double n = p?.Northing ?? 0; double e = p?.Easting ?? 0; double z = p?.Elevation ?? 0;

            string t = (s.Type ?? "").ToUpper();
            string key = $"Pt-{s.PointId}";

            RCS.Data.Entities.InstalledAsset? existing = null;
            RCS.Data.Entities.InstalledAsset? assetToSave = null;
            
            // Helper to process common logic (unfortunately types are different)
            // We have to iterate types manually or use reflection (too risky here)
            
            if (t.StartsWith("JEAW") && !t.StartsWith("JEAWW")) 
            {
                if (t.EndsWith("V")) 
                {
                     var specific = InstalledAssets.WaterValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
                else if (t.EndsWith("H")) 
                {
                     var specific = InstalledAssets.WaterHydrants.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterHydrant();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
                else 
                {
                     var specific = InstalledAssets.WaterFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
            }
            else if (t.Contains("JEAWW") || t.Contains("MH") || t.Contains("MANHOLE")) 
            {
                if (t.Contains("MH") || t.Contains("MANHOLE")) 
                {
                     var specific = InstalledAssets.Manholes.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.Manhole();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
                else if (t.EndsWith("V")) 
                {
                     var specific = InstalledAssets.WWValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WWValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
                else 
                {
                     var specific = InstalledAssets.WWFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WWFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
            }
            else if (t.StartsWith("JEAR")) 
            {
                 if (t.EndsWith("V")) 
                 {
                     var specific = InstalledAssets.ReclaimedValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
                 else if (t.EndsWith("H")) 
                 {
                     var specific = InstalledAssets.ReclaimedHydrants.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedHydrant();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.ReclaimedFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
            }

            if (assetToSave != null)
            {
                assetToSave.UpdatedUtc = DateTime.UtcNow;
                if (existing == null)
                {
                     assetToSave.CreatedUtc = DateTime.UtcNow;
                     await InstalledAssets.AddItemAsync(assetToSave);
                     count++;
                }
                else
                {
                    await InstalledAssets.SaveItemAsync(assetToSave);
                    updated++;
                }
            }
        }

        _context.Log($"[AUDIT] Synced to Installed Assets: {count} Added, {updated} Updated.");
    }

    // --- Dropdown Sources ---
    public ObservableCollection<string> AvailableDisciplines { get; } = new();
    public ObservableCollection<string> AvailableFeatureTypes { get; } = new();
    public ObservableCollection<string> AvailableSizes { get; } = new();
    public ObservableCollection<string> AvailableMaterials { get; } = new();

    private string _filterDiscipline = "";
    public string FilterDiscipline { 
        get => _filterDiscipline; 
        set { SetField(ref _filterDiscipline, value); FilterCatalog(); } 
    }

    private string _filterFeatureType = "";
    public string FilterFeatureType { 
        get => _filterFeatureType; 
        set { SetField(ref _filterFeatureType, value); FilterCatalog(); } 
    }

    private string _filterSize = "";
    public string FilterSize { 
        get => _filterSize; 
        set { SetField(ref _filterSize, value); FilterCatalog(); } 
    }

    private string _filterMaterial = "";
    public string FilterMaterial { 
        get => _filterMaterial; 
        set { SetField(ref _filterMaterial, value); FilterCatalog(); } 
    }
    
    // We need a separate collection for the 'View' if we are filtering, 
    // OR we use a CollectionViewSource. ViewModel filtering is easier to control.
    public ObservableCollection<RCS.Piping.Core.Models.MaterialItem> FilteredCatalog { get; } = new();

    private void PopulateDropdowns()
    {
        AvailableDisciplines.Clear();
        AvailableDisciplines.Add(""); // All
        foreach (var x in MasterCatalog.Select(x => x.Discipline).Distinct().OrderBy(x => x)) AvailableDisciplines.Add(x);

        AvailableFeatureTypes.Clear();
        AvailableFeatureTypes.Add("");
        foreach (var x in MasterCatalog.Select(x => x.FeatureType).Distinct().OrderBy(x => x)) AvailableFeatureTypes.Add(x);

        AvailableSizes.Clear();
        AvailableSizes.Add("");
        foreach (var x in MasterCatalog.Select(x => x.Size).Distinct().OrderBy(x => x)) AvailableSizes.Add(x);

        AvailableMaterials.Clear();
        AvailableMaterials.Add("");
        foreach (var x in MasterCatalog.Select(x => x.Material).Distinct().OrderBy(x => x)) AvailableMaterials.Add(x);
        
        FilterCatalog();
    }
    
    private void FilterCatalog()
    {
        var query = MasterCatalog.AsEnumerable();
        
        if (!string.IsNullOrEmpty(FilterDiscipline)) query = query.Where(x => x.Discipline == FilterDiscipline);
        if (!string.IsNullOrEmpty(FilterFeatureType)) query = query.Where(x => x.FeatureType == FilterFeatureType);
        if (!string.IsNullOrEmpty(FilterSize)) query = query.Where(x => x.Size == FilterSize);
        if (!string.IsNullOrEmpty(FilterMaterial)) query = query.Where(x => x.Material == FilterMaterial);
        
        FilteredCatalog.Clear();
        foreach(var item in query) FilteredCatalog.Add(item);
    }

    private void ValidateNetwork()
    {
        CommandLog.Add("--- Validating Pipe Network ---");
        
        List<string>? validTypes = null;
        if (CogoCodes.Count > 0)
        {
            validTypes = CogoCodes.Select(c => c.LocalCode)
                          .Concat(CogoCodes.Select(c => c.SystemCode))
                          .Where(s => !string.IsNullOrEmpty(s))
                          .Distinct()
                          .ToList();
            CommandLog.Add($"Validating against {CogoCodes.Count} imported codes.");
        }

        // Pass validTypes for both Structures and Pipes (assuming codes cover both)
        var issues = _pipelineRunner.ValidateNetwork(validStructureTypes: validTypes, validPipeTypes: validTypes);
        
        if (issues.Count == 0)
        {
            CommandLog.Add("Network is Valid.");
        }
        else
        {
            foreach(var issue in issues)
            {
                CommandLog.Add($"[ISSUE] {issue}");
            }
        }
        CommandLog.Add("-------------------------------");
    }

    private void ExecuteUtilConvert()
    {
        if (double.TryParse(UtilDecInput, out double d))
        {
            UtilDmsOutput = DegreeToDmsString(d);
        }
        else
        {
            UtilDmsOutput = "Invalid Input";
        }
    }

    private void ExecuteUtilSupplement()
    {
        if (double.TryParse(UtilSuppInput, out double d))
        {
            // Supplement = 180 - Angle.
            double supp = 180.0 - d;
            // Normalize? Usually 0-180 or 0-360.
            // If input is > 180, technically supplement implies geometrical construct, usually 180-x. 
            // Result can be negative if x > 180. Let's keep it raw.
            UtilSuppOutput = DegreeToDmsString(supp);
        }
        else
        {
            UtilSuppOutput = "Invalid Input";
        }
    }

    private void ClearCurveSolver()
    {
        // Clear Curve Inputs/Outputs
        CurveRadius = "";
        CurveTangent = "";
        CurveChord = "";
        CurveArc = "";
        CurveDelta = "";
        CurveDeltaDms = "";

        // Clear Utility Inputs/Outputs
        UtilDecInput = "";
        UtilDmsOutput = "";
        UtilSuppInput = "";
        UtilSuppOutput = "";
        
        CommandLog.Add("Curve Solver Reset.");
    }

    private string DegreeToDmsString(double decimalDegrees)
    {
        // Handle Negative
        bool isNeg = decimalDegrees < 0;
        decimalDegrees = Math.Abs(decimalDegrees);
        
        int d = (int)decimalDegrees;
        double rem = (decimalDegrees - d) * 60.0;
        int m = (int)rem;
        double s = (rem - m) * 60.0;
        
        // F1 gives one decimal place for seconds, e.g. 12.5"
        return $"{(isNeg ? "-" : "")}{d}° {m:00}' {s:00.0}\"";
    }

    private void SolveCurve()
    {
        // Collect inputs
        double? r = double.TryParse(CurveRadius, out double dr) ? dr : null;
        double? t = double.TryParse(CurveTangent, out double dt) ? dt : null;
        double? c = double.TryParse(CurveChord, out double dc) ? dc : null;
        double? l = double.TryParse(CurveArc, out double dl) ? dl : null;
        double? d = double.TryParse(CurveDelta, out double dd) ? dd : null; // Degrees

        // Need exactly 2 inputs
        int count = (r.HasValue ? 1 : 0) + (t.HasValue ? 1 : 0) + (c.HasValue ? 1 : 0) + (l.HasValue ? 1 : 0) + (d.HasValue ? 1 : 0);
        
        if (count != 2)
        {
            CommandLog.Add("Error: Please provide exactly two curve parameters.");
            return;
        }

        double deltaRad = 0;
        double R = 0;
        bool solved = false;

        // Convert Delta to Radians if provided
        if (d.HasValue) deltaRad = d.Value * (Math.PI / 180.0);

        try
        {
            // Case 1: R & Delta
            if (r.HasValue && d.HasValue)
            {
                R = r.Value;
                solved = true;
            }
            // Case 2: R & T
            else if (r.HasValue && t.HasValue)
            {
                R = r.Value;
                deltaRad = 2 * Math.Atan(t.Value / R);
                solved = true;
            }
            // Case 3: R & C
            else if (r.HasValue && c.HasValue)
            {
                R = r.Value;
                // sin(delta/2) = C/2R
                // Domain check: C <= 2R
                if (c.Value > 2 * R) throw new Exception("Chord cannot be larger than Diameter.");
                deltaRad = 2 * Math.Asin(c.Value / (2 * R));
                solved = true;
            }
            // Case 4: R & Arc
            else if (r.HasValue && l.HasValue)
            {
                R = r.Value;
                deltaRad = l.Value / R;
                solved = true;
            }
            // Case 5: T & Delta
            else if (t.HasValue && d.HasValue)
            {
                R = t.Value / Math.Tan(deltaRad / 2);
                solved = true;
            }
            // Case 6: C & Delta
            else if (c.HasValue && d.HasValue)
            {
                R = c.Value / (2 * Math.Sin(deltaRad / 2));
                solved = true;
            }
            // Case 7: Arc & Delta
            else if (l.HasValue && d.HasValue)
            {
                R = l.Value / deltaRad;
                solved = true;
            }
            // Case 8: T & C
            else if (t.HasValue && c.HasValue)
            {
                // cos(delta/2) = C/2T
                if (c.Value >= 2 * t.Value) throw new Exception("Chord must be less than 2*Tangent (for simple curve < 180).");
                 deltaRad = 2 * Math.Acos(c.Value / (2 * t.Value));
                 R = t.Value / Math.Tan(deltaRad / 2);
                 solved = true;
            }
            // Case 9: Arc & T (Transcendental)
            else if (l.HasValue && t.HasValue)
            {
                 // Iterate to find Delta
                 // T = R tan(d/2), L = R d => R = L/d
                 // T = (L/d) * tan(d/2) -> T/L = tan(d/2)/d
                 // f(d) = tan(d/2)/d - T/L = 0
                 double expectedRatio = t.Value / l.Value;
                 double estDelta = 2 * Math.Atan(expectedRatio); // Approximation? tan(x) ~ x for small x. tan(d/2)/d ~ (d/2)/d = 0.5. T/L ~ 0.5?
                 // Wait for small angles T ~ L/2.
                 // Actually for very small angles, T ~ L/2. 
                 // Simple Newton Method
                 deltaRad = SolveDeltaFromTangentArc(t.Value, l.Value);
                 R = l.Value / deltaRad;
                 solved = true;
            }
            // Case 10: Arc & C (Transcendental)
            else if (l.HasValue && c.HasValue)
            {
                 // C = 2R sin(d/2), L = R d => R = L/d
                 // C = 2(L/d) sin(d/2)
                 // C/L = sin(d/2) / (d/2) = sinc(d/2)
                 deltaRad = SolveDeltaFromChordArc(c.Value, l.Value);
                 R = l.Value / deltaRad;
                 solved = true;
            }

            if (solved)
            {
                double finalDeltaDeg = deltaRad * (180.0 / Math.PI);
                double finalT = R * Math.Tan(deltaRad / 2);
                double finalC = 2 * R * Math.Sin(deltaRad / 2);
                double finalL = R * deltaRad;

                // Update Fields (Check for NaN)
                CurveRadius = R.ToString("F3");
                CurveTangent = finalT.ToString("F3");
                CurveChord = finalC.ToString("F3");
                CurveArc = finalL.ToString("F3");
                CurveDelta = finalDeltaDeg.ToString("F6"); // High precision dec
                CurveDeltaDms = DegreeToDmsString(finalDeltaDeg); // DMS
                
                CommandLog.Add($"Curve Solved: R={CurveRadius}, T={CurveTangent}, L={CurveArc}, C={CurveChord}, D={CurveDelta} ({CurveDeltaDms})");
            }
        }
        catch (Exception ex)
        {
            CommandLog.Add($"Curve Solver Error: {ex.Message}");
        }
    }

    private double SolveDeltaFromTangentArc(double T, double L)
    {
        // f(x) = tan(x/2) - (T/L)x = 0
        // Find x (delta)
        // Derivative f'(x) = 0.5 * sec^2(x/2) - T/L
        
        double targetRatio = T / L;
        double x = 2.0 * Math.Atan(targetRatio); // Initial guess
        
        for(int i=0; i<20; i++)
        {
            double fx = Math.Tan(x/2) - targetRatio * x;
            double dfx = 0.5 * Math.Pow(1/Math.Cos(x/2), 2) - targetRatio;
            
            double xNew = x - fx/dfx;
            if (Math.Abs(xNew - x) < 1e-6) return xNew;
            x = xNew;
        }
        return x;
    }

    private double SolveDeltaFromChordArc(double C, double L)
    {
        // f(x) = 2 sin(x/2) - (C/L)x = 0
        // Derivative f'(x) = cos(x/2) - C/L
        
        double targetRatio = C / L;
        // sinc(x/2) = targetRatio.
        // For small x, sinc(x) ~ 1 - x^2/6.
        // 1 - (x/2)^2/6 = ratio => (x/2)^2 = 6(1-ratio) => x/2 = sqrt(6(1-ratio)) => x = 2*sqrt...
        
        double x = Math.Sqrt(24 * (1 - targetRatio)); 
        if (double.IsNaN(x)) x = 0.1;

        for(int i=0; i<20; i++)
        {
            double fx = 2 * Math.Sin(x/2) - targetRatio * x;
            double dfx = Math.Cos(x/2) - targetRatio;
            
            double xNew = x - fx/dfx;
            if (Math.Abs(xNew - x) < 1e-6) return xNew;
            x = xNew;
        }
        return x;
    }


    
    private void ExportScript()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "Script.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                System.IO.File.WriteAllText(dialog.FileName, BatchScriptContent);
                CommandLog.Add($"Script exported to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting script: {ex.Message}");
            }
        }
    }

    private void ExportPointsTxt()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "Points.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                var sb = new System.Text.StringBuilder();
                // Header (Optional, user didn't ask but good practice, user specified format: Number,N,E,Z,Desc)
                // sb.AppendLine("Number,Northing,Easting,Elevation,Description");
                
                foreach (var p in _context.GetAllPoints())
                {
                    sb.AppendLine($"{p.Id},{p.Point.Northing:F4},{p.Point.Easting:F4},{p.Point.Elevation:F4},{p.Description}");
                }
                
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString());
                CommandLog.Add($"Points exported (TXT) to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting points: {ex.Message}");
            }
        }
    }

    public System.Windows.Input.ICommand ExportDxfCommand { get; }

    private void ExportDxf()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "DXF File (*.dxf)|*.dxf|All Files (*.*)|*.*",
            DefaultExt = ".dxf",
            FileName = "Project.dxf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var writer = new RCS.Cogo.Wpf.Services.ProfessionalDxfWriter();
                writer.Begin();
                
                // Export Points
                foreach (var p in _context.GetAllPoints())
                {
                    writer.AddPoint(p.Point, "POINTS");
                    writer.AddText(p.Id, p.Point.Easting + 1, p.Point.Northing + 1, 0.5, "POINT_IDS");
                    writer.AddText(p.Description, p.Point.Easting + 1, p.Point.Northing - 1, 0.4, "POINT_DESC");
                }
                
                // Export Figures (Pipes)
                foreach(var fig in Figures)
                {
                    if (fig.Points.Count >= 2)
                    {
                        for (int i=0; i < fig.Points.Count - 1; i++)
                        {
                            var p1 = fig.Points[i];
                            var p2 = fig.Points[i+1];
                            writer.AddLine(p1.X, p1.Y, p2.X, p2.Y, "FIGURES");
                        }
                    }
                }
                
                // Export Structures
                foreach(var s in StructureGraphics)
                {
                    // Map Symbol Type to Block
                    string block = "MANHOLE";
                    string t = (s.Type ?? "").ToUpper();
                    
                    if (t.Contains("VALVE") || t.EndsWith("V")) block = "VALVE";
                    else if (t.Contains("HYDRANT") || t.EndsWith("H")) block = "HYDRANT";
                    else if (t.Contains("METER")) block = "METER"; 
                    else if (t.Contains("FITTING")) block = "FITTING";
                    
                    // Use Insert Block
                    writer.InsertBlock(block, s.Easting, s.Northing, 1.0, "STRUCTURES");
                }

                writer.End();
                writer.Save(dialog.FileName);
                _context.Log($"[AUDIT] Exported DXF to {dialog.FileName}");
                System.Windows.MessageBox.Show("DXF Export Successful.");
            }
            catch (Exception ex)
            {
                 _context.Log($"[AUDIT] Error exporting DXF: {ex.Message}");
                 System.Windows.MessageBox.Show($"Error exporting DXF: {ex.Message}");
            }
        }
    }

    private void ExportPointsXml()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "LandXML File (*.xml)|*.xml|All Files (*.*)|*.*",
            DefaultExt = ".xml",
            FileName = "Points.xml"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\"?>");
                sb.AppendLine("<LandXML xmlns=\"http://www.landxml.org/schema/LandXML-1.2\" version=\"1.2\" date=\"" + DateTime.Now.ToString("yyyy-MM-dd") + "\" time=\"" + DateTime.Now.ToString("HH:mm:ss") + "\">");
                sb.AppendLine("  <Units>");
                sb.AppendLine("    <Metric areaUnit=\"squareMeter\" linearUnit=\"meter\" volumeUnit=\"cubicMeter\" temperatureUnit=\"celsius\" pressureUnit=\"mmHG\"/>");
                sb.AppendLine("  </Units>");
                sb.AppendLine("  <CgPoints>");
                
                foreach (var p in _context.GetAllPoints())
                {
                    // LandXML CgPoint format: North East Elev (typically) or Y X Z
                    // The standard is Y X Z (North East Elev)
                    sb.AppendLine($"    <CgPoint name=\"{p.Id}\" desc=\"{p.Description}\">{p.Point.Northing:F4} {p.Point.Easting:F4} {p.Point.Elevation:F4}</CgPoint>");
                }
                
                sb.AppendLine("  </CgPoints>");
                sb.AppendLine("</LandXML>");
                
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString());
                CommandLog.Add($"Points exported (LandXML) to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting points: {ex.Message}");
            }
        }
    }

    private void ImportBatchScript()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "COGO Script (*.cogo)|*.cogo|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".cogo"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                BatchScriptContent = System.IO.File.ReadAllText(dialog.FileName);
                CommandLog.Add($"Loaded script: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error reading file: {ex.Message}");
            }
        }
    }

    private async Task RunBatchScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(BatchScriptContent)) return;

        CommandLog.Add("--- Running Batch Script ---");
        
        var lines = BatchScriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                continue;
                
            CommandLog.Add($"> {trimmed}");
            await _engine.ExecuteAsync(trimmed, _context);
        }
        
        CommandLog.Add("--- Batch Complete ---");
        RefreshData();
    }

    private async Task ExecuteCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput)) return;

        string cmd = CommandInput;
        CommandInput = ""; // Clear input immediately
        
        CommandLog.Add($"> {cmd}"); // Echo command to Command Log

        await _engine.ExecuteAsync(cmd, _context);

        RefreshData();
    }

    private void RefreshData()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() => 
        {
            // Update Points
            Points.Clear();
            var validCodes = CogoCodes.Select(c => c.LocalCode).ToList();
            if (validCodes.Count == 0) 
            {
                // If no codes loaded, maybe everything is valid? Or nothing?
                // User requirement: "Highlight in red missing or incorrect matches."
                // So if no code database, everything is red.
            }

            foreach (var p in _context.GetAllPoints())
            {
                Points.Add(new PointViewModel(p.Id, p.Point, p.Description, validCodes));
            }

            // Update Figures
            Figures.Clear();
            foreach (var fig in _context.GetAllFigures())
            {
                var pts = new System.Collections.Generic.List<Point3D>();
                foreach (var id in fig.PointIds)
                {
                    var p = _context.GetPoint(id);
                    if (p != null) pts.Add(p);
                }
                
                if (pts.Count > 1)
                {
                    Figures.Add(new FigureViewModel(fig.Name, pts));
                }
            }
            
            // Temporary: Render Pipes as Grey Figures
            // Render Pipes with Colors
            foreach(var run in PipeRuns)
            {
                var pStart = _context.GetPoint(run.FromPointId);
                var pEnd = _context.GetPoint(run.ToPointId);
                if (pStart != null && pEnd != null)
                {
                    var pts = new System.Collections.Generic.List<Point3D> { pStart, pEnd };
                    
                    // Color Logic based on Service/Type
                    System.Windows.Media.Brush pipeBrush = System.Windows.Media.Brushes.Gray;
                    
                    var type = run.Type?.ToUpper() ?? "";
                if (type == "WW" || type.Contains("SAN")) pipeBrush = System.Windows.Media.Brushes.Green;
                else if (type == "S" || type.Contains("SW") || type.Contains("STORM")) pipeBrush = System.Windows.Media.Brushes.Cyan;
                else if (type == "W" || type.Contains("WATER")) pipeBrush = System.Windows.Media.Brushes.Blue;
                else if (type == "R" || type.Contains("RECLAIM")) pipeBrush = System.Windows.Media.Brushes.Purple;
                else if (type.Contains("GAS")) pipeBrush = System.Windows.Media.Brushes.Yellow;
                else if (type.Contains("PP") || type.Contains("PRESS")) pipeBrush = System.Windows.Media.Brushes.Red;
                    
                    var fig = new FigureViewModel($"Pipe-{run.Id}", pts, pipeBrush);
                    Figures.Add(fig);
                }
            }

            // Structures
            StructureGraphics.Clear();
            foreach(var s in Structures) // Note: s is PipeStructure model
            {
                var p = _context.GetPoint(s.PointId);
                if (p != null)
                {
                    StructureGraphics.Add(new StructureViewModel(s.PointId, p, s.Type));
                }
            }

            // Auto-Zoom Extents after refresh
            ZoomExtentsRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    private void NewProject()
    {
        CurrentProject = new Project();
        _context.ClearState();
        
        // Reset Piping Backend
        _pipeNetwork = new RCS.Piping.Core.Models.PipeNetwork();
        _pipelineRunner = new RCS.Piping.Core.Runner.PipelineRunner(_context, _pipeNetwork);
        
        // Reset UI Collections
        PipeRuns.Clear();
        Structures.Clear();
        StructureGraphics.Clear();
        Points.Clear();
        Figures.Clear();
        
        RefreshData();
        _context.Log("[AUDIT] Created New Project");
        
        // Force User to Enter Details
        EditProject();
    }

    private void EditProject()
    {
        var window = new RCS.Cogo.Wpf.Views.ProjectDetailsWindow(CurrentProject);
        if (window.ShowDialog() == true)
        {
            _context.Log($"[AUDIT] Updated Project Details: {CurrentProject.ProjectName}");
            // Refresh title or status bar if bound?
            // For now, logging confirms update.
        }
    }

    public System.Windows.Input.ICommand OpenReportSettingsCommand { get; }

    private void OpenReportSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.ReportSettingsWindow(CurrentProject.ReportConfig);
        if (window.ShowDialog() == true)
        {
            _context.Log($"[AUDIT] Updated Report Settings for {CurrentProject.ProjectName}");
        }
    }

    private void SaveProject()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "RCS Project (*.db)|*.db|All Files (*.*)|*.*",
            FileName = $"{CurrentProject.ProjectName.Replace(" ", "_")}.db"
        };

        if (dialog.ShowDialog() == true)
        {
            try 
            {
                // Sync Data to Project Model
                CurrentProject.Points = _context.GetAllPoints().Select(p => new PointEntry 
                {
                    Id = p.Id,
                    Northing = p.Point.Northing,
                    Easting = p.Point.Easting,
                    Elevation = p.Point.Elevation,
                    Description = p.Description
                }).ToList();

                CurrentProject.PipeRuns = _pipeNetwork.Runs.Values.ToList();
                CurrentProject.Structures = _pipeNetwork.Structures.Values.ToList();
                CurrentProject.Materials = ProjectMaterials.ToList();

                var service = new LiteDbProjectService();
                service.SaveProject(dialog.FileName, CurrentProject);
                
                _context.Log($"[AUDIT] Saved project to {dialog.FileName} (LiteDB)");
            }
            catch(Exception ex)
            {
                _context.Log($"[AUDIT] Error Saving Project: {ex.Message}");
                System.Windows.MessageBox.Show($"Error Saving Project: {ex.Message}");
            }
        }
    }

    private string _currentDbPath = string.Empty; // Store path for maintenance operations

    private void OpenProject()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "RCS Project (*.db)|*.db|All Files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                NewProject(); // Reset State first
                
                var service = new LiteDbProjectService();
                CurrentProject = service.LoadProject(dialog.FileName);
                _currentDbPath = dialog.FileName;

                // Repopulate Context
                if (CurrentProject.Points != null)
                {
                    foreach(var p in CurrentProject.Points)
                    {
                        _context.AddPoint(p.Id, new Point3D(p.Northing, p.Easting, p.Elevation), p.Description);
                    }
                }
                
                // Repopulate Piping
                PipeRuns.Clear();
                Structures.Clear();

                if (CurrentProject.PipeRuns != null)
                {
                    foreach(var run in CurrentProject.PipeRuns) 
                    {
                        _pipeNetwork.AddRun(run);
                        PipeRuns.Add(run);
                    }
                }
                
                if (CurrentProject.Structures != null)
                {
                    foreach(var s in CurrentProject.Structures) 
                    {
                        _pipeNetwork.AddStructure(s);
                        Structures.Add(s);
                    }
                }
                
                if (CurrentProject.Materials != null)
                {
                    ProjectMaterials.Clear();
                    foreach(var m in CurrentProject.Materials) ProjectMaterials.Add(m);
                }
                
                _context.Log($"[AUDIT] Opened Project: {dialog.FileName}");
                RefreshData();
            }
            catch (Exception ex)
            {
                 _context.Log($"[AUDIT] Error Opening Project: {ex.Message}");
                 System.Windows.MessageBox.Show($"Error Opening Project: {ex.Message}");
            }
        }
    }

    // --- Database Maintenance ---
    public System.Windows.Input.ICommand CompactDbCommand { get; }
    public System.Windows.Input.ICommand VerifyDbCommand { get; }
    public System.Windows.Input.ICommand RepairDbCommand { get; }
    public System.Windows.Input.ICommand ExportDbCsvCommand { get; }

    private void CompactDatabase()
    {
        if (string.IsNullOrEmpty(_currentDbPath) || !File.Exists(_currentDbPath))
        {
            System.Windows.MessageBox.Show("Please save or open a project first to define the DB path.");
            return;
        }
        
        try 
        {
            var service = new LiteDbProjectService();
            // Assuming we must close connection? LiteDB handles rebuild on open connection usually or exclusive.
            // Just attempting call.
            bool result = service.CompactDatabase(_currentDbPath);
            _context.Log(result ? "[AUDIT] Database Compacted Successfully." : "[AUDIT] Database Compaction Failed.");
            if(result) System.Windows.MessageBox.Show("Database Compacted Successfully.");
        }
        catch(Exception ex)
        {
            _context.Log($"[AUDIT] Error Compacting DB: {ex.Message}");
        }
    }

    private void VerifyDatabase()
    {
        if (string.IsNullOrEmpty(_currentDbPath) || !File.Exists(_currentDbPath))
        {
            System.Windows.MessageBox.Show("Please save or open a project first.");
            return;
        }

        var service = new LiteDbProjectService();
        bool result = service.VerifyDatabase(_currentDbPath);
        string msg = result ? "Database Verification Passed (Integrity OK)." : "Database Verification FAILED.";
        _context.Log($"[AUDIT] {msg}");
        System.Windows.MessageBox.Show(msg);
    }
    
    private void RepairDatabase()
    {
        if (string.IsNullOrEmpty(_currentDbPath) || !File.Exists(_currentDbPath))
        {
            System.Windows.MessageBox.Show("Please save or open a project first.");
            return;
        }
        
        try
        {
            var service = new LiteDbProjectService();
            bool result = service.RepairDatabase(_currentDbPath);
             _context.Log(result ? "[AUDIT] Database Repair/Rebuild Successful." : "[AUDIT] Database Repair Failed.");
             if(result) System.Windows.MessageBox.Show("Database Repaired Successfully.");
        }
        catch (Exception ex)
        {
             _context.Log($"[AUDIT] Error Reparing DB: {ex.Message}");
        }
    }

    private void ExportDatabaseCsv()
    {
         var dialog = new Microsoft.Win32.SaveFileDialog
         {
             Filter = "CSV File (*.csv)|*.csv",
             FileName = "ProjectPoints_Export.csv"
         };

         if (dialog.ShowDialog() == true)
         {
             try
             {
                 var sb = new System.Text.StringBuilder();
                 sb.AppendLine("PointID,Northing,Easting,Elevation,Description");
                 foreach(var p in Points)
                 {
                     sb.AppendLine($"{p.Id},{p.Northing},{p.Easting},{p.Elevation},{p.Description}");
                 }
                 File.WriteAllText(dialog.FileName, sb.ToString());
                 _context.Log($"[AUDIT] Exported Database Points to {dialog.FileName}");
             }
             catch(Exception ex)
             {
                 _context.Log($"[AUDIT] Export Error: {ex.Message}");
             }
         }
    }

    public System.Windows.Input.ICommand ExportInstalledAssetsCommand { get; }

    private void ExportInstalledAssets()
    {
         var dialog = new Microsoft.Win32.SaveFileDialog
         {
             Title = "Export Installed Assets Report (Base Filename)",
             Filter = "CSV File (*.csv)|*.csv",
             FileName = "InstalledAssets_Report.csv"
         };

         if (dialog.ShowDialog() == true)
         {
             try
             {
                 InstalledAssets.ExportToFolder(dialog.FileName);
                 _context.Log($"[AUDIT] Exported Installed Assets to {System.IO.Path.GetDirectoryName(dialog.FileName)}");
             }
             catch(Exception ex)
             {
                 _context.Log($"[AUDIT] Export Error: {ex.Message}");
             }
         }
    }

    private void CloseProject()
    {
        NewProject(); // Effectively closes by resetting
        _context.Log("[AUDIT] Closed Project");
    }

    private void ImportPointsList()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = File.ReadAllLines(dialog.FileName);
                int count = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // Simple PNEZD parser: 100 5000 5000 100 Desc
                    // Supports comma, tab, or space separation
                    var parts = line.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // Need at least 3 parts for NEZ, usually 5 for PNEZD
                    if (parts.Length >= 3)
                    {
                        var id = parts[0];
                        double n, e, z = 0;
                        string desc = "";

                        // Check if first part is ID (string) or Coordinate
                        // Standard: ID N E Z D
                        if (double.TryParse(parts[1], out n) && double.TryParse(parts[2], out e))
                        {
                            if (parts.Length > 3) double.TryParse(parts[3], out z);
                            if (parts.Length > 4) desc = string.Join(" ", parts.Skip(4));
                            
                            _context.AddPoint(id, new Point3D(n, e, z), desc);
                            count++;
                        }
                    }
                }
                _context.Log($"[AUDIT] Imported {count} points from {dialog.FileName}");
                RefreshData();
            }
            catch (Exception ex)
            {
                _context.Log($"[AUDIT] Error importing points: {ex.Message}");
                System.Windows.MessageBox.Show($"Error importing points: {ex.Message}");
            }
        }
    }

}

public class FigureViewModel : ViewModelBase
{
    public string Name { get; }
    public System.Windows.Media.PointCollection Points { get; }
    public System.Windows.Media.Brush Stroke { get; }

    public FigureViewModel(string name, System.Collections.Generic.IEnumerable<Point3D> points, System.Windows.Media.Brush? stroke = null)
    {
        Name = name;
        Points = new System.Windows.Media.PointCollection();
        foreach (var p in points)
        {
            Points.Add(new System.Windows.Point(p.Easting, p.Northing));
        }
        Stroke = stroke ?? System.Windows.Media.Brushes.Yellow; // Default to Yellow
    }
}

public class StructureViewModel : ViewModelBase
{
    public string Id { get; }
    public double Northing { get; }
    public double Easting { get; }
    public string Type { get; }
    public string SymbolType { get; }
    public System.Windows.Media.Brush Fill { get; }

    public StructureViewModel(string id, Point3D p, string type)
    {
        Id = id;
        Northing = p.Northing;
        Easting = p.Easting;
        Type = type;
        
        string t = type.ToUpper();
        if (t.Contains("MANHOLE") || t.Equals("MH") || t.Contains("INLET") || t.Contains("CB")) SymbolType = "Manhole";
        else if (t.Contains("VALVE") || t.EndsWith("V") || t.EndsWith("VLV")) SymbolType = "Valve";
        else if (t.Contains("HYDRANT") || t.EndsWith("H") || t.EndsWith("HYD")) SymbolType = "Hydrant";
        else if (t.Contains("METER") || t.EndsWith("M")) SymbolType = "Meter";
        else if (t.Contains("FITTING") || t.Contains("BEND") || t.Contains("TEE")) SymbolType = "Fitting";
        else SymbolType = "Default";
        // Color Logic
        if (type.Equals("Manhole", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Magenta;
        else if (type.Equals("Valve", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Red;
        else if (type.Equals("Inlet", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Orange;
        // Utility Types
        else if (type.Contains("WW") || type.Contains("SAN")) Fill = System.Windows.Media.Brushes.Green;
        else if (type.Contains("SW") || type.Contains("STORM")) Fill = System.Windows.Media.Brushes.Cyan;
        else if (type.Contains("W") || type.Contains("WATER")) Fill = System.Windows.Media.Brushes.Blue;
        else if (type.Contains("GAS")) Fill = System.Windows.Media.Brushes.Yellow;
        else Fill = System.Windows.Media.Brushes.White; // Default 
    }
}
