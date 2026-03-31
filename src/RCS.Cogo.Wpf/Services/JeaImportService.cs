using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/// into the project database. Sheet/column layout matches Simulated_JEA_AsBuilt_Template.xlsx.
/// Row 1 = header (skipped). Blank rows (no ID in col 1) are skipped.
/// GPS Y Coord = Northing, GPS X Coord = Easting throughout the template.
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

            // ── Sewer Manhole ─────────────────────────────────────────────────
            // C1=Manhole#  C2=Subtype  C3=FacilityOwner  C4=ManholeType
            // C5=DropType  C6=Size(ft) C7=Material  C8=LiningMaterial
            // C9=Depth     C10=RimElev C11=GPS_Y(N) C12=GPS_X(E) C13=Lat C14=Lon
            result.Sheets.Add(ImportSheet(pkg, "Sewer Manhole", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new Manhole
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    ManholeType = T(ws, r, 4), DropType = T(ws, r, 5),
                    Size = T(ws, r, 6), Material = T(ws, r, 7),
                    LiningMaterial = T(ws, r, 8),
                    Depth = D(ws, r, 9), RimElevation = D(ws, r, 10),
                    Northing = D(ws, r, 11), Easting = D(ws, r, 12),
                    Latitude = D(ws, r, 13), Longitude = D(ws, r, 14),
                    Discipline = "SEWER", FeatureType = "MANHOLE"
                };
            }, db.Manholes));

            // ── Sewer Pipe ────────────────────────────────────────────────────
            // C1=Pipe#  C2=Subtype  C3=FacilityOwner  C4=Material  C5=Size(in)
            // C6=UpstreamMH  C7=DownstreamMH  C8=UpInvert  C9=DownInvert
            // C10=Length(ft)  C11=Slope(%)
            result.Sheets.Add(ImportSheet(pkg, "Sewer Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWGravityPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Material = T(ws, r, 4), Size = T(ws, r, 5),
                    UpstreamPointId = T(ws, r, 6), DownstreamPointId = T(ws, r, 7),
                    UpstreamInvert = D(ws, r, 8), DownstreamInvert = D(ws, r, 9),
                    Length = D(ws, r, 10), Slope = D(ws, r, 11),
                    Discipline = "SEWER", FeatureType = "GRAVITY_PIPE"
                };
            }, db.WWGravityPipes));

            // ── Sewer Fitting ─────────────────────────────────────────────────
            // C1=Fitting#  C2=Subtype  C3=FacilityOwner  C4=Size  C5=SizeReducer
            // C6=Material  C7=Elev  C8=GPS_Y(N)  C9=GPS_X(E)  C10=Lat  C11=Lon
            result.Sheets.Add(ImportSheet(pkg, "Sewer Fitting", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWFitting
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), SizeSecondary = T(ws, r, 5),
                    Material = T(ws, r, 6), GradeElevation = D(ws, r, 7),
                    Northing = D(ws, r, 8), Easting = D(ws, r, 9),
                    Latitude = D(ws, r, 10), Longitude = D(ws, r, 11),
                    Discipline = "SEWER", FeatureType = "FITTING"
                };
            }, db.WWFittings));

            // ── Sewer Valve ───────────────────────────────────────────────────
            // C1=Valve#  C2=Subtype  C3=FacilityOwner  C4=Size  C5=Elev
            // C6=GPS_Y(N)  C7=GPS_X(E)  C8=Lat  C9=Lon
            result.Sheets.Add(ImportSheet(pkg, "Sewer Valve", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWValve
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), GradeElevation = D(ws, r, 5),
                    Northing = D(ws, r, 6), Easting = D(ws, r, 7),
                    Latitude = D(ws, r, 8), Longitude = D(ws, r, 9),
                    Discipline = "SEWER", FeatureType = "VALVE"
                };
            }, db.WWValves));

            // ── Sewer Meter ───────────────────────────────────────────────────
            // C1=Meter#  C2=Subtype  C3=FacilityOwner  C4=Size
            // C5=GPS_Y(N)  C6=GPS_X(E)  C7=Lat  C8=Lon
            result.Sheets.Add(ImportSheet(pkg, "Sewer Meter", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WWServicePoint
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4),
                    Northing = D(ws, r, 5), Easting = D(ws, r, 6),
                    Latitude = D(ws, r, 7), Longitude = D(ws, r, 8),
                    Discipline = "SEWER", FeatureType = "METER"
                };
            }, db.WWServicePoints));

            // ── Water Pipe ────────────────────────────────────────────────────
            // C1=Pipe#  C2=Subtype  C3=FacilityOwner  C4=Material  C5=Size  C6=Length
            // C7=GPS_StartY(N) C8=GPS_StartX(E) C9=GPS_EndY(N) C10=GPS_EndX(E)
            result.Sheets.Add(ImportSheet(pkg, "Water Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Material = T(ws, r, 4), Size = T(ws, r, 5),
                    Length = D(ws, r, 6),
                    StartNorthing = D(ws, r, 7), StartEasting = D(ws, r, 8),
                    EndNorthing = D(ws, r, 9), EndEasting = D(ws, r, 10),
                    Discipline = "WATER", FeatureType = "PIPE"
                };
            }, db.WaterPipes));

            // ── Water Fitting ─────────────────────────────────────────────────
            // C1=Fitting#  C2=Subtype  C3=FacilityOwner  C4=Size  C5=SizeReducer
            // C6=Material  C7=Elev  C8=GPS_Y(N)  C9=GPS_X(E)  C10=Lat  C11=Lon
            result.Sheets.Add(ImportSheet(pkg, "Water Fitting", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterFitting
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), SizeSecondary = T(ws, r, 5),
                    Material = T(ws, r, 6), GradeElevation = D(ws, r, 7),
                    Northing = D(ws, r, 8), Easting = D(ws, r, 9),
                    Latitude = D(ws, r, 10), Longitude = D(ws, r, 11),
                    Discipline = "WATER", FeatureType = "FITTING"
                };
            }, db.WaterFittings));

            // ── Water Valve ───────────────────────────────────────────────────
            // C1=Valve#  C2=Subtype  C3=FacilityOwner  C4=Size  C5=OpenDir
            // C6=TurnsToOpen  C7=Elev  C8=GPS_Y(N)  C9=GPS_X(E)  C10=Lat  C11=Lon
            result.Sheets.Add(ImportSheet(pkg, "Water Valve", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterValve
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), OpenDirection = T(ws, r, 5),
                    TurnsToOpen = D(ws, r, 6), GradeElevation = D(ws, r, 7),
                    Northing = D(ws, r, 8), Easting = D(ws, r, 9),
                    Latitude = D(ws, r, 10), Longitude = D(ws, r, 11),
                    Discipline = "WATER", FeatureType = "VALVE"
                };
            }, db.WaterValves));

            // ── Water Hydrant ─────────────────────────────────────────────────
            // C1=Hydrant#  C2=Subtype  C3=FacilityOwner  C4=Manufacturer  C5=Elev
            // C6=GPS_Y(N)  C7=GPS_X(E)  C8=Lat  C9=Lon
            result.Sheets.Add(ImportSheet(pkg, "Water Hydrant", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterHydrant
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Manufacturer = T(ws, r, 4), GradeElevation = D(ws, r, 5),
                    Northing = D(ws, r, 6), Easting = D(ws, r, 7),
                    Latitude = D(ws, r, 8), Longitude = D(ws, r, 9),
                    Discipline = "WATER", FeatureType = "HYDRANT"
                };
            }, db.WaterHydrants));

            // ── Water Meter ───────────────────────────────────────────────────
            // C1=Meter#  C2=Subtype  C3=FacilityOwner  C4=Size
            // C5=GPS_Y(N)  C6=GPS_X(E)  C7=Lat  C8=Lon
            result.Sheets.Add(ImportSheet(pkg, "Water Meter", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new WaterMeter
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4),
                    Northing = D(ws, r, 5), Easting = D(ws, r, 6),
                    Latitude = D(ws, r, 7), Longitude = D(ws, r, 8),
                    Discipline = "WATER", FeatureType = "METER"
                };
            }, db.WaterMeters));

            // ── Reclaimed Pipe ────────────────────────────────────────────────
            // C1=Pipe#  C2=Subtype  C3=FacilityOwner  C4=Material  C5=Size  C6=Length
            // C7=GPS_StartY(N) C8=GPS_StartX(E) C9=GPS_EndY(N) C10=GPS_EndX(E)
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Pipe", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedPipe
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Material = T(ws, r, 4), Size = T(ws, r, 5),
                    Length = D(ws, r, 6),
                    StartNorthing = D(ws, r, 7), StartEasting = D(ws, r, 8),
                    EndNorthing = D(ws, r, 9), EndEasting = D(ws, r, 10),
                    Discipline = "RECLAIM", FeatureType = "PIPE"
                };
            }, db.ReclaimedPipes));

            // ── Reclaimed Fitting ─────────────────────────────────────────────
            // C1=Fitting#  C2=Subtype  C3=FacilityOwner  C4=Size  C5=SizeReducer
            // C6=Material  C7=Elev  C8=GPS_Y(N)  C9=GPS_X(E)  C10=Lat  C11=Lon
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Fitting", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedFitting
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), SizeSecondary = T(ws, r, 5),
                    Material = T(ws, r, 6), GradeElevation = D(ws, r, 7),
                    Northing = D(ws, r, 8), Easting = D(ws, r, 9),
                    Latitude = D(ws, r, 10), Longitude = D(ws, r, 11),
                    Discipline = "RECLAIM", FeatureType = "FITTING"
                };
            }, db.ReclaimedFittings));

            // ── Reclaimed Valve ───────────────────────────────────────────────
            // C1=Valve#  C2=Subtype  C3=FacilityOwner  C4=Size  C5=OpenDir
            // C6=TurnsToOpen  C7=Elev  C8=GPS_Y(N)  C9=GPS_X(E)  C10=Lat  C11=Lon
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Valve", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedValve
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4), OpenDirection = T(ws, r, 5),
                    TurnsToOpen = D(ws, r, 6), GradeElevation = D(ws, r, 7),
                    Northing = D(ws, r, 8), Easting = D(ws, r, 9),
                    Latitude = D(ws, r, 10), Longitude = D(ws, r, 11),
                    Discipline = "RECLAIM", FeatureType = "VALVE"
                };
            }, db.ReclaimedValves));

            // ── Reclaimed Hydrant ─────────────────────────────────────────────
            // C1=Hydrant#  C2=Subtype  C3=FacilityOwner  C4=Manufacturer  C5=Elev
            // C6=GPS_Y(N)  C7=GPS_X(E)  C8=Lat  C9=Lon
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Hydrant", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedHydrant
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Manufacturer = T(ws, r, 4), GradeElevation = D(ws, r, 5),
                    Northing = D(ws, r, 6), Easting = D(ws, r, 7),
                    Latitude = D(ws, r, 8), Longitude = D(ws, r, 9),
                    Discipline = "RECLAIM", FeatureType = "HYDRANT"
                };
            }, db.ReclaimedHydrants));

            // ── Reclaimed Meter ───────────────────────────────────────────────
            // C1=Meter#  C2=Subtype  C3=FacilityOwner  C4=Size
            // C5=GPS_Y(N)  C6=GPS_X(E)  C7=Lat  C8=Lon
            result.Sheets.Add(ImportSheet(pkg, "Reclaimed Meter", db, projectId, (ws, r) =>
            {
                string? id = T(ws, r, 1); if (id == null) return null;
                return new ReclaimedMeter
                {
                    ProjectId = projectId, PartKey = id,
                    Subtype = T(ws, r, 2), FacilityOwner = T(ws, r, 3),
                    Size = T(ws, r, 4),
                    Northing = D(ws, r, 5), Easting = D(ws, r, 6),
                    Latitude = D(ws, r, 7), Longitude = D(ws, r, 8),
                    Discipline = "RECLAIM", FeatureType = "METER"
                };
            }, db.ReclaimedMeters));

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
    private static string? T(ExcelWorksheet ws, int r, int c)
    {
        var v = ws.Cells[r, c].Text?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    private static double? D(ExcelWorksheet ws, int r, int c)
    {
        var cell = ws.Cells[r, c];
        if (cell.Value is double d) return d;
        if (double.TryParse(cell.Text?.Trim(), out var parsed)) return parsed;
        return null;
    }
}
