using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using RCS.Geo.Abstractions;
using RCS.Geo.Core;

namespace RCS.Geo.Wpf.ViewModels;

// ── CRS descriptor ────────────────────────────────────────────────────────────
public class CrsItem
{
    public string DisplayName { get; }
    public string CrsId       { get; }
    public CrsItem(string displayName, string crsId) { DisplayName = displayName; CrsId = crsId; }
}

// ── Transform direction ───────────────────────────────────────────────────────
public enum TransformDirection
{
    StatePlaneToLatLon,
    LatLonToStatePlane
}

// ── GPS coordinate display format ─────────────────────────────────────────────
/// <summary>Controls how GPS lat/lon outputs are displayed and exported.</summary>
public enum GpsDisplayFormat
{
    /// <summary>Show as decimal degrees, e.g. 30.318600°</summary>
    DecimalDegrees,
    /// <summary>Show as degrees-minutes-seconds, e.g. 30°19'07.00"N</summary>
    DMS
}

// ─────────────────────────────────────────────────────────────────────────────
// CoordinateTransformViewModel
// ─────────────────────────────────────────────────────────────────────────────
public class CoordinateTransformViewModel : ViewModelBase
{
    private readonly ICoordinateTransformService? _transformService;

    // ── Optional delegates injected from the host ViewModel ──────────────────
    /// <summary>
    /// Called when BulkApplyCommand fires.
    /// Args: direction, crsId.  Action applies the transform to all drawing points.
    /// </summary>
    public Action<TransformDirection, string>? BulkApplyAction { get; set; }

    /// <summary>Called when the user requests GPS CSV import. Arg is file path.</summary>
    public Action<string>? ImportGpsCsvAction { get; set; }

    /// <summary>
    /// Called when the user requests GPS export.
    /// Args: outputPathBase, bool fullCsv (true) or compact txt (false).
    /// </summary>
    public Action<string, bool>? ExportGpsAction { get; set; }

    // ── GPS display format ────────────────────────────────────────────────────
    private GpsDisplayFormat _coordinateFormat = GpsDisplayFormat.DecimalDegrees;
    /// <summary>
    /// Controls whether lat/lon outputs are shown as decimal degrees or DMS.
    /// Set by the host (ShellViewModel) from GlobalSettingsService on load.
    /// </summary>
    public GpsDisplayFormat CoordinateFormat
    {
        get => _coordinateFormat;
        set
        {
            if (SetField(ref _coordinateFormat, value))
            {
                OnPropertyChanged(nameof(Output1Dms));
                OnPropertyChanged(nameof(Output2Dms));
                OnPropertyChanged(nameof(OutputLatitudeDms));
                OnPropertyChanged(nameof(OutputLongitudeDms));
                OnPropertyChanged(nameof(ConvertedOutput));
                OnPropertyChanged(nameof(OutputLabel1));
                OnPropertyChanged(nameof(OutputLabel2));
            }
        }
    }

    // ── CRS picker ────────────────────────────────────────────────────────────
    public ObservableCollection<CrsItem> AvailableCrs { get; }

    private CrsItem? _selectedSourceCrs;
    public CrsItem? SelectedSourceCrs
    {
        get => _selectedSourceCrs;
        set { SetField(ref _selectedSourceCrs, value); ConvertCommand.Execute(null); }
    }

    // ── Direction toggle ──────────────────────────────────────────────────────
    private TransformDirection _direction = TransformDirection.StatePlaneToLatLon;
    public TransformDirection Direction
    {
        get => _direction;
        set
        {
            SetField(ref _direction, value);
            OnPropertyChanged(nameof(DirectionLabel));
            OnPropertyChanged(nameof(InputLabel1));
            OnPropertyChanged(nameof(InputLabel2));
            OnPropertyChanged(nameof(OutputLabel1));
            OnPropertyChanged(nameof(OutputLabel2));
            ClearOutputs();
        }
    }

    public string DirectionLabel => Direction == TransformDirection.StatePlaneToLatLon
        ? "State Plane  →  Lat / Lon"
        : "Lat / Lon  →  State Plane";

