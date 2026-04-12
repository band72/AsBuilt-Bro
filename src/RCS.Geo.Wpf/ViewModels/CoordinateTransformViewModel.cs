using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using RCS.Geo.Abstractions;
using RCS.Geo.Core;

namespace RCS.Geo.Wpf.ViewModels;

public class CrsItem
{
    public string DisplayName { get; }
    public string CrsId { get; }
    
    public CrsItem(string displayName, string crsId)
    {
        DisplayName = displayName;
        CrsId = crsId;
    }
}

public class CoordinateTransformViewModel : ViewModelBase
{
    private readonly ICoordinateTransformService? _transformService;

    public ObservableCollection<CrsItem> AvailableCrs { get; }

    private CrsItem? _selectedSourceCrs;
    public CrsItem? SelectedSourceCrs
    {
        get => _selectedSourceCrs;
        set { SetField(ref _selectedSourceCrs, value); ConvertCommand.Execute(null); }
    }

    private string _inputEasting = "";
    public string InputEasting
    {
        get => _inputEasting;
        set { SetField(ref _inputEasting, value); ConvertCommand.Execute(null); }
    }

    private string _inputNorthing = "";
    public string InputNorthing
    {
        get => _inputNorthing;
        set { SetField(ref _inputNorthing, value); ConvertCommand.Execute(null); }
    }

    private string _outputLatitude = "";
    public string OutputLatitude
    {
        get => _outputLatitude;
        set { SetField(ref _outputLatitude, value); OnPropertyChanged(nameof(OutputLatitudeDms)); }
    }

    private string _outputLongitude = "";
    public string OutputLongitude
    {
        get => _outputLongitude;
        set { SetField(ref _outputLongitude, value); OnPropertyChanged(nameof(OutputLongitudeDms)); }
    }

    // ── DMS derived outputs ────────────────────────────────────────────────────
    public string OutputLatitudeDms  => DecimalToDms(OutputLatitude,  isLat: true);
    public string OutputLongitudeDms => DecimalToDms(OutputLongitude, isLat: false);

    // ── Input validation badge ────────────────────────────────────────────────
    private bool _inputIsValid;
    public bool InputIsValid
    {
        get => _inputIsValid;
        set => SetField(ref _inputIsValid, value);
    }

    public ICommand CopyToClipboardCommand { get; }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public ICommand ConvertCommand { get; }

    // Design time or testing constructor
    public CoordinateTransformViewModel() : this(null)
    {
    }

    public CoordinateTransformViewModel(ICoordinateTransformService? transformService)
    {
        _transformService = transformService;

        AvailableCrs = new ObservableCollection<CrsItem>
        {
            new CrsItem("Florida East NAD83(2011) ftUS", "EPSG:6438"),
            new CrsItem("Florida West NAD83(2011) ftUS", "EPSG:6443"),
            new CrsItem("Florida North NAD83(2011) ftUS", "EPSG:6439"),
            new CrsItem("WGS 84 (Lat/Lon)", "EPSG:4326")
        };

        if (AvailableCrs.Count > 0)
        {
            _selectedSourceCrs = AvailableCrs[0];
        }

        ConvertCommand         = new RelayCommand(ExecuteConvert);
        CopyToClipboardCommand = new RelayCommand(ExecuteCopy, () => InputIsValid);
    }

    private void ExecuteConvert()
    {
        ErrorMessage = "";
        OutputLatitude = "";
        OutputLongitude = "";

        if (_transformService == null)
        {
            ErrorMessage = "Transform service is not registered.";
            return;
        }

        if (SelectedSourceCrs == null) return;

        if (string.IsNullOrWhiteSpace(InputEasting) || string.IsNullOrWhiteSpace(InputNorthing))
            return; // Wait for full input

        if (!double.TryParse(InputEasting, out double easting))
        {
            ErrorMessage = "Invalid Easting value.";
            return;
        }

        if (!double.TryParse(InputNorthing, out double northing))
        {
            ErrorMessage = "Invalid Northing value.";
            return;
        }

        try
        {
            if (SelectedSourceCrs.CrsId == "EPSG:4326")
            {
                // Source is already Lat/Lon. Let's just output it or we could do the inverse transform 
                // if we added inverse fields. For now this UI assumes projecting from State Plane to GPS.
                // Assuming Easting is Longitude, Northing is Latitude in the UI text boxes.
                OutputLatitude = northing.ToString("F6");
                OutputLongitude = easting.ToString("F6");
                return;
            }

            var projected = new ProjectedPoint(easting, northing, SelectedSourceCrs.CrsId);
            var result = _transformService.ToLatLon(projected, "EPSG:4326");

            OutputLatitude = result.Latitude.ToString("F8");
            OutputLongitude = result.Longitude.ToString("F8");
        }
        catch (GeoTransformException ex)
        {
            ErrorMessage = $"Transformation error: {ex.Message}";
            InputIsValid = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            InputIsValid = false;
        }

        // Mark valid if outputs are populated
        InputIsValid = !string.IsNullOrEmpty(OutputLatitude) && string.IsNullOrEmpty(ErrorMessage);
    }

    private void ExecuteCopy()
    {
        if (!InputIsValid) return;
        var text = $"Latitude:  {OutputLatitude} ({OutputLatitudeDms})\n" +
                   $"Longitude: {OutputLongitude} ({OutputLongitudeDms})\n" +
                   $"Zone: {SelectedSourceCrs?.DisplayName}";
        System.Windows.Clipboard.SetText(text);
    }

    /// <summary>Converts a decimal degree string to DD°MM'SS.ss" N/S/E/W notation.</summary>
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
        return $"{deg}°{min:D2}'{sec:F2}\" {dir}";
    }
}
