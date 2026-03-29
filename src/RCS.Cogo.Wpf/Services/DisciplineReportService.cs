using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Cogo.Wpf.Services;

// ── Per-discipline report result ─────────────────────────────────────────────
public class DisciplineReportResult
{
    public bool    Success      { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputPath   { get; set; }

    /// <summary>Total rows written across all sheets in this report.</summary>
    public int TotalRows { get; set; }

    /// <summary>Summary lines (sheet name → count) for the audit log.</summary>
    public List<(string Sheet, int Count)> SheetCounts { get; } = new();

    public static DisciplineReportResult Fail(string msg) =>
        new() { Success = false, ErrorMessage = msg };

    public string Summary()
    {
        var lines = SheetCounts
            .Where(s => s.Count > 0)
            .Select(s => $"  {s.Sheet,-32}: {s.Count,4}");
        return string.Join(Environment.NewLine, lines)
               + $"{Environment.NewLine}  TOTAL ROWS : {TotalRows,4}";
    }
}

/// <summary>
/// Generates a standalone, single-discipline Excel (.xlsx) report directly from the
/// project database.  Each discipline gets its own workbook with one sheet per asset
/// type.  Uses the same EPPlus helpers already established in JeaExportService.
/// </summary>
public static class DisciplineReportService
{
    static DisciplineReportService() =>
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    // ── Public entry points (one per discipline) ──────────────────────────────

    public static DisciplineReportResult ExportWater(
        string outputPath, string projectId, string projectName) =>
        Export(outputPath, projectId, projectName, "Water", WriteWaterSheets);

    public static DisciplineReportResult ExportSewer(
        string outputPath, string projectId, string projectName) =>
        Export(outputPath, projectId, projectName, "Sanitary Sewer", WriteSewerSheets);

    public static DisciplineReportResult ExportGas(
        string outputPath, string projectId, string projectName) =>
        Export(outputPath, projectId, projectName, "Gas", WriteGasSheets);

    public static DisciplineReportResult ExportElectric(
        string outputPath, string projectId, string projectName) =>
        Export(outputPath, projectId, projectName, "Electric", WriteElectricSheets);

    public static DisciplineReportResult ExportDrainage(
        string outputPath, string projectId, string projectName) =>
        Export(outputPath, projectId, projectName, "Storm Drainage", WriteDrainageSheets);

    // ── Generic workbook driver ───────────────────────────────────────────────