    public string InputLabel1  => Direction == TransformDirection.StatePlaneToLatLon ? "Easting (ft):"  : "Latitude:";
    public string InputLabel2  => Direction == TransformDirection.StatePlaneToLatLon ? "Northing (ft):" : "Longitude:";
    // Output labels reflect both direction and active GPS format
    private string FmtTag => CoordinateFormat == GpsDisplayFormat.DMS ? " (DMS)" : " (DD)";
    public string OutputLabel1 => Direction == TransformDirection.StatePlaneToLatLon ? $"Latitude{FmtTag}:"  : "Easting (ft):";
    public string OutputLabel2 => Direction == TransformDirection.StatePlaneToLatLon ? $"Longitude{FmtTag}:" : "Northing (ft):";

    // ── Inputs ────────────────────────────────────────────────────────────────
    private string _input1 = "";
    public string Input1
    {
        get => _input1;
        set { SetField(ref _input1, value); ConvertCommand.Execute(null); }
    }

    private string _input2 = "";
    public string Input2
    {
        get => _input2;
        set { SetField(ref _input2, value); ConvertCommand.Execute(null); }
    }

    // Keep legacy property names for backward-compat with existing XAML bindings
    public string InputEasting  { get => Input1; set => Input1 = value; }
    public string InputNorthing { get => Input2; set => Input2 = value; }

    // ── Outputs ───────────────────────────────────────────────────────────────
    private string _output1 = "";
    public string Output1
    {
        get => _output1;
        set { SetField(ref _output1, value); OnPropertyChanged(nameof(Output1Dms)); }
    }

    private string _output2 = "";
    public string Output2
    {
        get => _output2;
        set { SetField(ref _output2, value); OnPropertyChanged(nameof(Output2Dms)); }
    }

    // Legacy property names for XAML
    public string OutputLatitude  => Direction == TransformDirection.StatePlaneToLatLon ? Output1 : "";
    public string OutputLongitude => Direction == TransformDirection.StatePlaneToLatLon ? Output2 : "";

    /// <summary>
    /// The primary formatted output for Output 1 (lat or easting depending on direction).
    /// Respects <see cref="CoordinateFormat"/>: DMS or decimal degrees.
    /// For SP-direction output this returns the lat in the configured format;
    /// for LatLon-direction output this is the easting number (not a lat/lon so always decimal).
    /// </summary>
    public string Output1Dms => Direction == TransformDirection.StatePlaneToLatLon
        ? FormatCoord(Output1, isLat: true)
        : "";

    public string Output2Dms => Direction == TransformDirection.StatePlaneToLatLon
        ? FormatCoord(Output2, isLat: false)
        : "";

    /// <summary>Single-string composite output shaped by CoordinateFormat — shown in clipboard copy and status bar.</summary>
    public string ConvertedOutput
    {
        get
        {
            if (!InputIsValid || string.IsNullOrEmpty(Output1)) return string.Empty;
            if (Direction == TransformDirection.StatePlaneToLatLon)
                return CoordinateFormat == GpsDisplayFormat.DMS
                    ? $"{FormatCoord(Output1, isLat: true)},  {FormatCoord(Output2, isLat: false)}"
                    : $"{Output1}°,  {Output2}°";
            else
                return $"N: {Output1} ft,  E: {Output2} ft";
        }
    }

    // Legacy DMS names for XAML
    public string OutputLatitudeDms  => Output1Dms;
    public string OutputLongitudeDms => Output2Dms;

