using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using RCS.Data;
using RCS.Cogo.Wpf.Services;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Preview window for the seven JEA field asset tables.
/// Primary deliverable = "Export to DXF" button, which uses JeaTableDxfExporter
/// to produce a JEA-standard LINE+TEXT DXF table matching the CAD template style.
/// </summary>
public partial class InstalledAssetsTablesWindow : Window
{
    private readonly string _projectId;
    private readonly JeaTableDxfExporter _dxf = new();

    // ── Column definitions per table (Header, DXF-unit width) ─────────
    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsForceFitting = new[]
    {
        new JeaTableDxfExporter.TableColumn("NO.",                         0.30),
        new JeaTableDxfExporter.TableColumn("FITTING\nDESCRIPTION",        0.95),
        new JeaTableDxfExporter.TableColumn("FACILITY\nOWNER",             0.65),
        new JeaTableDxfExporter.TableColumn("FITTING SIZE\nPRIMARY (INCH)",0.72),
        new JeaTableDxfExporter.TableColumn("FITTING SIZE\nSECONDARY (INCH)",0.80),
        new JeaTableDxfExporter.TableColumn("FITTING TYPE",                0.90),
        new JeaTableDxfExporter.TableColumn("MANUFACTURER",                0.90),
        new JeaTableDxfExporter.TableColumn("FITTING\nMATERIAL",           0.68),
        new JeaTableDxfExporter.TableColumn("LINING\nMATERIAL",            0.68),
        new JeaTableDxfExporter.TableColumn("TOP FITTING\nELEVATION (FEET)",0.80),
        new JeaTableDxfExporter.TableColumn("FINISH\nGRADE",               0.55),
        new JeaTableDxfExporter.TableColumn("DEPTH\n(FEET)",               0.50),
        new JeaTableDxfExporter.TableColumn("EASTING",                     0.75),
        new JeaTableDxfExporter.TableColumn("NORTHING",                    0.75),
        new JeaTableDxfExporter.TableColumn("LATITUDE\n(DECIMAL DEGREE)",  0.90),
        new JeaTableDxfExporter.TableColumn("LONGITUDE\n(DECIMAL DEGREES)",0.95),
    };

    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsManholes = new[]
    {
        new JeaTableDxfExporter.TableColumn("NO.",                               0.30),
        new JeaTableDxfExporter.TableColumn("MANHOLE\nSUBTYPE",                  0.85),
        new JeaTableDxfExporter.TableColumn("FACILITY\nOWNER",                   0.65),
        new JeaTableDxfExporter.TableColumn("MANHOLE\nTYPE",                     0.80),
        new JeaTableDxfExporter.TableColumn("MANHOLE\nDROP TYPE",                0.75),
        new JeaTableDxfExporter.TableColumn("MANUFACTURER\nOR SUPPLIER",         0.95),
        new JeaTableDxfExporter.TableColumn("MANHOLE\nSIZE",                     0.65),
        new JeaTableDxfExporter.TableColumn("MANHOLE LINING\nMATERIAL",          0.90),
        new JeaTableDxfExporter.TableColumn("RIM\nELEVATION",                    0.65),
        new JeaTableDxfExporter.TableColumn("INVERT ELEVATIONS\n& DIRECTIONS",   1.10),
        new JeaTableDxfExporter.TableColumn("NORTHING",                          0.75),
        new JeaTableDxfExporter.TableColumn("EASTING",                           0.75),
        new JeaTableDxfExporter.TableColumn("LATITUDE",                          0.80),
        new JeaTableDxfExporter.TableColumn("LONGITUDE",                         0.85),
        new JeaTableDxfExporter.TableColumn("EXTERIOR JOINT TAPE\nTYPE & MANUFACTURER", 1.20),
        new JeaTableDxfExporter.TableColumn("RFID / BARCODE\nNUMBER",            0.90),
    };

    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsWaterFitting = ColsForceFitting;

    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsValves = new[]
    {
        new JeaTableDxfExporter.TableColumn("NO.",               0.30),
        new JeaTableDxfExporter.TableColumn("VALVE\nTYPE",       0.80),
        new JeaTableDxfExporter.TableColumn("FACILITY\nOWNER",   0.65),
        new JeaTableDxfExporter.TableColumn("VALVE SIZE\n(INCH)",0.70),
        new JeaTableDxfExporter.TableColumn("DIRECTION\nTO OPEN",0.75),
        new JeaTableDxfExporter.TableColumn("NO. TURNS\nTO OPEN",0.70),
        new JeaTableDxfExporter.TableColumn("TOP NUT",           0.60),
        new JeaTableDxfExporter.TableColumn("FINISH\nGRADE",     0.55),
        new JeaTableDxfExporter.TableColumn("DEPTH\nTO NUT",     0.60),
        new JeaTableDxfExporter.TableColumn("MANUFACTURER",      0.95),
        new JeaTableDxfExporter.TableColumn("NORTHING\nLATITUDE",0.75),
        new JeaTableDxfExporter.TableColumn("EASTING\nLONGITUDE",0.75),
        new JeaTableDxfExporter.TableColumn("LATITUDE\n(DECIMAL DEGREES)",  0.90),
        new JeaTableDxfExporter.TableColumn("LONGITUDE\n(DECIMAL DEGREES)", 0.95),
    };

    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsHydrants = new[]
    {
        new JeaTableDxfExporter.TableColumn("NO.",                         0.30),
        new JeaTableDxfExporter.TableColumn("HYDRANT\nSUBTYPE",            1.00),
        new JeaTableDxfExporter.TableColumn("FACILITY\nOWNER",             0.70),
        new JeaTableDxfExporter.TableColumn("YEAR\nMANUFACTURED",          0.78),
        new JeaTableDxfExporter.TableColumn("MANUFACTURER",                1.00),
        new JeaTableDxfExporter.TableColumn("EASTING",                     0.80),
        new JeaTableDxfExporter.TableColumn("NORTHING",                    0.80),
        new JeaTableDxfExporter.TableColumn("LATITUDE\n(DECIMAL DEGREES)", 1.00),
        new JeaTableDxfExporter.TableColumn("LONGITUDE",                   1.00),
    };

    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsServices = new[]
    {
        new JeaTableDxfExporter.TableColumn("NO.",                    0.30),
        new JeaTableDxfExporter.TableColumn("SERVICE\nTYPE",          0.85),
        new JeaTableDxfExporter.TableColumn("METER BOX\nSUBTYPE",    0.85),
        new JeaTableDxfExporter.TableColumn("FACILITY\nOWNER",        0.70),
        new JeaTableDxfExporter.TableColumn("METER BOX\nMANUFACTURER",1.00),
        new JeaTableDxfExporter.TableColumn("METER BOX\nMATERIAL",    0.80),
        new JeaTableDxfExporter.TableColumn("NORTHING\nLATITUDE",     0.85),
        new JeaTableDxfExporter.TableColumn("EASTING\nLONGITUDE",     0.85),
    };