    private static DisciplineReportResult Export(
        string outputPath, string projectId, string projectName,
        string disciplineLabel,
        Action<ExcelPackage, AppDbContext, string, DisciplineReportResult> writeSheets)
    {
        var result = new DisciplineReportResult();
        try
        {
            using var pkg = new ExcelPackage();

            // ── Cover sheet ──────────────────────────────────────────────────
            var cover = pkg.Workbook.Worksheets.Add("Report Info");
            StyleCoverSheet(cover, disciplineLabel, projectName, projectId);

            using var db = new AppDbContext();
            writeSheets(pkg, db, projectId, result);

            pkg.SaveAs(new FileInfo(outputPath));

            result.Success    = true;
            result.OutputPath = outputPath;
        }
        catch (Exception ex)
        {
            result.Success      = false;
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    // ── Water ─────────────────────────────────────────────────────────────────

    private static void WriteWaterSheets(
        ExcelPackage pkg, AppDbContext db, string pid, DisciplineReportResult res)
    {
        res.SheetCounts.Add(("Pipe Crossing Table",
            AddSheet(pkg, "Pipe Crossing Table",
                db.PipeCrossings.Where(x => x.ProjectId == pid).ToList(),
                PipeCrossingHeaders,
                (ws, r, x) =>
                {
                    Set(ws,r,1,x.CrossingNumber); Set(ws,r,2,x.UpperPipeType); Set(ws,r,3,x.UpperPipeSize);
                    SetN(ws,r,4,x.GradeElevation); SetN(ws,r,5,x.UpperPipeTopElevation);
                    SetN(ws,r,6,x.UpperCover); SetN(ws,r,7,x.UpperPipeBottomElevation);
                    Set(ws,r,8,x.LowerPipeType); Set(ws,r,9,x.LowerPipeSize);
                    SetN(ws,r,10,x.LowerPipeTopElevation); SetN(ws,r,11,x.LowerCover);
                    SetN(ws,r,12,x.Separation);
                    SetN(ws,r,13,x.Easting); SetN(ws,r,14,x.Northing);
                    SetN(ws,r,15,x.Latitude); SetN(ws,r,16,x.Longitude);
                })));

        res.SheetCounts.Add(("Water Pipe Run",
            AddSheet(pkg, "Water Pipe Run",
                db.WaterPipes.Where(x => x.ProjectId == pid).ToList(),
                PipeRunHeaders,
                (ws, r, x) => FillPipeRun(ws, r, x))));

        res.SheetCounts.Add(("Water Points along Pipe",
            AddSheet(pkg, "Water Points along Pipe",
                db.WaterPoints.Where(x => x.ProjectId == pid).ToList(),
                PointAlongPipeHeaders,
                (ws, r, x) => FillPointAlongPipe(ws, r, x))));

        res.SheetCounts.Add(("Water Fitting",
            AddSheet(pkg, "Water Fitting",
                db.WaterFittings.Where(x => x.ProjectId == pid).ToList(),
                FittingHeaders,
                (ws, r, x) => FillFitting(ws, r, x))));

        res.SheetCounts.Add(("Water Valve",
            AddSheet(pkg, "Water Valve",
                db.WaterValves.Where(x => x.ProjectId == pid).ToList(),
                ValveHeaders,
                (ws, r, x) => FillValve(ws, r, x))));

        res.SheetCounts.Add(("Water Hydrant",
            AddSheet(pkg, "Water Hydrant",
                db.WaterHydrants.Where(x => x.ProjectId == pid).ToList(),
                new[] { "Part Key","Facility Owner","Year Mfg","Manufacturer","Easting","Northing","Latitude","Longitude","RFID Barcode" },
                (ws, r, x) =>
                {
                    Set(ws,r,1,x.PartKey); Set(ws,r,2,x.FacilityOwner);
                    Set(ws,r,3,x.YearManufactured); Set(ws,r,4,x.Manufacturer);
                    SetN(ws,r,5,x.Easting); SetN(ws,r,6,x.Northing);
                    SetN(ws,r,7,x.Latitude); SetN(ws,r,8,x.Longitude);
                    Set(ws,r,9,x.RfidBarcode);
                })));

        res.SheetCounts.Add(("Water Meter",
            AddSheet(pkg, "Water Meter",
                db.WaterMeters.Where(x => x.ProjectId == pid).ToList(),
                MeterHeaders,
                (ws, r, x) => FillMeter(ws, r, x))));

        res.SheetCounts.Add(("Water Locate Box",
            AddSheet(pkg, "Water Locate Box",
                db.WaterLocateBoxes.Where(x => x.ProjectId == pid).ToList(),
                LocateBoxHeaders,
                (ws, r, x) => FillLocateBox(ws, r, x))));

        res.TotalRows = res.SheetCounts.Sum(s => s.Count);
    }

    // ── Sewer ─────────────────────────────────────────────────────────────────

    private static void WriteSewerSheets(
        ExcelPackage pkg, AppDbContext db, string pid, DisciplineReportResult res)
    {
        res.SheetCounts.Add(("WW Gravity Pipe Run",
            AddSheet(pkg, "WW Gravity Pipe Run",
                db.WWGravityPipes.Where(x => x.ProjectId == pid).ToList(),
                GravityPipeHeaders,
                (ws, r, x) => FillGravityPipe(ws, r, x))));

        res.SheetCounts.Add(("WW Pressure Pipe Run",
            AddSheet(pkg, "WW Pressure Pipe Run",
                db.WWPressurePipes.Where(x => x.ProjectId == pid).ToList(),
                PipeRunHeaders,
                (ws, r, x) => FillPipeRun(ws, r, x))));

        res.SheetCounts.Add(("WW Points along Pipe",
            AddSheet(pkg, "WW Points along Pipe",
                db.WWPoints.Where(x => x.ProjectId == pid).ToList(),
                PointAlongPipeHeaders,
                (ws, r, x) => FillPointAlongPipe(ws, r, x))));

        res.SheetCounts.Add(("WW Fitting",
            AddSheet(pkg, "WW Fitting",
                db.WWFittings.Where(x => x.ProjectId == pid).ToList(),
                FittingHeaders,
                (ws, r, x) => FillFitting(ws, r, x))));

        res.SheetCounts.Add(("Manhole",
            AddSheet(pkg, "Manhole",
                db.Manholes.Where(x => x.ProjectId == pid).ToList(),
                ManholeHeaders,
                (ws, r, x) => FillManhole(ws, r, x))));

        res.SheetCounts.Add(("WW Service Point",
            AddSheet(pkg, "WW Service Point",
                db.WWServicePoints.Where(x => x.ProjectId == pid).ToList(),
                ServicePointHeaders,
                (ws, r, x) => FillServicePoint(ws, r, x))));

        res.SheetCounts.Add(("WW Valve",
            AddSheet(pkg, "WW Valve",
                db.WWValves.Where(x => x.ProjectId == pid).ToList(),
                ValveHeaders,
                (ws, r, x) => FillValve(ws, r, x))));

        res.SheetCounts.Add(("WW Locate Box",
            AddSheet(pkg, "WW Locate Box",
                db.WWLocateBoxes.Where(x => x.ProjectId == pid).ToList(),
                LocateBoxHeaders,
                (ws, r, x) => FillLocateBox(ws, r, x))));

        res.TotalRows = res.SheetCounts.Sum(s => s.Count);
    }

    // ── Gas ───────────────────────────────────────────────────────────────────

    private static void WriteGasSheets(
        ExcelPackage pkg, AppDbContext db, string pid, DisciplineReportResult res)
    {
        res.SheetCounts.Add(("Gas Gravity Pipe Run",
            AddSheet(pkg, "Gas Gravity Pipe Run",
                db.GGravityPipes.Where(x => x.ProjectId == pid).ToList(),
                GravityPipeHeaders,
                (ws, r, x) => FillGravityPipe(ws, r, x))));

        res.SheetCounts.Add(("Gas Pressure Pipe Run",
            AddSheet(pkg, "Gas Pressure Pipe Run",
                db.GPressurePipes.Where(x => x.ProjectId == pid).ToList(),
                PipeRunHeaders,
                (ws, r, x) => FillPipeRun(ws, r, x))));

        res.SheetCounts.Add(("Gas Points along Pipe",
            AddSheet(pkg, "Gas Points along Pipe",
                db.GPoints.Where(x => x.ProjectId == pid).ToList(),
                PointAlongPipeHeaders,
                (ws, r, x) => FillPointAlongPipe(ws, r, x))));

        res.SheetCounts.Add(("Gas Fitting",
            AddSheet(pkg, "Gas Fitting",
                db.GFittings.Where(x => x.ProjectId == pid).ToList(),
                FittingHeaders,
                (ws, r, x) => FillFitting(ws, r, x))));

        res.SheetCounts.Add(("Gas Manhole",
            AddSheet(pkg, "Gas Manhole",
                db.GManholes.Where(x => x.ProjectId == pid).ToList(),
                ManholeHeaders,
                (ws, r, x) => FillManhole(ws, r, x))));

        res.SheetCounts.Add(("Gas Service Point",
            AddSheet(pkg, "Gas Service Point",
                db.GServicePoints.Where(x => x.ProjectId == pid).ToList(),
                ServicePointHeaders,
                (ws, r, x) => FillServicePoint(ws, r, x))));

        res.SheetCounts.Add(("Gas Valve",
            AddSheet(pkg, "Gas Valve",
                db.GValves.Where(x => x.ProjectId == pid).ToList(),
                ValveHeaders,
                (ws, r, x) => FillValve(ws, r, x))));

        res.SheetCounts.Add(("Gas Locate Box",
            AddSheet(pkg, "Gas Locate Box",
                db.GLocateBoxes.Where(x => x.ProjectId == pid).ToList(),
                LocateBoxHeaders,
                (ws, r, x) => FillLocateBox(ws, r, x))));

        res.TotalRows = res.SheetCounts.Sum(s => s.Count);
    }

    // ── Electric ─────────────────────────────────────────────────────────────

    private static void WriteElectricSheets(
        ExcelPackage pkg, AppDbContext db, string pid, DisciplineReportResult res)
    {
        res.SheetCounts.Add(("Electric Gravity Conduit Run",
            AddSheet(pkg, "Electric Gravity Conduit Run",
                db.EGravityPipes.Where(x => x.ProjectId == pid).ToList(),
                GravityPipeHeaders,
                (ws, r, x) => FillGravityPipe(ws, r, x))));

        res.SheetCounts.Add(("Electric Pressure Conduit Run",
            AddSheet(pkg, "Electric Pressure Conduit Run",
                db.EPressurePipes.Where(x => x.ProjectId == pid).ToList(),
                PipeRunHeaders,
                (ws, r, x) => FillPipeRun(ws, r, x))));

        res.SheetCounts.Add(("Electric Points along Conduit",
            AddSheet(pkg, "Electric Points along Conduit",
                db.EPoints.Where(x => x.ProjectId == pid).ToList(),
                PointAlongPipeHeaders,
                (ws, r, x) => FillPointAlongPipe(ws, r, x))));

        res.SheetCounts.Add(("Electric Fitting",
            AddSheet(pkg, "Electric Fitting",
                db.EFittings.Where(x => x.ProjectId == pid).ToList(),
                FittingHeaders,
                (ws, r, x) => FillFitting(ws, r, x))));

        res.SheetCounts.Add(("Electric Manhole",
            AddSheet(pkg, "Electric Manhole",
                db.EManholes.Where(x => x.ProjectId == pid).ToList(),
                ManholeHeaders,
                (ws, r, x) => FillManhole(ws, r, x))));

        res.SheetCounts.Add(("Electric Service Point",
            AddSheet(pkg, "Electric Service Point",
                db.EServicePoints.Where(x => x.ProjectId == pid).ToList(),
                ServicePointHeaders,
                (ws, r, x) => FillServicePoint(ws, r, x))));

        res.SheetCounts.Add(("Electric Valve",
            AddSheet(pkg, "Electric Valve",
                db.EValves.Where(x => x.ProjectId == pid).ToList(),
                ValveHeaders,
                (ws, r, x) => FillValve(ws, r, x))));

        res.SheetCounts.Add(("Electric Locate Box",
            AddSheet(pkg, "Electric Locate Box",
                db.ELocateBoxes.Where(x => x.ProjectId == pid).ToList(),
                LocateBoxHeaders,
                (ws, r, x) => FillLocateBox(ws, r, x))));

        res.TotalRows = res.SheetCounts.Sum(s => s.Count);
    }

    // ── Storm Drainage ────────────────────────────────────────────────────────

    private static void WriteDrainageSheets(
        ExcelPackage pkg, AppDbContext db, string pid, DisciplineReportResult res)
    {
        res.SheetCounts.Add(("Storm Gravity Pipe Run",
            AddSheet(pkg, "Storm Gravity Pipe Run",
                db.STGravityPipes.Where(x => x.ProjectId == pid).ToList(),
                GravityPipeHeaders,
                (ws, r, x) => FillGravityPipe(ws, r, x))));

        res.SheetCounts.Add(("Storm Pressure Pipe Run",
            AddSheet(pkg, "Storm Pressure Pipe Run",
                db.STPressurePipes.Where(x => x.ProjectId == pid).ToList(),
                PipeRunHeaders,
                (ws, r, x) => FillPipeRun(ws, r, x))));

        res.SheetCounts.Add(("Storm Points along Pipe",
            AddSheet(pkg, "Storm Points along Pipe",
                db.STPoints.Where(x => x.ProjectId == pid).ToList(),
                PointAlongPipeHeaders,
                (ws, r, x) => FillPointAlongPipe(ws, r, x))));

        res.SheetCounts.Add(("Storm Fitting",
            AddSheet(pkg, "Storm Fitting",
                db.STFittings.Where(x => x.ProjectId == pid).ToList(),
                FittingHeaders,
                (ws, r, x) => FillFitting(ws, r, x))));

        res.SheetCounts.Add(("Storm Manhole",
            AddSheet(pkg, "Storm Manhole",
                db.STManholes.Where(x => x.ProjectId == pid).ToList(),
                ManholeHeaders,
                (ws, r, x) => FillManhole(ws, r, x))));

        res.SheetCounts.Add(("Storm Service Point",
            AddSheet(pkg, "Storm Service Point",
                db.STServicePoints.Where(x => x.ProjectId == pid).ToList(),
                ServicePointHeaders,
                (ws, r, x) => FillServicePoint(ws, r, x))));

        res.SheetCounts.Add(("Storm Valve",
            AddSheet(pkg, "Storm Valve",
                db.STValves.Where(x => x.ProjectId == pid).ToList(),
                ValveHeaders,
                (ws, r, x) => FillValve(ws, r, x))));

        res.SheetCounts.Add(("Storm Locate Box",
            AddSheet(pkg, "Storm Locate Box",
                db.STLocateBoxes.Where(x => x.ProjectId == pid).ToList(),
                LocateBoxHeaders,
                (ws, r, x) => FillLocateBox(ws, r, x))));

        res.TotalRows = res.SheetCounts.Sum(s => s.Count);
    }

    // ── Generic sheet builder ─────────────────────────────────────────────────

    private static int AddSheet<T>(
        ExcelPackage pkg,
        string sheetName,
        List<T> rows,
        string[] headers,
        Action<ExcelWorksheet, int, T> fill)
    {
        var ws = pkg.Workbook.Worksheets.Add(sheetName);
        StyleHeaderRow(ws, headers);

        int currentRow = 2;
        foreach (var item in rows)
        {
            fill(ws, currentRow, item);
            // Alternate row shading
            if (currentRow % 2 == 0)
                ws.Cells[currentRow, 1, currentRow, headers.Length]
                  .Style.Fill.SetBackground(System.Drawing.Color.FromArgb(242, 242, 242));
            currentRow++;
        }

        ws.Cells[ws.Dimension?.Address ?? "A1"].AutoFitColumns(8, 40);
        return rows.Count;
    }

    // ── Column-fill helpers (shared across disciplines) ───────────────────────

    private static void FillPipeRun(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey);  Set(ws,r,2,x.Subtype);     Set(ws,r,3,x.FacilityOwner);
        Set(ws,r,4,x.Size);     Set(ws,r,5,x.PipeClass);    Set(ws,r,6,x.Manufacturer);
        Set(ws,r,7,x.Material); Set(ws,r,8,x.LiningManufacturer);
        Set(ws,r,9,x.LiningMaterial); SetN(ws,r,10,x.Length);
    }

    private static void FillGravityPipe(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        FillPipeRun(ws, r, x);
        SetN(ws,r,11,x.DownstreamInvert); SetN(ws,r,12,x.DownstreamGrade);
        SetN(ws,r,13,x.UpstreamInvert);   SetN(ws,r,14,x.UpstreamGrade);
        SetN(ws,r,15,x.Slope);
    }

    private static void FillPointAlongPipe(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey);  Set(ws,r,2,x.PipeRole);    Set(ws,r,3,x.Subtype);
        Set(ws,r,4,x.FacilityOwner); Set(ws,r,5,x.Size);   Set(ws,r,6,x.Orientation);
        Set(ws,r,7,x.PipeClass); Set(ws,r,8,x.Manufacturer); Set(ws,r,9,x.Material);
        Set(ws,r,10,x.LiningManufacturer); Set(ws,r,11,x.LiningMaterial);
        SetN(ws,r,12,x.GradeElevation); SetN(ws,r,13,x.TopElevation); SetN(ws,r,14,x.Cover);
        SetN(ws,r,15,x.Easting); SetN(ws,r,16,x.Northing);
        SetN(ws,r,17,x.Latitude); SetN(ws,r,18,x.Longitude);
    }