    // ── Status ────────────────────────────────────────────────────────────────
    private bool _inputIsValid;
    public bool InputIsValid
    {
        get => _inputIsValid;
        set => SetField(ref _inputIsValid, value);
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand ConvertCommand         { get; }
    public ICommand CopyToClipboardCommand { get; }
    public ICommand FlipDirectionCommand   { get; }
    public ICommand BulkApplyCommand       { get; }
    public ICommand ImportGpsCsvCommand    { get; }
    public ICommand ExportGpsCsvCommand    { get; }
    public ICommand ExportLatLonTxtCommand { get; }

    // ── Constructors ──────────────────────────────────────────────────────────
    public CoordinateTransformViewModel() : this(null) { }

    public CoordinateTransformViewModel(ICoordinateTransformService? transformService)
    {
        _transformService = transformService;

        AvailableCrs = new ObservableCollection<CrsItem>
        {
            new("Florida East  NAD83(2011) ftUS",  "EPSG:6438"),
            new("Florida West  NAD83(2011) ftUS",  "EPSG:6443"),
            new("Florida North NAD83(2011) ftUS",  "EPSG:6439"),
            new("Florida East  NAD83 ftUS (legacy)","EPSG:2236"),
            new("WGS 84 (Lat/Lon only)",            "EPSG:4326")
        };
        _selectedSourceCrs = AvailableCrs[0];

        ConvertCommand         = new RelayCommand(ExecuteConvert);
        CopyToClipboardCommand = new RelayCommand(ExecuteCopy, () => InputIsValid);
        FlipDirectionCommand   = new RelayCommand(() =>
            Direction = Direction == TransformDirection.StatePlaneToLatLon
                ? TransformDirection.LatLonToStatePlane
                : TransformDirection.StatePlaneToLatLon);

        BulkApplyCommand = new RelayCommand(() =>
        {
            if (SelectedSourceCrs == null) return;
            BulkApplyAction?.Invoke(Direction, SelectedSourceCrs.CrsId);
            StatusMessage = $"✅ Bulk transform applied ({Direction}).";
        });

        ImportGpsCsvCommand = new RelayCommand(() =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Import GPS Coordinates",
                Filter = "GPS Files (*.csv;*.txt)|*.csv;*.txt|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                ImportGpsCsvAction?.Invoke(dlg.FileName);
                StatusMessage = $"✅ Imported: {System.IO.Path.GetFileName(dlg.FileName)}";
            }
        });

