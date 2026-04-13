using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

/// <summary>
/// Generates a paginated WPF <see cref="FlowDocument"/> As-Built Report.
/// <list type="bullet">
///   <item><description><see cref="Print"/> — sends to the system PrintDialog.</description></item>
///   <item><description><see cref="SaveAsXps"/> — writes an XPS file the user can open in
///     Edge and print to PDF via "Microsoft Print to PDF".</description></item>
/// </list>
/// Zero NuGet dependencies — uses only WPF's built-in FlowDocument + XPS stack.
/// </summary>
public static class AsBuiltReportPrinter
{
    // ── Page geometry (US Letter @ 96 dpi) ───────────────────────────────────
    private const double PageWidth  = 816;
    private const double PageHeight = 1056;
    private const double Margin     = 60;

    // ── Palette (prints well on white paper) ─────────────────────────────────
    private static readonly Brush BrHdrBg   = new SolidColorBrush(Color.FromRgb(0x1A, 0x1E, 0x2A));
    private static readonly Brush BrAltBg   = new SolidColorBrush(Color.FromRgb(0xF2, 0xF4, 0xFF));
    private static readonly Brush BrHdrText = Brushes.White;

    static AsBuiltReportPrinter()
    {
        ((SolidColorBrush)BrHdrBg).Freeze();
        ((SolidColorBrush)BrAltBg).Freeze();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Opens the system PrintDialog and prints the report.</summary>
    public static void Print(AsBuiltJob job)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var pag = Paginate(job);
        dlg.PrintDocument(pag, $"As-Built Report – {job.Identity?.JobNumber ?? "Job"}");
    }

    /// <summary>
    /// Saves the report as an XPS document.
    /// Open the resulting .xps in Microsoft Edge → File → Print → "Microsoft Print to PDF".
    /// </summary>
    public static void SaveAsXps(AsBuiltJob job, Window? owner = null)
    {
        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
            Title      = "Save As-Built Report",
            Filter     = "XPS Document (*.xps)|*.xps",
            DefaultExt = ".xps",
            FileName   = $"AsBuiltReport_{job.Identity?.JobNumber ?? "Job"}_{DateTime.Now:yyyyMMdd}"
        };
        if (saveDlg.ShowDialog(owner) != true) return;

        var pag = Paginate(job);

        using var xpsDoc = new XpsDocument(saveDlg.FileName, FileAccess.Write);
        XpsDocumentWriter xdw = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
        xdw.Write(pag);

        MessageBox.Show(
            $"Saved:\n{saveDlg.FileName}\n\nTip: Open in Microsoft Edge → Print → Microsoft Print to PDF.",
            "Report Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static DocumentPaginator Paginate(AsBuiltJob job)
    {
        var doc = Build(job);
        var pag = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        pag.PageSize = new Size(PageWidth, PageHeight);
        return pag;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Document builder
    // ─────────────────────────────────────────────────────────────────────────

    private static FlowDocument Build(AsBuiltJob job)
    {
        string ver = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "2.x";

        var doc = new FlowDocument
        {
            PageWidth      = PageWidth,
            PageHeight     = PageHeight,
            PagePadding    = new Thickness(Margin),
            FontFamily     = new FontFamily("Consolas"),
            FontSize       = 10,
            Foreground     = Brushes.Black,
            Background     = Brushes.White,
            ColumnWidth    = double.PositiveInfinity
        };

        var id = job.Identity ?? new ProjectIdentity();

        // ── Title ─────────────────────────────────────────────────────────────
        doc.Blocks.Add(new Paragraph(new Bold(new Run("AS-BUILT UTILITY SURVEY REPORT")))
        {
            FontSize      = 18,
            TextAlignment = TextAlignment.Center,
            Margin        = new Thickness(0, 0, 0, 4)
        });

        doc.Blocks.Add(Rule());

        // ── Identity Stats ───────────────────────────────────────────────────
        var identityGrid = new Table { CellSpacing = 0 };
        identityGrid.Columns.Add(new TableColumn { Width = new GridLength(100) });
        identityGrid.Columns.Add(new TableColumn { Width = new GridLength(200) });
        identityGrid.Columns.Add(new TableColumn { Width = new GridLength(100) });
        identityGrid.Columns.Add(new TableColumn { Width = new GridLength(200) });

        var iRowGroup = new TableRowGroup();
        identityGrid.RowGroups.Add(iRowGroup);
        
        var iterRow1 = new TableRow();
        iterRow1.Cells.Add(Cell(new Bold(new Run("Job Number:")), align: TextAlignment.Left));
        iterRow1.Cells.Add(Cell(id.JobNumber, align: TextAlignment.Left));
        iterRow1.Cells.Add(Cell(new Bold(new Run("Field Date:")), align: TextAlignment.Left));
        iterRow1.Cells.Add(Cell(id.FieldDate?.ToString("MM/dd/yyyy") ?? "—", align: TextAlignment.Left));
        iRowGroup.Rows.Add(iterRow1);

        var iterRow2 = new TableRow();
        iterRow2.Cells.Add(Cell(new Bold(new Run("Client:")), align: TextAlignment.Left));
        iterRow2.Cells.Add(Cell(id.ClientName, align: TextAlignment.Left));
        iterRow2.Cells.Add(Cell(new Bold(new Run("Drafter:")), align: TextAlignment.Left));
        iterRow2.Cells.Add(Cell(id.Drafter, align: TextAlignment.Left));
        iRowGroup.Rows.Add(iterRow2);

        var iterRow3 = new TableRow();
        iterRow3.Cells.Add(Cell(new Bold(new Run("Utility Owner:")), align: TextAlignment.Left));
        iterRow3.Cells.Add(Cell(id.UtilityOwner, align: TextAlignment.Left));
        iterRow3.Cells.Add(Cell(new Bold(new Run("Checker:")), align: TextAlignment.Left));
        iterRow3.Cells.Add(Cell(id.Checker, align: TextAlignment.Left));
        iRowGroup.Rows.Add(iterRow3);

        doc.Blocks.Add(identityGrid);
        doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 0, 0, 10) });

