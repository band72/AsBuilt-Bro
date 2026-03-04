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
using Microsoft.EntityFrameworkCore;
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
    private string _commandHint = "";
    public string CommandHint
    {
        get => _commandHint;
        set => SetField(ref _commandHint, value);
    }

    public string CommandInput
    {
        get => _commandInput;
        set 
        {
            SetField(ref _commandInput, value);
            UpdateCommandHint();
        }
    }

    private void UpdateCommandHint()
    {
        if (string.IsNullOrWhiteSpace(_commandInput))
        {
            CommandHint = "";
            return;
        }

        string cmd = _commandInput.Split(' ')[0].ToUpper();
        switch (cmd)
        {
            // Point & Traverse Commands
            case "ST": CommandHint = "ST <Pt> <Northing> <Easting> <Elev> [Desc]"; break;
            case "NE": CommandHint = "NE <Pt> <Northing> <Easting> [Desc]"; break;
            case "NEZ": CommandHint = "NEZ <Pt> <Northing> <Easting> <Elev> [Desc]"; break;
            case "PT": 
            case "PNT": CommandHint = "PNT <Pt> <Northing> <Easting> <Elev> [Desc]"; break;
            case "OC": CommandHint = "OC <Pt> [InstrumentHeight]"; break;
            case "BS": CommandHint = "BS <Pt> <Azimuth_DMS>"; break;
            case "FS": CommandHint = "FS <NewPt> <Angle_DMS> <Dist> [Desc]"; break;
            case "TRAV": CommandHint = "TRAV <NewPt> <Angle_DMS> <Dist> [Desc]"; break;

            // Geometry/Intersection Commands
            case "AZAZ": CommandHint = "AZAZ <NewPt> <Pt1> <Az1> <Pt2> <Az2> [Desc]"; break;
            case "BB": CommandHint = "BB <NewPt> <Pt1> <Brg1> <Quad1> <Pt2> <Brg2> <Quad2> [Desc]"; break;
            case "BD": CommandHint = "BD <NewPt> <Bearing_DMS> <Quad(1-4)> <Dist> [Desc]"; break;
            case "LNLN": CommandHint = "LNLN <NewPt> <Line1Start> <Line1End> <Line2Start> <Line2End> [Desc]"; break;
            case "RKRK": CommandHint = "RKRK <NewPt> <Pt1> <Radius1> <Pt2> <Radius2> [Desc]"; break;
            case "AD": CommandHint = "AD <NewPt> <AngleRight_DMS> <Dist> [Desc]"; break;
            case "DD": CommandHint = "DD <NewPt> <Deflection_DMS> <Dist> [Desc]"; break;
            case "ZD": CommandHint = "ZD <NewPt> <Zenith_DMS> <Dist> [Desc]"; break;

            // Figure & Linework Commands
            case "B": 
            case "BEG": CommandHint = "BEG <Pt> (Begins a active figure)"; break;
            case "L": CommandHint = "L <Pt> (Draws line to node)"; break;
            case "LN": CommandHint = "LN <Pt1> <Pt2> (Inverse Bearing & Distance of line)"; break;
            case "C": CommandHint = "C (Closes active figure back to Begin point)"; break;
            case "CONT": CommandHint = "CONT <Pt> (Continues active figure to node)"; break;
            case "E":
            case "END": CommandHint = "END (Ends the active figure without closing)"; break;
            case "XC": CommandHint = "XC PTS <Radius> <RadiusPt> <EndPt> (Synthesize Curve)"; break;
            case "ARCARC": CommandHint = "ARCARC <NewPt> <Pt1> <Radius1> <Pt2> <Radius2> [Desc]"; break;

            // Analytics
            case "IN":
            case "INV": CommandHint = "INV <Pt1> <Pt2> (Inverse calculation)"; break;
            case "AZ": CommandHint = "AZ <Pt1> <Pt2> (Calculates absolute Azimuth)"; break;

            // Utilities & Transformations
            case "AP": CommandHint = "AP <ON/OFF> (Toggles Auto Point Numbering)"; break;
            case "TRN": CommandHint = "TRN <SourcePt> <DestPt> <PtsToMove> (Translates points)"; break;
            case "ROT": CommandHint = "ROT <Line1> <Line2> <PtsToRotate> (Rotates points)"; break;

            default: CommandHint = ""; break;
        }
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
                OnPropertyChanged(nameof(HasActiveProject));
                _ = LoadInstalledAssetsAsync();
            }
        }
    }

    public bool HasActiveProject 
    {
        get => CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.ProjectName) && CurrentProject.ProjectName != "New Project";
    }

    private bool EnsureActiveProject()
    {
        if (!HasActiveProject)
        {
            System.Windows.MessageBox.Show("You must have an open active project to import, edit, or delete information. Please use File -> New Project or Open Project.", "Active Project Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }
        return true;
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
                OnPropertyChanged(nameof(LineMarkerScale));
                OnPropertyChanged(nameof(PointMarkerScale));
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
                OnPropertyChanged(nameof(LineMarkerScale));
                OnPropertyChanged(nameof(PointMarkerScale));
            }
        }
    }

    private bool _showViewportLegend = true;
    public bool ShowViewportLegend
    {
        get => _showViewportLegend;
        set => SetField(ref _showViewportLegend, value);
    }

    private bool _isOutputLogDescending = true;
    public bool IsOutputLogDescending
    {
        get => _isOutputLogDescending;
        set => SetField(ref _isOutputLogDescending, value);
    }

    public ObservableCollection<double> AvailableSymbolScales { get; } = new(new[] { 0.5, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0, 10.0 });
    public ObservableCollection<double> AvailablePointNumberSizes { get; } = new(new[] { 8.0, 10.0, 12.0, 14.0, 16.0, 18.0, 20.0, 24.0, 28.0, 32.0, 36.0, 48.0, 64.0 });
    public ObservableCollection<double> AvailablePointMarkerSizes { get; } = new(new[] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0, 3.0, 4.0, 5.0 });
    public ObservableCollection<double> AvailableFigureLineWidths { get; } = new(new[] { 0.5, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0 });
    public ObservableCollection<double> AvailableClosureTolerances { get; } = new(new[] { 0.2, 0.1, 0.01, 0.001 });

    public double MapCheckClosureTolerance
    {
        get => _context.MapCheckClosureTolerance;
        set
        {
            _context.MapCheckClosureTolerance = value;
            OnPropertyChanged();
        }
    }
    
    private double _pointNumberSize = 24.0;
    public double PointNumberSize
    {
        get => _pointNumberSize;
        set => SetField(ref _pointNumberSize, value);
    }
    
    private double _pointMarkerSize = 1.0;
    public double PointMarkerSize
    {
        get => _pointMarkerSize;
        set
        {
            if (SetField(ref _pointMarkerSize, value))
            {
                OnPropertyChanged(nameof(PointMarkerScale));
            }
        }
    }
    
    private double _figureLineWidth = 3.0;
    public double FigureLineWidth
    {
        get => _figureLineWidth;
        set
        {
            if (SetField(ref _figureLineWidth, value))
            {
                OnPropertyChanged(nameof(LineMarkerScale));
            }
        }
    }

    // Marker scale prevents points from becoming microscopic when zoomed out.
    // Reverted to 1.5 since symbols are now built on a 28x28 pixel base.
    public double MarkerScale => (1.0 / Math.Abs(_currentViewScale)) * 1.5 * SymbolScaleMultiplier;
    
    // Separated scale specifically for Pipeline thickness.
    public double LineMarkerScale => (1.0 / Math.Abs(_currentViewScale)) * FigureLineWidth * SymbolScaleMultiplier;

    // Scale precisely targeted to point (X and number) rendering 
    public double PointMarkerScale => (1.0 / Math.Abs(_currentViewScale)) * PointMarkerSize * SymbolScaleMultiplier;

    private bool _showPointNumbers = true;
    public bool ShowPointNumbers
    {
        get => _showPointNumbers;
        set => SetField(ref _showPointNumbers, value);
    }

    private bool _showPointMarkers = true;
    public bool ShowPointMarkers
    {
        get => _showPointMarkers;
        set { SetField(ref _showPointMarkers, value); RefreshData(false); }
    }

    public bool ShowHorizontalAlignmentLabels
    {
        get => _context.ShowAlignmentLabels;
        set 
        { 
            _context.ShowAlignmentLabels = value; 
            OnPropertyChanged(); 
            RefreshData(false); 
        }
    }
    
    private bool _showFigureLabels = true;
    public bool ShowFigureLabels
    {
        get => _showFigureLabels;
        set => SetField(ref _showFigureLabels, value);
    }

    private bool _isRunningScript;
    public bool IsRunningScript
    {
        get => _isRunningScript;
        set => SetField(ref _isRunningScript, value);
    }

    public System.Windows.Input.ICommand SubmitCommand { get; }
    public System.Windows.Input.ICommand ImportBatchCommand { get; }
    public System.Windows.Input.ICommand RunBatchCommand { get; }
    public System.Windows.Input.ICommand WalkBatchCommand { get; }

    public System.Windows.Input.ICommand ZoomInCommand { get; }
    public System.Windows.Input.ICommand ZoomOutCommand { get; }
    public System.Windows.Input.ICommand ZoomExtentsCommand { get; }
    public System.Windows.Input.ICommand ZoomWindowCommand { get; }
    public System.Windows.Input.ICommand ZoomToImportedPointCommand { get; }

    public event EventHandler? ZoomExtentsRequested;
    public event EventHandler? ZoomWindowRequested;
    public event EventHandler? ZoomInRequested;
    public event EventHandler? ZoomOutRequested;
    public event EventHandler<System.Windows.Point>? ZoomToPointRequested;

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
                    if (IsOutputLogDescending)
                        ResultLogText = log + Environment.NewLine + ResultLogText;
                    else
                        ResultLogText += log + Environment.NewLine;
                }
            });
        })
        {
             SaveHorizontalAlignmentAction = (name, desc) => 
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(async () => {
                     var ha = new RCS.Data.Entities.HorizontalAlignment { AlignmentName = name, Description = desc, ScriptContent = this.BatchScriptContent };
                     InstalledAssets.HorizontalAlignments.Add(ha);
                     await InstalledAssets.SaveItemAsync(ha);
                 });
             },
             SaveProfileAlignmentAction = (name, desc) => 
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(async () => {
                     var pa = new RCS.Data.Entities.ProfileAlignment { ProfileName = name, Description = desc, ScriptContent = this.BatchScriptContent };
                     InstalledAssets.ProfileAlignments.Add(pa);
                     await InstalledAssets.SaveItemAsync(pa);
                 });
             },
             SyncPointsAction = () => 
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                     var proj = CurrentProject;
                     if (proj == null) return;
                     
                     #pragma warning disable CS8602
                     proj.Points = _context.GetAllPoints().Select(p => new RCS.Cogo.App.Models.PointEntry 
                     {
                         Id = p.Id ?? "",
                         Northing = p.Point?.Northing ?? 0.0,
                         Easting = p.Point?.Easting ?? 0.0,
                         Elevation = p.Point?.Elevation ?? 0.0,
                         Description = p.Description ?? ""
                     }).ToList();
                     #pragma warning restore CS8602

                     if (!string.IsNullOrEmpty(_currentDbPath))
                     {
                         try
                         {
                             var service = new RCS.Cogo.App.Persistence.LiteDbProjectService();
                             service.SaveProject(_currentDbPath, proj);
                         }
                         catch (Exception ex)
                         {
                             System.Diagnostics.Debug.WriteLine($"Sync Failed: {ex.Message}");
                         }
                     }
                 });
             }
        };

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
                    CogoCodes.Add(new CogoCode(c.LocalCode, c.SystemCode, c.Description, c.Block ?? ""));
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
            
            // Load DB settings
            if (double.TryParse(RCS.Services.GlobalSettingsService.GetSetting("SymbolScaleMultiplier", "1.0"), out double scale)) SymbolScaleMultiplier = scale;
            if (bool.TryParse(RCS.Services.GlobalSettingsService.GetSetting("ShowViewportLegend", "True"), out bool leg)) ShowViewportLegend = leg;
            if (bool.TryParse(RCS.Services.GlobalSettingsService.GetSetting("IsOutputLogDescending", "True"), out bool desc)) IsOutputLogDescending = desc;
            if (double.TryParse(RCS.Services.GlobalSettingsService.GetSetting("PointNumberSize", "24.0"), out double pns)) PointNumberSize = pns;
            if (double.TryParse(RCS.Services.GlobalSettingsService.GetSetting("PointMarkerSize", "1.0"), out double pms)) PointMarkerSize = pms;
            if (double.TryParse(RCS.Services.GlobalSettingsService.GetSetting("FigureLineWidth", "3.0"), out double flw)) FigureLineWidth = flw;
            if (double.TryParse(RCS.Services.GlobalSettingsService.GetSetting("MapCheckClosureTolerance", "0.01"), out double tol)) MapCheckClosureTolerance = tol;
        }
        catch (Exception ex)
        {
             _context.Log($"[ERROR] Failed to load Master Database: {ex.Message}");
             CommandLog.Add($"[ERROR] DB Load Failed: {ex.Message}");
        }

        PopulateDropdowns();

        SubmitCommand = new RelayCommand(async _ => await ExecuteCommandAsync());
        ImportBatchCommand = new RelayCommand(_ => ImportBatchScript());
        OpenConvertImageCommand = new RelayCommand(_ => OpenConvertImage());
        RunBatchCommand = new RelayCommand(async _ => await RunBatchScriptAsync());
        WalkBatchCommand = new RelayCommand(async _ => await WalkBatchScriptAsync());

        ZoomInCommand = new RelayCommand(_ => ZoomInRequested?.Invoke(this, EventArgs.Empty));
        ZoomOutCommand = new RelayCommand(_ => ZoomOutRequested?.Invoke(this, EventArgs.Empty));
        ZoomExtentsCommand = new RelayCommand(_ => ZoomExtentsRequested?.Invoke(this, EventArgs.Empty));
        ZoomWindowCommand = new RelayCommand(_ => ZoomWindowRequested?.Invoke(this, EventArgs.Empty));
        ZoomToImportedPointCommand = new RelayCommand(obj => {
            if (obj is PointViewModel pt) {
                var zoomTarget = new System.Windows.Point(pt.Easting, pt.Northing);
                ZoomToPointRequested?.Invoke(this, zoomTarget);
            }
        });
        ExportDxfCommand = new RelayCommand(_ => ExportDxf());
        ExportBomCommand = new RelayCommand(_ => ExportBom());
        ExportEpanetCommand = new RelayCommand(_ => ExportEpanet());
        ExportScheduleCommand = new RelayCommand(_ => ExportSchedule());
        
        ExportScriptCommand = new RelayCommand(_ => ExportScript());
        AnalyzeScriptCommand = new RelayCommand(_ => AnalyzeScript());
        ExportOutputLogCommand = new RelayCommand(_ => ExportOutputLog());
        ExportPointsTxtCommand = new RelayCommand(_ => ExportPointsTxt());
        ExportPointsXmlCommand = new RelayCommand(_ => ExportPointsXml());
        // ExportDxfCommand = new RelayCommand(_ => ExportDxf()); // This line was moved up
        SyncToAssetsCommand = new RelayCommand(_ => SyncAssets());
        
        // Report Commands
        ReportWaterCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Water Report Not Implemented", "Reports"));
        ReportSewerCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Sewer Report Not Implemented", "Reports"));
        ReportGasCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Gas Report Not Implemented", "Reports"));
        ReportElectricCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Electric Report Not Implemented", "Reports"));
        ReportDrainageCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Drainage Report Not Implemented", "Reports"));
        ReportAllAssetsCsvCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("All Assets CSV Report Not Implemented", "Reports"));
        ReportAllAssetsTxtCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("All Assets TXT Report Not Implemented", "Reports"));
        ReportAllAssetsXlsCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("All Assets XLS Report Not Implemented", "Reports"));
        
        SaveHorizontalAlignmentCommand = new RelayCommand(_ => SaveHorizontalAlignmentFromMenu());
        SaveVerticalAlignmentCommand = new RelayCommand(_ => SaveVerticalAlignmentFromMenu());
        DeleteHorizontalAlignmentCommand = new RelayCommand(_ => DeleteHorizontalAlignmentFromMenu());
        DeleteVerticalAlignmentCommand = new RelayCommand(_ => DeleteVerticalAlignmentFromMenu());

        SolveCurveCommand = new RelayCommand(_ => SolveCurve());
        UtilConvertCommand = new RelayCommand(_ => ExecuteUtilConvert());
        UtilConvertDmsToDdCommand = new RelayCommand(_ => ExecuteUtilConvertDmsToDd());
        UtilSupplementCommand = new RelayCommand(_ => ExecuteUtilSupplement());
        ClearCurveSolverCommand = new RelayCommand(_ => ClearCurveSolver());
        AddBearingsCommand = new RelayCommand(_ => ExecuteBearingMath(true));
        SubtractBearingsCommand = new RelayCommand(_ => ExecuteBearingMath(false));

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
        OpenSymbolManagerCommand = new RelayCommand(_ => OpenSymbolManager());
        SearchCodesCommand = new RelayCommand(_ => SearchCodes());
        
        ImportCatalogCommand = new RelayCommand(_ => ImportCatalog());
        AddMaterialToProjectCommand = new RelayCommand(_ => AddMaterialToProject());
        ExportMaterialsCommand = new RelayCommand(_ => ExportMaterials());
        
        ProcessPipingScriptCommand = new RelayCommand(_ => ProcessPipingScript());
        ImportPipingScriptCommand = new RelayCommand(_ => ImportPipingScript());
        ExportPipingScriptCommand = new RelayCommand(_ => ExportPipingScript());
        AnalyzePipingScriptCommand = new RelayCommand(_ => AnalyzePipingScript());
        
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
        AboutCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("RCS COGO Enterprise\nVersion 2.0\n\nAdvanced Agentic Coding Demo", "About"));
        OpenSurveyCommandsCommand = new RelayCommand(_ => OpenDocument("docs\\USER_GUIDE.md"));
        OpenPipeCommandsCommand = new RelayCommand(_ => OpenDocument("docs\\PIPING_MANUAL.md"));
        OpenManualCommand = new RelayCommand(_ => OpenDocument("USER_MANUAL_AND_TESTING_GUIDE.txt"));
        
        OpenExampleCogoCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Azimuth_Script_Example.txt"));
        OpenExampleBearingCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Bearing_Script_Example.txt"));
        OpenExamplePipeCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Pipe_Script_Example.txt"));
        OpenExampleMixCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Mix_Script_Example.txt"));
        OpenExampleFiguresCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Cogo_Figures_Example.txt"));
        OpenExampleAngleCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Angle_Script_Example.txt"));
        
        OpenExampleGasCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Gas_Script_Example.txt"));
        OpenExampleElectricCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Electric_Script_Example.txt"));
        OpenExampleWaterCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Water_Script_Example.txt"));
        OpenExampleWwCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\WW_Script_Example.txt"));
        OpenExampleStormCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Storm_Script_Example.txt"));
        OpenExampleRCCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Reclaimed_Script_Example.txt"));
        OpenExampleChCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Chilled_Script_Example.txt"));

        OpenExampleCogoV2Command = new RelayCommand(_ => OpenDocument("TEST_COGO_V2.txt"));
        OpenExampleWaterV2Command = new RelayCommand(_ => OpenDocument("TEST_WATER_V2.txt"));
        OpenExampleStormV2Command = new RelayCommand(_ => OpenDocument("TEST_STORM_V2.txt"));
        OpenExampleSewerV2Command = new RelayCommand(_ => OpenDocument("TEST_SEWER_V2.txt"));
        OpenExampleWwV2Command = new RelayCommand(_ => OpenDocument("TEST_WASTE_WATER_V2.txt"));
        OpenExampleGasV2Command = new RelayCommand(_ => OpenDocument("TEST_GAS_V2.txt"));
        OpenExampleElectricV2Command = new RelayCommand(_ => OpenDocument("TEST_ELECTRIC_V2.txt"));

        TestNativeSecurityCommand = new RelayCommand(_ => ExecuteTestNativeSecurity());
        OpenLicensingAgentCommand = new RelayCommand(_ => OpenLicensingAgentWindow());
        
        InstalledAssets = new InstalledAssetsViewModel();
        InstalledAssets.LogAction = (msg) => CommandLog.Add(msg);
        OpenValidationSettingsCommand = new RelayCommand(_ => OpenValidationSettings());
        OpenGeneralSettingsCommand = new RelayCommand(_ => OpenGeneralSettings());
        OpenAlignmentWindowCommand = new RelayCommand(_ => OpenAlignmentWindow());
        OpenAlignmentSettingsCommand = new RelayCommand(_ => OpenAlignmentSettings());
        OpenPipeCharacteristicsCommand = new RelayCommand(_ => OpenPipeCharacteristics());
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
    public System.Windows.Input.ICommand OpenConvertImageCommand { get; }

    public System.Windows.Input.ICommand OpenSurveyCommandsCommand { get; }
    public System.Windows.Input.ICommand OpenPipeCommandsCommand { get; }
    public System.Windows.Input.ICommand OpenManualCommand { get; }
    public System.Windows.Input.ICommand OpenExampleCogoCommand { get; }
    public System.Windows.Input.ICommand OpenExampleBearingCommand { get; }
    public System.Windows.Input.ICommand OpenExamplePipeCommand { get; }
    public System.Windows.Input.ICommand OpenExampleMixCommand { get; }
    public System.Windows.Input.ICommand OpenExampleFiguresCommand { get; }
    public System.Windows.Input.ICommand OpenExampleAngleCommand { get; }
    
    public System.Windows.Input.ICommand OpenExampleGasCommand { get; }
    public System.Windows.Input.ICommand OpenExampleElectricCommand { get; }
    public System.Windows.Input.ICommand OpenExampleWaterCommand { get; }
    public System.Windows.Input.ICommand OpenExampleWwCommand { get; }
    public System.Windows.Input.ICommand OpenExampleStormCommand { get; }
    public System.Windows.Input.ICommand OpenExampleRCCommand { get; }
    public System.Windows.Input.ICommand OpenExampleChCommand { get; }

    public System.Windows.Input.ICommand OpenExampleCogoV2Command { get; }
    public System.Windows.Input.ICommand OpenExampleWaterV2Command { get; }
    public System.Windows.Input.ICommand OpenExampleStormV2Command { get; }
    public System.Windows.Input.ICommand OpenExampleSewerV2Command { get; }
    public System.Windows.Input.ICommand OpenExampleWwV2Command { get; }
    public System.Windows.Input.ICommand OpenExampleGasV2Command { get; }
    public System.Windows.Input.ICommand OpenExampleElectricV2Command { get; }

    // Security
    public System.Windows.Input.ICommand TestNativeSecurityCommand { get; }
    public System.Windows.Input.ICommand OpenLicensingAgentCommand { get; }

    public System.Windows.Input.ICommand OpenAlignmentWindowCommand { get; }
    public System.Windows.Input.ICommand OpenAlignmentSettingsCommand { get; }
    public System.Windows.Input.ICommand OpenPipeCharacteristicsCommand { get; }

    public System.Windows.Input.ICommand CloseCommand { get; }
    public System.Windows.Input.ICommand AboutCommand { get; }

    private void OpenConvertImage()
    {
        var window = new RCS.Cogo.Wpf.Views.ImageToCogoWindow();
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    private void OpenDocument(string relativePath)
    {
        try
        {
            var baseDir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (baseDir != null && !System.IO.File.Exists(System.IO.Path.Combine(baseDir.FullName, "RCS.Cogo.Enterprise.Modern.sln")))
            {
                baseDir = baseDir.Parent;
            }
            
            string fullPath = relativePath;
            if (baseDir != null)
            {
                fullPath = System.IO.Path.Combine(baseDir.FullName, relativePath);
            }

            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = fullPath
            };
            p.Start();
        }
        catch (Exception ex)
        {
            CommandLog.Add($"Error opening document: {ex.Message}");
            System.Windows.MessageBox.Show($"Could not open documentation file {relativePath}. It may be missing.", "Error");
        }
    }

    public System.Windows.Input.ICommand ExportScriptCommand { get; }
    public System.Windows.Input.ICommand AnalyzeScriptCommand { get; }

    private void AnalyzeScript()
    {
        if (string.IsNullOrWhiteSpace(BatchScriptContent)) return;

        if (BatchScriptContent.Contains("PIPE-ENGINE-ON", StringComparison.OrdinalIgnoreCase) || 
            BatchScriptContent.Contains("PRUN", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show("This script contains Piping Commands. Please run it through the Piping Script tab instead.", "Piping Script Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var analyzer = new RCS.Cogo.AI.AiAnalyzer();
        var results = analyzer.AnalyzeScript(BatchScriptContent);
        
        var aiWindow = new RCS.Cogo.Wpf.Views.AiAnalysisWindow(results);
        aiWindow.Owner = App.Current.MainWindow;
        aiWindow.ShowDialog();
    }
    public System.Windows.Input.ICommand ExportOutputLogCommand { get; }
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

    // DMS to DD Converter
    private string _utilDmsInput = "";
    public string UtilDmsInput { get => _utilDmsInput; set => SetField(ref _utilDmsInput, value); }

    private string _utilDdOutput = "";
    public string UtilDdOutput { get => _utilDdOutput; set => SetField(ref _utilDdOutput, value); }

    public System.Windows.Input.ICommand UtilConvertDmsToDdCommand { get; }

    // Bearing Math
    private string _bearing1Input = "";
    public string Bearing1Input { get => _bearing1Input; set => SetField(ref _bearing1Input, value); }

    private string _bearing2Input = "";
    public string Bearing2Input { get => _bearing2Input; set => SetField(ref _bearing2Input, value); }

    private string _bearingMathDdOutput = "";
    public string BearingMathDdOutput { get => _bearingMathDdOutput; set => SetField(ref _bearingMathDdOutput, value); }

    private string _bearingMathDmsOutput = "";
    public string BearingMathDmsOutput { get => _bearingMathDmsOutput; set => SetField(ref _bearingMathDmsOutput, value); }

    public System.Windows.Input.ICommand AddBearingsCommand { get; }
    public System.Windows.Input.ICommand SubtractBearingsCommand { get; }

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
    public ObservableCollection<PointViewModel> DesignPoints { get; } = new();
    
    private string _codeSearchText = string.Empty;
    public string CodeSearchText
    {
        get => _codeSearchText;
        set => SetField(ref _codeSearchText, value);
    }
    
    public System.Windows.Input.ICommand SearchCodesCommand { get; }
    public System.Windows.Input.ICommand ImportCodesCommand { get; }
    public System.Windows.Input.ICommand ExportCodesCommand { get; }

    private void SearchCodes()
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(CogoCodes);
        if (string.IsNullOrWhiteSpace(CodeSearchText))
        {
            view.Filter = null;
        }
        else
        {
            try
            {
                string pattern = System.Text.RegularExpressions.Regex.Escape(CodeSearchText).Replace("\\*", ".*");
                var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                view.Filter = item =>
                {
                    if (item is CogoCode c)
                    {
                        return regex.IsMatch(c.LocalCode ?? "") || 
                               regex.IsMatch(c.SystemCode ?? "") || 
                               regex.IsMatch(c.Description ?? "");
                    }
                    return false;
                };
            }
            catch
            {
                // Fallback on invalid regex issues
                string lowerSearch = CodeSearchText.ToLowerInvariant().Replace("*", "");
                view.Filter = item =>
                {
                    if (item is CogoCode c)
                    {
                        return (c.LocalCode?.ToLowerInvariant().Contains(lowerSearch) == true) || 
                               (c.SystemCode?.ToLowerInvariant().Contains(lowerSearch) == true) || 
                               (c.Description?.ToLowerInvariant().Contains(lowerSearch) == true);
                    }
                    return false;
                };
            }
        }
    }

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
                using (var db = new RCS.Data.AppDbContext())
                {
                    var existingEntities = db.CogoCodes.ToList();
                    
                    foreach(var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',');
                        
                        if (parts.Length >= 2) 
                        { 
                            string local = parts[0].Trim(); 
                            string sys = parts[1].Trim(); 
                            
                            var existing = existingEntities.FirstOrDefault(e => string.Equals(e.SystemCode, sys, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.LocalCode = local;
                                existing.Block = parts.Length >= 4 ? parts[3].Trim() : existing.Block;
                            }
                            else
                            {
                                string desc = parts.Length >= 3 ? parts[2].Trim() : sys;
                                string block = parts.Length >= 4 ? parts[3].Trim() : "";
                                var newEntity = new RCS.Data.Entities.CogoCodeEntity 
                                { 
                                    LocalCode = local, 
                                    SystemCode = sys, 
                                    Description = desc,
                                    Block = block
                                };
                                db.CogoCodes.Add(newEntity);
                                existingEntities.Add(newEntity);
                            }
                        }
                    }
                    
                    db.SaveChanges();
                    
                    CogoCodes.Clear();
                    foreach (var e in db.CogoCodes.ToList())
                    {
                        CogoCodes.Add(new CogoCode(e.LocalCode, e.SystemCode, e.Description, e.Block ?? ""));
                    }
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
    public System.Windows.Input.ICommand OpenSymbolManagerCommand { get; }

    private void OpenSymbolManager()
    {
        var win = new RCS.Cogo.Wpf.Views.SymbolManagerWindow();
        win.ShowDialog();
    }

    private void OpenAlignmentWindow()
    {
        var win = new RCS.Cogo.Wpf.Views.AlignmentWindow(this);
        win.Show();
    }

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
                    sb.AppendLine($"{code.LocalCode},{code.SystemCode},{code.Description},{code.Block}");
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
        if (!EnsureActiveProject()) return;
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
    private string _pipingScriptHint = "";
    public string PipingScriptHint
    {
        get => _pipingScriptHint;
        set => SetField(ref _pipingScriptHint, value);
    }

    private string _pipingScriptText = "";
    public string PipingScriptText 
    { 
        get => _pipingScriptText; 
        set 
        {
            SetField(ref _pipingScriptText, value);
            UpdatePipingScriptHint();
        }
    }

    private void UpdatePipingScriptHint()
    {
        if (string.IsNullOrWhiteSpace(_pipingScriptText))
        {
            PipingScriptHint = "";
            return;
        }

        var lines = _pipingScriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string lastTypingLine = lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(lastTypingLine))
        {
            PipingScriptHint = "";
            return;
        }

        string cmd = lastTypingLine.Split(' ')[0].ToUpper();

        if (cmd == "PRUN" && lastTypingLine.ToUpper().Contains("START"))
            PipingScriptHint = "PRUN START <Code> DIAM <Size> MAT <Material>\nCodes: W, WW, ST, G, E, R, CH";
        else if (cmd == "PRUN")
            PipingScriptHint = "PRUN END (Closes the pipe run)";
        else if (cmd == "PIPE-ENGINE-ON")
            PipingScriptHint = "PIPE-ENGINE-ON (Enables Pipe Logic)";
        else if (cmd == "PIPE-ENGINE-OFF")
            PipingScriptHint = "PIPE-ENGINE-OFF (Pauses Pipe Logic)";
        else if (cmd.Contains("-B") || cmd.Contains("-C") || cmd.Contains("-E") || cmd.Contains("-CLS"))
            PipingScriptHint = $"{cmd.Split('-')[0]}-<B/C/E/CLS> <NodeID> [StructureType]\nB=Begin, C=Continue, E=End";
        else if (cmd == "NEZ" || cmd == "ST")
            PipingScriptHint = "NEZ <NodeID> <Northing> <Easting> <Elevation> [Desc]";
        else
            PipingScriptHint = "";
    }
    public System.Windows.Input.ICommand ProcessPipingScriptCommand { get; }
    public System.Windows.Input.ICommand ImportPipingScriptCommand { get; }
    public System.Windows.Input.ICommand ExportPipingScriptCommand { get; }
    public System.Windows.Input.ICommand AnalyzePipingScriptCommand { get; }

    private void AnalyzePipingScript()
    {
        var analyzer = new RCS.Cogo.AI.AiAnalyzer();
        var results = analyzer.AnalyzeScript(PipingScriptText);
        
        var aiWindow = new RCS.Cogo.Wpf.Views.AiAnalysisWindow(results);
        aiWindow.Owner = App.Current.MainWindow;
        aiWindow.ShowDialog();
    }

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
    public System.Windows.Input.ICommand ExportEpanetCommand { get; }
    public System.Windows.Input.ICommand ExportScheduleCommand { get; }

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

    private void ExportEpanet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "EPANET INP File (*.inp)|*.inp|All Files (*.*)|*.*",
            DefaultExt = ".inp",
            FileName = "PipingNetwork.inp"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("[TITLE]");
                writer.WriteLine("Generated EPANET Network");
                writer.WriteLine();

                writer.WriteLine("[JUNCTIONS]");
                writer.WriteLine(";ID              Elev        Demand      Pattern         ");
                foreach (var s in Structures)
                {
                    var pt = _context.GetPoint(s.PointId);
                    double elev = pt?.Elevation ?? 0.0;
                    writer.WriteLine($" Node_{s.PointId,-8} {elev,-10:F2} 0           ");
                }
                writer.WriteLine();

                writer.WriteLine("[PIPES]");
                writer.WriteLine(";ID              Node1           Node2           Length      Diameter    Roughness   MinorLoss   Status");
                int pipeId = 1;
                foreach (var r in PipeRuns)
                {
                    var p1 = _context.GetPoint(r.FromPointId);
                    var p2 = _context.GetPoint(r.ToPointId);
                    double len = 100.0; // Default if missing
                    if (p1 != null && p2 != null)
                    {
                        len = Math.Sqrt(Math.Pow(p2.Easting - p1.Easting, 2) + Math.Pow(p2.Northing - p1.Northing, 2));
                    }
                    if (len < 0.1) len = 0.1;

                    writer.WriteLine($" Pipe_{pipeId,-8} Node_{r.FromPointId,-8} Node_{r.ToPointId,-8} {len,-10:F2} {r.Diameter,-10:F2} 150         0           Open");
                    pipeId++;
                }
                writer.WriteLine();

                writer.WriteLine("[COORDINATES]");
                writer.WriteLine(";Node            X-Coord         Y-Coord");
                foreach (var s in Structures)
                {
                    var pt = _context.GetPoint(s.PointId);
                    if (pt != null)
                    {
                        writer.WriteLine($" Node_{s.PointId,-8} {pt.Easting,-15:F2} {pt.Northing,-15:F2}");
                    }
                }
                writer.WriteLine();
                
                writer.WriteLine("[END]");

                CommandLog.Add($"EPANET Network exported to: {dialog.FileName}");
                _context.Log($"[AUDIT] EPANET INP Exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting EPANET: {ex.Message}");
            }
        }
    }

    private void ExportSchedule()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "Appurtenance_Schedule.csv"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("NodeID,StructureType,Northing,Easting,Elevation,Notes");

                foreach (var s in Structures)
                {
                    var pt = _context.GetPoint(s.PointId);
                    double n = pt?.Northing ?? 0.0;
                    double e = pt?.Easting ?? 0.0;
                    double z = pt?.Elevation ?? 0.0;
                    
                    writer.WriteLine($"{s.PointId},{s.Type},{n:F2},{e:F2},{z:F2},");
                }

                CommandLog.Add($"Schedule exported to: {dialog.FileName}");
                _context.Log($"[AUDIT] Schedule Exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting Schedule: {ex.Message}");
            }
        }
    }

    private async void ProcessPipingScript()
    {
        if (string.IsNullOrWhiteSpace(PipingScriptText)) { CommandLog.Add("Script is empty."); return; }

        IsRunningScript = true;
        try
        {
            await Task.Delay(1500); // UI delay to enforce progress bar visibility
            _context.Log("--- Processing Unified Cogo Context ---");
            var lines = PipingScriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            bool cogoEngineOn = true;
            int counter = 0;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                    continue;

                string cmdLower = trimmed.ToLowerInvariant();
                
                // Engine Toggles for Pre-processor
                if (cmdLower == "cogo-engine-off")
                {
                    cogoEngineOn = false;
                    _context.Log("Cogo Engine Scripting PAUSED.");
                    continue;
                }
                if (cmdLower == "cogo-engine-on")
                {
                    cogoEngineOn = true;
                    _context.Log("Cogo Engine Scripting RESUMED.");
                    continue;
                }
                if (cmdLower == "pipe-engine-off" || cmdLower == "pipe-engine-on")
                {
                    // We ignore pipe commands in the Cogo context preprocessing
                    continue;
                }

                // Filter out clear Pipe Engine commands to prevent Cogo Engine from logging 'Unknown command'
                var parts = cmdLower.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                bool isPipeCommand = false;
                if (parts.Length > 0)
                {
                    string p0 = parts[0];
                    if (p0 == "prun" || p0.StartsWith("ss-") || p0.EndsWith("-b") || p0.EndsWith("-c") || p0.EndsWith("-e"))
                    {
                        isPipeCommand = true;
                    }
                }

                if (cogoEngineOn && !isPipeCommand)
                {
                    await _engine.ExecuteAsync(trimmed, _context);
                }
                
                counter++;
                if (counter % 50 == 0) await Task.Delay(1); // Keep UI responsive for thousands of lines
            }

            _context.Log("--- Compiling Piping Script ---");
            
            var compiler = new RCS.Piping.Core.Scripting.PipeScriptCompiler();
            
            var validMaterials = new HashSet<string>(MasterCatalog.Select(m => m.Material), StringComparer.OrdinalIgnoreCase);
            
            // Allow generic materials for non-pipe utilities (like power/electric runs)
            validMaterials.Add("NONE");
            validMaterials.Add("UNKNOWN");

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
            validCodes.Add("G");
            validCodes.Add("E");
            validCodes.Add("EL");
            validCodes.Add("ST");
            validCodes.Add("CH");
            validCodes.Add("D");
            
            // Map common structural aliases
            validCodes.Add("POLE");
            validCodes.Add("BOX");
            validCodes.Add("EPOLE");
            validCodes.Add("EBOX");
            validCodes.Add("VAULT");
            validCodes.Add("METER");
            validCodes.Add("MANHOLE");
            validCodes.Add("VALVE");
            validCodes.Add("HYDRANT");

            var scriptTextCapture = PipingScriptText; // Ensure no cross-thread property access
            var result = await Task.Run(() => compiler.Compile(scriptTextCapture, (id) => _context.GetPoint(id), validMaterials, validCodes));

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
        finally
        {
            IsRunningScript = false;
        }
    }

    public System.Windows.Input.ICommand SaveHorizontalAlignmentCommand { get; }
    public System.Windows.Input.ICommand SaveVerticalAlignmentCommand { get; }
    public System.Windows.Input.ICommand DeleteHorizontalAlignmentCommand { get; }
    public System.Windows.Input.ICommand DeleteVerticalAlignmentCommand { get; }
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
            else if (type == "WWP" || type == "WWFM")
            {
                 var specific = InstalledAssets.WWPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.WWPressurePipe();
                 
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
            else if (type == "G")
            {
                 var specific = InstalledAssets.GGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.GGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "GP")
            {
                 var specific = InstalledAssets.GPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.GPressurePipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "E")
            {
                 var specific = InstalledAssets.EGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.EGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "EP")
            {
                 var specific = InstalledAssets.EPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.EPressurePipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "ST" || type == "D")
            {
                 var specific = InstalledAssets.STGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.STGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Diameter = run.Diameter; item.Material = run.Material; 
                 item.NorthingStart = n1; item.EastingStart = e1; item.NorthingEnd = n2; item.EastingEnd = e2;
                 item.InvertStart = run.InvertStart; item.InvertEnd = run.InvertEnd; item.Source = "Script";
                 item.Notes = AddScriptKey(item.Notes, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "STP" || type == "STFM")
            {
                 var specific = InstalledAssets.STPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.STPressurePipe();
                 
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
            
            if (t.StartsWith("JEAW") && !t.StartsWith("JEAWW") || t == "VALVE" || t == "HYDRANT" || t.StartsWith("W-")) 
            {
                if (t.EndsWith("V") || t == "VALVE") 
                {
                     var specific = InstalledAssets.WaterValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                }
                else if (t.EndsWith("H") || t == "HYDRANT") 
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
            else if (t.StartsWith("JEAG"))
            {
                 if (t.EndsWith("V")) 
                 {
                     var specific = InstalledAssets.GValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.GValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.GFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.GFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
            }
            else if (t.StartsWith("JEAE"))
            {
                 if (t.EndsWith("V")) 
                 {
                     var specific = InstalledAssets.EValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.EValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.EFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.EFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
            }
            else if (t.StartsWith("JEAST"))
            {
                 if (t.EndsWith("V")) 
                 {
                     var specific = InstalledAssets.STValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.STValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
                 else if (t.Contains("STM") || t.Contains("CBI") || t.Contains("MANHOLE") || t.Contains("INLET") || t.Contains("BASIN"))
                 {
                     var specific = InstalledAssets.STManholes.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.STManhole();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.Elevation = z; item.Source = "Script";
                     item.Notes = AddScriptKey(item.Notes, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.STFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.STFitting();
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

    private void LogToOutput(string msg)
    {
        CommandLog.Add(msg);
        _context.Log(msg);
    }

    private void ExecuteUtilConvert()
    {
        if (double.TryParse(UtilDecInput, out double d))
        {
            UtilDmsOutput = DegreeToDmsString(d);
            LogToOutput($"Converted Decimal to DMS: {d} -> {UtilDmsOutput}");
        }
        else
        {
            UtilDmsOutput = "Invalid Input";
            LogToOutput("Error: Invalid Decimal Input.");
        }
    }

    private void ExecuteUtilConvertDmsToDd()
    {
        try
        {
            if (double.TryParse(UtilDmsInput, out double dms))
            {
                double d = Angle.FromDMS(dms).Degrees;
                UtilDdOutput = $"{d:F6}°";
                LogToOutput($"Converted DMS to Decimal: {dms} -> {UtilDdOutput}");
            }
            else
            {
                UtilDdOutput = "Invalid Input";
                LogToOutput("Error: Invalid DMS Input.");
            }
        }
        catch
        {
            UtilDdOutput = "Invalid Input";
            LogToOutput("Error: Failed to process DMS Input.");
        }
    }

    private void ExecuteBearingMath(bool isAdd)
    {
        try
        {
            if (double.TryParse(Bearing1Input, out double b1) && double.TryParse(Bearing2Input, out double b2))
            {
                double d1 = Angle.FromDMS(b1).Degrees;
                double d2 = Angle.FromDMS(b2).Degrees;
                double res = isAdd ? (d1 + d2) : (d1 - d2);
                
                while(res < 0) res += 360;
                while(res >= 360) res -= 360;

                BearingMathDdOutput = $"{res:F6}°";
                BearingMathDmsOutput = DegreeToDmsString(res);
                string op = isAdd ? "+" : "-";
                LogToOutput($"Bearing Math ({op}): {b1} {op} {b2} -> {BearingMathDdOutput} / {BearingMathDmsOutput}");
            }
            else
            {
                BearingMathDdOutput = "Invalid Input";
                BearingMathDmsOutput = "";
                LogToOutput("Error: Invalid Bearing Input.");
            }
        }
        catch
        {
            BearingMathDdOutput = "Error";
            BearingMathDmsOutput = "";
            LogToOutput("Error: Failed to process Bearing Math.");
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
            LogToOutput($"Supplement Finder: 180 - {d} -> {UtilSuppOutput}");
        }
        else
        {
            UtilSuppOutput = "Invalid Input";
            LogToOutput("Error: Invalid Supplement Input.");
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
        UtilDmsInput = "";
        UtilDdOutput = "";
        UtilSuppInput = "";
        UtilSuppOutput = "";
        Bearing1Input = "";
        Bearing2Input = "";
        BearingMathDdOutput = "";
        BearingMathDmsOutput = "";
        
        LogToOutput("Curve Solver Reset.");
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
            LogToOutput("Error: Please provide exactly two curve parameters.");
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
                
                LogToOutput($"Curve Solved: R={CurveRadius}, T={CurveTangent}, L={CurveArc}, C={CurveChord}, D={CurveDelta} ({CurveDeltaDms})");
            }
        }
        catch (Exception ex)
        {
            LogToOutput($"Curve Solver Error: {ex.Message}");
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

    private void SaveHorizontalAlignmentFromMenu()
    {
        // Execute SAVE-HALN with default name if no args passed, or prompt
        // Alternatively, since they just want to save the active script...
        // The script itself might be empty, but we'll try to save it.
        string name = "Menu_HALN_" + DateTime.Now.ToString("HHmmss");
        _engine.ExecuteAsync($"SAVE-HALN \"{name}\" \"Saved from Menu\"", _context).Wait();
        // Force refresh
        RefreshData(false);
    }

    private void SaveVerticalAlignmentFromMenu()
    {
        string name = "Menu_PFL_" + DateTime.Now.ToString("HHmmss");
        _engine.ExecuteAsync($"SAVE-PFL \"{name}\" \"Saved from Menu\"", _context).Wait();
        // Force refresh
        RefreshData(false);
    }

    private async void DeleteHorizontalAlignmentFromMenu()
    {
        var win = new RCS.Cogo.Wpf.Views.DeleteAlignmentWindow("Delete Horizontal Alignment", InstalledAssets.HorizontalAlignments) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true && win.SelectedItem is RCS.Data.Entities.HorizontalAlignment ha)
        {
            await InstalledAssets.DeleteAssetAsync(ha);
            InstalledAssets.HorizontalAlignments.Remove(ha);
            RefreshData(false);
        }
    }

    private async void DeleteVerticalAlignmentFromMenu()
    {
        var win = new RCS.Cogo.Wpf.Views.DeleteAlignmentWindow("Delete Profile Alignment", InstalledAssets.ProfileAlignments) { Owner = App.Current.MainWindow };
        if (win.ShowDialog() == true && win.SelectedItem is RCS.Data.Entities.ProfileAlignment pa)
        {
            await InstalledAssets.DeleteAssetAsync(pa);
            InstalledAssets.ProfileAlignments.Remove(pa);
            RefreshData(false);
        }
    }

    private void ExportOutputLog()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "OutputLog.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                System.IO.File.WriteAllText(dialog.FileName, ResultLogText);
                CommandLog.Add($"Output log exported to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting output log: {ex.Message}");
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
    
    // Report Commands
    public System.Windows.Input.ICommand ReportWaterCommand { get; }
    public System.Windows.Input.ICommand ReportSewerCommand { get; }
    public System.Windows.Input.ICommand ReportGasCommand { get; }
    public System.Windows.Input.ICommand ReportElectricCommand { get; }
    public System.Windows.Input.ICommand ReportDrainageCommand { get; }
    public System.Windows.Input.ICommand ReportAllAssetsCsvCommand { get; }
    public System.Windows.Input.ICommand ReportAllAssetsTxtCommand { get; }
    public System.Windows.Input.ICommand ReportAllAssetsXlsCommand { get; }

    private void ExecuteTestNativeSecurity()
    {
        try
        {
            // Pick a seed, maybe the day of the year, or just a constant. 
            // In a real app this would be part of your hardware ID.
            int seed = DateTime.Now.DayOfYear;
            
            // Call into Machine Code (Native C++)!
            string secureData = RCS.Cogo.Wpf.Services.NativeSecurityWrapper.GetSecureData(seed);
            string mId = RCS.Cogo.Wpf.Services.NativeSecurityWrapper.GetHardwareFingerprint();

            System.Windows.MessageBox.Show(
                $"Unmanaged C++ DLL executed successfully!\n\nExtracted Native Machine ID:\n{mId}\n\nSeed Requested: {seed}\nEncrypted C++ Response: {secureData}", 
                "Hardware Level Security Achieved", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to call C++ DLL: {ex.Message}", "Error");
        }
    }

    private void OpenLicensingAgentWindow()
    {
        var win = new RCS.Cogo.Wpf.Views.LicenseAgentWindow
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }

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
                    
                    if (ShowFigureLabels)
                    {
                        foreach(var label in fig.Labels)
                        {
                            writer.AddText(label.Text, label.Easting, label.Northing, 1.0, "FIGURE_LABELS", "CENTER", label.RotationDegrees);
                        }
                    }
                }
                
                // Export PipeRuns
                foreach (var run in PipeRuns)
                {
                    var p1 = _context.GetPoint(run.FromPointId);
                    var p2 = _context.GetPoint(run.ToPointId);
                    if (p1 != null && p2 != null)
                    {
                        string layer = $"PIPE_{run.Type}_{run.Diameter}_{run.Material}";
                        writer.AddLine(p1.Easting, p1.Northing, p2.Easting, p2.Northing, layer);
                        
                        // Add textual label at mid point
                        double midX = (p1.Easting + p2.Easting) / 2;
                        double midY = (p1.Northing + p2.Northing) / 2;
                        writer.AddText($"{run.Diameter}\" {run.Material} {run.Type}", midX, midY, 1.5, layer + "_TEXT", "CENTER");
                    }
                }
                
                // Export Structures
                foreach(var s in StructureGraphics)
                {
                    string block = "MANHOLE";
                    string t = (s.Type ?? "").ToUpper();
                    
                    if (t.Contains("VALVE") || t.EndsWith("V") || t == "WV" || t == "WWV" || t == "STV" || t == "EV" || t == "GV" || t == "RV") block = "VALVE";
                    else if (t.Contains("HYDRANT") || t.EndsWith("H") || t == "HYD") block = "HYDRANT";
                    else if (t.Contains("METER") || t == "WMET" || t == "EMET" || t == "GMET" || t == "RMET") block = "METER"; 
                    else if (t.Contains("POLE") || t == "EPOLE") block = "POLE";
                    else if (t.Contains("BOX") || t.Contains("VAULT") || t == "EBOX") block = "BOX";
                    else if (t.Contains("FITTING") || t.EndsWith("F") || t == "WF" || t == "WWF" || t == "STF" || t == "EF" || t == "GF") block = "FITTING";
                    
                    writer.InsertBlock(block, s.Easting, s.Northing, 1.0, $"STRUCT_{s.Type}");
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
        if (!EnsureActiveProject()) return;

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

        if (BatchScriptContent.Contains("PIPE-ENGINE-ON", StringComparison.OrdinalIgnoreCase) || 
            BatchScriptContent.Contains("PRUN", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show("This script contains Piping Commands. Please run it through the Piping Script tab instead.", "Piping Script Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsRunningScript = true;
        
        // As requested: display progress bar for 1.5 seconds minimum
        await Task.Delay(1500);

        CommandLog.Add("--- Running Batch Script ---");
        var lines = BatchScriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        // Check for RESET directives at the top of the script
        bool shouldReset = true;
        foreach (var line in lines)
        {
            string t = line.Trim().ToUpper();
            if (string.IsNullOrEmpty(t) || t.StartsWith("!") || t.StartsWith("//")) continue;
            
            if (t == "RESET-OFF") 
            {
                shouldReset = false;
                break;
            }
            if (t == "RESET-ON")
            {
                shouldReset = true;
                break;
            }
            // If the first real command is something else, stop looking and default to true
            break;
        }

        if (shouldReset)
        {
            // Reset state before running
            await _engine.ExecuteAsync("CLEAR", _context);
            await _engine.ExecuteAsync("DEL PTS", _context);
            await _engine.ExecuteAsync("DEL FIG", _context);
        }
        int counter = 0;
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                continue;
                
            CommandLog.Add($"> {trimmed}");
            await _engine.ExecuteAsync(trimmed, _context);
            
            counter++;
            if (counter % 50 == 0) await Task.Delay(1); // Force WPF dispatcher to render progress bar and unblock UI
        }
        
        CommandLog.Add("--- Batch Complete ---");
        RefreshData();
        
        IsRunningScript = false;
    }

    private async Task WalkBatchScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(BatchScriptContent)) return;

        if (BatchScriptContent.Contains("PIPE-ENGINE-ON", StringComparison.OrdinalIgnoreCase) || 
            BatchScriptContent.Contains("PRUN", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show("This script contains Piping Commands. Please run it through the Piping Script tab instead.", "Piping Script Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        CommandLog.Add("--- Walking Batch Script ---");
        
        var lines = BatchScriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        bool shouldReset = true;
        foreach (var line in lines)
        {
            string t = line.Trim().ToUpper();
            if (string.IsNullOrEmpty(t) || t.StartsWith("!") || t.StartsWith("//")) continue;
            
            if (t == "RESET-OFF") 
            {
                shouldReset = false;
                break;
            }
            if (t == "RESET-ON")
            {
                shouldReset = true;
                break;
            }
            break;
        }

        if (shouldReset)
        {
            // Reset state before walking
            await _engine.ExecuteAsync("CLEAR", _context);
            await _engine.ExecuteAsync("DEL PTS", _context);
            await _engine.ExecuteAsync("DEL FIG", _context);
            RefreshData(true);
        }
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                continue;
                
            CommandLog.Add($"> {trimmed}");
            await _engine.ExecuteAsync(trimmed, _context);
            
            // Force data refresh but don't reset bounds
            RefreshData(false);

            System.Windows.Point zoomTarget = new System.Windows.Point();
            bool foundTarget = false;
            
            if (_context.CurrentFigure != null && _context.CurrentFigure.PointIds.Count > 0)
            {
                var pt = _context.GetPoint(_context.CurrentFigure.PointIds.LastOrDefault() ?? "");
                if (pt != null)
                {
                    zoomTarget = new System.Windows.Point(pt.Easting, pt.Northing);
                    foundTarget = true;
                }
            }

            if (!foundTarget)
            {
                var lastPt = Points.LastOrDefault();
                if (lastPt != null)
                {
                    zoomTarget = new System.Windows.Point(lastPt.Easting, lastPt.Northing);
                    foundTarget = true;
                }
            }

            if (foundTarget)
            {
                 System.Windows.Application.Current?.Dispatcher.Invoke(() => 
                    ZoomToPointRequested?.Invoke(this, zoomTarget));
            }
            
            await Task.Delay(1000); // 1-second delay for visually tracking the build
        }
        
        CommandLog.Add("--- Walk Complete ---");
        RefreshData(true);
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

    private void RefreshData(bool autoZoomExtents = true)
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
                    var stroke = fig.MapCheckFailed ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Yellow;
                    Figures.Add(new FigureViewModel(fig.Name, pts, stroke, fig.Labels));
                }
            }
            
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
                else if (type == "ST" || type == "S" || type == "D" || type.Contains("SW") || type.Contains("STORM")) pipeBrush = System.Windows.Media.Brushes.Cyan;
                else if (type == "W" || type.Contains("WATER")) pipeBrush = System.Windows.Media.Brushes.Blue;
                else if (type == "R" || type.Contains("RECLAIM")) pipeBrush = System.Windows.Media.Brushes.Purple;
                else if (type == "G" || type.Contains("GAS")) pipeBrush = System.Windows.Media.Brushes.Orange;
                else if (type == "E" || type == "EL" || type.Contains("ELEC")) pipeBrush = System.Windows.Media.Brushes.Red;
                else if (type == "CH" || type.Contains("CHILL")) pipeBrush = System.Windows.Media.Brushes.LightSkyBlue;
                else if (type.Contains("PP") || type.Contains("PRESS")) pipeBrush = System.Windows.Media.Brushes.Red;
                    
                    var fig = new FigureViewModel($"Pipe-{run.Id}", pts, pipeBrush);
                    Figures.Add(fig);
                }
            }
            
            // Render Alignments
            foreach (var algn in _context.GetAllAlignments())
            {
                var pts = new System.Collections.Generic.List<Point3D>();
                var labels = new System.Collections.Generic.List<RCS.Cogo.App.State.FigureLabel>();

                string FormatStation(double st)
                {
                    int hundreds = (int)(st / 100);
                    double remainder = st - (hundreds * 100);
                    return $"{hundreds}+{remainder:00.00}";
                }

                if (_context.ShowAlignmentLabels && algn.Elements.Count > 0)
                {
                    // Start Label
                    var startPt = algn.Elements[0].GetCoordinateAt(algn.StartStation);
                    labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = $"POB {FormatStation(algn.StartStation)}", Easting = startPt.Easting, Northing = startPt.Northing, RotationDegrees = 0 });
                }

                foreach (var element in algn.Elements)
                {
                    if (element is RCS.Alignments.Core.ArcElement)
                    {
                        // Interpolate arcs for smooth drawing
                        double step = 10.0;
                        for (double s = element.StartStation; s < element.EndStation; s += step)
                        {
                            pts.Add(element.GetCoordinateAt(s));
                        }
                        pts.Add(element.GetCoordinateAt(element.EndStation)); // Ensure flush fit
                    }
                    else
                    {
                        // Lines just need ends
                        pts.Add(element.GetCoordinateAt(element.StartStation));
                        pts.Add(element.GetCoordinateAt(element.EndStation));
                    }

                    if (_context.ShowAlignmentLabels)
                    {
                        // Midpoint
                        double midStation = element.StartStation + (element.Length / 2);
                        var midPt = element.GetCoordinateAt(midStation);
                        labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = $"MID {FormatStation(midStation)}", Easting = midPt.Easting, Northing = midPt.Northing, RotationDegrees = 0 });

                        // Endpoint
                        var endPt = element.GetCoordinateAt(element.EndStation);
                        labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = $"PT {FormatStation(element.EndStation)}", Easting = endPt.Easting, Northing = endPt.Northing, RotationDegrees = 0 });
                    }
                }
                
                if (pts.Count > 1)
                {
                    Figures.Add(new FigureViewModel($"ALGN-{algn.Name}", pts, System.Windows.Media.Brushes.Cyan, labels));
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
            if (autoZoomExtents) ZoomExtentsRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    private void NewProject(bool skipEdit = false)
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
        
        // Force User to Enter Details unless skipping
        if (!skipEdit)
        {
            EditProject();
        }
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
    public System.Windows.Input.ICommand OpenValidationSettingsCommand { get; }
    public System.Windows.Input.ICommand OpenGeneralSettingsCommand { get; }

    private void OpenGeneralSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.GeneralSettingsWindow
        {
            DataContext = this
        };
        window.ShowDialog();
    }

    private void OpenAlignmentSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.AlignmentSettingsWindow(this);
        window.ShowDialog();
    }

    private void OpenPipeCharacteristics()
    {
        var window = new RCS.Cogo.Wpf.Views.PipeCharacteristicsWindow();
        window.ShowDialog();
    }

    private void OpenValidationSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.ValidationSettingsWindow();
        window.ShowDialog();
        _context.Log("[AUDIT] Validation Settings Window Closed.");
    }

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
            var loader = new RCS.Cogo.Wpf.Views.LoadingWindow("Loading Project...", async () =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => NewProject(true)); // Reset State first (skip edit dialog)
                    
                    var service = new LiteDbProjectService();
                    var loadedProject = await Task.Run(() => service.LoadProject(dialog.FileName));
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentProject = loadedProject;
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
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                         _context.Log($"[AUDIT] Error Opening Project: {ex.Message}");
                         System.Windows.MessageBox.Show($"Error Opening Project: {ex.Message}");
                    });
                }
            });
            
            loader.ShowDialog();
        }
    }

    // --- Database Maintenance ---
    public System.Windows.Input.ICommand CompactDbCommand { get; }
    public System.Windows.Input.ICommand VerifyDbCommand { get; }
    public System.Windows.Input.ICommand RepairDbCommand { get; }
    public System.Windows.Input.ICommand ExportDbCsvCommand { get; }

    private void CompactDatabase()
    {
        try 
        {
            using (var db = new RCS.Data.AppDbContext())
            {
                db.Database.ExecuteSqlRaw("VACUUM;");
            }
            _context.Log("[AUDIT] System Database Compacted Successfully (VACUUM).");
            System.Windows.MessageBox.Show("System Database Compacted Successfully.");
        }
        catch(Exception ex)
        {
            _context.Log($"[AUDIT] Error Compacting DB: {ex.Message}");
        }
    }

    private void VerifyDatabase()
    {
        try 
        {
            string status = "Unknown";
            using (var db = new RCS.Data.AppDbContext())
            {
                var conn = db.Database.GetDbConnection();
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "PRAGMA integrity_check;";
                    status = command.ExecuteScalar()?.ToString() ?? "Unknown";
                }
                conn.Close();
            }
            
            bool ok = status?.Contains("ok", StringComparison.OrdinalIgnoreCase) == true;
            string msg = ok ? "System Database Verification Passed (Integrity OK)." : $"System Database Verification FAILED: {status}";
            _context.Log($"[AUDIT] {msg}");
            System.Windows.MessageBox.Show(ok ? "Verification Passed: Integrity OK" : $"Verification Failed: {status}");
        }
        catch(Exception ex)
        {
            _context.Log($"[AUDIT] Error Verifying DB: {ex.Message}");
        }
    }

    private void RepairDatabase()
    {
        try
        {
            // SQLite doesn't easily 'repair' in-place via simple commands aside from VACUUM/REINDEX. 
            using (var db = new RCS.Data.AppDbContext())
            {
                db.Database.ExecuteSqlRaw("REINDEX; VACUUM;");
            }
            _context.Log("[AUDIT] System Database Repair Sequence completed (REINDEX/VACUUM).");
            System.Windows.MessageBox.Show("System Database Repair Sequence Completed.");
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
        NewProject(true); // Effectively closes by resetting and skips edit dialog
        _context.Log("[AUDIT] Closed Project");
    }

    private void ImportPointsList()
    {
        if (!EnsureActiveProject()) return;

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

public class FigureLabelViewModel : ViewModelBase
{
    public string Text { get; }
    public double Easting { get; }
    public double Northing { get; }
    public double RotationDegrees { get; }

    public FigureLabelViewModel(string text, double easting, double northing, double rotationDegrees)
    {
        Text = text;
        Easting = easting;
        Northing = northing;
        RotationDegrees = rotationDegrees;
    }
}

public class FigureViewModel : ViewModelBase
{
    public string Name { get; }
    public System.Windows.Media.PointCollection Points { get; }
    public System.Windows.Media.Brush Stroke { get; }
    public System.Collections.ObjectModel.ObservableCollection<FigureLabelViewModel> Labels { get; } = new();

    public FigureViewModel(string name, System.Collections.Generic.IEnumerable<Point3D> points, System.Windows.Media.Brush? stroke = null, System.Collections.Generic.IEnumerable<RCS.Cogo.App.State.FigureLabel>? labels = null)
    {
        Name = name;
        Points = new System.Windows.Media.PointCollection();
        foreach (var p in points)
        {
            Points.Add(new System.Windows.Point(p.Easting, p.Northing));
        }
        Stroke = stroke ?? System.Windows.Media.Brushes.Yellow; // Default to Yellow

        if (labels != null)
        {
            foreach(var l in labels) Labels.Add(new FigureLabelViewModel(l.Text, l.Easting, l.Northing, l.RotationDegrees));
        }
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
        else if (t.Contains("POLE")) SymbolType = "Pole";
        else if (t.Contains("BOX")) SymbolType = "Box";
        else SymbolType = "Default";
        // Color Logic
        if (type.Equals("Manhole", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Magenta;
        else if (type.Equals("Valve", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Red;
        else if (type.Equals("Inlet", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Orange;
        // Utility Types
        else if (type.Contains("WW") || type.Contains("SAN")) Fill = System.Windows.Media.Brushes.Green;
        else if (type == "ST" || type == "S" || type == "D" || type.Contains("SW") || type.Contains("STORM")) Fill = System.Windows.Media.Brushes.Cyan;
        else if (type == "W" || type.Contains("WATER")) Fill = System.Windows.Media.Brushes.Blue;
        else if (type == "R" || type.Contains("RECLAIM")) Fill = System.Windows.Media.Brushes.Purple;
        else if (type == "G" || type.Contains("GAS") || type.StartsWith("GAS")) Fill = System.Windows.Media.Brushes.Orange;
        else if (type == "E" || type == "EL" || type.Contains("ELEC") || type.StartsWith("E") || type.StartsWith("E-")) Fill = System.Windows.Media.Brushes.Red;
        else if (type == "CH" || type.Contains("CHILL")) Fill = System.Windows.Media.Brushes.LightSkyBlue;
        else Fill = System.Windows.Media.Brushes.White; // Default 
    }
}