    private static readonly IReadOnlyList<JeaTableDxfExporter.TableColumn> ColsLocate = new[]
    {
        new JeaTableDxfExporter.TableColumn("NO.",                0.30),
        new JeaTableDxfExporter.TableColumn("LOCATE BOX\nSUBTYPE",1.20),
        new JeaTableDxfExporter.TableColumn("NORTHING",           0.90),
        new JeaTableDxfExporter.TableColumn("EASTING",            0.90),
        new JeaTableDxfExporter.TableColumn("LATITUDE",           0.95),
        new JeaTableDxfExporter.TableColumn("LONGITUDE",          1.00),
    };

    // ── Table metadata ─────────────────────────────────────────────────
    private record TableDef(
        string Title,
        string DefaultFileName,
        IReadOnlyList<JeaTableDxfExporter.TableColumn> Columns,
        Func<AppDbContext, IReadOnlyList<IReadOnlyList<string?>>> LoadRows);

    private TableDef[] _tables = null!;

    // ─────────────────────────────────────────────────────────────
    public InstalledAssetsTablesWindow(string projectId, int initialTab = 0)
    {
        _projectId = projectId;
        InitializeComponent();
        MainTabs.SelectedIndex = initialTab;

        // Show the project ID we're querying for diagnostic purposes
        Loaded += (_, _2) =>
        {
            if (FindName("TxtProjectLabel") is System.Windows.Controls.TextBlock lbl)
                lbl.Text = $"Project ID: {_projectId}";
        };

        _tables = new TableDef[]
        {
            new("FORCE MAIN FITTING LOCATION TABLE",
                "ForceFittings_Table.dxf",
                ColsForceFitting,
                db => db.WWFittings
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype, r.FacilityOwner,
                        r.Size, r.SizeSecondary, r.FeatureType, r.Manufacturer,
                        r.Material, r.LiningMaterial,
                        Fmt(r.TopElevation), Fmt(r.GradeElevation), Fmt(r.Depth),
                        Fmt(r.Easting), Fmt(r.Northing),
                        Fmt6(r.Latitude), Fmt6(r.Longitude)
                    })
                    .ToList()),

