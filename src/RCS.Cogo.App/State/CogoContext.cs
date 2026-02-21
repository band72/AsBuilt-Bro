using System;
using System.Collections.Generic;
using System.Linq;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.State;

public class CogoContext : ICogoContext, RCS.Piping.Core.Abstractions.IPointProvider
{
    private readonly Dictionary<string, (Point3D Point, string Description)> _points = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Figure> _figures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<string> _logger;

    public Point3D? CurrentStation { get; set; }
    public Point3D? CurrentBacksight { get; set; }
    public bool TraverseMode { get; set; }
    public Figure? CurrentFigure { get; set; }
    public (Point3D? Left, Point3D? Right) LastIntersections { get; set; }

    // Defaults
    public string Units { get; set; } = "FOOT";
    public double Temperature { get; set; } = 68.0;
    public double Pressure { get; set; } = 29.92;
    public double ScaleFactor { get; set; } = 1.000000;
    public bool AtmosCorrection { get; set; } = false;
    public bool CurvatureRefraction { get; set; } = false;
    public bool AutoPoint { get; set; } = false;
    public string AngleFormat { get; set; } = "RIGHT";
    public string VerticalFormat { get; set; } = "ZENITH";
    public string EdmMode { get; set; } = "STD";
    public string PrismMode { get; set; } = "0";
    public double MapCheckClosureTolerance { get; set; } = 0.01;

    public CogoContext(Action<string> logger)
    {
        _logger = logger;
    }

    public void AddPoint(string pointId, Point3D point, string description = "")
    {
        // Enforce numeric Point IDs? User allows non-numeric via suffixes (e.g. 100_L).
        _points[pointId] = (point, description);
    }
    
    public int GetNextPointId()
    {
        // Find max integer ID
        int maxId = 0;
        foreach(var key in _points.Keys)
        {
            if (int.TryParse(key, out int id))
            {
                if (id > maxId) maxId = id;
            }
        }
        return maxId + 1;
    }

    public Point3D? GetPoint(string pointId)
    {
        return _points.TryGetValue(pointId, out var data) ? data.Point : null;
    }

    public bool PointExists(string id)
    {
        return _points.ContainsKey(id);
    }


    public void AddFigure(Figure figure)
    {
        _figures[figure.Name] = figure;
    }
    
    public bool DeletePoint(string pointId)
    {
        return _points.Remove(pointId);
    }
    
    public bool DeleteFigure(string name)
    {
        if (CurrentFigure?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
        {
            CurrentFigure = null;
        }
        return _figures.Remove(name);
    }
    
    public void ClearLog()
    {
        // Special signal to logger
        _logger?.Invoke("[CLEAR]");
    }

    public Figure? GetFigure(string name)
    {
        return _figures.TryGetValue(name, out var fig) ? fig : null;
    }

    public void Log(string message)
    {
        _logger?.Invoke(message);
    }

    public IEnumerable<(string Id, Point3D Point, string Description)> GetAllPoints()
    {
        return _points.Select(kvp => (kvp.Key, kvp.Value.Point, kvp.Value.Description));
    }

    public IEnumerable<Figure> GetAllFigures()
    {
        return _figures.Values;
    }

    public void ClearState()
    {
        _points.Clear();
        _figures.Clear();
        CurrentStation = null;
        CurrentBacksight = null;
        CurrentFigure = null;
        Log("[AUDIT] Context State Cleared.");
    }
}
