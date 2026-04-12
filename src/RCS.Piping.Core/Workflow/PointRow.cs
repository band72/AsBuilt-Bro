using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RCS.Piping.Core.Workflow;

/// <summary>
/// Flat row model representing a survey point within the As-Built job.
/// Used as the DataGrid item source in PointsPhaseView.
/// Implements INotifyPropertyChanged so edits are reflected immediately.
/// </summary>
public class PointRow : INotifyPropertyChanged
{
    private string  _pointId    = string.Empty;
    private double  _northing;
    private double  _easting;
    private double  _elevation;
    private string  _desc       = string.Empty;
    private bool    _isControl;
    private bool    _isOrphan;

    public string  PointId    { get => _pointId;   set { _pointId    = value; OnPC(); } }
    public double  Northing   { get => _northing;  set { _northing   = value; OnPC(); } }
    public double  Easting    { get => _easting;   set { _easting    = value; OnPC(); } }
    public double  Elevation  { get => _elevation; set { _elevation  = value; OnPC(); } }
    public string  Description{ get => _desc;      set { _desc       = value; OnPC(); } }
    public bool    IsControl  { get => _isControl; set { _isControl  = value; OnPC(); } }
    public bool    IsOrphan   { get => _isOrphan;  set { _isOrphan   = value; OnPC(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPC([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