        // ── Structures ────────────────────────────────────────────────────────
        var strs = job.Network?.GetAllStructures().ToList() ?? [];
        if (strs.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Bold(new Run($"STRUCTURES ({strs.Count})")))
            {
                FontSize = 14,
                Margin = new Thickness(0, 10, 0, 6)
            });
            doc.Blocks.Add(BuildStructuresTable(strs));
        }

        // ── Pipe Runs ─────────────────────────────────────────────────────────
        var runs = job.Network?.GetAllRuns().ToList() ?? [];
        if (runs.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Bold(new Run($"PIPE RUNS ({runs.Count})")))
            {
                FontSize = 14,
                Margin = new Thickness(0, 15, 0, 6)
            });
            doc.Blocks.Add(BuildPipeRunsTable(runs));
        }

        // ── Parts Mapping ─────────────────────────────────────────────────────
        var parts = job.PartMappings?.ToList() ?? [];
        if (parts.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Bold(new Run($"PARTS MAPPING ({parts.Count})")))
            {
                FontSize = 14,
                Margin = new Thickness(0, 15, 0, 6)
            });
            doc.Blocks.Add(BuildPartsTable(parts));
        }

        // ── Summary ───────────────────────────────────────────────────────────
        doc.Blocks.Add(new Paragraph(new Bold(new Run("SUMMARY")))
        {
            FontSize = 14,
            Margin = new Thickness(0, 15, 0, 6)
        });

        var summaryP = new Paragraph();
        summaryP.Inlines.Add(new Run($"Total Structures: {strs.Count}\n"));
        summaryP.Inlines.Add(new Run($"Total Pipe Runs : {runs.Count}\n"));
        summaryP.Inlines.Add(new Run($"Total Length    : {runs.Sum(r => r.ComputedLength):F2} ft\n"));
        summaryP.Inlines.Add(new Run($"Unique Utilities: {runs.Select(r => r.Type).Distinct().Count()}\n"));
        doc.Blocks.Add(summaryP);

        // ── Footer ────────────────────────────────────────────────────────────
        doc.Blocks.Add(Rule());
        doc.Blocks.Add(new Paragraph(new Run(
            $"Generated by RCS As-Built Pro v{ver}  •  Coordinates: {job.Environment}  •  {DateTime.Now:R}"))
        {
            FontSize      = 8,
            Foreground    = Brushes.Gray,
            TextAlignment = TextAlignment.Center,
            Margin        = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }

    private static Paragraph Rule() =>
        new(new Run(new string('─', 95))) { Margin = new Thickness(0, 2, 0, 2) };

    private static Table BuildStructuresTable(List<RCS.Piping.Core.Models.PipeStructure> strs)
    {
        var t = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };

        double[] widths = new[] { 70d, 180d, 80d, 80d, 80d };
        foreach (var w in widths) t.Columns.Add(new TableColumn { Width = new GridLength(w) });

        var hg = new TableRowGroup();
        t.RowGroups.Add(hg);
        var hdr = new TableRow { Background = BrHdrBg };
        hg.Rows.Add(hdr);

        string[] cols = new[] { "Point", "Type", "Rim Elev", "Inv In", "Inv Out" };
        foreach (var h in cols) hdr.Cells.Add(Cell(new Bold(new Run(h)), BrHdrText, TextAlignment.Center));

        var dg = new TableRowGroup();
        t.RowGroups.Add(dg);
        for (int i = 0; i < strs.Count; i++)
        {
            var st = strs[i];
            var row = new TableRow { Background = i % 2 == 0 ? Brushes.White : BrAltBg };
            dg.Rows.Add(row);
            row.Cells.Add(Cell(st.PointId, align: TextAlignment.Left));
            row.Cells.Add(Cell(st.Type ?? "—", align: TextAlignment.Left));
            row.Cells.Add(Cell(Fmt(st.RimElevation)));
            row.Cells.Add(Cell(Fmt(st.InvertIn)));
            row.Cells.Add(Cell(Fmt(st.InvertOut)));
        }
        return t;
    }

    private static Table BuildPipeRunsTable(List<RCS.Piping.Core.Models.PipeRun> runs)
    {
        var t = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };

        double[] widths = new[] { 50d, 50d, 120d, 40d, 60d, 60d, 50d, 60d, 60d };
        foreach (var w in widths) t.Columns.Add(new TableColumn { Width = new GridLength(w) });

        var hg = new TableRowGroup();
        t.RowGroups.Add(hg);
        var hdr = new TableRow { Background = BrHdrBg };
        hg.Rows.Add(hdr);

        string[] cols = new[] { "From", "To", "Type", "Dia", "Mat", "Length", "Slope%", "InvFr", "InvTo" };
        foreach (var h in cols) hdr.Cells.Add(Cell(new Bold(new Run(h)), BrHdrText, TextAlignment.Center));

        var dg = new TableRowGroup();
        t.RowGroups.Add(dg);
        for (int i = 0; i < runs.Count; i++)
        {
            var r = runs[i];
            var row = new TableRow { Background = i % 2 == 0 ? Brushes.White : BrAltBg };
            dg.Rows.Add(row);
            row.Cells.Add(Cell(r.FromPointId, align: TextAlignment.Left));
            row.Cells.Add(Cell(r.ToPointId, align: TextAlignment.Left));
            row.Cells.Add(Cell(r.Type ?? "—", align: TextAlignment.Left));
            row.Cells.Add(Cell($"{r.Diameter:F1}"));
            row.Cells.Add(Cell(r.Material ?? "—", align: TextAlignment.Left));
            row.Cells.Add(Cell($"{r.ComputedLength:F2}"));
            row.Cells.Add(Cell($"{r.SlopePercent:F2}"));
            row.Cells.Add(Cell(Fmt(r.InvertStart)));
            row.Cells.Add(Cell(Fmt(r.InvertEnd)));
        }
        return t;
    }

    private static Table BuildPartsTable(List<PartMappingEntry> parts)
    {
        var t = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };

        double[] widths = new[] { 100d, 150d, 150d, 90d };
        foreach (var w in widths) t.Columns.Add(new TableColumn { Width = new GridLength(w) });

        var hg = new TableRowGroup();
        t.RowGroups.Add(hg);
        var hdr = new TableRow { Background = BrHdrBg };
        hg.Rows.Add(hdr);

        string[] cols = new[] { "AssetId", "PartKey", "Manufacturer", "Status" };
        foreach (var h in cols) hdr.Cells.Add(Cell(new Bold(new Run(h)), BrHdrText, TextAlignment.Center));

        var dg = new TableRowGroup();
        t.RowGroups.Add(dg);
        for (int i = 0; i < parts.Count; i++)
        {
            var m = parts[i];
            var row = new TableRow { Background = i % 2 == 0 ? Brushes.White : BrAltBg };
            dg.Rows.Add(row);
            row.Cells.Add(Cell(m.AssetId, align: TextAlignment.Left));
            row.Cells.Add(Cell(m.PartKey, align: TextAlignment.Left));
            row.Cells.Add(Cell(m.Manufacturer, align: TextAlignment.Left));
            row.Cells.Add(Cell(m.Status.ToString(), align: TextAlignment.Left));
        }
        return t;
    }

    private static TableCell Cell(Inline inline, Brush? fg = null, TextAlignment align = TextAlignment.Right)
    {
        var p = new Paragraph(inline) { Margin = new Thickness(4, 2, 4, 2), TextAlignment = align };
        if (fg != null) p.Foreground = fg;
        return new TableCell(p);
    }

    private static TableCell Cell(string text, Brush? fg = null, TextAlignment align = TextAlignment.Right)
        => Cell(new Run(text), fg, align);

    private static string Fmt(double? val) => val.HasValue ? $"{val:F2}" : "—";
}
