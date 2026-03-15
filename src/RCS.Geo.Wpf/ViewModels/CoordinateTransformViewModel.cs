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
        set => SetField(ref _outputLatitude, value);
    }

    private string _outputLongitude = "";
    public string OutputLongitude
    {
        get => _outputLongitude;
        set => SetField(ref _outputLongitude, value);
    }

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

        ConvertCommand = new RelayCommand(ExecuteConvert);
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
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }
}
