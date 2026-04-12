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

using RCS.Geo.Core;
using RCS.Geo.ProjNet;
using RCS.Geo.Abstractions;
using GeoWpf = RCS.Geo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.ViewModels;

/// <summary>One entry in the Recent Files (MRU) list.</summary>
public class RecentFileEntry
{
    public string FilePath    { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime LastOpened { get; set; } = DateTime.Now;
    /// <summary>Display label shown in the Recent Files menu.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(ProjectName) ? System.IO.Path.GetFileName(FilePath) : ProjectName;
}

public class PointViewModel : ViewModelBase
{
    private string _id;
    public string Id { get => _id; set => SetField(ref _id, value); }

    /// <summary>Numeric parse of Id for correct integer DataGrid sort order (1,2,10 not 1,10,2).</summary>
    public int NumericId => int.TryParse(_id, out var n) ? n : int.MaxValue;

    private bool _isSelected;
    /// <summary>True when this point is the currently highlighted/selected point in the viewport.</summary>
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }

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

    // ── GPS computed coordinates ─────────────────────────────────────────
    /// <summary>
    /// Application-wide GPS display format — set by ShellViewModel whenever
    /// GeneralSettings changes. Changing this raises property changed on all live instances.
    /// </summary>
    public static RCS.Geo.Wpf.ViewModels.GpsDisplayFormat CoordinateFormat { get; set; }
        = RCS.Geo.Wpf.ViewModels.GpsDisplayFormat.DecimalDegrees;

    /// <summary>
    /// Active FL State Plane zone (EPSG string) — set by ShellViewModel from
    /// <c>JeaStatePlaneZone</c>. Defaults to FL East (EPSG:2236).
    /// Changing this causes RefreshGpsDisplay() to re-project every visible point.
    /// </summary>
    public static string ActiveZone { get; set; } = "EPSG:2236";

    public double LatitudeGps  => RCS.Geo.Core.StatePlaneProjection.ToLatLon(Easting, Northing, ActiveZone).Lat;
    public double LongitudeGps => RCS.Geo.Core.StatePlaneProjection.ToLatLon(Easting, Northing, ActiveZone).Lon;

    /// <summary>Latitude formatted per <see cref="CoordinateFormat"/> (DD or DMS).</summary>
    public string LatitudeDisplay => CoordinateFormat == RCS.Geo.Wpf.ViewModels.GpsDisplayFormat.DMS
        ? RCS.Geo.Core.StatePlaneProjection.ToDms(LatitudeGps,  isLatitude: true)
        : $"{LatitudeGps:F7}";

    /// <summary>Longitude formatted per <see cref="CoordinateFormat"/> (DD or DMS).</summary>
    public string LongitudeDisplay => CoordinateFormat == RCS.Geo.Wpf.ViewModels.GpsDisplayFormat.DMS
        ? RCS.Geo.Core.StatePlaneProjection.ToDms(LongitudeGps, isLatitude: false)
        : $"{LongitudeGps:F7}";

    /// <summary>Refreshes all GPS display properties — called when CoordinateFormat or ActiveZone changes.</summary>
    public void RefreshGpsDisplay()
    {
        OnPropertyChanged(nameof(LatitudeDisplay));
        OnPropertyChanged(nameof(LongitudeDisplay));
        OnPropertyChanged(nameof(LatitudeGps));
        OnPropertyChanged(nameof(LongitudeGps));
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

public partial class ShellViewModel : ViewModelBase
{
    private readonly ScriptEngine _engine;
    private readonly CogoContext _context;

    // ── Production UX sub-ViewModels ───────────────────────────────────────────
    /// <summary>Drives the left-pane workflow step navigator.</summary>
    public WorkflowNavigatorViewModel Navigator { get; } = new();

    /// <summary>Drives the bottom diagnostics pane (Errors / Warnings / Info / Audit).</summary>
    public DiagnosticsViewModel Diagnostics { get; } = new();

    /// <summary>Drives the start-screen Job Dashboard.</summary>
    public JobDashboardViewModel Dashboard { get; } = new();

    private bool _showDashboard = false;   // Only shown when user clicks 🏗 As-Built tab with no active job
    /// <summary>
    /// True when no job is open → shows JobDashboardView in the center pane.
    /// False when a job is active → shows the workflow-step editor.
    /// </summary>
    public bool ShowDashboard
    {
        get => _showDashboard;
        set { _showDashboard = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowWorkspace)); }
    }
    public bool ShowWorkspace => !_showDashboard;

    /// <summary>
    /// The currently open as-built job — set by ShellWindow after constructing
    /// AsBuiltWorkspaceViewModel. Used by RefreshWorkflowState to drive the Navigator.
    /// </summary>
    public RCS.Piping.Core.Workflow.AsBuiltJob? ActiveJob { get; set; }

    /// <summary>
    /// Central refresh — call after any mutation that affects job state,
    /// validation results, or workflow phase.
    /// </summary>
    public void RefreshWorkflowState()
    {
        var job = ActiveJob;
        if (job == null) return;
        var engine = new RCS.Piping.Core.Workflow.ValidationEngine();
        var result = engine.Validate(job);
        Navigator.Refresh(job, result);
        Diagnostics.Refresh(result);
    }

    public CogoContext GetContext() => _context;


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
            case "COPYPT":
            case "COPY-PT": CommandHint = "COPY-PT <OldPt> <NewPt> [Desc]"; break;
            case "DELPT": CommandHint = "DELPT <PointID | StartPt-EndPt>"; break;

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

    /// <summary>
    /// CollectionView wrapper — lets the DataGrid sort Points by any column
    /// (including Point Number / Id) simply by clicking the column header.
    /// </summary>
    public System.ComponentModel.ICollectionView PointsView { get; }

    // ── Points Quick-Filter ───────────────────────────────────────────────────
    private string _pointsFilter = string.Empty;
    /// <summary>
    /// Bound to the Points-tab search TextBox. Filters the PointsView live by
    /// point number (Id) or Description containing the typed text.
    /// </summary>
    public string PointsFilter
    {
        get => _pointsFilter;
        set
        {
            if (SetField(ref _pointsFilter, value ?? string.Empty))
                PointsView.Refresh();
        }
    }

    public ObservableCollection<FigureViewModel> Figures { get; } = new();
    public ObservableCollection<StructureViewModel> StructureGraphics { get; } = new();
    public ObservableCollection<StructureViewModel> HighlightedAssets { get; } = new();

    // ── Asset Inspector ───────────────────────────────────────────────────────
    private StructureViewModel? _selectedStructure;
    public StructureViewModel? SelectedStructure
    {
        get => _selectedStructure;
        set
        {
            if (_selectedStructure != null) _selectedStructure.IsSelected = false;
            SetField(ref _selectedStructure, value);
            if (_selectedStructure != null) _selectedStructure.IsSelected = true;
            OnPropertyChanged(nameof(InspectorVisible));
        }
    }
    public bool InspectorVisible => _selectedStructure != null;
    public System.Windows.Input.ICommand ClearInspectorCommand => new RelayCommand(_ => { SelectedStructure = null; });


    private Project _currentProject = new Project();
    public Project CurrentProject
    {
        get => _currentProject;
        set
        {
            if (SetField(ref _currentProject, value))
            {
                OnPropertyChanged(nameof(HasActiveProject));
                if (_context != null) _context.ProjectDirectory = _currentProject?.SaveLocation;
                _ = LoadInstalledAssetsAsync();
                UpdateWindowTitle();
            }
        }
    }

