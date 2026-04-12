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

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Generates a paginated WPF <see cref="FlowDocument"/> COGO Point Report.
/// <list type="bullet">
///   <item><description><see cref="Print"/> — sends to the system PrintDialog.</description></item>
///   <item><description><see cref="SaveAsXps"/> — writes an XPS file the user can open in
///     Edge and print to PDF via "Microsoft Print to PDF".</description></item>
/// </list>
/// Zero NuGet dependencies — uses only WPF's built-in FlowDocument + XPS stack.
/// </summary>
public static class PointReportPrinter
{
    // ── Page geometry (US Letter @ 96 dpi) ───────────────────────────────────
    private const double PageWidth  = 816;
    private const double PageHeight = 1056;
    private const double Margin     = 60;

    // ── Palette (prints well on white paper) ─────────────────────────────────
    private static readonly Brush BrHdrBg   = new SolidColorBrush(Color.FromRgb(0x1A, 0x1E, 0x2A));
    private static readonly Brush BrAltBg   = new SolidColorBrush(Color.FromRgb(0xF2, 0xF4, 0xFF));
    private static readonly Brush BrHdrText = Brushes.White;

    static PointReportPrinter()
    {
        ((SolidColorBrush)BrHdrBg).Freeze();
        ((SolidColorBrush)BrAltBg).Freeze();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Row model
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>A single printable point row.</summary>
    public sealed record PointRow(
        string  Id,
        double  Northing,
        double  Easting,
        double  Elevation,
        string  Description,
        string? Latitude  = null,
        string? Longitude = null
    );

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Opens the system PrintDialog and prints the report.</summary>
    public static void Print(IEnumerable<PointRow> rows, string projectName,
        bool includeGps = false)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        var pag = Paginate(rows, projectName, includeGps);
        dlg.PrintDocument(pag, $"COGO Points – {projectName}");
    }

    /// <summary>
    /// Saves the report as an XPS document.
    /// Open the resulting .xps in Microsoft Edge → File → Print → "Microsoft Print to PDF".
    /// </summary>
    public static void SaveAsXps(IEnumerable<PointRow> rows, string projectName,
        bool includeGps = false, Window? owner = null)
    {
        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
            Title      = "Save Point Report",
            Filter     = "XPS Document (*.xps)|*.xps",
            DefaultExt = ".xps",
            FileName   = $"PointReport_{DateTime.Now:yyyyMMdd}"
        };
        if (saveDlg.ShowDialog(owner) != true) return;

        var pag = Paginate(rows, projectName, includeGps);

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

    private static DocumentPaginator Paginate(IEnumerable<PointRow> rows,
        string projectName, bool includeGps)
    {
        var doc = Build(rows.ToList(), projectName, includeGps);
        var pag = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        pag.PageSize = new Size(PageWidth, PageHeight);
        return pag;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Document builder
    // ─────────────────────────────────────────────────────────────────────────

    private static FlowDocument Build(List<PointRow> rows, string projectName, bool includeGps)
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

        // ── Title ─────────────────────────────────────────────────────────────
        doc.Blocks.Add(new Paragraph(new Bold(new Run("COGO POINT REPORT")))
        {
            FontSize      = 18,
            TextAlignment = TextAlignment.Center,
            Margin        = new Thickness(0, 0, 0, 4)
        });

        doc.Blocks.Add(new Paragraph(new Run(
            $"Project: {projectName}   |   {DateTime.Now:yyyy-MM-dd HH:mm}   |   RCS COGO Enterprise v{ver}"))
        {
            TextAlignment = TextAlignment.Center,
            Margin        = new Thickness(0, 0, 0, 8)
        });

        doc.Blocks.Add(Rule());

        // ── Stats ─────────────────────────────────────────────────────────────
        if (rows.Count > 0)
        {
            doc.Blocks.Add(new Paragraph(new Run(
                $"Total Points: {rows.Count}   |   " +
                $"N: {rows.Min(r => r.Northing):N3} – {rows.Max(r => r.Northing):N3}   |   " +
                $"E: {rows.Min(r => r.Easting):N3} – {rows.Max(r => r.Easting):N3}"))
            { Margin = new Thickness(0, 6, 0, 10) });
        }

        // ── Table ─────────────────────────────────────────────────────────────
        doc.Blocks.Add(BuildTable(rows, includeGps));

        // ── Footer ────────────────────────────────────────────────────────────
        doc.Blocks.Add(Rule());
        doc.Blocks.Add(new Paragraph(new Run(
            $"Generated by RCS COGO Enterprise v{ver}  •  All coordinates US Survey Feet  •  {DateTime.Now:R}"))
        {
            FontSize      = 8,
            Foreground    = Brushes.Gray,
            TextAlignment = TextAlignment.Center
        });

        return doc;
    }

    private static Paragraph Rule() =>
        new(new Run(new string('─', 95))) { Margin = new Thickness(0, 2, 0, 2) };

    private static Table BuildTable(List<PointRow> rows, bool includeGps)
    {
        var t = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };

        double[] widths = includeGps
            ? new[] { 48d, 82, 82, 58, 118, 86, 86 }
            : new[] { 52d, 105, 105, 68, 170 };

        foreach (var w in widths)
            t.Columns.Add(new TableColumn { Width = new GridLength(w) });

        // Header
        var hg  = new TableRowGroup();
        t.RowGroups.Add(hg);
        var hdr = new TableRow { Background = BrHdrBg };
        hg.Rows.Add(hdr);

        string[] cols = includeGps
            ? new[] { "Pt #", "Northing", "Easting", "Elev (ft)", "Description", "Latitude", "Longitude" }
            : new[] { "Pt #", "Northing", "Easting", "Elev (ft)", "Description" };

        foreach (var h in cols)
            hdr.Cells.Add(Cell(new Bold(new Run(h)), BrHdrText, TextAlignment.Center));

        // Data rows
        var dg = new TableRowGroup();
        t.RowGroups.Add(dg);
        for (int i = 0; i < rows.Count; i++)
        {
            var r   = rows[i];
            var row = new TableRow { Background = i % 2 == 0 ? Brushes.White : BrAltBg };
            dg.Rows.Add(row);
            row.Cells.Add(Cell(r.Id,              align: TextAlignment.Left));
            row.Cells.Add(Cell($"{r.Northing:N4}"));
            row.Cells.Add(Cell($"{r.Easting:N4}"));
            row.Cells.Add(Cell($"{r.Elevation:N4}"));
            row.Cells.Add(Cell(r.Description,     align: TextAlignment.Left));
            if (includeGps)
            {
                row.Cells.Add(Cell(r.Latitude  ?? "—"));
                row.Cells.Add(Cell(r.Longitude ?? "—"));
            }
        }
        return t;
    }

    private static TableCell Cell(Inline inline, Brush? fg = null,
        TextAlignment align = TextAlignment.Right)
    {
        var p = new Paragraph(inline)
            { Margin = new Thickness(4, 2, 4, 2), TextAlignment = align };
        if (fg != null) p.Foreground = fg;
        return new TableCell(p);
    }

    private static TableCell Cell(string text, Brush? fg = null,
        TextAlignment align = TextAlignment.Right)
        => Cell(new Run(text), fg, align);
}