    private static void FillFitting(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey);  Set(ws,r,2,x.Subtype);         Set(ws,r,3,x.FacilityOwner);
        Set(ws,r,4,x.Size);     Set(ws,r,5,x.SizeSecondary);   Set(ws,r,6,x.Manufacturer);
        Set(ws,r,7,x.Material); Set(ws,r,8,x.LiningManufacturer); Set(ws,r,9,x.LiningMaterial);
        SetN(ws,r,10,x.TopElevation); SetN(ws,r,11,x.GradeElevation); SetN(ws,r,12,x.Depth);
        SetN(ws,r,13,x.Easting); SetN(ws,r,14,x.Northing);
        SetN(ws,r,15,x.Latitude); SetN(ws,r,16,x.Longitude);
    }

    private static void FillValve(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey);  Set(ws,r,2,x.Subtype);      Set(ws,r,3,x.ValveType);
        Set(ws,r,4,x.FacilityOwner); Set(ws,r,5,x.Size);    Set(ws,r,6,x.Orientation);
        Set(ws,r,7,x.OpenDirection); SetN(ws,r,8,x.TurnsToOpen);
        SetN(ws,r,9,x.NutElevation); SetN(ws,r,10,x.GradeElevation);
        SetN(ws,r,11,x.DepthToNut); Set(ws,r,12,x.Manufacturer);
        SetN(ws,r,13,x.Easting); SetN(ws,r,14,x.Northing);
        SetN(ws,r,15,x.Latitude); SetN(ws,r,16,x.Longitude);
    }

    private static void FillMeter(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey); Set(ws,r,2,x.Size);          Set(ws,r,3,x.Subtype);
        Set(ws,r,4,x.FacilityOwner); Set(ws,r,5,x.Orientation);
        Set(ws,r,6,x.Manufacturer);  Set(ws,r,7,x.Material);
        SetN(ws,r,8,x.Easting); SetN(ws,r,9,x.Northing);
        SetN(ws,r,10,x.Latitude); SetN(ws,r,11,x.Longitude);
    }

    private static void FillLocateBox(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey); Set(ws,r,2,x.Subtype);
        SetN(ws,r,3,x.Easting); SetN(ws,r,4,x.Northing);
        SetN(ws,r,5,x.Latitude); SetN(ws,r,6,x.Longitude);
    }

    private static void FillManhole(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey);  Set(ws,r,2,x.Subtype);       Set(ws,r,3,x.FacilityOwner);
        Set(ws,r,4,x.ManholeType); Set(ws,r,5,x.DropType);   Set(ws,r,6,x.Manufacturer);
        Set(ws,r,7,x.Size);     Set(ws,r,8,x.Material);       Set(ws,r,9,x.LiningMaterial);
        Set(ws,r,10,x.LiningManufacturer);
        SetN(ws,r,11,x.RimElevation);
        Set(ws,r,12,x.InvertElevationsWithDirections);
        SetN(ws,r,13,x.LowestInvertElevation);
        Set(ws,r,14,x.ExteriorJointTapeType);
        Set(ws,r,15,x.ExteriorJointTapeManufacturer);
        SetN(ws,r,16,x.Easting); SetN(ws,r,17,x.Northing);
        SetN(ws,r,18,x.Latitude); SetN(ws,r,19,x.Longitude);
        Set(ws,r,20,x.RfidBarcode);
    }

    private static void FillServicePoint(ExcelWorksheet ws, int r, InstalledAsset x)
    {
        Set(ws,r,1,x.PartKey); Set(ws,r,2,x.Subtype);
        SetN(ws,r,3,x.GradeElevation); SetN(ws,r,4,x.TopElevation); SetN(ws,r,5,x.Cover);
        SetN(ws,r,6,x.Easting); SetN(ws,r,7,x.Northing);
        SetN(ws,r,8,x.Latitude); SetN(ws,r,9,x.Longitude);
    }

    // ── Header column definitions ─────────────────────────────────────────────

    private static readonly string[] PipeCrossingHeaders =
    {
        "Crossing #","Upper Pipe Type","Upper Pipe Size",
        "Grade Elev","Upper Top Elev","Upper Cover","Upper Bot Elev",
        "Lower Pipe Type","Lower Pipe Size","Lower Top Elev","Lower Cover","Separation",
        "Easting","Northing","Latitude","Longitude"
    };

    private static readonly string[] PipeRunHeaders =
    {
        "Part Key","Subtype","Facility Owner","Size","Pipe Class",
        "Manufacturer","Material","Lining Mfg","Lining Material","Length (ft)"
    };

    private static readonly string[] GravityPipeHeaders =
    {
        "Part Key","Subtype","Facility Owner","Size","Pipe Class",
        "Manufacturer","Material","Lining Mfg","Lining Material","Length (ft)",
        "DS Invert","DS Grade","US Invert","US Grade","Slope"
    };

    private static readonly string[] PointAlongPipeHeaders =
    {
        "Part Key","Pipe Role","Subtype","Facility Owner","Size","Orientation",
        "Pipe Class","Manufacturer","Material","Lining Mfg","Lining Material",
        "Grade Elev","Top Elev","Cover","Easting","Northing","Latitude","Longitude"
    };

    private static readonly string[] FittingHeaders =
    {
        "Part Key","Subtype","Facility Owner","Size","Size 2",
        "Manufacturer","Material","Lining Mfg","Lining Material",
        "Top Elev","Grade Elev","Depth","Easting","Northing","Latitude","Longitude"
    };

    private static readonly string[] ValveHeaders =
    {
        "Part Key","Subtype","Valve Type","Facility Owner","Size","Orientation",
        "Open Direction","Turns To Open","Nut Elev","Grade Elev","Depth To Nut",
        "Manufacturer","Easting","Northing","Latitude","Longitude"
    };

    private static readonly string[] MeterHeaders =
    {
        "Part Key","Size","Subtype","Facility Owner","Orientation",
        "Manufacturer","Material","Easting","Northing","Latitude","Longitude"
    };

    private static readonly string[] LocateBoxHeaders =
    {
        "Part Key","Subtype","Easting","Northing","Latitude","Longitude"
    };

    private static readonly string[] ManholeHeaders =
    {
        "Part Key","Subtype","Facility Owner","Manhole Type","Drop Type",
        "Manufacturer","Size","Material","Lining Material","Lining Mfg",
        "Rim Elev","Invert Elev w/Dir","Lowest Invert Elev",
        "Ext Tape Type","Ext Tape Mfg",
        "Easting","Northing","Latitude","Longitude","RFID Barcode"
    };

    private static readonly string[] ServicePointHeaders =
    {
        "Part Key","Subtype","Grade Elev","Top Elev","Cover",
        "Easting","Northing","Latitude","Longitude"
    };

    // ── Styling helpers ───────────────────────────────────────────────────────

    private static void StyleHeaderRow(ExcelWorksheet ws, string[] headers)
    {
        for (int c = 1; c <= headers.Length; c++)
        {
            ws.Cells[1, c].Value = headers[c - 1];
        }

        var headerRange = ws.Cells[1, 1, 1, headers.Length];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        ws.Row(1).Height = 20;
        ws.View.FreezePanes(2, 1);
    }

    private static void StyleCoverSheet(
        ExcelWorksheet ws, string discipline, string projectName, string projectId)
    {
        ws.Cells["A1"].Value = $"RCS COGO Enterprise — {discipline} Report";
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 16;
        ws.Cells["A2"].Value = $"Project : {projectName}";
        ws.Cells["A3"].Value = $"Project ID: {projectId}";
        ws.Cells["A4"].Value = $"Generated : {DateTime.Now:yyyy-MM-dd HH:mm}";
        ws.Cells["A5"].Value = "Each tab contains all assets of that type for this project.";
        ws.Column(1).Width = 60;
    }

    // ── Cell write helpers ────────────────────────────────────────────────────

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
}