            new("SANITARY MANHOLES",
                "SanitaryManholes_Table.dxf",
                ColsManholes,
                db => db.Manholes
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype, r.FacilityOwner,
                        r.ManholeType, r.DropType, r.Manufacturer,
                        r.Size, r.LiningMaterial,
                        Fmt(r.RimElevation), r.InvertElevationsWithDirections,
                        Fmt(r.Northing), Fmt(r.Easting),
                        Fmt6(r.Latitude), Fmt6(r.Longitude),
                        JoinTape(r.ExteriorJointTapeType, r.ExteriorJointTapeManufacturer),
                        r.RfidBarcode
                    })
                    .ToList()),

            new("WATER MAIN FITTING LOCATION TABLE",
                "WaterFittings_Table.dxf",
                ColsWaterFitting,
                db => db.WaterFittings
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype, r.FacilityOwner,
                        r.Size, r.SizeSecondary, r.FeatureType, r.Manufacturer,
                        r.Material, r.LiningMaterial,
                        Fmt(r.TopElevation), Fmt(r.GradeElevation), Fmt(r.Depth),
                        Fmt(r.Easting), Fmt(r.Northing),
                        Fmt6(r.Latitude), Fmt6(r.Longitude)
                    })
                    .ToList()),

            new("WATER VALVE DETAIL TABLE",
                "WaterValves_Table.dxf",
                ColsValves,
                db => db.WaterValves
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype, r.FacilityOwner,
                        r.Size, r.OpenDirection,
                        r.TurnsToOpen.HasValue ? r.TurnsToOpen.Value.ToString("F1") : null,
                        Fmt(r.NutElevation), Fmt(r.GradeElevation), Fmt(r.DepthToNut),
                        r.Manufacturer,
                        Fmt(r.Northing), Fmt(r.Easting),
                        Fmt6(r.Latitude), Fmt6(r.Longitude)
                    })
                    .ToList()),

            new("HYDRANT TABLE",
                "Hydrants_Table.dxf",
                ColsHydrants,
                db => db.WaterHydrants
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype, r.FacilityOwner,
                        r.YearManufactured, r.Manufacturer,
                        Fmt(r.Easting), Fmt(r.Northing),
                        Fmt6(r.Latitude), Fmt6(r.Longitude)
                    })
                    .ToList()),

            new("WATER SERVICES",
                "WaterServices_Table.dxf",
                ColsServices,
                db => db.WaterMeters
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype, r.FeatureType,
                        r.FacilityOwner, r.Manufacturer, r.Material,
                        Fmt(r.Northing), Fmt(r.Easting)
                    })
                    .ToList()),

            new("LOCATE WIRE BOX TABLE",
                "LocateBoxes_Table.dxf",
                ColsLocate,
                db => db.WaterLocateBoxes
                    .Where(x => x.ProjectId == _projectId)
                    .OrderBy(x => x.PartKey)
                    .ToList()
                    .Select((r, i) => (IReadOnlyList<string?>)new[]
                    {
                        (i+1).ToString(), r.Subtype,
                        Fmt(r.Northing), Fmt(r.Easting),
                        Fmt6(r.Latitude), Fmt6(r.Longitude)
                    })
                    .ToList()),
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Window Events
    // ─────────────────────────────────────────────────────────────

    private void Window_Loaded(object sender, RoutedEventArgs e) => LoadData();

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

    private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        int tabIdx = MainTabs.SelectedIndex;
        if (tabIdx < 0 || tabIdx >= _tables.Length) return;

        var def = _tables[tabIdx];

        var dlg = new SaveFileDialog
        {
            Title       = $"Export {def.Title} to DXF",
            Filter      = "DXF File (*.dxf)|*.dxf|All Files (*.*)|*.*",
            DefaultExt  = ".dxf",
            FileName    = def.DefaultFileName
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            using var db = new AppDbContext();
            var rows = def.LoadRows(db);

            _dxf.Export(def.Title, def.Columns, rows, dlg.FileName);

            TxtStatus.Text = $"DXF exported → {dlg.FileName}  ({rows.Count} data rows)";
            MessageBox.Show(
                $"DXF table exported successfully.\n\n{dlg.FileName}\n{rows.Count} rows",
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Export error: {ex.Message}";
            MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Data loading (WPF preview grids)
    // ─────────────────────────────────────────────────────────────

    private void LoadData()
    {
        TxtStatus.Text = "Loading…";
        try
        {
            using var db = new AppDbContext();

            // ── DIAGNOSTIC: raw totals across ALL projects ─────────────────
            int totalWF  = db.WaterFittings.Count();
            int totalMH  = db.Manholes.Count();
            int totalWV  = db.WaterValves.Count();
            int totalWH  = db.WaterHydrants.Count();
            int totalWM  = db.WaterMeters.Count();
            int totalWWF = db.WWFittings.Count();

            // Get all distinct project IDs that have data
            var distinctProjIds = db.WaterFittings.Select(x => x.ProjectId)
                .Union(db.WaterValves.Select(x => x.ProjectId))
                .Union(db.Manholes.Select(x => x.ProjectId))
                .Union(db.WaterHydrants.Select(x => x.ProjectId))
                .Distinct().ToList();

            string diagMsg = $"[DIAG] Querying ProjectId='{_projectId}' | " +
                             $"DB totals (all projects): WaterFittings={totalWF}, Manholes={totalMH}, " +
                             $"WaterValves={totalWV}, Hydrants={totalWH}, Meters={totalWM}, WWFittings={totalWWF} | " +
                             $"Project IDs in DB: [{string.Join(", ", distinctProjIds.Take(5).Select(p => $"'{p}'"))}]";
            TxtStatus.Text = diagMsg;
            // ──────────────────────────────────────────────────────────────

            // ── Force Main Fittings (WWFittings) ──────────────────────
            var ff = db.WWFittings.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridForceFittings.ItemsSource = ff.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.FacilityOwner, r.Size, r.SizeSecondary,
                r.FeatureType, r.Manufacturer, r.Material, r.LiningMaterial,
                r.TopElevation, r.GradeElevation, r.Depth,
                r.Easting, r.Northing, r.Latitude, r.Longitude
            }).ToList();

            // ── Sanitary Manholes ─────────────────────────────────────
            var mh = db.Manholes.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridManholes.ItemsSource = mh.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.FacilityOwner, r.ManholeType, r.DropType,
                r.Manufacturer, r.Size, r.LiningMaterial, r.RimElevation,
                r.InvertElevationsWithDirections, r.Northing, r.Easting,
                r.Latitude, r.Longitude,
                ExteriorJointTape = JoinTape(r.ExteriorJointTapeType, r.ExteriorJointTapeManufacturer),
                r.RfidBarcode
            }).ToList();

            // ── Water Main Fittings ───────────────────────────────────
            var wf = db.WaterFittings.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridWaterFittings.ItemsSource = wf.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.FacilityOwner, r.Size, r.SizeSecondary,
                r.FeatureType, r.Manufacturer, r.Material, r.LiningMaterial,
                r.TopElevation, r.GradeElevation, r.Depth,
                r.Easting, r.Northing, r.Latitude, r.Longitude
            }).ToList();

            // ── Water Valves ──────────────────────────────────────────
            var wv = db.WaterValves.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridWaterValves.ItemsSource = wv.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.FacilityOwner, r.Size, r.OpenDirection,
                r.TurnsToOpen, r.NutElevation, r.GradeElevation, r.DepthToNut,
                r.Manufacturer, r.Northing, r.Easting, r.Latitude, r.Longitude
            }).ToList();

            // ── Water Hydrants ────────────────────────────────────────
            var wh = db.WaterHydrants.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridWaterHydrants.ItemsSource = wh.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.FacilityOwner, r.YearManufactured,
                r.Manufacturer, r.Easting, r.Northing, r.Latitude, r.Longitude
            }).ToList();

            // ── Water Services (Meters) ───────────────────────────────
            var ws = db.WaterMeters.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridWaterServices.ItemsSource = ws.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.FeatureType, r.FacilityOwner,
                r.Manufacturer, r.Material, r.Northing, r.Easting
            }).ToList();

            // ── Locate Wire Boxes ─────────────────────────────────────
            var lb = db.WaterLocateBoxes.Where(x => x.ProjectId == _projectId)
                       .OrderBy(x => x.PartKey).ToList();
            GridLocateBoxes.ItemsSource = lb.Select((r, i) => new
            {
                No = i + 1, r.Subtype, r.Northing, r.Easting, r.Latitude, r.Longitude
            }).ToList();

            // ── Update header labels ──────────────────────────────────
            TxtCounts.Text =
                $"Force Main Fittings: {ff.Count}  |  Sanitary Manholes: {mh.Count}  |  " +
                $"Water Fittings: {wf.Count}  |  Valves: {wv.Count}  |  " +
                $"Hydrants: {wh.Count}  |  Services: {ws.Count}  |  Locate Boxes: {lb.Count}";

            TxtStatus.Text = "Data loaded — click \"Export Tab to DXF\" to generate the field table DXF.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Load error: {ex.Message}";
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────
    private static string? Fmt(double? v)  => v.HasValue ? v.Value.ToString("F2") : null;
    private static string? Fmt6(double? v) => v.HasValue ? v.Value.ToString("F6") : null;
    private static string? JoinTape(string? type, string? mfr)
        => string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(mfr)
            ? null
            : $"{type} {mfr}".Trim();
}
