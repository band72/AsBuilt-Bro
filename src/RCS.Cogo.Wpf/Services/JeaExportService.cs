using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Cogo.Wpf.Services;

/// <summary>
/// Fills the JEA As-Built Excel template with data from the project database.
/// Each entity type maps to a specific sheet and column layout matching the
/// "JEA As Built Template 2024.xlsx" column headers exactly.
/// </summary>
public static class JeaExportService
{
    // EPPlus 7+ requires NonCommercial license or commercial license context.
    // Set this once at app startup.
    static JeaExportService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Fills the JEA template with all assets for the given project and saves to outputPath.
    /// </summary>
    /// <param name="templatePath">Path to the blank JEA As Built Template 2024.xlsx</param>
    /// <param name="outputPath">Where to save the filled workbook</param>
    /// <param name="projectId">The project GUID string to filter assets</param>
    /// <param name="projectName">Used for the As Built Info sheet</param>
    public static JeaExportResult Export(string templatePath, string outputPath,
        string projectId, string projectName)
    {
        if (!File.Exists(templatePath))
            return JeaExportResult.Fail($"Template not found: {templatePath}");

        File.Copy(templatePath, outputPath, overwrite: true);

        using var pkg = new ExcelPackage(new FileInfo(outputPath));

        using var db = new AppDbContext();
        var result = new JeaExportResult();

        // ── Pipe Crossing Table ──────────────────────────────────────────
        result.PipeCrossings = FillSheet(pkg, "Pipe Crossing Table", 2,
            db.PipeCrossings.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.CrossingNumber);
                Set(ws, r, 2,  x.UpperPipeType);
                Set(ws, r, 3,  x.UpperPipeSize);
                SetN(ws, r, 4,  x.GradeElevation);
                SetN(ws, r, 5,  x.UpperPipeTopElevation);
                SetN(ws, r, 6,  x.UpperCover);
                SetN(ws, r, 7,  x.UpperPipeBottomElevation);
                Set(ws, r, 8,  x.LowerPipeType);
                Set(ws, r, 9,  x.LowerPipeSize);
                SetN(ws, r, 10, x.LowerPipeTopElevation);
                SetN(ws, r, 11, x.LowerCover);
                SetN(ws, r, 12, x.Separation);
                SetN(ws, r, 13, x.Easting);
                SetN(ws, r, 14, x.Northing);
                SetN(ws, r, 15, x.Latitude);
                SetN(ws, r, 16, x.Longitude);
            });

        // ── Water Pipe Run ──────────────────────────────────────────────
        result.WaterPipes = FillSheet(pkg, "Water Pipe Run", 2,
            db.WaterPipes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.FacilityOwner);
                Set(ws, r, 4,  x.Size);
                Set(ws, r, 5,  x.PipeClass);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Material);
                Set(ws, r, 8,  x.LiningManufacturer);
                Set(ws, r, 9,  x.LiningMaterial);
                SetN(ws, r, 10, x.Length);
            });

        // ── Water Points along Pipe ─────────────────────────────────────
        result.WaterPoints = FillSheet(pkg, "Water Points along Pipe", 2,
            db.WaterPoints.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.PipeRole);
                Set(ws, r, 3,  x.Subtype);
                Set(ws, r, 4,  x.FacilityOwner);
                Set(ws, r, 5,  x.Size);
                Set(ws, r, 6,  x.Orientation);
                Set(ws, r, 7,  x.PipeClass);
                Set(ws, r, 8,  x.Manufacturer);
                Set(ws, r, 9,  x.Material);
                Set(ws, r, 10, x.LiningManufacturer);
                Set(ws, r, 11, x.LiningMaterial);
                SetN(ws, r, 12, x.GradeElevation);
                SetN(ws, r, 13, x.TopElevation);
                SetN(ws, r, 14, x.Cover);
                SetN(ws, r, 15, x.Easting);
                SetN(ws, r, 16, x.Northing);
                SetN(ws, r, 17, x.Latitude);
                SetN(ws, r, 18, x.Longitude);
            });

        // ── Water Fitting ───────────────────────────────────────────────
        result.WaterFittings = FillSheet(pkg, "Water Fitting", 2,
            db.WaterFittings.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.FacilityOwner);
                Set(ws, r, 4,  x.Size);
                Set(ws, r, 5,  x.SizeSecondary);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Material);
                Set(ws, r, 8,  x.LiningManufacturer);
                Set(ws, r, 9,  x.LiningMaterial);
                SetN(ws, r, 10, x.TopElevation);
                SetN(ws, r, 11, x.GradeElevation);
                SetN(ws, r, 12, x.Depth);
                SetN(ws, r, 13, x.Easting);
                SetN(ws, r, 14, x.Northing);
                SetN(ws, r, 15, x.Latitude);
                SetN(ws, r, 16, x.Longitude);
            });

        // ── Water Valve ─────────────────────────────────────────────────
        result.WaterValves = FillSheet(pkg, "Water Valve", 2,
            db.WaterValves.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.ValveType);
                Set(ws, r, 4,  x.FacilityOwner);
                Set(ws, r, 5,  x.Size);
                Set(ws, r, 6,  x.Orientation);
                Set(ws, r, 7,  x.OpenDirection);
                SetN(ws, r, 8,  x.TurnsToOpen);
                SetN(ws, r, 9,  x.NutElevation);
                SetN(ws, r, 10, x.GradeElevation);
                SetN(ws, r, 11, x.DepthToNut);
                Set(ws, r, 12, x.Manufacturer);
                SetN(ws, r, 13, x.Easting);
                SetN(ws, r, 14, x.Northing);
                SetN(ws, r, 15, x.Latitude);
                SetN(ws, r, 16, x.Longitude);
            });

        // ── Water Hydrant ───────────────────────────────────────────────
        result.WaterHydrants = FillSheet(pkg, "Water Hydrant", 2,
            db.WaterHydrants.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.FacilityOwner);
                Set(ws, r, 3,  x.YearManufactured);
                Set(ws, r, 4,  x.Manufacturer);
                SetCoords(ws, r, 5, 6, 7, 8,
                    x.Easting, x.Northing, x.Latitude, x.Longitude);
                Set(ws, r, 9,  x.RfidBarcode);
            });

        // ── Water Meter ─────────────────────────────────────────────────
        result.WaterMeters = FillSheet(pkg, "Water Meter", 2,
            db.WaterMeters.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Size);
                Set(ws, r, 3,  x.Subtype);
                Set(ws, r, 4,  x.FacilityOwner);
                Set(ws, r, 5,  x.Orientation);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Material);
                SetCoords(ws, r, 8, 9, 10, 11,
                    x.Easting, x.Northing, x.Latitude, x.Longitude);
            });

        // ── Water Locate Box ────────────────────────────────────────────
        result.WaterLocateBoxes = FillSheet(pkg, "Water Locate Box", 2,
            db.WaterLocateBoxes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                SetN(ws, r, 3,  x.Easting);
                SetN(ws, r, 4,  x.Northing);
                SetN(ws, r, 5,  x.Latitude);
                SetN(ws, r, 6,  x.Longitude);
            });

        // ── WW Gravity Pipe Run ─────────────────────────────────────────
        result.WWGravityPipes = FillSheet(pkg, "WW Gravity Pipe Run", 2,
            db.WWGravityPipes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.FacilityOwner);
                Set(ws, r, 4,  x.Size);
                Set(ws, r, 5,  x.PipeClass);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Material);
                Set(ws, r, 8,  x.LiningManufacturer);
                Set(ws, r, 9,  x.LiningMaterial);
                SetN(ws, r, 10, x.Length);
                SetN(ws, r, 11, x.DownstreamInvert);
                SetN(ws, r, 12, x.DownstreamGrade);
                SetN(ws, r, 13, x.UpstreamInvert);
                SetN(ws, r, 14, x.UpstreamGrade);
                SetN(ws, r, 15, x.Slope);
            });

        // ── WW Pressure Pipe Run ────────────────────────────────────────
        result.WWPressurePipes = FillSheet(pkg, "WW Pressure Pipe Run", 2,
            db.WWPressurePipes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.FacilityOwner);
                Set(ws, r, 4,  x.Size);
                Set(ws, r, 5,  x.PipeClass);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Material);
                Set(ws, r, 8,  x.LiningManufacturer);
                Set(ws, r, 9,  x.LiningMaterial);
                SetN(ws, r, 10, x.Length);
            });

        // ── WW Points along Pipe ────────────────────────────────────────
        result.WWPoints = FillSheet(pkg, "WW Points along Pipe", 2,
            db.WWPoints.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.PipeRole);
                Set(ws, r, 3,  x.Subtype);
                Set(ws, r, 4,  x.FacilityOwner);
                Set(ws, r, 5,  x.Size);
                Set(ws, r, 6,  x.Orientation);
                Set(ws, r, 7,  x.PipeClass);
                Set(ws, r, 8,  x.Manufacturer);
                Set(ws, r, 9,  x.Material);
                Set(ws, r, 10, x.LiningManufacturer);
                Set(ws, r, 11, x.LiningMaterial);
                SetN(ws, r, 12, x.GradeElevation);
                SetN(ws, r, 13, x.TopElevation);
                SetN(ws, r, 14, x.Cover);
                SetN(ws, r, 15, x.Easting);
                SetN(ws, r, 16, x.Northing);
                SetN(ws, r, 17, x.Latitude);
                SetN(ws, r, 18, x.Longitude);
            });

        // ── WW Fitting ──────────────────────────────────────────────────
        result.WWFittings = FillSheet(pkg, "WW Fitting", 2,
            db.WWFittings.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.FacilityOwner);
                Set(ws, r, 4,  x.Size);
                Set(ws, r, 5,  x.SizeSecondary);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Material);
                Set(ws, r, 8,  x.LiningManufacturer);
                Set(ws, r, 9,  x.LiningMaterial);
                SetN(ws, r, 10, x.TopElevation);
                SetN(ws, r, 11, x.GradeElevation);
                SetN(ws, r, 12, x.Depth);
                SetN(ws, r, 13, x.Easting);
                SetN(ws, r, 14, x.Northing);
                SetN(ws, r, 15, x.Latitude);
                SetN(ws, r, 16, x.Longitude);
            });

        // ── Manhole ─────────────────────────────────────────────────────
        result.Manholes = FillSheet(pkg, "Manhole", 2,
            db.Manholes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.FacilityOwner);
                Set(ws, r, 4,  x.ManholeType);
                Set(ws, r, 5,  x.DropType);
                Set(ws, r, 6,  x.Manufacturer);
                Set(ws, r, 7,  x.Size);
                Set(ws, r, 8,  x.Material);
                Set(ws, r, 9,  x.LiningMaterial);
                Set(ws, r, 10, x.LiningManufacturer);
                SetN(ws, r, 11, x.RimElevation);
                Set(ws, r, 12, x.InvertElevationsWithDirections);
                SetN(ws, r, 13, x.LowestInvertElevation);
                Set(ws, r, 14, x.ExteriorJointTapeType);
                Set(ws, r, 15, x.ExteriorJointTapeManufacturer);
                SetCoords(ws, r, 16, 17, 18, 19,
                    x.Easting, x.Northing, x.Latitude, x.Longitude);
                Set(ws, r, 20, x.RfidBarcode);
            });

        // ── WW Service Point & Meter ────────────────────────────────────
        result.WWServicePoints = FillSheet(pkg, "WW Service Point & Meter", 2,
            db.WWServicePoints.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                SetN(ws, r, 3,  x.GradeElevation);
                SetN(ws, r, 4,  x.TopElevation);
                SetN(ws, r, 5,  x.Cover);
                SetN(ws, r, 6,  x.Easting);
                SetN(ws, r, 7,  x.Northing);
                SetN(ws, r, 8,  x.Latitude);
                SetN(ws, r, 9,  x.Longitude);
            });

        // ── WW Valve ────────────────────────────────────────────────────
        result.WWValves = FillSheet(pkg, "WW Valve", 2,
            db.WWValves.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                Set(ws, r, 3,  x.ValveType);
                Set(ws, r, 4,  x.FacilityOwner);
                Set(ws, r, 5,  x.Size);
                Set(ws, r, 6,  x.Orientation);
                Set(ws, r, 7,  x.OpenDirection);
                SetN(ws, r, 8,  x.TurnsToOpen);
                SetN(ws, r, 9,  x.NutElevation);
                SetN(ws, r, 10, x.GradeElevation);
                SetN(ws, r, 11, x.DepthToNut);
                Set(ws, r, 12, x.Manufacturer);
                SetN(ws, r, 13, x.Easting);
                SetN(ws, r, 14, x.Northing);
                SetN(ws, r, 15, x.Latitude);
                SetN(ws, r, 16, x.Longitude);
            });

        // ── WW Locate Box ───────────────────────────────────────────────
        result.WWLocateBoxes = FillSheet(pkg, "WW Locate Box", 2,
            db.WWLocateBoxes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1,  x.PartKey);
                Set(ws, r, 2,  x.Subtype);
                SetN(ws, r, 3,  x.Easting);
                SetN(ws, r, 4,  x.Northing);
                SetN(ws, r, 5,  x.Latitude);
                SetN(ws, r, 6,  x.Longitude);
            });

        // ── Reclaimed Pipe Run ──────────────────────────────────────────
        result.ReclaimedPipes = FillSheet(pkg, "Reclaimed Pipe Run", 2,
            db.ReclaimedPipes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Subtype); Set(ws, r, 3, x.FacilityOwner);
                Set(ws, r, 4, x.Size); Set(ws, r, 5, x.PipeClass); Set(ws, r, 6, x.Manufacturer);
                Set(ws, r, 7, x.Material); Set(ws, r, 8, x.LiningManufacturer);
                Set(ws, r, 9, x.LiningMaterial); SetN(ws, r, 10, x.Length);
            });

        // ── Reclaimed Points along Pipe ─────────────────────────────────
        result.ReclaimedPoints = FillSheet(pkg, "Reclaimed Points along Pipe", 2,
            db.ReclaimedPoints.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillPointCols(ws, r, x));

        // ── Reclaimed Fitting ───────────────────────────────────────────
        result.ReclaimedFittings = FillSheet(pkg, "Reclaimed Fitting", 2,
            db.ReclaimedFittings.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillFittingCols(ws, r, x));

        // ── Reclaimed Valve ─────────────────────────────────────────────
        result.ReclaimedValves = FillSheet(pkg, "Reclaimed Valve", 2,
            db.ReclaimedValves.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillValveCols(ws, r, x));

        // ── Reclaimed Hydrant ───────────────────────────────────────────
        result.ReclaimedHydrants = FillSheet(pkg, "Reclaimed Hydrant", 2,
            db.ReclaimedHydrants.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.FacilityOwner);
                Set(ws, r, 3, x.YearManufactured); Set(ws, r, 4, x.Manufacturer);
                SetN(ws, r, 5, x.Easting); SetN(ws, r, 6, x.Northing);
                SetN(ws, r, 7, x.Latitude); SetN(ws, r, 8, x.Longitude);
                Set(ws, r, 9, x.RfidBarcode);
            });

        // ── Reclaimed Meter / Locate Box ────────────────────────────────
        result.ReclaimedMeters = FillSheet(pkg, "Reclaimed Meter", 2,
            db.ReclaimedMeters.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillMeterCols(ws, r, x));

        result.ReclaimedLocateBoxes = FillSheet(pkg, "Reclaimed Locate Box", 2,
            db.ReclaimedLocateBoxes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => { Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Subtype);
                SetN(ws, r, 3, x.Easting); SetN(ws, r, 4, x.Northing);
                SetN(ws, r, 5, x.Latitude); SetN(ws, r, 6, x.Longitude); });

        // ── Chilled Pipe Run ────────────────────────────────────────────
        result.ChilledPipes = FillSheet(pkg, "Chilled Pipe Run", 2,
            db.ChilledPipes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) =>
            {
                Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Subtype); Set(ws, r, 3, x.FacilityOwner);
                Set(ws, r, 4, x.Size); Set(ws, r, 5, x.PipeClass); Set(ws, r, 6, x.Manufacturer);
                Set(ws, r, 7, x.Material); Set(ws, r, 8, x.LiningManufacturer);
                Set(ws, r, 9, x.LiningMaterial); SetN(ws, r, 10, x.Length);
            });

        result.ChilledPoints = FillSheet(pkg, "Chilled Points along Pipe", 2,
            db.ChilledPoints.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillPointCols(ws, r, x));

        result.ChilledFittings = FillSheet(pkg, "Chilled Fitting", 2,
            db.ChilledFittings.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillFittingCols(ws, r, x));

        result.ChilledValves = FillSheet(pkg, "Chilled Valve", 2,
            db.ChilledValves.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillValveCols(ws, r, x));

        result.ChilledMeters = FillSheet(pkg, "Chilled Meter", 2,
            db.ChilledMeters.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => FillMeterCols(ws, r, x));

        result.ChilledLocateBoxes = FillSheet(pkg, "Chilled Locate Box", 2,
            db.ChilledLocateBoxes.Where(x => x.ProjectId == projectId).ToList(),
            (ws, r, x) => { Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Subtype);
                SetN(ws, r, 3, x.Easting); SetN(ws, r, 4, x.Northing);
                SetN(ws, r, 5, x.Latitude); SetN(ws, r, 6, x.Longitude); });

        // ── As Built Info (JEA project meta) ────────────────────────────
        try
        {
            var infoSheet = pkg.Workbook.Worksheets["As Built Info transpose"];
            if (infoSheet != null)
            {
                infoSheet.Cells[2, 1].Value = projectName;
                infoSheet.Cells[2, 2].Value = DateTime.Now.ToString("MM/dd/yyyy");
            }
        }
        catch { /* Non-critical */ }

        pkg.Save();

        result.Success = true;
        result.OutputPath = outputPath;
        return result;
    }

    // ── Generic sheet filler ─────────────────────────────────────────────
    private static int FillSheet<T>(ExcelPackage pkg, string sheetName, int startRow,
        List<T> rows, Action<ExcelWorksheet, int, T> fill)
    {
        var ws = pkg.Workbook.Worksheets[sheetName];
        if (ws == null) return 0;

        int currentRow = startRow;
        foreach (var item in rows)
        {
            fill(ws, currentRow, item);
            currentRow++;
        }
        return rows.Count;
    }

    // ── Shared column-fill helpers (avoid duplicate lambda code) ─────────
    private static void FillPointCols(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.PipeRole); Set(ws, r, 3, x.Subtype);
        Set(ws, r, 4, x.FacilityOwner); Set(ws, r, 5, x.Size); Set(ws, r, 6, x.Orientation);
        Set(ws, r, 7, x.PipeClass); Set(ws, r, 8, x.Manufacturer); Set(ws, r, 9, x.Material);
        Set(ws, r, 10, x.LiningManufacturer); Set(ws, r, 11, x.LiningMaterial);
        SetN(ws, r, 12, x.GradeElevation); SetN(ws, r, 13, x.TopElevation);
        SetN(ws, r, 14, x.Cover); SetN(ws, r, 15, x.Easting); SetN(ws, r, 16, x.Northing);
        SetN(ws, r, 17, x.Latitude); SetN(ws, r, 18, x.Longitude);
    }

    private static void FillFittingCols(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Subtype); Set(ws, r, 3, x.FacilityOwner);
        Set(ws, r, 4, x.Size); Set(ws, r, 5, x.SizeSecondary); Set(ws, r, 6, x.Manufacturer);
        Set(ws, r, 7, x.Material); Set(ws, r, 8, x.LiningManufacturer); Set(ws, r, 9, x.LiningMaterial);
        SetN(ws, r, 10, x.TopElevation); SetN(ws, r, 11, x.GradeElevation); SetN(ws, r, 12, x.Depth);
        SetN(ws, r, 13, x.Easting); SetN(ws, r, 14, x.Northing);
        SetN(ws, r, 15, x.Latitude); SetN(ws, r, 16, x.Longitude);
    }

    private static void FillValveCols(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Subtype); Set(ws, r, 3, x.ValveType);
        Set(ws, r, 4, x.FacilityOwner); Set(ws, r, 5, x.Size); Set(ws, r, 6, x.Orientation);
        Set(ws, r, 7, x.OpenDirection); SetN(ws, r, 8, x.TurnsToOpen);
        SetN(ws, r, 9, x.NutElevation); SetN(ws, r, 10, x.GradeElevation);
        SetN(ws, r, 11, x.DepthToNut); Set(ws, r, 12, x.Manufacturer);
        SetN(ws, r, 13, x.Easting); SetN(ws, r, 14, x.Northing);
        SetN(ws, r, 15, x.Latitude); SetN(ws, r, 16, x.Longitude);
    }

    private static void FillMeterCols(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws, r, 1, x.PartKey); Set(ws, r, 2, x.Size); Set(ws, r, 3, x.Subtype);
        Set(ws, r, 4, x.FacilityOwner); Set(ws, r, 5, x.Orientation);
        Set(ws, r, 6, x.Manufacturer); Set(ws, r, 7, x.Material);
        SetCoords(ws, r, 8, 9, 10, 11, x.Easting, x.Northing, x.Latitude, x.Longitude);
    }

    // ── Cell write helpers ───────────────────────────────────────────────
    private static void Set(ExcelWorksheet ws, int r, int c, string? val)
    {
        if (!string.IsNullOrEmpty(val))
            ws.Cells[r, c].Value = val;
    }

    private static void SetN(ExcelWorksheet ws, int r, int c, double? val)
    {
        if (val.HasValue && val.Value != 0)
            ws.Cells[r, c].Value = val.Value;
    }

    /// <summary>
    /// Writes Lat/Lon to the worksheet. If Lat/Lon is missing/zero but State Plane
    /// coords are present, automatically projects via StatePlaneConverter (EPSG:2236).
    /// </summary>
    private static void SetCoords(ExcelWorksheet ws, int r,
        int eastCol, int northCol, int latCol, int lonCol,
        double? easting, double? northing, double? lat, double? lon)
    {
        SetN(ws, r, eastCol,  easting);
        SetN(ws, r, northCol, northing);

        bool hasLatLon = lat.HasValue && lat != 0 && lon.HasValue && lon != 0;

        if (!hasLatLon && easting.HasValue && easting != 0
                       && northing.HasValue && northing != 0
                       && StatePlaneConverter.IsInJeaBounds(easting.Value, northing.Value))
        {
            try
            {
                var (computedLat, computedLon) = StatePlaneConverter.ToLatLon(
                    easting.Value, northing.Value);
                lat = computedLat;
                lon = computedLon;
            }
            catch { /* leave null if projection fails */ }
        }

        SetN(ws, r, latCol, lat);
        SetN(ws, r, lonCol, lon);
    }
}