        ExportGpsCsvCommand = new RelayCommand(() =>
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Export GPS CSV",
                Filter     = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName   = "GPS_Export.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                ExportGpsAction?.Invoke(dlg.FileName, true);
                StatusMessage = $"✅ Exported: {System.IO.Path.GetFileName(dlg.FileName)}";
            }
        });

        ExportLatLonTxtCommand = new RelayCommand(() =>
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Export Compact Lat/Lon TXT",
                Filter     = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName   = "LatLon_Export.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                ExportGpsAction?.Invoke(dlg.FileName, false);
                StatusMessage = $"✅ Exported: {System.IO.Path.GetFileName(dlg.FileName)}";
            }
        });
    }

    // ── Convert logic ─────────────────────────────────────────────────────────

    private void ExecuteConvert()
    {
        ErrorMessage = "";
        ClearOutputs(notify: false);

        if (string.IsNullOrWhiteSpace(Input1) || string.IsNullOrWhiteSpace(Input2))
            return;

        try
        {
            if (Direction == TransformDirection.StatePlaneToLatLon)
                ConvertSpToLatLon();
            else
                ConvertLatLonToSp();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            InputIsValid = false;
        }

        InputIsValid = !string.IsNullOrEmpty(Output1) && string.IsNullOrEmpty(ErrorMessage);
        OnPropertyChanged(nameof(OutputLatitude));
        OnPropertyChanged(nameof(OutputLongitude));
        OnPropertyChanged(nameof(Output1Dms));
        OnPropertyChanged(nameof(Output2Dms));
        OnPropertyChanged(nameof(OutputLatitudeDms));
        OnPropertyChanged(nameof(OutputLongitudeDms));
    }

    private void ConvertSpToLatLon()
    {
        if (!double.TryParse(Input1, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double e))
        { ErrorMessage = "Invalid Easting value."; return; }
        if (!double.TryParse(Input2, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double n))
        { ErrorMessage = "Invalid Northing value."; return; }

        string crsId = SelectedSourceCrs?.CrsId ?? "EPSG:2236";
        // Native fast-path for all three FL zones (no ProjNet dependency)
        if (crsId is "EPSG:2236" or "EPSG:2237" or "EPSG:2238")
        {
            var (lat, lon) = StatePlaneProjection.ToLatLon(e, n, crsId);
            Output1 = lat.ToString("F8");
            Output2 = lon.ToString("F8");
            return;
        }
        // ProjNet fallback for other / modern NAD83(2011) CRS IDs
        if (_transformService != null)
        {
            var proj   = new ProjectedPoint(e, n, crsId);
            var latlon = _transformService.ToLatLon(proj, "EPSG:4326");
            Output1 = latlon.Latitude.ToString("F8");
            Output2 = latlon.Longitude.ToString("F8");
            return;
        }
        // Final fallback: FL East
        var (la, lo) = StatePlaneProjection.ToLatLon(e, n);
        Output1 = la.ToString("F8");
        Output2 = lo.ToString("F8");
    }

    private void ConvertLatLonToSp()
    {
        double lat = StatePlaneProjection.ParseLatLon(Input1);
        double lon = StatePlaneProjection.ParseLatLon(Input2);

        if (double.IsNaN(lat)) { ErrorMessage = "Cannot parse Latitude. Use decimal (30.3322) or DMS (30\u00b019'56\"N)."; return; }
        if (double.IsNaN(lon)) { ErrorMessage = "Cannot parse Longitude. Use decimal (-81.655) or DMS (81\u00b039'19\"W)."; return; }

        string crsId = SelectedSourceCrs?.CrsId ?? "EPSG:2236";
        // Native fast-path for all three FL zones
        if (crsId is "EPSG:2236" or "EPSG:2237" or "EPSG:2238")
        {
            var (eft, nft) = StatePlaneProjection.ToStatePlane(lat, lon, crsId);
            Output1 = eft.ToString("F3");
            Output2 = nft.ToString("F3");
            return;
        }
        // ProjNet fallback
        if (_transformService != null)
        {
            var geo  = new GeographicPoint(lat, lon);
            var proj = _transformService.ToStatePlane(geo, "EPSG:4326", crsId);
            Output1 = proj.Easting.ToString("F3");
            Output2 = proj.Northing.ToString("F3");
            return;
        }
        // Final fallback: FL East
        var (e2, n2) = StatePlaneProjection.ToStatePlane(lat, lon);
        Output1 = e2.ToString("F3");
        Output2 = n2.ToString("F3");
    }

    private void ExecuteCopy()
    {
        if (!InputIsValid) return;
        string text = ConvertedOutput + $"\nZone: {SelectedSourceCrs?.DisplayName}";
        System.Windows.Clipboard.SetText(text);
    }

    private void ClearOutputs(bool notify = true)
    {
        _output1 = "";
        _output2 = "";
        if (notify)
        {
            OnPropertyChanged(nameof(Output1));
            OnPropertyChanged(nameof(Output2));
            OnPropertyChanged(nameof(ConvertedOutput));
        }
    }

    // -- Format-aware output helper ---------------------------------------------------
    // Returns decimalDeg as DMS or raw decimal depending on CoordinateFormat setting.
    private string FormatCoord(string decimalDeg, bool isLat)
        => CoordinateFormat == GpsDisplayFormat.DMS
            ? DecimalToDms(decimalDeg, isLat)
            : decimalDeg;

    // ── DMS formatter ─────────────────────────────────────────────────────────
    private static string DecimalToDms(string decimalDeg, bool isLat)
    {
        if (!double.TryParse(decimalDeg,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double d))
            return "";
        char dir = isLat ? (d >= 0 ? 'N' : 'S') : (d >= 0 ? 'E' : 'W');
        double abs = Math.Abs(d);
        int deg    = (int)abs;
        double rem = (abs - deg) * 60.0;
        int min    = (int)rem;
        double sec = (rem - min) * 60.0;
        return $"{deg}\u00b0{min:D2}'{sec:F2}\" {dir}";
    }
}

