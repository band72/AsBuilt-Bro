using System;
using System.Collections.Generic;

namespace RCS.Cogo.App.State;

/// <summary>Lifecycle QC status set by MAPCHECK and persisted across re-renders.</summary>
public enum FigureQcStatus
{
    Unknown,    // MAPCHECK has never been run on this figure
    Passed,     // closure ≤ tolerance
    Failed,     // closure > tolerance
}

public class Figure
{
    public string Name { get; }
    public List<string> PointIds { get; } = new();
    public List<FigureLabel> Labels { get; } = new();

    // ── Legacy flag (kept for backward compat with existing rendering code) ──
    public bool MapCheckFailed { get; set; } = false;

    // ── Persistent MAPCHECK results ───────────────────────────────────────────
    /// <summary>QC pass/fail status. Unknown until MAPCHECK is run.</summary>
    public FigureQcStatus QcStatus { get; set; } = FigureQcStatus.Unknown;

    /// <summary>Last measured closure error in feet.</summary>
    public double? ClosureError { get; set; }

    /// <summary>Bearing azimuth of the closure leg (degrees).</summary>
    public double? ClosureBearing { get; set; }

    /// <summary>Computed area in square feet (Shoelace formula).</summary>
    public double? AreaSqFt { get; set; }

    /// <summary>Computed area in acres.</summary>
    public double? Acres { get; set; }

    /// <summary>Total perimeter in feet.</summary>
    public double? Perimeter { get; set; }

    /// <summary>Precision ratio denominator (1 : PrecisionRatio). Null when closure is perfect.</summary>
    public double? PrecisionRatio { get; set; }

    /// <summary>UTC timestamp of the last MAPCHECK run.</summary>
    public DateTime? LastQcRun { get; set; }

    public Figure(string name)
    {
        Name = name;
    }

    public string Color { get; set; } = "#FFFF00"; // Yellow default
    public bool IsInvalidCrosslink { get; set; } = false;

    public void AddPoint(string pointId)
    {
        PointIds.Add(pointId);
    }
}

public class FigureLabel
{
    public string Text { get; set; } = "";
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double RotationDegrees { get; set; }
}