/// <summary>Summary of what the export produced — row counts per sheet.</summary>
public class JeaExportResult
{
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }

    public int PipeCrossings     { get; set; }
    public int WaterPipes        { get; set; }
    public int WaterPoints       { get; set; }
    public int WaterFittings     { get; set; }
    public int WaterValves       { get; set; }
    public int WaterHydrants     { get; set; }
    public int WaterMeters       { get; set; }
    public int WaterLocateBoxes  { get; set; }
    public int WWGravityPipes    { get; set; }
    public int WWPressurePipes   { get; set; }
    public int WWPoints          { get; set; }
    public int WWFittings        { get; set; }
    public int Manholes          { get; set; }
    public int WWServicePoints   { get; set; }
    public int WWValves          { get; set; }
    public int WWLocateBoxes     { get; set; }
    public int ReclaimedPipes    { get; set; }
    public int ReclaimedPoints   { get; set; }
    public int ReclaimedFittings { get; set; }
    public int ReclaimedValves   { get; set; }
    public int ReclaimedHydrants { get; set; }
    public int ReclaimedMeters   { get; set; }
    public int ReclaimedLocateBoxes { get; set; }
    public int ChilledPipes      { get; set; }
    public int ChilledPoints     { get; set; }
    public int ChilledFittings   { get; set; }
    public int ChilledValves     { get; set; }
    public int ChilledMeters     { get; set; }
    public int ChilledLocateBoxes { get; set; }

    public int TotalRows =>
        PipeCrossings + WaterPipes + WaterPoints + WaterFittings + WaterValves +
        WaterHydrants + WaterMeters + WaterLocateBoxes + WWGravityPipes +
        WWPressurePipes + WWPoints + WWFittings + Manholes + WWServicePoints +
        WWValves + WWLocateBoxes + ReclaimedPipes + ReclaimedPoints +
        ReclaimedFittings + ReclaimedValves + ReclaimedHydrants + ReclaimedMeters +
        ReclaimedLocateBoxes + ChilledPipes + ChilledPoints + ChilledFittings +
        ChilledValves + ChilledMeters + ChilledLocateBoxes;

    public static JeaExportResult Fail(string msg) =>
        new() { Success = false, ErrorMessage = msg };

    public string Summary()
    {
        return $"""
            ╔═══════════════════════════════════════════════
            ║  JEA As-Built Export Summary
            ╠═══════════════════════════════════════════════
            ║  Pipe Crossings        : {PipeCrossings,4}
            ║  Water Pipes           : {WaterPipes,4}
            ║  Water Points along Pipe: {WaterPoints,3}
            ║  Water Fittings        : {WaterFittings,4}
            ║  Water Valves          : {WaterValves,4}
            ║  Water Hydrants        : {WaterHydrants,4}
            ║  Water Meters          : {WaterMeters,4}
            ║  Water Locate Boxes    : {WaterLocateBoxes,4}
            ║  WW Gravity Pipes      : {WWGravityPipes,4}
            ║  WW Pressure Pipes     : {WWPressurePipes,4}
            ║  WW Points along Pipe  : {WWPoints,4}
            ║  WW Fittings           : {WWFittings,4}
            ║  Manholes              : {Manholes,4}
            ║  WW Service Points     : {WWServicePoints,4}
            ║  WW Valves             : {WWValves,4}
            ║  WW Locate Boxes       : {WWLocateBoxes,4}
            ║  Reclaimed Pipes       : {ReclaimedPipes,4}
            ║  Reclaimed Points      : {ReclaimedPoints,4}
            ║  Reclaimed Fittings    : {ReclaimedFittings,4}
            ║  Reclaimed Valves      : {ReclaimedValves,4}
            ║  Reclaimed Hydrants    : {ReclaimedHydrants,4}
            ║  Reclaimed Meters      : {ReclaimedMeters,4}
            ║  Reclaimed Locate Boxes: {ReclaimedLocateBoxes,4}
            ║  Chilled Pipes         : {ChilledPipes,4}
            ║  Chilled Points        : {ChilledPoints,4}
            ║  Chilled Fittings      : {ChilledFittings,4}
            ║  Chilled Valves        : {ChilledValves,4}
            ║  Chilled Meters        : {ChilledMeters,4}
            ║  Chilled Locate Boxes  : {ChilledLocateBoxes,4}
            ╠═══════════════════════════════════════════════
            ║  TOTAL ROWS EXPORTED   : {TotalRows,4}
            ╚═══════════════════════════════════════════════
            Saved to: {OutputPath}
            """;
    }
}
