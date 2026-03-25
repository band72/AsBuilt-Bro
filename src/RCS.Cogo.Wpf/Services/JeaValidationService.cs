using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Cogo.Wpf.Services;

public enum JeaSeverity { Error, Warning, Info }

public record JeaIssue(
    string Sheet,
    string AssetId,
    string Field,
    string Message,
    JeaSeverity Severity);

/// <summary>
/// Validates all project assets against the JEA As-Built Validation Rules.
///
/// Rules derived from the "Validation Rules" sheet in JEA As Built Template 2024.xlsx:
///   - Coordinate bounds: Easting 320k–590k, Northing 1.92M–2.37M, Lat 29–31, Lon -83 to -80
///   - Required fields on point/structure records
///   - Decimal value checks (non-null, non-zero) for elevations and depths
///   - Slope must be positive for gravity pipes
/// </summary>
public static class JeaValidationService
{
    // JEA coordinate bounds from the Validation Rules sheet
    private const double EastMin  =   320_000, EastMax  =   590_000;
    private const double NorthMin = 1_920_000, NorthMax = 2_370_000;
    private const double LatMin   = 29.0,      LatMax   = 31.0;
    private const double LonMin   = -83.0,     LonMax   = -80.0;

    public static JeaValidationReport Validate(string projectId)
    {
        var issues = new List<JeaIssue>();
        using var db = new AppDbContext();

        // ── Pipe Crossings ───────────────────────────────────────────────
        foreach (var x in db.PipeCrossings.Where(e => e.ProjectId == projectId))
        {
            string id = x.CrossingNumber ?? x.Id;
            RequireText(issues, "Pipe Crossing Table", id, "Crossing Number", x.CrossingNumber);
            RequireText(issues, "Pipe Crossing Table", id, "Upper Pipe Type",  x.UpperPipeType);
            RequireText(issues, "Pipe Crossing Table", id, "Lower Pipe Type",  x.LowerPipeType);
            RequireDecimal(issues, "Pipe Crossing Table", id, "Grade Elevation",       x.GradeElevation);
            RequireDecimal(issues, "Pipe Crossing Table", id, "Upper Pipe Top Elev",   x.UpperPipeTopElevation);
            RequireDecimal(issues, "Pipe Crossing Table", id, "Lower Pipe Top Elev",   x.LowerPipeTopElevation);
            RequireDecimal(issues, "Pipe Crossing Table", id, "Separation",            x.Separation);
            CheckCoords(issues,   "Pipe Crossing Table", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Water Points ─────────────────────────────────────────────────
        foreach (var x in db.WaterPoints.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues, "Water Points along Pipe", id, "Pipe Location Number", x.PartKey);
            RequireText(issues, "Water Points along Pipe", id, "Facility Owner",       x.FacilityOwner);
            RequireText(issues, "Water Points along Pipe", id, "Pipe Size",            x.Size);
            RequireText(issues, "Water Points along Pipe", id, "Pipe Material",        x.Material);
            RequireDecimal(issues, "Water Points along Pipe", id, "Grade Elevation",   x.GradeElevation);
            RequireDecimal(issues, "Water Points along Pipe", id, "Pipe Top Elevation",x.TopElevation);
            RequireDecimal(issues, "Water Points along Pipe", id, "Cover",             x.Cover);
            CheckCoords(issues,   "Water Points along Pipe", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Water Fittings ───────────────────────────────────────────────
        foreach (var x in db.WaterFittings.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues, "Water Fitting", id, "Fitting Number",    x.PartKey);
            RequireText(issues, "Water Fitting", id, "Facility Owner",    x.FacilityOwner);
            RequireText(issues, "Water Fitting", id, "Fitting Material",  x.Material);
            RequireText(issues, "Water Fitting", id, "Fitting Size",      x.Size);
            RequireDecimal(issues, "Water Fitting", id, "Top Elevation",  x.TopElevation);
            RequireDecimal(issues, "Water Fitting", id, "Grade Elevation",x.GradeElevation);
            CheckCoords(issues,   "Water Fitting", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Water Valves ─────────────────────────────────────────────────
        foreach (var x in db.WaterValves.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues, "Water Valve", id, "Valve Number",    x.PartKey);
            RequireText(issues, "Water Valve", id, "Valve Type",      x.ValveType);
            RequireText(issues, "Water Valve", id, "Facility Owner",  x.FacilityOwner);
            RequireText(issues, "Water Valve", id, "Valve Size",      x.Size);
            RequireDecimal(issues, "Water Valve", id, "Nut Elevation",    x.NutElevation);
            RequireDecimal(issues, "Water Valve", id, "Grade Elevation",  x.GradeElevation);
            CheckCoords(issues,   "Water Valve", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Water Hydrants ───────────────────────────────────────────────
        foreach (var x in db.WaterHydrants.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues, "Water Hydrant", id, "Hydrant Number",  x.PartKey);
            RequireText(issues, "Water Hydrant", id, "Facility Owner",  x.FacilityOwner);
            CheckCoords(issues, "Water Hydrant", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Water Meters ─────────────────────────────────────────────────
        foreach (var x in db.WaterMeters.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues, "Water Meter", id, "Meter Number",    x.PartKey);
            RequireText(issues, "Water Meter", id, "Facility Owner",  x.FacilityOwner);
            CheckCoords(issues, "Water Meter", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── WW Gravity Pipes ─────────────────────────────────────────────
        foreach (var x in db.WWGravityPipes.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues,    "WW Gravity Pipe Run", id, "Pipe Run Number",   x.PartKey);
            RequireText(issues,    "WW Gravity Pipe Run", id, "Facility Owner",    x.FacilityOwner);
            RequireText(issues,    "WW Gravity Pipe Run", id, "Pipe Size",         x.Size);
            RequireText(issues,    "WW Gravity Pipe Run", id, "Pipe Material",     x.Material);
            RequireDecimal(issues, "WW Gravity Pipe Run", id, "Pipe Length",       x.Length);
            RequireDecimal(issues, "WW Gravity Pipe Run", id, "Downstream Invert", x.DownstreamInvert);
            RequireDecimal(issues, "WW Gravity Pipe Run", id, "Upstream Invert",   x.UpstreamInvert);
            if (x.Slope.HasValue && x.Slope <= 0)
                issues.Add(new JeaIssue("WW Gravity Pipe Run", id, "Slope", "Slope must be positive for gravity pipe.", JeaSeverity.Error));
        }

        // ── WW Points ────────────────────────────────────────────────────
        foreach (var x in db.WWPoints.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues,    "WW Points along Pipe", id, "Point Number",      x.PartKey);
            RequireText(issues,    "WW Points along Pipe", id, "Facility Owner",    x.FacilityOwner);
            RequireText(issues,    "WW Points along Pipe", id, "Pipe Material",     x.Material);
            RequireDecimal(issues, "WW Points along Pipe", id, "Grade Elevation",   x.GradeElevation);
            RequireDecimal(issues, "WW Points along Pipe", id, "Pipe Top Elevation",x.TopElevation);
            RequireDecimal(issues, "WW Points along Pipe", id, "Cover",             x.Cover);
            CheckCoords(issues,    "WW Points along Pipe", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Manholes ─────────────────────────────────────────────────────
        foreach (var x in db.Manholes.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues,    "Manhole", id, "Manhole Number",         x.PartKey);
            RequireText(issues,    "Manhole", id, "Manhole Type",           x.ManholeType);
            RequireText(issues,    "Manhole", id, "Facility Owner",         x.FacilityOwner);
            RequireText(issues,    "Manhole", id, "Manhole Material",       x.Material);
            RequireDecimal(issues, "Manhole", id, "Rim Elevation",          x.RimElevation);
            RequireDecimal(issues, "Manhole", id, "Lowest Invert Elevation",x.LowestInvertElevation);
            if (string.IsNullOrWhiteSpace(x.InvertElevationsWithDirections))
                issues.Add(new JeaIssue("Manhole", id, "Invert Elevations with Directions",
                    "Invert elevations with pipe directions are required.", JeaSeverity.Warning));
            CheckCoords(issues, "Manhole", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── WW Valves ────────────────────────────────────────────────────
        foreach (var x in db.WWValves.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues,    "WW Valve", id, "Valve Number",    x.PartKey);
            RequireText(issues,    "WW Valve", id, "Valve Type",      x.ValveType);
            RequireText(issues,    "WW Valve", id, "Facility Owner",  x.FacilityOwner);
            RequireDecimal(issues, "WW Valve", id, "Nut Elevation",   x.NutElevation);
            CheckCoords(issues,    "WW Valve", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── WW Service Points ─────────────────────────────────────────────
        foreach (var x in db.WWServicePoints.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues,    "WW Service Point & Meter", id, "Service Point Number", x.PartKey);
            RequireDecimal(issues, "WW Service Point & Meter", id, "Grade Elevation",      x.GradeElevation);
            RequireDecimal(issues, "WW Service Point & Meter", id, "Top of Pipe Elevation",x.TopElevation);
            CheckCoords(issues,    "WW Service Point & Meter", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        // ── Reclaimed Points ─────────────────────────────────────────────
        foreach (var x in db.ReclaimedPoints.Where(e => e.ProjectId == projectId))
        {
            string id = x.PartKey ?? x.Id;
            RequireText(issues,    "Reclaimed Points along Pipe", id, "Point Number",     x.PartKey);
            RequireText(issues,    "Reclaimed Points along Pipe", id, "Pipe Material",    x.Material);
            RequireDecimal(issues, "Reclaimed Points along Pipe", id, "Grade Elevation",  x.GradeElevation);
            RequireDecimal(issues, "Reclaimed Points along Pipe", id, "Pipe Top Elev",    x.TopElevation);
            CheckCoords(issues,    "Reclaimed Points along Pipe", id, x.Easting, x.Northing, x.Latitude, x.Longitude);
        }

        return new JeaValidationReport(issues, projectId);
    }

    // ── Rule helpers ──────────────────────────────────────────────────────

    private static void RequireText(List<JeaIssue> issues, string sheet, string id,
        string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            issues.Add(new JeaIssue(sheet, id, field, $"Required field is empty.", JeaSeverity.Error));
    }

    private static void RequireDecimal(List<JeaIssue> issues, string sheet, string id,
        string field, double? value)
    {
        if (!value.HasValue || value == 0)
            issues.Add(new JeaIssue(sheet, id, field,
                value == null ? "Required decimal value is missing." : "Value is zero — verify this is correct.",
                value == null ? JeaSeverity.Error : JeaSeverity.Warning));
    }

    private static void CheckCoords(List<JeaIssue> issues, string sheet, string id,
        double? easting, double? northing, double? lat, double? lon)
    {
        bool hasStatePlane = easting.HasValue && northing.HasValue
                             && easting != 0 && northing != 0;
        bool hasLatLon = lat.HasValue && lon.HasValue && lat != 0 && lon != 0;

        if (!hasStatePlane)
        {
            issues.Add(new JeaIssue(sheet, id, "State Plane Coordinates",
                "Easting/Northing coordinates are missing.", JeaSeverity.Error));
        }
        else
        {
            if (easting < EastMin || easting > EastMax)
                issues.Add(new JeaIssue(sheet, id, "X Coord (Easting)",
                    $"Easting {easting:N0} is outside JEA bounds ({EastMin:N0}–{EastMax:N0} ft).", JeaSeverity.Error));
            if (northing < NorthMin || northing > NorthMax)
                issues.Add(new JeaIssue(sheet, id, "Y Coord (Northing)",
                    $"Northing {northing:N0} is outside JEA bounds ({NorthMin:N0}–{NorthMax:N0} ft).", JeaSeverity.Error));
        }

        if (!hasLatLon)
        {
            // Warn if missing but we can auto-compute from State Plane if coords are present
            var sev = hasStatePlane ? JeaSeverity.Warning : JeaSeverity.Error;
            var msg = hasStatePlane
                ? "Lat/Lon is empty — will be auto-computed from State Plane on export."
                : "Lat/Lon coordinates are missing and no State Plane coords to derive from.";
            issues.Add(new JeaIssue(sheet, id, "Latitude / Longitude", msg, sev));
        }
        else
        {
            if (lat < LatMin || lat > LatMax)
                issues.Add(new JeaIssue(sheet, id, "Latitude",
                    $"Latitude {lat:F4}° is outside JEA bounds ({LatMin}–{LatMax}°).", JeaSeverity.Error));
            if (lon < LonMin || lon > LonMax)
                issues.Add(new JeaIssue(sheet, id, "Longitude",
                    $"Longitude {lon:F4}° is outside JEA bounds ({LonMin}–{LonMax}°).", JeaSeverity.Error));
        }
    }
}

public class JeaValidationReport
{
    public string ProjectId   { get; }
    public List<JeaIssue> Issues { get; }

    public int ErrorCount   => Issues.Count(i => i.Severity == JeaSeverity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == JeaSeverity.Warning);
    public int InfoCount    => Issues.Count(i => i.Severity == JeaSeverity.Info);
    public bool IsValid     => ErrorCount == 0;

    public JeaValidationReport(List<JeaIssue> issues, string projectId)
    {
        Issues    = issues;
        ProjectId = projectId;
    }
}