    // ── Window Title ─────────────────────────────────────────────────────────
    private string _windowTitle = "RCS COGO Enterprise";
    public string WindowTitle
    {
        get => _windowTitle;
        private set => SetField(ref _windowTitle, value);
    }

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        private set { if (SetField(ref _isDirty, value)) UpdateWindowTitle(); }
    }

    private void SetDirty() => IsDirty = true;

    private void UpdateWindowTitle()
    {
        string name = string.IsNullOrWhiteSpace(_currentProject?.ProjectName)
            ? "Untitled Project"
            : _currentProject!.ProjectName;

        string file = string.IsNullOrWhiteSpace(_currentDbPath)
            ? "(unsaved)"
            : System.IO.Path.GetFileName(_currentDbPath);

        string dirty = _isDirty ? " *" : "";
        WindowTitle = $"{name}  [{file}]{dirty}  —  RCS COGO Enterprise";
    }

    // ── Recent Files (MRU) ───────────────────────────────────────────────────
    private const int MaxRecentFiles = 10;
    private static readonly string RecentFilesPath =
        System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RCS.Cogo.Enterprise", "recentfiles.json");

    public System.Collections.ObjectModel.ObservableCollection<RecentFileEntry> RecentFiles { get; }
        = new();

    public System.Windows.Input.ICommand OpenRecentFileCommand { get; private set; }
        = new RelayCommand(param => { }); // initialized in ctor via InitRecentFileCommand()

    private void LoadRecentFiles()
    {
        try
        {
            if (!System.IO.File.Exists(RecentFilesPath)) return;
            var json = System.IO.File.ReadAllText(RecentFilesPath);
            var list = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<RecentFileEntry>>(json);
            if (list == null) return;
            foreach (var e in list.Where(e => System.IO.File.Exists(e.FilePath)))
                RecentFiles.Add(e);
        }
        catch { /* non-fatal */ }
    }

    private void PushRecentFile(string filePath, string projectName)
    {
        // Remove any existing entry for this path
        var existing = RecentFiles.FirstOrDefault(r =>
            r.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null) RecentFiles.Remove(existing);

        // Insert at top
        RecentFiles.Insert(0, new RecentFileEntry
        {
            FilePath    = filePath,
            ProjectName = projectName,
            LastOpened  = DateTime.Now
        });

        // Trim to max
        while (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);

        SaveRecentFiles();
    }

    private void SaveRecentFiles()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(RecentFilesPath)!;
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(RecentFiles.ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(RecentFilesPath, json);
        }
        catch { /* non-fatal */ }
    }

    private void OpenRecentFile(object? param)
    {
        if (param is not RecentFileEntry entry) return;
        if (!System.IO.File.Exists(entry.FilePath))
        {
            System.Windows.MessageBox.Show(
                $"The file no longer exists:\n{entry.FilePath}\n\nIt will be removed from the recent list.",
                "File Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            RecentFiles.Remove(entry);
            SaveRecentFiles();
            return;
        }
        if (!ConfirmDiscardChanges()) return;
        LoadProjectFromPath(entry.FilePath);
    }


    /// <summary>
    /// Scans all rendered figures for segment-segment intersections and saves
    /// PipeCrossing records to the project's installed-assets DB.
    /// Pipe sizes are resolved from the installed-pipe tables using a
    /// discipline-keyword match on each figure's name.
    /// </summary>
    private async void FindCrossings()
    {
        if (!EnsureActiveProject()) return;

        // ── 1. Build segment list from all current figures ───────────────────
        var allSegments = new System.Collections.Generic.List<(Point3D Start, Point3D End, string Source)>();
        foreach (var fig in Figures)
        {
            if (fig.Points == null || fig.Points.Count < 2) continue;
            for (int i = 0; i < fig.Points.Count - 1; i++)
            {
                allSegments.Add((
                    new Point3D(fig.Points[i].Y,   fig.Points[i].X,   0),
                    new Point3D(fig.Points[i+1].Y, fig.Points[i+1].X, 0),
                    fig.Name));
            }
        }

        // ── 2. Build figure-name → typical pipe size map ─────────────────────
        // We match on discipline keywords in the figure name, then pull the
        // first non-null Size found for that discipline in the project DB.
        var sizeMap = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var db  = new RCS.Data.AppDbContext();
            string   pid  = CurrentProject.Id.ToString();

            // Local helper: first non-null Size for the given table + project
            string? FirstSize<T>(System.Linq.IQueryable<T> table)
                where T : RCS.Data.Entities.InstalledAsset =>
                table.Where(x => x.ProjectId == pid && x.Size != null)
                     .Select(x => x.Size)
                     .FirstOrDefault();

            foreach (var fig in Figures)
            {
                if (sizeMap.ContainsKey(fig.Name)) continue;
                string upper = fig.Name.ToUpperInvariant();
                string? size = null;

                if      (upper.Contains("WATER")   && !upper.Contains("WASTE") && !upper.Contains("STORM"))
                    size = FirstSize(db.WaterPipes);
                else if (upper.Contains("WW")      || upper.Contains("SEWER") ||
                         upper.Contains("SANITARY")|| upper.Contains("GRAVITY"))
                    size = FirstSize(db.WWGravityPipes) ?? FirstSize(db.WWPressurePipes);
                else if (upper.Contains("GAS"))
                    size = FirstSize(db.GGravityPipes)  ?? FirstSize(db.GPressurePipes);
                else if (upper.Contains("ELEC") || upper.Contains("POWER") || upper.Contains("CONDUIT"))
                    size = FirstSize(db.EGravityPipes)  ?? FirstSize(db.EPressurePipes);
                else if (upper.Contains("STORM") || upper.Contains("DRAIN"))
                    size = FirstSize(db.STGravityPipes) ?? FirstSize(db.STPressurePipes);
                else if (upper.Contains("RECLAIM") || upper.Contains("REUSE"))
                    size = FirstSize(db.ReclaimedPipes);
                else if (upper.Contains("CHILL"))
                    size = FirstSize(db.ChilledPipes);

                if (size != null) sizeMap[fig.Name] = size;
            }
        }
        catch { /* size lookup is best-effort; crossings still save without sizes */ }

        // ── 3. Find intersections and persist PipeCrossing records ───────────
        int matchCount = 0;
        for (int i = 0; i < allSegments.Count; i++)
        {
            for (int j = i + 1; j < allSegments.Count; j++)
            {
                if (allSegments[i].Source == allSegments[j].Source) continue;

                var intersection = RCS.Cogo.Core.Maths.GeometryEngine.IntersectionSegmentSegment(
                    allSegments[i].Start, allSegments[i].End,
                    allSegments[j].Start, allSegments[j].End);

                if (intersection != null)
                {
                    matchCount++;
                    HighlightedAssets.Add(new StructureViewModel(
                        $"x{matchCount}", intersection, "CONFLICT"));

                    var crossing = new RCS.Data.Entities.PipeCrossing
                    {
                        CrossingNumber = $"X-{matchCount}",
                        UpperPipeType  = allSegments[i].Source,
                        UpperPipeSize  = sizeMap.TryGetValue(allSegments[i].Source, out var us) ? us : null,
                        LowerPipeType  = allSegments[j].Source,
                        LowerPipeSize  = sizeMap.TryGetValue(allSegments[j].Source, out var ls) ? ls : null,
                        Northing       = intersection.Northing,
                        Easting        = intersection.Easting,
                        ProjectId      = CurrentProject.Id.ToString()
                    };

                    await InstalledAssets.AddItemAsync(crossing);
                }
            }
        }

        CommandLog.Add($"[SYSTEM] Generated {matchCount} pipe crossing(s) to Installed Assets.");
    }

    public bool HasActiveProject 
    {
        get => CurrentProject != null && !string.IsNullOrWhiteSpace(CurrentProject.ProjectName) && CurrentProject.ProjectName != "New Project";
    }

    public bool EnsureActiveProject()
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
                    8 => "GPS Proj",
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

    // ── GPS coordinate display format ─────────────────────────────────────
    private GeoWpf.GpsDisplayFormat _gpsCoordinateFormat = GeoWpf.GpsDisplayFormat.DecimalDegrees;
    public GeoWpf.GpsDisplayFormat GpsCoordinateFormat
    {
        get => _gpsCoordinateFormat;
        set
        {
            if (SetField(ref _gpsCoordinateFormat, value))
            {
                CoordinateTransformVm.CoordinateFormat = value;
                // Push to PointViewModel static so all DataGrid rows re-format live
                PointViewModel.CoordinateFormat = value;
                foreach (var pt in Points)
                    pt.RefreshGpsDisplay();
            }
        }
    }

    public GeoWpf.GpsDisplayFormat[] AvailableGpsFormats { get; } =
        Enum.GetValues<GeoWpf.GpsDisplayFormat>();

    /// <summary>When true, the Points DataGrid shows Latitude and Longitude columns.</summary>
    private bool _showGpsColumnsInGrid = false;
    public bool ShowGpsColumnsInGrid
    {
        get => _showGpsColumnsInGrid;
        set => SetField(ref _showGpsColumnsInGrid, value);
    }

    /// <summary>Context-menu command: shows GPS lat/lon popup for the selected point.</summary>
    public System.Windows.Input.ICommand ShowGpsCoordinatesCommand { get; private set; }
        = new RelayCommand(_ => { }); // initialized in InitGpsCommand()

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

    public double MinimumBoundaryArea
    {
        get => _context.MinimumBoundaryArea;
        set
        {
            if (_context.MinimumBoundaryArea != value)
            {
                _context.MinimumBoundaryArea = value;
                OnPropertyChanged();
                RefreshData(false);
            }
        }
    }

    // ── JEA Settings ──────────────────────────────────────────────────────
    private string _jeaTemplatePath = string.Empty;
    public string JeaTemplatePath
    {
        get => _jeaTemplatePath;
        set { _jeaTemplatePath = value ?? string.Empty; OnPropertyChanged(); }
    }

    // ── JEA Validation Badge ────────────────────────────────────────────────
    private int _jeaValidationIssueCount;
    /// <summary>
    /// Total issue count (errors + warnings + info) from the last JEA validation
    /// run. Shown as a badge on the Validate JEA menu item. 0 = never run / clear.
    /// </summary>
    public int JeaValidationIssueCount
    {
        get => _jeaValidationIssueCount;
        set
        {
            if (SetField(ref _jeaValidationIssueCount, value))
            {
                OnPropertyChanged(nameof(JeaValidationBadgeText));
                OnPropertyChanged(nameof(JeaValidationBadgeVisible));
            }
        }
    }

    private int _jeaValidationErrorCount;
    /// <summary>Error-only subset of the last validation run.</summary>
    public int JeaValidationErrorCount
    {
        get => _jeaValidationErrorCount;
        set
        {
            if (SetField(ref _jeaValidationErrorCount, value))
                OnPropertyChanged(nameof(JeaValidationBadgeText));
        }
    }

    /// <summary>Human-readable badge label shown on the Validate menu item.</summary>
    public string JeaValidationBadgeText =>
        _jeaValidationIssueCount == 0 ? ""
        : _jeaValidationErrorCount > 0
            ? $"  ⛔ {_jeaValidationErrorCount} error{(_jeaValidationErrorCount == 1 ? "" : "s")}"
            : $"  ⚠ {_jeaValidationIssueCount} issue{(_jeaValidationIssueCount == 1 ? "" : "s")}";

    /// <summary>True when a validation has been run and found at least one issue.</summary>
    public bool JeaValidationBadgeVisible => _jeaValidationIssueCount > 0;

    private string _cogoScriptDefaultSavePath = string.Empty;
    public string CogoScriptDefaultSavePath
    {
        get => _cogoScriptDefaultSavePath;
        set { _cogoScriptDefaultSavePath = value ?? string.Empty; OnPropertyChanged(); }
    }

    private string _rcsBlocksPath = string.Empty;
    /// <summary>User-overridable path to the RCS_Blocks .dwg library folder.
    /// When empty, EditCogoCodeWindow auto-detects by walking up the directory tree.</summary>
    public string RcsBlocksPath
    {
        get => _rcsBlocksPath;
        set
        {
            _rcsBlocksPath = value ?? string.Empty;
            OnPropertyChanged();
            // Push the override into the shared static resolver immediately
            if (!string.IsNullOrWhiteSpace(value) && System.IO.Directory.Exists(value))
                RCS.Cogo.Wpf.Views.EditCogoCodeWindow.OverrideBlocksDirectory(value);
        }
    }

    private string _jeaStatePlaneZone = "Florida East (EPSG:2236)";
    public string JeaStatePlaneZone
    {
        get => _jeaStatePlaneZone;
        set
        {
            _jeaStatePlaneZone = value ?? string.Empty;
            OnPropertyChanged();
            // Propagate to PointViewModel so GPS DataGrid columns re-project immediately
            PointViewModel.ActiveZone = RCS.Geo.Core.StatePlaneProjection.NormalizeZone(value);
            foreach (var pt in Points)
                pt.RefreshGpsDisplay();
        }
    }

    public System.Collections.ObjectModel.ObservableCollection<string> AvailableStatePlaneZones { get; } =
        new(new[]
        {
            "Florida East (EPSG:2236)",
            "Florida West (EPSG:2237)",
            "Florida North (EPSG:2238)",
        });

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

    public bool ShowVerticalAlignmentLabels
    {
        get => _context.ShowVerticalAlignmentLabels;
        set { _context.ShowVerticalAlignmentLabels = value; OnPropertyChanged(); RefreshData(false); }
    }

    public bool ShowVPIs
    {
        get => _context.ShowVPIs;
        set { _context.ShowVPIs = value; OnPropertyChanged(); RefreshData(false); }
    }

    public bool ShowGradePercent
    {
        get => _context.ShowGradePercent;
        set { _context.ShowGradePercent = value; OnPropertyChanged(); RefreshData(false); }
    }

    private bool _showFigureLabels = true;
    public bool ShowFigureLabels
    {
        get => _showFigureLabels;
        set => SetField(ref _showFigureLabels, value);
    }

    // ── Item 6: Asset ID label toggle ─────────────────────────────────────────
    private bool _showAssetLabels = true;
    public bool ShowAssetLabels
    {
        get => _showAssetLabels;
        set => SetField(ref _showAssetLabels, value);
    }

    // ── Commands (Items 4 & 7) ──────────────────────────────────────────
    public System.Windows.Input.ICommand ExportJeaCogoScriptCommand { get; private set; } = new RelayCommand(_ => {});
    public System.Windows.Input.ICommand SaveInspectorCommand        { get; private set; } = new RelayCommand(_ => {});

    /// <summary>Resets the Points tab quick-filter to show all points.</summary>
    public System.Windows.Input.ICommand ClearPointsFilterCommand =>
        new RelayCommand(_ => PointsFilter = string.Empty);

    // ── Installed Assets Quick-Filter ─────────────────────────────────────────
    private string _assetsFilter = string.Empty;
    /// <summary>
    /// Bound to the Installed Assets tab search TextBox. Filters the
    /// InstalledAssetsView code-behind via the AssetsFilterChanged event.
    /// </summary>
    public string AssetsFilter
    {
        get => _assetsFilter;
        set
        {
            if (SetField(ref _assetsFilter, value ?? string.Empty))
                AssetsFilterChanged?.Invoke(this, _assetsFilter);
        }
    }

    /// <summary>Raised when AssetsFilter changes; the View subscribes to apply filtering.</summary>
    public event EventHandler<string>? AssetsFilterChanged;

    /// <summary>Resets the Installed Assets tab quick-filter.</summary>
    public System.Windows.Input.ICommand ClearAssetsFilterCommand =>
        new RelayCommand(_ => AssetsFilter = string.Empty);

    private bool _isRunningScript;
    public bool IsRunningScript
    {
        get => _isRunningScript;
        set => SetField(ref _isRunningScript, value);
    }

    public GeoWpf.CoordinateTransformViewModel CoordinateTransformVm { get; }

    public System.Windows.Input.ICommand FindCrossingsCommand { get; }
    public System.Windows.Input.ICommand OpenPapCommand { get; }
    public System.Windows.Input.ICommand OpenTablesCommand { get; }
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
    public event EventHandler<double[]>? ViewRestoreRequested;

    public ShellViewModel()
    {
        var registry = AppInitializer.InitializeRegistry();
        
        // ── Points CollectionView (enables DataGrid column-header sorting) ──
        PointsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Points);
        PointsView.SortDescriptions.Add(
            new System.ComponentModel.SortDescription(
                nameof(PointViewModel.NumericId),
                System.ComponentModel.ListSortDirection.Ascending));
        // Live filter: matches Point Number (Id) or Description
        PointsView.Filter = o =>
        {
            if (o is not PointViewModel pt) return false;
            if (string.IsNullOrWhiteSpace(_pointsFilter)) return true;
            return (pt.Id?.Contains(_pointsFilter, StringComparison.OrdinalIgnoreCase) == true)
                || (pt.Description?.Contains(_pointsFilter, StringComparison.OrdinalIgnoreCase) == true);
        };

        var staticCrsRegistry = new StaticCrsRegistry();
        var projNetTransform = new ProjNetCoordinateTransformService(staticCrsRegistry);
        CoordinateTransformVm = new GeoWpf.CoordinateTransformViewModel(projNetTransform);

        // ── Bulk transform — applies SP⇔LatLon to all context COGO points ──
        CoordinateTransformVm.BulkApplyAction = (direction, _crsId) =>
        {
            // Resolve the active zone from the JEA/Project setting
            string zone = RCS.Geo.Core.StatePlaneProjection.NormalizeZone(JeaStatePlaneZone);
            int count = 0;
            foreach (var pt in _context.GetAllPoints())
            {
                var p = _context.GetPoint(pt.Id);
                if (p == null) continue;
                string desc = pt.Description ?? string.Empty;
                if (direction == GeoWpf.TransformDirection.StatePlaneToLatLon)
                {
                    var (lat, lon) = RCS.Geo.Core.StatePlaneProjection.ToLatLon(p.Easting, p.Northing, zone);
                    _context.AddPoint(pt.Id, new RCS.Cogo.Core.Primitives.Point3D(lat, lon, p.Elevation), desc);
                }
                else
                {
                    var (eft, nft) = RCS.Geo.Core.StatePlaneProjection.ToStatePlane(p.Northing, p.Easting, zone);
                    _context.AddPoint(pt.Id, new RCS.Cogo.Core.Primitives.Point3D(nft, eft, p.Elevation), desc);
                }
                count++;
            }
            System.Windows.MessageBox.Show(
                $"Bulk transform complete ({zone}).\n{count} point(s) updated.",
                "GPS Transform", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            OnPropertyChanged(nameof(Points));
        };

        // ── GPS CSV import ──────────────────────────────────────────────────
        CoordinateTransformVm.ImportGpsCsvAction = filePath =>
        {
            try
            {
                string zone    = RCS.Geo.Core.StatePlaneProjection.NormalizeZone(JeaStatePlaneZone);
                var records = RCS.Piping.Core.IO.GpsCsvIo.Import(filePath);
                foreach (var rec in records)
                {
                    var (eft, nft) = RCS.Geo.Core.StatePlaneProjection.ToStatePlane(rec.Latitude, rec.Longitude, zone);
                    _context.AddPoint(rec.PointId, new RCS.Cogo.Core.Primitives.Point3D(nft, eft, rec.Elevation), rec.Description ?? string.Empty);
                }
                ResultLogText += $"GPS Import: {records.Count} point(s) loaded from {System.IO.Path.GetFileName(filePath)} [{zone}]{Environment.NewLine}";
                OnPropertyChanged(nameof(Points));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Import error: {ex.Message}", "GPS Import",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        };

        // ── GPS CSV/TXT export ──────────────────────────────────────────────
        CoordinateTransformVm.ExportGpsAction = (outputPath, fullCsv) =>
        {
            try
            {
                var allPts = _context.GetAllPoints().ToList();
                var rows   = allPts.Select(pt =>
                {
                    var p3d = _context.GetPoint(pt.Id);
                    return new RCS.Piping.Core.Workflow.PointRow
                    {
                        PointId     = pt.Id,
                        Northing    = p3d?.Northing  ?? 0,
                        Easting     = p3d?.Easting   ?? 0,
                        Elevation   = p3d?.Elevation ?? 0,
                        Description = pt.Description ?? string.Empty
                    };
                }).ToList();

                var tempJob = new RCS.Piping.Core.Workflow.AsBuiltJob
                {
                    Identity  = new RCS.Piping.Core.Workflow.ProjectIdentity { JobNumber = "COGO-Export" },
                    PointRows = new System.Collections.ObjectModel.ObservableCollection
                        <RCS.Piping.Core.Workflow.PointRow>(rows)
                };

                bool useDms = GpsCoordinateFormat == GeoWpf.GpsDisplayFormat.DMS;
                string zone  = RCS.Geo.Core.StatePlaneProjection.NormalizeZone(JeaStatePlaneZone);
                if (fullCsv)
                    RCS.Piping.Core.IO.GpsCsvIo.ExportFullCsv(tempJob, outputPath, useDms, zone);
                else
                    RCS.Piping.Core.IO.GpsCsvIo.ExportLatLonTxt(tempJob, outputPath, useDms, zone);

                ResultLogText += $"GPS Export: {rows.Count} point(s) → {System.IO.Path.GetFileName(outputPath)}{Environment.NewLine}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "GPS Export",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        };

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
             OpenHelpWindowAction = (commands) =>
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                 {
                     var win = new RCS.Cogo.Wpf.Views.HelpCommandsWindow(commands);
                     win.Show();
                 });
             },
             SaveHorizontalAlignmentAction = (name, desc) => 
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(async () => {
                     var ha = new RCS.Data.Entities.Figure { Name = name, DescriptionText = desc, ScriptContent = this.BatchScriptContent, Layer = "Horizontal_Align" };
                     await InstalledAssets.AddItemAsync(ha);
                 });
             },
             SaveProfileAlignmentAction = (name, desc) => 
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(async () => {
                     var pa = new RCS.Data.Entities.Figure { Name = name, DescriptionText = desc, ScriptContent = this.BatchScriptContent, Layer = "Vertical_Align" };
                     await InstalledAssets.AddItemAsync(pa);
                 });
             },
             SyncPointsAction = () => 
             {
                 System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                     var proj = CurrentProject;
                     if (proj == null) return;
                     
                     // Explicit point mapping for LiteDb — skip any entries with a null Point3D
                     // (null Point3D would silently produce 0,0,0 coordinates in the DB)
                     // The .Where(p => p.Point != null) guard above ensures p.Point is non-null
                     // inside the Select, but the compiler cannot track nullability across lambdas.
#pragma warning disable CS8602
                     var mappedPoints = _context.GetAllPoints()
                         .Where(p => p.Point != null)
                         .Select(p => new RCS.Cogo.App.Models.PointEntry 
                         {
                             Id = p.Id ?? "",
                             Northing = p.Point.Northing,
                             Easting = p.Point.Easting,
                             Elevation = p.Point.Elevation,
                             Description = p.Description ?? ""
                         }).ToList();
#pragma warning restore CS8602
                     proj.Points = mappedPoints;

                     // Ensure SQLite is completely synced at the moment of Save as well
                     try
                     {
                         using (var db = new RCS.Data.AppDbContext())
                         {
                             var projIdString = proj.Id.ToString();
                             var newSurveyPoints = proj.Points.Select(p => new RCS.Data.Entities.SurveyPoint 
                             {
                                 Id = $"{projIdString}_{p.Id}", 
                                 PointNumber = p.Id,
                                 ProjectId = projIdString, 
                                 Northing = p.Northing, Easting = p.Easting, Elevation = p.Elevation, Description = p.Description 
                             }).ToList();

                             var existing = db.SurveyPoints.Where(p => p.ProjectId == projIdString).Select(p => p.Id).ToList();
                             var toInsert = newSurveyPoints.Where(p => !existing.Contains(p.Id)).ToList();
                             if (toInsert.Any())
                             {
                                 db.SurveyPoints.AddRange(toInsert);
                                 db.SaveChanges();
                             }
                         }
                     }
                     catch (Exception dbEx)
                     {
                         string inner = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                         _context.Log($"[AUDIT] Warning: Could not sync points via CancellationToken DB: {dbEx.Message}. Inner: {inner}");
                     }

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
                    CogoCodes.Add(new CogoCode(c.LocalCode, c.SystemCode, c.Description, c.Block ?? "", c.BlockScale));
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
            if (double.TryParse(RCS.Services.GlobalSettingsService.GetSetting("MinimumBoundaryArea", "100.0"), out double mba)) MinimumBoundaryArea = mba;
            // ── JEA settings ────────────────────────────────────────────────
            JeaTemplatePath   = RCS.Services.GlobalSettingsService.GetSetting("JeaTemplatePath",   string.Empty);
            JeaStatePlaneZone = RCS.Services.GlobalSettingsService.GetSetting("JeaStatePlaneZone", "Florida East (EPSG:2236)");
            // Initialise PointViewModel static so GPS columns are correct from the first load
            PointViewModel.ActiveZone = RCS.Geo.Core.StatePlaneProjection.NormalizeZone(JeaStatePlaneZone);
            // ── DXF Blocks Library ───────────────────────────────────────────
            RcsBlocksPath = RCS.Services.GlobalSettingsService.GetSetting("RcsBlocksPath", string.Empty);
            
            // ── Script Auto-Save Path ────────────────────────────────────────
            CogoScriptDefaultSavePath = RCS.Services.GlobalSettingsService.GetSetting("CogoScriptDefaultSavePath", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            // ── GPS coordinate display format ────────────────────────────────────────────
            var _gpsFmtStr = RCS.Services.GlobalSettingsService.GetSetting("GpsCoordinateFormat", nameof(RCS.Geo.Wpf.ViewModels.GpsDisplayFormat.DecimalDegrees));
            if (System.Enum.TryParse<RCS.Geo.Wpf.ViewModels.GpsDisplayFormat>(_gpsFmtStr, out var _gpsFmt))
                _gpsCoordinateFormat = _gpsFmt;   // set backing field directly to avoid null-ref on CoordinateTransformVm before it is wired
            if (bool.TryParse(RCS.Services.GlobalSettingsService.GetSetting("ShowGpsColumnsInGrid", "False"), out bool sgc))
                ShowGpsColumnsInGrid = sgc;
            // ── GPS Transform session state (Direction + Zone) ──────────────────────
            var dirStr = RCS.Services.GlobalSettingsService.GetSetting("GpsTransformDirection", nameof(GeoWpf.TransformDirection.StatePlaneToLatLon));
            if (System.Enum.TryParse<GeoWpf.TransformDirection>(dirStr, out var savedDir))
                CoordinateTransformVm.Direction = savedDir;
            var savedCrsId = RCS.Services.GlobalSettingsService.GetSetting("GpsTransformCrsId", "EPSG:6438");
            var savedCrs   = CoordinateTransformVm.AvailableCrs.FirstOrDefault(c => c.CrsId == savedCrsId);
            if (savedCrs != null)
                CoordinateTransformVm.SelectedSourceCrs = savedCrs;
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

        // ── GPS per-point coordinate viewer ──────────────────────────────────
        ShowGpsCoordinatesCommand = new RelayCommand(param =>
        {
            if (param is not PointViewModel pt) return;
            bool useDms = GpsCoordinateFormat == GeoWpf.GpsDisplayFormat.DMS;
            var (lat, lon) = RCS.Geo.Core.StatePlaneProjection.ToLatLon(pt.Easting, pt.Northing);
            string latStr = useDms
                ? RCS.Geo.Core.StatePlaneProjection.ToDms(lat, isLatitude: true)
                : $"{lat:F7}\u00b0";
            string lonStr = useDms
                ? RCS.Geo.Core.StatePlaneProjection.ToDms(lon, isLatitude: false)
                : $"{lon:F7}\u00b0";
            string msg =
                $"Point:      {pt.Id}\n" +
                $"Latitude:   {latStr}\n" +
                $"Longitude:  {lonStr}\n\n" +
                $"Northing:   {pt.Northing:F3} ft\n" +
                $"Easting:    {pt.Easting:F3} ft\n" +
                $"Elevation:  {pt.Elevation:F3} ft\n\n" +
                $"Decimal coords copied to clipboard: {lat:F7}, {lon:F7}";
            System.Windows.Clipboard.SetText($"{lat:F7}, {lon:F7}");
            System.Windows.MessageBox.Show(msg, $"GPS Coordinates \u2014 Pt {pt.Id}",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        });

        ZoomInCommand = new RelayCommand(_ => ZoomInRequested?.Invoke(this, EventArgs.Empty));
        ZoomOutCommand = new RelayCommand(_ => ZoomOutRequested?.Invoke(this, EventArgs.Empty));
        ZoomExtentsCommand = new RelayCommand(_ => ZoomExtentsRequested?.Invoke(this, EventArgs.Empty));
        ZoomWindowCommand = new RelayCommand(_ => ZoomWindowRequested?.Invoke(this, EventArgs.Empty));
        ZoomToImportedPointCommand = new RelayCommand(obj =>
        {
            if (obj is PointViewModel pt)
            {
                // Clear previous selection
                foreach (var p in Points) p.IsSelected = false;
                // Highlight the clicked point
                pt.IsSelected = true;

                var zoomTarget = new System.Windows.Point(pt.Easting, pt.Northing);
                ZoomToPointRequested?.Invoke(this, zoomTarget);
            }
        });
        ExportDxfCommand = new RelayCommand(_ => ExportDxf());
        ExportJeaCogoScriptCommand = new RelayCommand(_ => ExportJeaCogoScript());
        SaveInspectorCommand = new RelayCommand(async _ => await SaveInspectorAsync());

        ImportDxfCommand = new RelayCommand(_ => ImportDxfLinework());
        ExportBomCommand = new RelayCommand(_ => ExportBom());
        ExportEpanetCommand = new RelayCommand(_ => ExportEpanet());
        ExportScheduleCommand = new RelayCommand(_ => ExportSchedule());
        
        ExportScriptCommand = new RelayCommand(_ => ExportScript());
        AnalyzeScriptCommand = new RelayCommand(_ => AnalyzeScript());
        ExportOutputLogCommand = new RelayCommand(_ => ExportOutputLog());
        ExportPointsTxtCommand = new RelayCommand(_ => ExportPointsTxt());
        ExportPointsXmlCommand = new RelayCommand(_ => ExportPointsXml());
        SavePointsCommand = new RelayCommand(_ => SavePoints());
        EditPointsCommand = new RelayCommand(_ => EditPoints());
        RefreshPointsCommand = new RelayCommand(_ => RefreshPointsValidation());
        SaveFiguresCommand = new RelayCommand(_ => SaveFigures());
        SyncToAssetsCommand = new RelayCommand(_ => SyncAssets());
        FindCrossingsCommand = new RelayCommand(_ => FindCrossings());
        OpenPapCommand = new RelayCommand(_ => new Views.PointsAlongPipeWindow(this) { Owner = App.Current.MainWindow }.ShowDialog());

        OpenTablesCommand = new RelayCommand(param =>
        {
            if (!EnsureActiveProject()) return;
            int.TryParse(param?.ToString(), out int tabIndex);
            int tab = tabIndex < 0 ? 0 : tabIndex;
            var win = new Views.InstalledAssetsTablesWindow(CurrentProject.Id.ToString(), tab)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            win.TxtProjectLabel.Text = $"Project: {CurrentProject.ProjectName}  |  ID: {CurrentProject.Id}";
            win.Show();
        });


        
        // Report Commands
        ReportWaterCommand    = new RelayCommand(_ => ExportDisciplineReport("Water",           "Water_Report.xlsx",    RCS.Cogo.Wpf.Services.DisciplineReportService.ExportWater));
        ReportSewerCommand    = new RelayCommand(_ => ExportDisciplineReport("Sanitary Sewer",  "Sewer_Report.xlsx",    RCS.Cogo.Wpf.Services.DisciplineReportService.ExportSewer));
        ReportGasCommand      = new RelayCommand(_ => ExportDisciplineReport("Gas",             "Gas_Report.xlsx",      RCS.Cogo.Wpf.Services.DisciplineReportService.ExportGas));
        ReportElectricCommand = new RelayCommand(_ => ExportDisciplineReport("Electric",        "Electric_Report.xlsx", RCS.Cogo.Wpf.Services.DisciplineReportService.ExportElectric));
        ReportDrainageCommand = new RelayCommand(_ => ExportDisciplineReport("Storm Drainage",  "Drainage_Report.xlsx", RCS.Cogo.Wpf.Services.DisciplineReportService.ExportDrainage));
        ReportAllAssetsCsvCommand = new RelayCommand(_ => ExportAllAssets("csv"));
        ReportAllAssetsTxtCommand = new RelayCommand(_ => ExportAllAssets("txt"));
        ReportAllAssetsXlsCommand = new RelayCommand(_ => ExportAllAssets("xls"));
        
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
        EditCogoCodeCommand = new RelayCommand(_ => EditCogoCode());
        SaveCodesCommand        = new RelayCommand(_ => SaveCodes());
        AutoMatchBlocksCommand  = new RelayCommand(_ => AutoMatchBlocks());
        
        ImportCatalogCommand = new RelayCommand(_ => ImportCatalog());
        AddMaterialToProjectCommand = new RelayCommand(_ => AddMaterialToProject());
        ExportMaterialsCommand = new RelayCommand(_ => ExportMaterials());
        
        ProcessPipingScriptCommand = new RelayCommand(_ => ProcessPipingScript());
        ImportPipingScriptCommand = new RelayCommand(_ => ImportPipingScript());
        ExportPipingScriptCommand = new RelayCommand(_ => ExportPipingScript());
        AnalyzePipingScriptCommand = new RelayCommand(_ => AnalyzePipingScript());
        OpenAiChatCommand = new RelayCommand(_ => OpenAiChat());
        
        ImportPointsListCommand = new RelayCommand(_ => ImportPointsList());

        NewProjectCommand    = new RelayCommand(_ => NewProject());
        EditProjectCommand   = new RelayCommand(_ => EditProject());
        SaveProjectCommand   = new RelayCommand(_ => SaveProject());
        SaveProjectAsCommand = new RelayCommand(_ => SaveProjectAs());
        OpenProjectCommand   = new RelayCommand(_ => OpenProject());
        CloseProjectCommand  = new RelayCommand(_ => CloseProject());
        OpenRecentFileCommand = new RelayCommand(param => OpenRecentFile(param));
        LoadRecentFiles();
        UpdateWindowTitle();
        OpenReportSettingsCommand = new RelayCommand(_ => OpenReportSettings());
        
        CompactDbCommand = new RelayCommand(_ => CompactDatabase());
        VerifyDbCommand = new RelayCommand(_ => VerifyDatabase());
        RepairDbCommand = new RelayCommand(_ => RepairDatabase());
        ExportDbCsvCommand               = new RelayCommand(_ => ExportDatabaseCsv());
        ExportInstalledAssetsCommand     = new RelayCommand(_ => ExportInstalledAssets());
        ExportJeaTemplateCommand         = new RelayCommand(_ => ExportJeaTemplate());
        ExportJeaMixScriptCommand        = new RelayCommand(_ => ExportJeaMixScript());
        GenerateJeaLineworkCommand       = new RelayCommand(_ => GenerateJeaLinework());
        ValidateJeaCommand               = new RelayCommand(_ => OpenJeaValidation());
        ImportJeaTemplateCommand         = new RelayCommand(_ => ImportJeaFromTemplate());
        ImportS1AProjectCommand          = new RelayCommand(_ => ImportS1AProjectFromExcel());

        CloseCommand = new RelayCommand(_ => System.Windows.Application.Current.Shutdown());
        OpenErrorReportCommand = new RelayCommand(_ => new Views.ErrorReportWindow() { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog());
        AboutCommand = new RelayCommand(_ =>
        {
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string verStr = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "2.0";
            System.Windows.MessageBox.Show(
                $"RCS COGO Enterprise\nVersion {verStr}\n\nAdvanced Survey & Utility Data Platform",
                "About RCS COGO Enterprise",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        });
        OpenSurveyCommandsCommand = new RelayCommand(_ => OpenDocument("docs\\USER_GUIDE.md"));
        OpenPipeCommandsCommand = new RelayCommand(_ => OpenDocument("docs\\PIPING_MANUAL.md"));
        OpenManualCommand = new RelayCommand(_ => OpenDocument("USER_MANUAL_AND_TESTING_GUIDE.txt"));
        
        OpenExampleCogoCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Azimuth_Script_Example.txt"));
        OpenExampleBearingCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Bearing_Script_Example.txt"));
        OpenExamplePipeCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Pipe_Script_Example.txt"));
        OpenExampleMixCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Mix_Script_Example.txt"));
        OpenExampleFiguresCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Cogo_Figures_Example.txt"));
        OpenExampleAngleCommand = new RelayCommand(_ => OpenDocument("docs\\examples\\Angle_Script_Example.txt"));

        OpenTutorial01Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T01_Coordinates_and_Inverse.txt"));
        OpenTutorial02Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T02_Traverse.txt"));
        OpenTutorial03Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T03_Rotation.txt"));
        OpenTutorial04Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T04_Translation.txt"));
        OpenTutorial05Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T05_Alignment_Profile_CrossSection.txt"));
        OpenTutorial06Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T06_Horizontal_Curves.txt"));
        OpenTutorial07Command = new RelayCommand(_ => OpenDocument("SampleScripts\\Survey\\T07_Full_Road_Design.txt"));

        // JEA end-to-end example project
        OpenExampleWalkthroughCommand = new RelayCommand(_ => OpenDocument("docs\\EXAMPLE_PROJECT_WALKTHROUGH.md"));
        LoadOakwoodExampleCommand     = new RelayCommand(_ => OpenDocument("SampleScripts\\JEA_Oakwood_WaterMain_70498-W1A.cogo"));
        OpenImportReferenceCommand    = new RelayCommand(_ => OpenDocument("docs\\DATA_IMPORT_REFERENCE.md"));

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
        OpenExtractionScriptsFolderCommand = new RelayCommand(_ =>
        {
            // Walk up to .sln root the same way OpenDocument does
            var baseDir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (baseDir != null &&
                   !System.IO.File.Exists(System.IO.Path.Combine(baseDir.FullName, "RCS.Cogo.Enterprise.Modern.sln")))
            {
                baseDir = baseDir.Parent;
            }
            string root = baseDir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = root,
                UseShellExecute = true
            });
        });
        
        InstalledAssets = new InstalledAssetsViewModel();
        InstalledAssets.LogAction = (msg) => CommandLog.Add(msg);
        InstalledAssets.AssetSelected += InstalledAssets_AssetSelected;
        OpenValidationSettingsCommand = new RelayCommand(_ => OpenValidationSettings());
        OpenGeneralSettingsCommand = new RelayCommand(_ => OpenGeneralSettings());
        OpenAlignmentWindowCommand = new RelayCommand(_ => OpenAlignmentWindow());
        OpenAlignmentSettingsCommand = new RelayCommand(_ => OpenAlignmentSettings());
        OpenFiguresWindowCommand = new RelayCommand(_ => OpenFiguresWindow());
        OpenPipeCharacteristicsCommand = new RelayCommand(_ => OpenPipeCharacteristics());
        OpenCrossSectionWindowCommand = new RelayCommand(_ => OpenCrossSectionWindow());

        // Load default/empty project
        _ = LoadInstalledAssetsAsync();

        // ── Production UX: Dashboard + Navigator wiring ──────────────────
        Dashboard.NewJobCommand     = new RelayCommand(_ => LaunchNewJobWizard());
        Dashboard.OpenJobCommand    = new RelayCommand(_ => OpenExistingJob());
        Dashboard.ImportDataCommand = new RelayCommand(_ => ImportDataIntoActiveJob());
        Dashboard.TemplatesCommand  = new RelayCommand(_ => TemplatesDialog());

        // Wire each navigator step's SelectCommand to update current phase
        foreach (var step in Navigator.Steps)
        {
            var capturedPhase = step.Phase;
            step.SelectCommand = new RelayCommand(_ =>
            {
                Navigator.CurrentPhase = capturedPhase;
                ShowDashboard = false;
            });
        }
    }

    // ── New Job Wizard → Intake → Load ───────────────────────────────────────────────

    private void TemplatesDialog()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var saveItem = new System.Windows.Controls.MenuItem { Header = "💾  Save Current Settings as Template..." };
        saveItem.Click += (_, __) =>
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter      = "RCS Job Template (*.rcst)|*.rcst",
                DefaultExt  = ".rcst",
                FileName    = "JobTemplate",
                Title       = "Save Job Template"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var template = new
                {
                    Version       = "1.0",
                    CreatedUtc    = DateTime.UtcNow,
                    JobName       = CurrentProject?.ProjectName ?? "",
                    UtilityTypes  = Figures.Select(f => f.Name).Distinct().ToList(),
                    CodesSnapshot = CogoCodes.Select(c => new { c.LocalCode, c.SystemCode, c.Description, c.Block, c.BlockScale }).ToList()
                };
                var json = System.Text.Json.JsonSerializer.Serialize(template,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(dlg.FileName, json);
                CommandLog.Add($"[TEMPLATE] Saved template to {dlg.FileName}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save template:\n{ex.Message}",
                    "Template Save Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        };

        var loadItem = new System.Windows.Controls.MenuItem { Header = "📂  Load Template (Restore Codes)..." };
        loadItem.Click += (_, __) =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter      = "RCS Job Template (*.rcst)|*.rcst|All Files (*.*)|*.*",
                DefaultExt  = ".rcst",
                Title       = "Open Job Template"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var json = System.IO.File.ReadAllText(dlg.FileName);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("CodesSnapshot", out var codesEl)) return;

                using var db = new RCS.Data.AppDbContext();
                CogoCodes.Clear();
                foreach (var codeEl in codesEl.EnumerateArray())
                {
                    string local = codeEl.GetProperty("LocalCode").GetString() ?? "";
                    string sys   = codeEl.GetProperty("SystemCode").GetString() ?? "";
                    string desc  = codeEl.TryGetProperty("Description", out var d) ? d.GetString() ?? sys : sys;
                    string block = codeEl.TryGetProperty("Block", out var b) ? b.GetString() ?? "" : "";
                    double scale = codeEl.TryGetProperty("BlockScale", out var s) ? s.GetDouble() : 1.0;

                    if (!db.CogoCodes.Any(e => e.SystemCode == sys))
                        db.CogoCodes.Add(new RCS.Data.Entities.CogoCodeEntity { LocalCode = local, SystemCode = sys, Description = desc, Block = block, BlockScale = scale });

                    CogoCodes.Add(new CogoCode(local, sys, desc, block, scale));
                }
                db.SaveChanges();
                CommandLog.Add($"[TEMPLATE] Loaded {CogoCodes.Count} codes from {System.IO.Path.GetFileName(dlg.FileName)}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load template:\n{ex.Message}",
                    "Template Load Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        };

        menu.Items.Add(saveItem);
        menu.Items.Add(loadItem);
        menu.IsOpen = true;
    }

    private void LaunchNewJobWizard()
    {
        var wizard = new RCS.Cogo.Wpf.Views.AsBuilt.NewJobWizardWindow();
        wizard.Owner = System.Windows.Application.Current.MainWindow;
        if (wizard.ShowDialog() != true || wizard.ResultJob is not { } job) return;

        if (job.PendingImportPaths.Count > 0)
        {
            var engine = new RCS.Piping.Core.Engines.IntakeAnalysisEngine();
            foreach (var filePath in job.PendingImportPaths)
            {
                var fileType = DetectFileType(filePath);
                var report   = engine.Analyze(filePath, fileType, job);
                Diagnostics.Audit($"Intake: {System.IO.Path.GetFileName(filePath)} → {report.Summary}");
            }
            job.PendingImportPaths.Clear();
        }

        ActivateJob(job, saveImmediately: true);
    }

    private void OpenExistingJob()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Open RCS As-Built Job",
            Filter = "RCS Job Files (*.rcsj)|*.rcsj|All Files (*.*)|*.*",
            InitialDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RCS.Cogo", "Jobs")
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var persistence = new RCS.Piping.Core.Services.JobPersistenceService();
            var job         = persistence.Load(dlg.FileName);
            Diagnostics.Audit($"Job opened: {dlg.FileName}");
            ActivateJob(job, saveImmediately: false);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not open job file:\n{ex.Message}",
                "Open Job", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ImportDataIntoActiveJob()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title    = "Import Field / Design Data",
            Filter   = "Supported Files|*.csv;*.txt;*.cogo;*.dxf;*.xlsx|CSV/PNEZD|*.csv;*.txt|COGO Script|*.cogo|DXF|*.dxf|Excel|*.xlsx",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        var job    = ActiveJob ?? new RCS.Piping.Core.Workflow.AsBuiltJob();
        var engine = new RCS.Piping.Core.Engines.IntakeAnalysisEngine();
        foreach (var file in dlg.FileNames)
        {
            var report = engine.Analyze(file, DetectFileType(file), job);
            Diagnostics.Audit($"Imported: {System.IO.Path.GetFileName(file)} → {report.Summary}");
        }

        if (ActiveJob == null) ActivateJob(job, saveImmediately: true);
        else { RefreshWorkflowState(); AutosaveActiveJob(); }
    }

    /// <summary>Shared activation path for New and Open flows.</summary>
    public void ActivateJob(RCS.Piping.Core.Workflow.AsBuiltJob job, bool saveImmediately)
    {
        ActiveJob     = job;
        ShowDashboard = false;
        RefreshWorkflowState();
        Diagnostics.Audit($"Job activated: {job.Identity.JobNumber} Rev {job.Identity.RevisionNumber}");

        var persistence = new RCS.Piping.Core.Services.JobPersistenceService();
        string filePath = saveImmediately ? persistence.Save(job) : RCS.Piping.Core.Services.JobPersistenceService.DefaultPath(job);
        if (saveImmediately) Diagnostics.Audit($"Job saved: {filePath}");

        Dashboard.PushRecentJob(job, filePath);
        StartAutosave(job);
    }

    // ── Autosave timer ────────────────────────────────────────────────────

    private System.Windows.Threading.DispatcherTimer? _autosaveTimer;

    private void StartAutosave(RCS.Piping.Core.Workflow.AsBuiltJob job)
    {
        _autosaveTimer?.Stop();
        _autosaveTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(90) };
        _autosaveTimer.Tick += (_, _) => AutosaveActiveJob();
        _autosaveTimer.Start();
    }

    public void AutosaveActiveJob()
    {
        if (ActiveJob == null) return;
        try { new RCS.Piping.Core.Services.JobPersistenceService().Autosave(ActiveJob); }
        catch { /* best-effort */ }
    }

    // ── File type detection ───────────────────────────────────────────────────

    private static RCS.Piping.Core.Engines.IntakeFileType DetectFileType(string path) =>
        System.IO.Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".cogo"            => RCS.Piping.Core.Engines.IntakeFileType.CogoScript,
            ".dxf"             => RCS.Piping.Core.Engines.IntakeFileType.Dxf,
            ".xlsx" or ".xls"  => RCS.Piping.Core.Engines.IntakeFileType.JeaExcel,
            _                  => RCS.Piping.Core.Engines.IntakeFileType.Pnezd
        };



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
             string pNum = "0000"; 
             if (!string.IsNullOrEmpty(_currentProject.ProjectName)) pNum = _currentProject.ProjectName;
             
             await InstalledAssets.LoadProjectAsync(_currentProject.Id.ToString(), pNum);

             // Re-render canvas now that JEA asset collections are populated.
             // RefreshData ran before this async load completed, so the canvas had empty
             // InstalledAssets collections. This second pass paints the actual symbols.
             RefreshData(true);  // zoom-to-extents so JEA symbols come into view
        }
    }

    private void InstalledAssets_AssetSelected(object? sender, RCS.Data.Entities.InstalledAsset asset)
    {
        if (asset == null) return;

        double n = 0, e = 0;
        bool found = false;
        
        var type = asset.GetType();
        
        // Handle Figure Type explicitly
        if (asset is RCS.Data.Entities.Figure fig)
        {
            if (fig.Vertices != null && fig.Vertices.Count > 0)
            {
                var firstVertex = fig.Vertices.OrderBy(v => v.OrderIndex).FirstOrDefault();
                if (firstVertex?.Point != null)
                {
                    n = firstVertex.Point.Northing;
                    e = firstVertex.Point.Easting;
                    found = true;
                }
            }
            
            // If the vertices aren't loaded in the memory graph, try parsing the Script Content
            if (!found && !string.IsNullOrWhiteSpace(fig.ScriptContent))
            {
                // Rudimentary attempt to find the first coordinate in a script
                var firstLine = fig.ScriptContent.Split('\n').FirstOrDefault(s => s.Contains("N") && s.Contains("E"));
                // Fallback implemented later if needed, but the primary way to draw a figure is the script content.
                // Or if it's already rendered on the screen, the ViewModel should have it!
                var figureVisual = Figures.FirstOrDefault(f => f.Name == fig.Name);
                if (figureVisual != null && figureVisual.Points.Count > 0)
                {
                    n = figureVisual.Points[0].Y; // Northing
                    e = figureVisual.Points[0].X; // Easting
                    found = true;
                }
            }
        }

        
        System.Reflection.PropertyInfo? nProp = null;
        System.Reflection.PropertyInfo? eProp = null;

        // Check standard Point features first
        if (!found)
        {
            nProp = type.GetProperty("Northing");
            eProp = type.GetProperty("Easting");
            
            if (nProp != null && eProp != null)
            {
                var nVal = nProp.GetValue(asset);
                var eVal = eProp.GetValue(asset);
                if (nVal != null && eVal != null)
                {
                    if (double.TryParse(nVal.ToString(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var nParsed) &&
                        double.TryParse(eVal.ToString(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var eParsed))
                    {
                        n = nParsed; e = eParsed; found = true;
                    }
                }
            }
        }
        
        // If not found (either properties missing, or values were null), try Line features (Pipes)
        if (!found)
        {
            nProp = type.GetProperty("NorthingStart");
            eProp = type.GetProperty("EastingStart");
            if (nProp != null && eProp != null)
            {
                var nVal = nProp.GetValue(asset);
                var eVal = eProp.GetValue(asset);
                if (nVal != null && eVal != null)
                {
                    if (double.TryParse(nVal.ToString(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var nParsed2) &&
                        double.TryParse(eVal.ToString(), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var eParsed2))
                    {
                        n = nParsed2; e = eParsed2; found = true;
                    }
                }
            }
        }
        
        System.Windows.Application.Current?.Dispatcher.Invoke(() => {
            string diag = $"[ASSET_SELECTED] {type.Name} - N:{n} E:{e} found:{found}. Properties (N:{type.GetProperty("Northing")!=null}, E:{type.GetProperty("Easting")!=null}, NStart:{type.GetProperty("NorthingStart")!=null})";
            if (IsOutputLogDescending)
                ResultLogText = diag + "\n" + ResultLogText;
            else
                ResultLogText += diag + "\n";
        });
        
        if (found)
        {
            ZoomToPointRequested?.Invoke(this, new System.Windows.Point(e, n));
            
            HighlightedAssets.Clear();
            HighlightedAssets.Add(new StructureViewModel(asset.Id, new Point3D(n, e), "Highlight"));
        }
        else
        {
            HighlightedAssets.Clear();
        }
    }

    public System.Windows.Input.ICommand NewProjectCommand { get; }
    public System.Windows.Input.ICommand EditProjectCommand { get; }
    public System.Windows.Input.ICommand SaveProjectCommand { get; }
    public System.Windows.Input.ICommand SaveProjectAsCommand { get; }
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

    public System.Windows.Input.ICommand OpenTutorial01Command { get; }
    public System.Windows.Input.ICommand OpenTutorial02Command { get; }
    public System.Windows.Input.ICommand OpenTutorial03Command { get; }
    public System.Windows.Input.ICommand OpenTutorial04Command { get; }
    public System.Windows.Input.ICommand OpenTutorial05Command { get; }
    public System.Windows.Input.ICommand OpenTutorial06Command { get; }
    public System.Windows.Input.ICommand OpenTutorial07Command { get; }
    public System.Windows.Input.ICommand OpenExampleWalkthroughCommand { get; }
    public System.Windows.Input.ICommand LoadOakwoodExampleCommand { get; }
    public System.Windows.Input.ICommand OpenImportReferenceCommand { get; }
    
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

    // Civil 3D Extraction Tools
    public System.Windows.Input.ICommand OpenExtractionScriptsFolderCommand { get; }

    public System.Windows.Input.ICommand OpenAlignmentWindowCommand { get; }
    public System.Windows.Input.ICommand OpenAlignmentSettingsCommand { get; }
    public System.Windows.Input.ICommand OpenFiguresWindowCommand { get; }
    public System.Windows.Input.ICommand OpenPipeCharacteristicsCommand { get; }
    public System.Windows.Input.ICommand OpenCrossSectionWindowCommand { get; }


    public System.Windows.Input.ICommand CloseCommand { get; }
    public System.Windows.Input.ICommand AboutCommand { get; }
    public System.Windows.Input.ICommand OpenErrorReportCommand { get; }

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
            // ── 1. Developer mode: walk up from BaseDirectory looking for the .sln ──
            var baseDir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (baseDir != null &&
                   !System.IO.File.Exists(System.IO.Path.Combine(baseDir.FullName, "RCS.Cogo.Enterprise.Modern.sln")))
            {
                baseDir = baseDir.Parent;
            }

            // ── 2. Installed / fallback: use the EXE directory ────────────────────
            string root = baseDir?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = System.IO.Path.Combine(root, relativePath);

            // ── 3. If still not found, try BaseDirectory directly (publish layout) ─
            if (!System.IO.File.Exists(fullPath))
            {
                string publishPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (System.IO.File.Exists(publishPath))
                    fullPath = publishPath;
            }

            if (!System.IO.File.Exists(fullPath))
            {
                CommandLog.Add($"[WARN] Document not found: {fullPath}");
                System.Windows.MessageBox.Show(
                    $"Could not find the documentation file:\n\n{relativePath}\n\nIt may be missing from this installation.",
                    "File Not Found",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            CommandLog.Add($"[INFO] Opening: {fullPath}");
            var p = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = fullPath
                }
            };
            p.Start();
        }
        catch (Exception ex)
        {
            CommandLog.Add($"Error opening document: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Could not open documentation file:\n{relativePath}\n\n{ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
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
    public System.Windows.Input.ICommand SavePointsCommand { get; }
    public System.Windows.Input.ICommand EditPointsCommand { get; }
    public System.Windows.Input.ICommand RefreshPointsCommand { get; }
    public System.Windows.Input.ICommand SaveFiguresCommand { get; }

    private void RefreshPointsValidation()
    {
        RefreshData(false); // Refresh Data method already rebuilds the Points view models with the current valid codes
        _context.Log("[AUDIT] Points explicitly validated against Code Database via Refresh.");
    }

    private void EditPoints()
    {
        var validCodes = CogoCodes.Select(c => c.LocalCode).ToList();
        var win = new RCS.Cogo.Wpf.Views.EditPointsWindow(Points, validCodes)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
        
        // Push modifications mapping back to Context
        foreach(var pt in Points)
        {
            var p3d = new RCS.Cogo.Core.Primitives.Point3D(pt.Northing, pt.Easting, pt.Elevation);
            _context.AddPoint(pt.Id, p3d, pt.Description);
        }
        _context.Log("[AUDIT] Points explicitly edited via visual grid.");
    }

    private void SavePoints()
    {
        if (string.IsNullOrEmpty(_currentDbPath))
        {
            SaveProject(); // Prompt user if no project exists
        }
        else
        {
            _context.SyncPointsAction?.Invoke();
            _context.Log("[AUDIT] Points explicitly saved to database via Quick Save.");
            System.Windows.MessageBox.Show("Points explicitly saved to database.", "Save Points", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    private void SaveFigures()
    {
        if (string.IsNullOrEmpty(_currentDbPath))
        {
            System.Windows.MessageBox.Show("Please save your project first to create a database to store the figures.", "Project Not Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        System.Windows.Application.Current?.Dispatcher.Invoke(async () => 
        {
            try
            {
                _context.SyncPointsAction?.Invoke(); // Make sure new script points get recorded
                int count = 0;
                int updated = 0;
                var allPtsDict = _context.GetAllPoints().ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);

                foreach (var memFig in _context.GetAllFigures())
                {
                    bool isClosed = memFig.PointIds.Count > 2 && memFig.PointIds.First() == memFig.PointIds.Last();
                    
                    if (isClosed)
                    {
                        var pts = new System.Collections.Generic.List<Point3D>();
                        foreach (var id in memFig.PointIds)
                        {
                            var p = _context.GetPoint(id);
                            if (p != null) pts.Add(p);
                        }
                        double areaSum = 0;
                        for (int i = 0; i < pts.Count; i++)
                        {
                            var p1 = pts[i];
                            var p2 = pts[(i + 1) % pts.Count];
                            areaSum += (p1.Easting * p2.Northing) - (p2.Easting * p1.Northing);
                        }
                        double area = Math.Abs(areaSum) * 0.5;
                        if (area < MinimumBoundaryArea) 
                        {
                            _context.Log($"[DEBUG] Figure '{memFig.Name}' area ({area}) is less than MinimumBoundaryArea ({MinimumBoundaryArea}). Saving anyway.");
                            // continue; // Ignore parcels smaller than minimum
                        }
                    }

                    var existingFig = InstalledAssets.FigureAssets.FirstOrDefault(f => string.Equals(f.Name, memFig.Name, StringComparison.OrdinalIgnoreCase));
                    var newFig = existingFig ?? new RCS.Data.Entities.Figure 
                    { 
                         Name = memFig.Name, 
                         Layer = "Geometry",
                         PartKey = $"FIG-{Guid.NewGuid().ToString().Substring(0, 5)}",
                         ProjectId = _currentProject?.Id.ToString() ?? "",
                         DescriptionText = "Auto-saved figure from Script", 
                         ScriptContent = this.BatchScriptContent
                    };

                    if (string.IsNullOrEmpty(newFig.Subtype))
                    {
                        var upperName = memFig.Name.ToUpper();
                        if (upperName.Contains("LOT")) newFig.Subtype = "Lot";
                        else if (upperName.Contains("BLDG") || upperName.Contains("BUILDING")) newFig.Subtype = "Building";
                        else if (upperName.Contains("EDGE") || upperName.Contains("EOP") || upperName.Contains("EP") || upperName.Contains("ROAD")) newFig.Subtype = "Edge of Pavement";
                        else newFig.Subtype = "Linework";
                    }
                    
                    newFig.IsClosed = isClosed;
                    newFig.UpdatedUtc = DateTime.UtcNow;
                    
                    newFig.Vertices.Clear();
                    
                    int vIdx = 0;
                    foreach (var pid in memFig.PointIds)
                    {
                        if (allPtsDict.TryGetValue(pid, out var pData))
                        {
                            var survPt = pData.Point;
                            newFig.Vertices.Add(new RCS.Data.Entities.FigureVertex 
                            {
                                PointId = $"{_currentProject?.Id.ToString() ?? ""}_{pid}",
                                OrderIndex = vIdx++
                            });
                        }
                    }

                    if (existingFig == null)
                    {
                        newFig.CreatedUtc = DateTime.UtcNow;
                        await InstalledAssets.AddItemAsync(newFig);
                        count++;
                    }
                    else
                    {
                        await InstalledAssets.SaveItemAsync(newFig);
                        updated++;
                    }
                }
                
                _context.Log($"[AUDIT] Saved {count} figures to project database.");
                System.Windows.MessageBox.Show($"Successfully synced {count} active figures straight to the 'Geometry' layer in the Master Database.", "Save Figures", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _context.Log($"[AUDIT] Error saving figures: {ex.Message}");
                System.Windows.MessageBox.Show($"Error saving figures: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        });
    }

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
    
    private CogoCode? _selectedCogoCode;
    public CogoCode? SelectedCogoCode { get => _selectedCogoCode; set => SetField(ref _selectedCogoCode, value); }
    
    public System.Windows.Input.ICommand SearchCodesCommand { get; }
    public System.Windows.Input.ICommand ImportCodesCommand { get; }
    public System.Windows.Input.ICommand ExportCodesCommand { get; }
    public System.Windows.Input.ICommand EditCogoCodeCommand { get; }
    public System.Windows.Input.ICommand SaveCodesCommand { get; }
    public System.Windows.Input.ICommand AutoMatchBlocksCommand { get; }

    private void SaveCodes()
    {
        try
        {
            using (var db = new RCS.Data.AppDbContext())
            {
                var existing = db.CogoCodes.ToList();
                foreach (var c in CogoCodes)
                {
                    var ent = existing.FirstOrDefault(e => e.LocalCode == c.LocalCode && e.SystemCode == c.SystemCode);
                    if (ent != null)
                    {
                        ent.Block      = c.Block;
                        ent.BlockScale = c.BlockScale;
                    }
                    else
                    {
                        db.CogoCodes.Add(new RCS.Data.Entities.CogoCodeEntity
                        {
                            LocalCode   = c.LocalCode,
                            SystemCode  = c.SystemCode,
                            Description = c.Description,
                            Block       = c.Block,
                            BlockScale  = c.BlockScale
                        });
                    }
                }
                db.SaveChanges();
            }
            CommandLog.Add($"Manually saved {CogoCodes.Count} codes to database.");
            System.Windows.MessageBox.Show("Codes successfully saved to database.", "Save Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CommandLog.Add($"Error saving codes: {ex.Message}");
            System.Windows.MessageBox.Show($"Error saving codes: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditCogoCode()
    {
        if (SelectedCogoCode == null) return;
        var win = new RCS.Cogo.Wpf.Views.EditCogoCodeWindow(SelectedCogoCode);
        win.Owner = System.Windows.Application.Current.MainWindow;
        if (win.ShowDialog() == true)
        {
            if (win.ResultAction == "Save" || win.ResultAction == "Delete")
            {
                // Refresh list from DB
                LoadCodesFromDb();
            }
        }
    }

    private void LoadCodesFromDb()
    {
        try
        {
            using (var db = new RCS.Data.AppDbContext())
            {
                CogoCodes.Clear();
                var codes = db.CogoCodes.ToList();
                foreach (var c in codes)
                {
                    CogoCodes.Add(new CogoCode(c.LocalCode, c.SystemCode, c.Description, c.Block ?? "", c.BlockScale));
                }
            }
        }
        catch (Exception ex)
        {
            CommandLog.Add($"Error reloading codes: {ex.Message}");
        }
    }

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
                        // Skip header row (produced by ExportCodesCsv)
                        if (parts.Length >= 1 && parts[0].Trim().Equals("LocalCode", StringComparison.OrdinalIgnoreCase)) continue;
                        
                        if (parts.Length >= 2) 
                        { 
                            string local = parts[0].Trim(); 
                            string sys = parts[1].Trim();
                            string desc    = parts.Length >= 3 ? parts[2].Trim() : sys;
                            string block   = parts.Length >= 4 ? parts[3].Trim() : "";
                            double bscale  = 1.0;
                            if (parts.Length >= 5) double.TryParse(parts[4].Trim(), out bscale);
                            if (bscale <= 0) bscale = 1.0;
                            
                            var existing = existingEntities.FirstOrDefault(e => string.Equals(e.SystemCode, sys, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.LocalCode  = local;
                                existing.Block      = parts.Length >= 4 ? block : existing.Block;
                                existing.BlockScale = parts.Length >= 5 ? bscale : existing.BlockScale;
                            }
                            else
                            {
                                var newEntity = new RCS.Data.Entities.CogoCodeEntity 
                                { 
                                    LocalCode  = local, 
                                    SystemCode = sys, 
                                    Description = desc,
                                    Block      = block,
                                    BlockScale = bscale
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
                        CogoCodes.Add(new CogoCode(e.LocalCode, e.SystemCode, e.Description, e.Block ?? "", e.BlockScale));
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

    private void OpenCrossSectionWindow()
    {
        var sections = _context.CrossSections;
        if (sections == null || sections.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "No cross sections have been computed yet.\n\n" +
                "Run a COGO script containing:\n" +
                "  XS BEG <AlignmentName>\n" +
                "  XS TEMPLATE WIDTH <L> <R> SLOPE <L> <R>\n" +
                "  XS SHOT <Station> <Offset> <Elevation>  (repeat)\n" +
                "  XS COMPUTE <Interval>\n" +
                "  XS END",
                "No Cross Sections",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }
        var win = new RCS.Cogo.Wpf.Views.CrossSectionWindow(sections)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.Show();
    }

    private void OpenFiguresWindow()
    {
        if (CurrentProject == null) return;
        var win = new RCS.Cogo.Wpf.Views.FiguresWindow(CurrentProject.Id.ToString(), async () => 
        {
            if (InstalledAssets != null)
            {
                await InstalledAssets.ReloadAsync();
            }
        }) { Owner = App.Current.MainWindow };
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
                // Header — matches import column order: LocalCode, SystemCode, Description, Block, BlockScale
                sb.AppendLine("LocalCode,SystemCode,Description,Block,BlockScale");
                foreach (var code in CogoCodes)
                {
                    // Wrap fields containing commas in quotes
                    string desc  = code.Description.Contains(',') ? $"\"{code.Description}\"" : code.Description;
                    string block = (code.Block ?? "").Contains(',') ? $"\"{code.Block}\"" : (code.Block ?? "");
                    sb.AppendLine($"{code.LocalCode},{code.SystemCode},{desc},{block},{code.BlockScale:G}");
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

    // ── Bulk Auto-Match Blocks ────────────────────────────────────────────
    private void AutoMatchBlocks()
    {
        // Load all available block names once
        var blocksDir = RCS.Cogo.Wpf.Views.EditCogoCodeWindow.BlocksDirectory;
        if (string.IsNullOrEmpty(blocksDir) || !System.IO.Directory.Exists(blocksDir))
        {
            System.Windows.MessageBox.Show(
                $"RCS_Blocks directory not found.\nExpected at: {blocksDir}\n\nConfigure the path in Settings.",
                "Blocks Library Missing", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var allBlocks = System.IO.Directory.GetFiles(blocksDir, "*.dwg")
            .Select(f => System.IO.Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n)
            .ToList();

        if (allBlocks.Count == 0)
        {
            System.Windows.MessageBox.Show("No .dwg files found in the RCS_Blocks directory.",
                "Empty Library", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        int matched = 0, skipped = 0;
        using var db = new RCS.Data.AppDbContext();
        var entities = db.CogoCodes.ToList();

        foreach (var code in CogoCodes)
        {
            // Only fill in codes that are currently unassigned
            if (!string.IsNullOrWhiteSpace(code.Block)) { skipped++; continue; }

            string candidate = code.LocalCode.ToUpperInvariant();
            string desc      = (code.Description ?? "").ToUpperInvariant();

            // Tier 1 — exact match on LocalCode
            string? match = allBlocks.FirstOrDefault(b =>
                b.Equals(candidate, StringComparison.OrdinalIgnoreCase));

            // Tier 2 — prefix match  (e.g. SSMH → SSMH.dwg, SSMH1.dwg)
            if (match == null)
                match = allBlocks.FirstOrDefault(b =>
                    b.StartsWith(candidate, StringComparison.OrdinalIgnoreCase));

            // Tier 3 — keyword scan from description
            if (match == null)
            {
                var keywords = desc.Split(new[] { ' ', '-', '_', '/' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (var kw in keywords)
                {
                    match = allBlocks.FirstOrDefault(b =>
                        b.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match != null) break;
                }
            }

            if (match == null) continue;

            // Apply to in-memory model
            code.Block = match;
            matched++;

            // Persist to DB
            var entity = entities.FirstOrDefault(e =>
                e.LocalCode == code.LocalCode && e.SystemCode == code.SystemCode);
            if (entity != null)
                entity.Block = match;
        }

        db.SaveChanges();

        CommandLog.Add($"[Auto-Match] Matched {matched} codes from {allBlocks.Count} blocks. {skipped} already had blocks assigned.");
        System.Windows.MessageBox.Show(
            $"Auto-Match complete.\n\n✔ {matched} codes matched\n⏭ {skipped} codes already had a block assigned",
            "Auto-Match Blocks", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
            // MaterialItem is a POCO (no INotifyPropertyChanged).
            // Remove + Add forces ObservableCollection to fire CollectionChanged
            // so the DataGrid row repaint picks up the updated Quantity.
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
    public System.Windows.Input.ICommand OpenAiChatCommand { get; }

    // ── Item 7: Save Data directly to SQLite ─────────────────────────────────
    private async Task SaveInspectorAsync()
    {
        if (SelectedStructure?.UnderlyingAsset == null) return;
        var asset = SelectedStructure.UnderlyingAsset;

        foreach (var field in SelectedStructure.AssetData)
        {
            if (field.IsReadOnly) continue;
            string v = field.Value?.Trim() ?? "";

            switch (field.Key)
            {
                case "Discipline":     asset.Discipline = v; break;
                case "Subtype":        asset.Subtype = v; break;
                case "Facility Owner": asset.FacilityOwner = v; break;
                case "Size":           asset.Size = v; break;
                case "Material":       asset.Material = v; break;
                case "Manufacturer":   asset.Manufacturer = v; break;
                case "Valve Type":     asset.ValveType = string.IsNullOrEmpty(v) ? null : v; break;
                case "Open Direction": asset.OpenDirection = string.IsNullOrEmpty(v) ? null : v; break;
                case "Turns To Open":  if (double.TryParse(v, out double to)) asset.TurnsToOpen = to; break;
                case "Manhole Type":   asset.ManholeType = string.IsNullOrEmpty(v) ? null : v; break;
                case "Rim Elev.":      if (double.TryParse(v, out double re)) asset.RimElevation = re; break;
                case "Lowest Invert":  if (double.TryParse(v, out double lie)) asset.LowestInvertElevation = lie; break;
                case "Lining Material":asset.LiningMaterial = string.IsNullOrEmpty(v) ? null : v; break;
                case "RFID / Barcode": asset.RfidBarcode = string.IsNullOrEmpty(v) ? null : v; break;
                case "Grade Elev.":    if (double.TryParse(v, out double ge)) asset.GradeElevation = ge; break;
                case "Depth":          if (double.TryParse(v, out double d)) asset.Depth = d; break;
            }
        }

        if (InstalledAssets != null)
        {
             await InstalledAssets.SaveAssetAsync(asset);
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

    // ── QC Badge properties (populated by MAPCHECK command) ──────────────────
    public RCS.Cogo.App.State.FigureQcStatus QcStatus { get; }
    public double?  ClosureError   { get; }
    public double?  Acres          { get; }
    public double?  PrecisionRatio { get; }
    public DateTime? LastQcRun     { get; }

    // ── Run selection highlight ───────────────────────────────────────────────
    private bool _isHighlighted;
    /// <summary>True while this figure's matching pipe run is selected in the AsBuilt grid.
    /// Drives a bold stroke via data binding — no visual-tree walk required.</summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set { _isHighlighted = value; OnPropertyChanged(); OnPropertyChanged(nameof(HighlightThickness)); }
    }
    /// <summary>Returns 4x the normal line thickness when highlighted, 1x otherwise.</summary>
    public double HighlightThickness => IsHighlighted ? 4.0 : 1.0;

    /// <summary>
    /// Single-character glyph for the QC badge: ✓ (passed), ✗ (failed), ? (unknown).
    /// </summary>
    public string QcGlyph => QcStatus switch
    {
        RCS.Cogo.App.State.FigureQcStatus.Passed  => "✓",
        RCS.Cogo.App.State.FigureQcStatus.Failed  => "✗",
        _                                          => "?",
    };

    /// <summary>Tooltip text shown when hovering the QC badge in the Figures panel.</summary>
    public string QcTooltip
    {
        get
        {
            if (QcStatus == RCS.Cogo.App.State.FigureQcStatus.Unknown)
                return "MAPCHECK not yet run";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"QC: {QcStatus}");
            if (ClosureError.HasValue)
                sb.AppendLine($"Closure: {ClosureError.Value:F4} ft");
            if (Acres.HasValue)
                sb.AppendLine($"Area: {Acres.Value:F4} ac");
            if (PrecisionRatio.HasValue)
                sb.AppendLine($"Precision: 1:{PrecisionRatio.Value:F0}");
            if (LastQcRun.HasValue)
                sb.Append($"Checked: {LastQcRun.Value.ToLocalTime():yyyy-MM-dd HH:mm}");
            return sb.ToString().TrimEnd();
        }
    }

    public FigureViewModel(string name,
        System.Collections.Generic.IEnumerable<Point3D> points,
        System.Windows.Media.Brush? stroke = null,
        System.Collections.Generic.IEnumerable<RCS.Cogo.App.State.FigureLabel>? labels = null,
        RCS.Cogo.App.State.Figure? sourceFigure = null)
    {
        Name   = name;
        Points = new System.Windows.Media.PointCollection();
        foreach (var p in points)
            Points.Add(new System.Windows.Point(p.Easting, p.Northing));
        Stroke = stroke ?? System.Windows.Media.Brushes.Yellow;

        if (labels != null)
            foreach (var l in labels)
                Labels.Add(new FigureLabelViewModel(l.Text, l.Easting, l.Northing, l.RotationDegrees));

        // Bind QC data from the source Figure entity if provided
        if (sourceFigure != null)
        {
            QcStatus      = sourceFigure.QcStatus;
            ClosureError  = sourceFigure.ClosureError;
            Acres         = sourceFigure.Acres;
            PrecisionRatio= sourceFigure.PrecisionRatio;
            LastQcRun     = sourceFigure.LastQcRun;
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

    // ── Label & Inspect ───────────────────────────────────────────────────────
    /// <summary>Short label shown next to the symbol on canvas (e.g. "MH-001")</summary>
    public string Label { get; }
    /// <summary>Full attribute set displayed in the inspector popup.</summary>
    public System.Collections.ObjectModel.ObservableCollection<InspectorField> AssetData { get; }
    public RCS.Data.Entities.InstalledAsset? UnderlyingAsset { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public System.Windows.Input.ICommand SelectCommand { get; set; } = new RelayCommand(_ => {});

    public StructureViewModel(string id, Point3D p, string type,
        string? label = null,
        System.Collections.Generic.IEnumerable<InspectorField>? assetData = null,
        RCS.Data.Entities.InstalledAsset? underlyingAsset = null)
    {
        Id = id;
        Northing = p.Northing;
        Easting = p.Easting;
        Type = type;
        Label = label ?? id;
        AssetData = new System.Collections.ObjectModel.ObservableCollection<InspectorField>(assetData ?? Array.Empty<InspectorField>());
        UnderlyingAsset = underlyingAsset;
        
        string t = type.ToUpper();
        var tokens = t.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Any(x => x.Contains("MET") || x == "WMET" || x == "GMET" || x == "EMET")) SymbolType = "Meter";
        else if (tokens.Any(x => x.Contains("MH") || x.Contains("MANHOLE") || x.Contains("INLET") || x.Contains("CB") || x.EndsWith("M") || x.EndsWith("MH") || x.Contains("STM") || x == "WWM")) SymbolType = "Manhole";
        else if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV") || x == "WAR" || x == "WWV" || x.EndsWith("BFP") || x.EndsWith("BO"))) SymbolType = "Valve";
        else if (tokens.Any(x => x.Contains("HYDRANT") || x.EndsWith("H") || x.EndsWith("HYD") || x == "FH")) SymbolType = "Hydrant";
        else if (tokens.Any(x => x.Contains("FITTING") || x.Contains("BEND") || x.Contains("TEE") || x.EndsWith("F") || x == "WF" || x == "WWF" || x == "STF")) SymbolType = "Fitting";
        else if (tokens.Any(x => x.Contains("POLE") || x == "WPP" || x == "EPOLE" || x == "PP" || x == "GUY")) SymbolType = "Pole";
        else if (tokens.Any(x => x.Contains("BOX") || x == "EBOX" || x == "PB" || x == "JB")) SymbolType = "Box";
        else SymbolType = "Default";

        // Color Logic
        if (tokens.Any(x => x == "WW" || x == "WWM" || x == "WWV" || x.Contains("SAN") || x.Contains("SEW"))) Fill = System.Windows.Media.Brushes.Green;
        else if (tokens.Any(x => x == "ST" || x.StartsWith("STM") || x == "S" || x == "D" || x.Contains("SW") || x.Contains("STORM"))) Fill = System.Windows.Media.Brushes.Cyan;
        else if (tokens.Any(x => x.StartsWith("W") || x.Contains("WAT") || x == "FH")) Fill = System.Windows.Media.Brushes.Blue;
        else if (tokens.Any(x => x == "R" || x.Contains("RECLAIM"))) Fill = System.Windows.Media.Brushes.Purple;
        else if (tokens.Any(x => x.StartsWith("G") || x.Contains("GAS"))) Fill = System.Windows.Media.Brushes.Orange;
        else if (tokens.Any(x => x.StartsWith("E") || x.Contains("ELEC") || x.Contains("PWR") || x.Contains("POLE") || x == "PP" || x == "GUY")) Fill = System.Windows.Media.Brushes.Red;
        else if (tokens.Any(x => x == "CH" || x.Contains("CHILL"))) Fill = System.Windows.Media.Brushes.LightSkyBlue;
        else if (type.Equals("Manhole", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Gray;
        else if (type.Equals("Valve", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Gray;
        else if (type.Equals("Inlet", StringComparison.OrdinalIgnoreCase)) Fill = System.Windows.Media.Brushes.Gray;
        else Fill = System.Windows.Media.Brushes.White; // Default 
    }
}

public class InspectorField : ViewModelBase
{
    public string Key { get; }
    private string _value;
    public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
    public bool IsReadOnly { get; }

    public InspectorField(string key, string value, bool isReadOnly = false)
    {
        Key = key;
        _value = value ?? "";
        IsReadOnly = isReadOnly;
    }
}

