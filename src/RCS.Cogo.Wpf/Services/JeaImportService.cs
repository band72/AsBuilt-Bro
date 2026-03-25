using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Cogo.Wpf.Services;

// ── Per-sheet import result ───────────────────────────────────────────────────
public record JeaSheetImportResult(string SheetName, int Imported, int Skipped, List<string> Warnings);

// ── Overall import result ─────────────────────────────────────────────────────
public class JeaImportResult
{
    public bool   Success      { get; set; }
    public string? ErrorMessage { get; set; }
    public List<JeaSheetImportResult> Sheets { get; } = new();

    public int TotalImported => Sheets.Sum(s => s.Imported);
    public int TotalSkipped  => Sheets.Sum(s => s.Skipped);

    public static JeaImportResult Fail(string msg) =>
        new() { Success = false, ErrorMessage = msg };

    public string Summary() =>
        $"Imported {TotalImported} records across {Sheets.Count(s => s.Imported > 0)} sheets.\n" +
        $"{TotalSkipped} blank rows skipped.";
}

/// <summary>
/// Reads a filled JEA As-Built Template .xlsx and upserts matching records
/// into the project database.  Each sheet maps to a specific entity type.
/// Row 1 = header (skipped).  Blank rows (no PartKey / ID value) are skipped.
/// </summary>
public static class JeaImportService
{
    static JeaImportService() =>
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    public static JeaImportResult Import(string xlsxPath, string projectId)
    {
        if (!File.Exists(xlsxPath))
            return JeaImportResult.Fail($"File not found: {xlsxPath}");

        var result = new JeaImportResult();

        try
        {
            using var pkg = new ExcelPackage(new FileInfo(xlsxPath));
            using var db  = new AppDbContext();

            // ── Pipe Crossing Table ──────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Pipe Crossing Table", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new PipeCrossing
                {
                    ProjectId             = projectId,
                    CrossingNumber        = id,
                    UpperPipeType         = T(ws, r, 2),
                    UpperPipeSize         = T(ws, r, 3),
                    GradeElevation        = D(ws, r, 4),
                    UpperPipeTopElevation = D(ws, r, 5),
                    UpperCover            = D(ws, r, 6),
                    UpperPipeBottomElevation = D(ws, r, 7),
                    LowerPipeType         = T(ws, r, 8),
                    LowerPipeSize         = T(ws, r, 9),
                    LowerPipeTopElevation = D(ws, r, 10),
                    LowerCover            = D(ws, r, 11),
                    Separation            = D(ws, r, 12),
                    Easting               = D(ws, r, 13),
                    Northing              = D(ws, r, 14),
                    Latitude              = D(ws, r, 15),
                    Longitude             = D(ws, r, 16),
                    Discipline            = "CROSSING", FeatureType = "CROSSING"
                };
            }, db.PipeCrossings));

            // ── Water Pipe Run ───────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Pipe Run", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), PipeClass = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    Length = D(ws, r, 10),
                    Discipline = "WATER", FeatureType = "PIPE"
                };
            }, db.WaterPipes));

            // ── Water Points along Pipe ──────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Points along Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterPoint
                {
                    ProjectId = projectId, PartKey = id,
                    PipeRole = T(ws, r, 2), Subtype = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Size = T(ws, r, 5),
                    Orientation = T(ws, r, 6), PipeClass = T(ws, r, 7),
                    Manufacturer = T(ws, r, 8), Material = T(ws, r, 9),
                    LiningManufacturer = T(ws, r, 10), LiningMaterial = T(ws, r, 11),
                    GradeElevation = D(ws, r, 12), TopElevation = D(ws, r, 13),
                    Cover = D(ws, r, 14),
                    Easting = D(ws, r, 15), Northing = D(ws, r, 16),
                    Latitude = D(ws, r, 17), Longitude = D(ws, r, 18),
                    Discipline = "WATER", FeatureType = "POINT"
                };
            }, db.WaterPoints));

            // ── Water Fitting ────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Fitting", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterFitting
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), SizeSecondary = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    TopElevation = D(ws, r, 10), GradeElevation = D(ws, r, 11),
                    Depth = D(ws, r, 12),
                    Easting = D(ws, r, 13), Northing = D(ws, r, 14),
                    Latitude = D(ws, r, 15), Longitude = D(ws, r, 16),
                    Discipline = "WATER", FeatureType = "FITTING"
                };
            }, db.WaterFittings));

            // ── Water Valve ──────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Valve", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterValve
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), ValveType = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Size = T(ws, r, 5),
                    Orientation = T(ws, r, 6), OpenDirection = T(ws, r, 7),
                    TurnsToOpen = D(ws, r, 8), NutElevation = D(ws, r, 9),
                    GradeElevation = D(ws, r, 10), DepthToNut = D(ws, r, 11),
                    Manufacturer = T(ws, r, 12),
                    Easting = D(ws, r, 13), Northing = D(ws, r, 14),
                    Latitude = D(ws, r, 15), Longitude = D(ws, r, 16),
                    Discipline = "WATER", FeatureType = "VALVE"
                };
            }, db.WaterValves));

            // ── Water Hydrant ────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Hydrant", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterHydrant
                {
                    ProjectId = projectId, PartKey = id,
                    FacilityOwner = T(ws, r, 2), YearManufactured = T(ws, r, 3),
                    Manufacturer = T(ws, r, 4),
                    Easting = D(ws, r, 5), Northing = D(ws, r, 6),
                    Latitude = D(ws, r, 7), Longitude = D(ws, r, 8),
                    RfidBarcode = T(ws, r, 9),
                    Discipline = "WATER", FeatureType = "HYDRANT"
                };
            }, db.WaterHydrants));

            // ── Water Meter ──────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Meter", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterMeter
                {
                    ProjectId = projectId, PartKey = id,
                    Size = T(ws, r, 2), Subtype = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Orientation = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    Easting = D(ws, r, 8), Northing = D(ws, r, 9),
                    Latitude = D(ws, r, 10), Longitude = D(ws, r, 11),
                    Discipline = "WATER", FeatureType = "METER"
                };
            }, db.WaterMeters));

            // ── Water Locate Box ─────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Water Locate Box", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterLocateBox
                {
                    ProjectId = projectId, PartKey = id, Subtype = T(ws, r, 2),
                    Easting = D(ws, r, 3), Northing = D(ws, r, 4),
                    Latitude = D(ws, r, 5), Longitude = D(ws, r, 6),
                    Discipline = "WATER", FeatureType = "LOCATE_BOX"
                };
            }, db.WaterLocateBoxes));

            // ── WW Gravity Pipe Run ──────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Gravity Pipe Run", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWGravityPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), PipeClass = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    Length = D(ws, r, 10),
                    DownstreamInvert = D(ws, r, 11), DownstreamGrade = D(ws, r, 12),
                    UpstreamInvert = D(ws, r, 13), UpstreamGrade = D(ws, r, 14),
                    Slope = D(ws, r, 15),
                    Discipline = "SEWER", FeatureType = "GRAVITY_PIPE"
                };
            }, db.WWGravityPipes));

            // ── WW Pressure Pipe Run ─────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Pressure Pipe Run", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWPressurePipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), PipeClass = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    Length = D(ws, r, 10),
                    Discipline = "SEWER", FeatureType = "PRESSURE_PIPE"
                };
            }, db.WWPressurePipes));

            // ── WW Points along Pipe ─────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Points along Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWPoint
                {
                    ProjectId = projectId, PartKey = id,
                    PipeRole = T(ws, r, 2), Subtype = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Size = T(ws, r, 5),
                    Orientation = T(ws, r, 6), PipeClass = T(ws, r, 7),
                    Manufacturer = T(ws, r, 8), Material = T(ws, r, 9),
                    LiningManufacturer = T(ws, r, 10), LiningMaterial = T(ws, r, 11),
                    GradeElevation = D(ws, r, 12), TopElevation = D(ws, r, 13),
                    Cover = D(ws, r, 14),
                    Easting = D(ws, r, 15), Northing = D(ws, r, 16),
                    Latitude = D(ws, r, 17), Longitude = D(ws, r, 18),
                    Discipline = "SEWER", FeatureType = "POINT"
                };
            }, db.WWPoints));

            // ── WW Fitting ───────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Fitting", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWFitting
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), SizeSecondary = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    TopElevation = D(ws, r, 10), GradeElevation = D(ws, r, 11),
                    Depth = D(ws, r, 12),
                    Easting = D(ws, r, 13), Northing = D(ws, r, 14),
                    Latitude = D(ws, r, 15), Longitude = D(ws, r, 16),
                    Discipline = "SEWER", FeatureType = "FITTING"
                };
            }, db.WWFittings));

            // ── Manhole ──────────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Manhole", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new Manhole
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    ManholeType = T(ws, r, 4), DropType = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Size = T(ws, r, 7),
                    Material = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    LiningManufacturer = T(ws, r, 10),
                    RimElevation = D(ws, r, 11),
                    InvertElevationsWithDirections = T(ws, r, 12),
                    LowestInvertElevation = D(ws, r, 13),
                    ExteriorJointTapeType = T(ws, r, 14),
                    ExteriorJointTapeManufacturer = T(ws, r, 15),
                    Easting = D(ws, r, 16), Northing = D(ws, r, 17),
                    Latitude = D(ws, r, 18), Longitude = D(ws, r, 19),
                    RfidBarcode = T(ws, r, 20),
                    Discipline = "SEWER", FeatureType = "MANHOLE"
                };
            }, db.Manholes));

            // ── WW Service Point & Meter ─────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Service Point & Meter", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWServicePoint
                {
                    ProjectId = projectId, PartKey = id, Subtype = T(ws, r, 2),
                    GradeElevation = D(ws, r, 3), TopElevation = D(ws, r, 4),
                    Cover = D(ws, r, 5),
                    Easting = D(ws, r, 6), Northing = D(ws, r, 7),
                    Latitude = D(ws, r, 8), Longitude = D(ws, r, 9),
                    Discipline = "SEWER", FeatureType = "SERVICE_POINT"
                };
            }, db.WWServicePoints));

            // ── WW Valve ─────────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Valve", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWValve
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), ValveType = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Size = T(ws, r, 5),
                    Orientation = T(ws, r, 6), OpenDirection = T(ws, r, 7),
                    TurnsToOpen = D(ws, r, 8), NutElevation = D(ws, r, 9),
                    GradeElevation = D(ws, r, 10), DepthToNut = D(ws, r, 11),
                    Manufacturer = T(ws, r, 12),
                    Easting = D(ws, r, 13), Northing = D(ws, r, 14),
                    Latitude = D(ws, r, 15), Longitude = D(ws, r, 16),
                    Discipline = "SEWER", FeatureType = "VALVE"
                };
            }, db.WWValves));

            // ── WW Locate Box ────────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "WW Locate Box", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWLocateBox
                {
                    ProjectId = projectId, PartKey = id, Subtype = T(ws, r, 2),
                    Easting = D(ws, r, 3), Northing = D(ws, r, 4),
                    Latitude = D(ws, r, 5), Longitude = D(ws, r, 6),
                    Discipline = "SEWER", FeatureType = "LOCATE_BOX"
                };
            }, db.WWLocateBoxes));

            // ── Reclaimed Pipe Run ───────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Pipe Run", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), PipeClass = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    Length = D(ws, r, 10),
                    Discipline = "RECLAIM", FeatureType = "PIPE"
                };
            }, db.ReclaimedPipes));

            // ── Reclaimed Points ─────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Points along Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedPoint
                {
                    ProjectId = projectId, PartKey = id,
                    PipeRole = T(ws, r, 2), Subtype = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Size = T(ws, r, 5),
                    Orientation = T(ws, r, 6), PipeClass = T(ws, r, 7),
                    Manufacturer = T(ws, r, 8), Material = T(ws, r, 9),
                    LiningManufacturer = T(ws, r, 10), LiningMaterial = T(ws, r, 11),
                    GradeElevation = D(ws, r, 12), TopElevation = D(ws, r, 13),
                    Cover = D(ws, r, 14),
                    Easting = D(ws, r, 15), Northing = D(ws, r, 16),
                    Latitude = D(ws, r, 17), Longitude = D(ws, r, 18),
                    Discipline = "RECLAIM", FeatureType = "POINT"
                };
            }, db.ReclaimedPoints));

            // ── Chilled Pipe Run ─────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Chilled Pipe Run", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ChilledPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), PipeClass = T(ws, r, 5),
                    Manufacturer = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningManufacturer = T(ws, r, 8), LiningMaterial = T(ws, r, 9),
                    Length = D(ws, r, 10),
                    Discipline = "CHILLED", FeatureType = "PIPE"
                };
            }, db.ChilledPipes));

            // ── Chilled Points ───────────────────────────────────────────────
            result.Sheets.Add(ImportSheet(pkg, "Chilled Points along Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ChilledPoint
                {
                    ProjectId = projectId, PartKey = id,
                    PipeRole = T(ws, r, 2), Subtype = T(ws, r, 3),
                    FacilityOwner = T(ws, r, 4), Size = T(ws, r, 5),
                    Orientation = T(ws, r, 6), PipeClass = T(ws, r, 7),
                    Manufacturer = T(ws, r, 8), Material = T(ws, r, 9),
                    LiningManufacturer = T(ws, r, 10), LiningMaterial = T(ws, r, 11),
                    GradeElevation = D(ws, r, 12), TopElevation = D(ws, r, 13),
                    Cover = D(ws, r, 14),
                    Easting = D(ws, r, 15), Northing = D(ws, r, 16),
                    Latitude = D(ws, r, 17),
                    Discipline = "CHILLED", FeatureType = "POINT"
                };
            }, db.ChilledPoints));

            db.SaveChanges();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success      = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // ── Generic sheet importer ────────────────────────────────────────────────
    private static JeaSheetImportResult ImportSheet<T>(
        ExcelPackage pkg, string sheetName,
        AppDbContext db,  string projectId,
        Func<ExcelWorksheet, int, T?> rowMapper,
        DbSet<T> dbSet) where T : InstalledAsset
    {
        var ws = pkg.Workbook.Worksheets[sheetName];
        if (ws == null)
            return new JeaSheetImportResult(sheetName, 0, 0, new() { "Sheet not found" });

        int rows     = ws.Dimension?.Rows ?? 1;
        int imported = 0, skipped = 0;
        var warnings = new List<string>();

        // Get all existing PartKeys for this project+sheet to detect duplicates
        var existingKeys = dbSet
            .Where(e => e.ProjectId == projectId && e.PartKey != null)
            .Select(e => e.PartKey!)
            .ToHashSet();

        for (int r = 2; r <= rows; r++)
        {
            try
            {
                var entity = rowMapper(ws, r);
                if (entity == null) { skipped++; continue; }

                // Upsert: skip if already imported (same PartKey)
                if (entity.PartKey != null && existingKeys.Contains(entity.PartKey))
                {
                    warnings.Add($"Row {r}: '{entity.PartKey}' already exists — skipped.");
                    skipped++;
                    continue;
                }

                dbSet.Add(entity);
                imported++;
            }
            catch (Exception ex)
            {
                warnings.Add($"Row {r}: {ex.Message}");
                skipped++;
            }
        }

        return new JeaSheetImportResult(sheetName, imported, skipped, warnings);
    }

    // ── Cell read helpers ─────────────────────────────────────────────────────

    /// <summary>Returns trimmed text, or null if empty.</summary>
    private static string? T(ExcelWorksheet ws, int r, int c)
    {
        var v = ws.Cells[r, c].Text?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    /// <summary>Returns double or null.</summary>
    private static double? D(ExcelWorksheet ws, int r, int c)
    {
        var cell = ws.Cells[r, c];
        if (cell.Value is double d) return d;
        if (double.TryParse(cell.Text?.Trim(), out var parsed)) return parsed;
        return null;
    }
}
