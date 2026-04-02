using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RCS.Cogo.Wpf.Services;

/// <summary>
/// Generates DXF files containing JEA-standard field table layouts.
/// Tables are rendered as grids of LINE entities + TEXT entities,
/// matching the AutoCAD TABLE style shown on JEA As-Built drawings.
/// </summary>
public class JeaTableDxfExporter
{
    // ── Standard JEA table geometry (drawing units = survey feet) ──
    private const double ColHeight     = 0.28;   // height of each data row
    private const double HdrHeight     = 0.40;   // height of header row (wraps)
    private const double TitleHeight   = 0.35;   // height of title banner row
    private const double TxtData       = 0.10;   // data text height
    private const double TxtHeader     = 0.09;   // header text height (smaller to fit multis)
    private const double TxtTitle      = 0.14;   // title text height
    private const double MarginX       = 0.04;   // inner left padding for cell text
    private const double OriginX       = 0.0;
    private const double OriginY       = 0.0;

    // ── DXF color indices (ACI) ──
    private const int ColorCyan    = 4;  // header / title text
    private const int ColorWhite   = 7;  // data text
    private const int ColorBorder  = 6;  // magenta border (JEA standard)

    // ─────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Defines a single column: its header label and pixel-unit width in DXF drawing units.
    /// </summary>
    public record TableColumn(string Header, double Width);
    public record TableRow(IReadOnlyList<string?> Cells, string? BlockName, double? Northing, double? Easting);

    /// <summary>
    /// Render a single table and write it to <paramref name="outputPath"/>.
    /// </summary>
    public void Export(
        string title,
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        string outputPath)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);

        double x0 = OriginX;
        double y0 = OriginY;

        // Total table width
        double tableWidth = 0;
        foreach (var c in columns) tableWidth += c.Width;

        // Build from TOP down (y decreases with each row)
        double curY = y0;

        // 1. Title row
        DrawFilledRow(sb, x0, curY, tableWidth, TitleHeight, ColorBorder);
        DrawText(sb, x0 + tableWidth / 2, curY - TitleHeight / 2,
                 title, TxtTitle, ColorCyan, "ROMAND", hAlign: 1);
        curY -= TitleHeight;

        // 2. Column header row
        DrawRowBorder(sb, x0, curY, tableWidth, HdrHeight, ColorBorder);
        double cx = x0;
        foreach (var col in columns)
        {
            // Center headers in their cell
            DrawText(sb, cx + col.Width / 2, curY - HdrHeight / 2,
                     col.Header, TxtHeader, ColorCyan, "ROMAND", hAlign: 1);
            cx += col.Width;
        }
        // Vertical dividers inside header
        cx = x0;
        foreach (var col in columns)
        {
            DrawLine(sb, cx, curY, cx, curY - HdrHeight, ColorBorder);
            cx += col.Width;
        }
        DrawLine(sb, cx, curY, cx, curY - HdrHeight, ColorBorder); // right edge
        curY -= HdrHeight;

        // 3. Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            TableRow row = rows[r];

            // Compute dynamic height based on max wrapped lines
            int maxLines = 1;
            var wrappedCells = new List<IReadOnlyList<string>>();
            
            for (int c = 0; c < columns.Count; c++)
            {
                string? val = c < row.Cells.Count ? row.Cells[c] : null;
                var lines = WrapText(val, columns[c].Width - MarginX * 2, TxtData);
                if (lines.Count > maxLines) maxLines = lines.Count;
                wrappedCells.Add(lines);
            }

            double dynamicRowHeight = Math.Max(ColHeight, (maxLines * (TxtData * 1.5)) + (MarginX * 2));

            DrawRowBorder(sb, x0, curY, tableWidth, dynamicRowHeight, ColorBorder);
            cx = x0;

            for (int c = 0; c < columns.Count; c++)
            {
                var lines = wrappedCells[c];
                double lineY = curY - MarginX;

                if (lines.Count == 1) // center vertically if 1 line
                {
                    DrawText(sb, cx + MarginX, curY - (dynamicRowHeight / 2),
                             lines[0], TxtData, ColorWhite, "SIMPLEX", hAlign: 0);
                }
                else
                {
                    foreach (var lineText in lines)
                    {
                        DrawText(sb, cx + MarginX, lineY - TxtData,
                                 lineText, TxtData, ColorWhite, "SIMPLEX", hAlign: 0);
                        lineY -= (TxtData * 1.5);
                    }
                }

                DrawLine(sb, cx, curY, cx, curY - dynamicRowHeight, ColorBorder);
                cx += columns[c].Width;
            }

            DrawLine(sb, cx, curY, cx, curY - dynamicRowHeight, ColorBorder);
            curY -= dynamicRowHeight;

            // Automatically place an INSERT block at real-world coordinates if data is present
            if (!string.IsNullOrEmpty(row.BlockName) && row.Easting.HasValue && row.Northing.HasValue)
            {
                DrawBlockInsert(sb, row.BlockName, row.Easting.Value, row.Northing.Value);
            }
        }

        // Bottom closing line (already drawn by last row bottom)
        AppendFooter(sb);
        File.WriteAllText(outputPath, sb.ToString(), Encoding.ASCII);
    }

    // ─────────────────────────────────────────────────────────────
    //  DXF primitive helpers
    // ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<string> WrapText(string? text, double maxColWidth, double textHeight)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        string safe = text.Replace("\r\n", " ").Replace("\n", " ").Replace("&#10;", " ");
        
        // Rough chars per line heuristic. Simplex chars are approx textHeight * 0.8 wide.
        int maxChars = (int)(maxColWidth / (textHeight * 0.8));
        if (maxChars < 1) maxChars = 1;

        var words = safe.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        string curLine = "";
        
        foreach (var w in words)
        {
            if (curLine.Length + w.Length + 1 <= maxChars)
            {
                curLine += (curLine == "" ? "" : " ") + w;
            }
            else
            {
                if (curLine != "") lines.Add(curLine);
                curLine = w;
            }
        }
        if (curLine != "") lines.Add(curLine);
        return lines;
    }

    private static void DrawBlockInsert(StringBuilder sb, string blockName, double x, double y)
    {
        sb.AppendLine("0\nINSERT");
        sb.AppendLine("8\nMODEL_BLOCKS");
        sb.AppendLine($"2\n{blockName}");
        sb.AppendLine($"10\n{x:F4}");
        sb.AppendLine($"20\n{y:F4}");
        sb.AppendLine("30\n0.0");
    }

    private static void DrawLine(StringBuilder sb,
        double x1, double y1, double x2, double y2, int color)
    {
        sb.AppendLine("0\nLINE");
        sb.AppendLine($"8\nTABLE_BORDER");
        sb.AppendLine($"62\n{color}");
        sb.AppendLine($"10\n{x1:F4}");
        sb.AppendLine($"20\n{y1:F4}");
        sb.AppendLine($"30\n0.0");
        sb.AppendLine($"11\n{x2:F4}");
        sb.AppendLine($"21\n{y2:F4}");
        sb.AppendLine($"31\n0.0");
    }

    /// <summary>Draw the 4 border lines of a row (top, bottom, outer sides — no internal dividers).</summary>
    private static void DrawRowBorder(StringBuilder sb,
        double x, double y, double width, double height, int color)
    {
        DrawLine(sb, x, y,          x + width, y,          color); // top
        DrawLine(sb, x, y - height, x + width, y - height, color); // bottom
        DrawLine(sb, x, y,          x,          y - height, color); // left
        DrawLine(sb, x + width, y,  x + width,  y - height, color); // right
    }

    /// <summary>Draw a filled (solid-hatch) title row using a SOLID entity.</summary>
    private static void DrawFilledRow(StringBuilder sb,
        double x, double y, double width, double height, int color)
    {
        // DXF SOLID (filled quad) — used as title background
        sb.AppendLine("0\nSOLID");
        sb.AppendLine($"8\nTABLE_TITLE");
        sb.AppendLine($"62\n{color}");
        sb.AppendLine($"10\n{x:F4}");
        sb.AppendLine($"20\n{y - height:F4}");
        sb.AppendLine($"30\n0.0");
        sb.AppendLine($"11\n{x + width:F4}");
        sb.AppendLine($"21\n{y - height:F4}");
        sb.AppendLine($"31\n0.0");
        sb.AppendLine($"12\n{x:F4}");
        sb.AppendLine($"22\n{y:F4}");
        sb.AppendLine($"32\n0.0");
        sb.AppendLine($"13\n{x + width:F4}");
        sb.AppendLine($"23\n{y:F4}");
        sb.AppendLine($"33\n0.0");
    }

    /// <summary>
    /// Draw a TEXT entity.
    /// hAlign: 0=left, 1=center, 2=right
    /// </summary>
    private static void DrawText(StringBuilder sb,
        double x, double y, string text,
        double height, int color, string style,
        int hAlign = 0)
    {
        // Sanitise: strip newlines for basic TEXT (use \ for line breaks in display)
        string safe = text.Replace("\r\n", " ").Replace("\n", " ").Replace("&#10;", " ");

        sb.AppendLine("0\nTEXT");

        string layer = hAlign == 1 && height >= TxtHeader
            ? (height >= TxtTitle ? "TABLE_TITLE" : "TABLE_HEADER")
            : "TABLE_DATA";
        sb.AppendLine($"8\n{layer}");
        sb.AppendLine($"62\n{color}");
        sb.AppendLine($"10\n{x:F4}");      // insertion X
        sb.AppendLine($"20\n{y:F4}");      // insertion Y
        sb.AppendLine($"30\n0.0");
        sb.AppendLine($"40\n{height:F4}"); // text height
        sb.AppendLine($"1\n{safe}");       // string
        sb.AppendLine($"7\n{style}");      // text style
        sb.AppendLine($"72\n{hAlign}");    // H alignment
        sb.AppendLine($"11\n{x:F4}");     // alignment point X
        sb.AppendLine($"21\n{y:F4}");     // alignment point Y
        sb.AppendLine($"31\n0.0");
    }

    // ─────────────────────────────────────────────────────────────
    //  DXF boilerplate
    // ─────────────────────────────────────────────────────────────

    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine("0\nSECTION");
        sb.AppendLine("2\nHEADER");
        sb.AppendLine("9\n$ACADVER");
        sb.AppendLine("1\nAC1015");
        sb.AppendLine("9\n$INSUNITS");
        sb.AppendLine("70\n2");            // Feet
        sb.AppendLine("0\nENDSEC");

        // TABLES section — define layers and text styles
        sb.AppendLine("0\nSECTION");
        sb.AppendLine("2\nTABLES");

        // --- LTYPE ---
        sb.AppendLine("0\nTABLE");
        sb.AppendLine("2\nLTYPE");
        sb.AppendLine("70\n1");
        sb.AppendLine("0\nLTYPE");
        sb.AppendLine("2\nCONTINUOUS");
        sb.AppendLine("70\n0");
        sb.AppendLine("3\nSolid line");
        sb.AppendLine("72\n65");
        sb.AppendLine("73\n0");
        sb.AppendLine("40\n0.0");
        sb.AppendLine("0\nENDTAB");

        // --- LAYER ---
        sb.AppendLine("0\nTABLE");
        sb.AppendLine("2\nLAYER");
        sb.AppendLine("70\n4");
        AppendLayer(sb, "TABLE_BORDER", ColorBorder);
        AppendLayer(sb, "TABLE_TITLE",  ColorCyan);
        AppendLayer(sb, "TABLE_HEADER", ColorCyan);
        AppendLayer(sb, "TABLE_DATA",   ColorWhite);
        AppendLayer(sb, "MODEL_BLOCKS", ColorWhite);
        sb.AppendLine("0\nENDTAB");

        // --- STYLE (text styles) ---
        sb.AppendLine("0\nTABLE");
        sb.AppendLine("2\nSTYLE");
        sb.AppendLine("70\n3");
        AppendTextStyle(sb, "STANDARD", "txt");
        AppendTextStyle(sb, "SIMPLEX",  "simplex.shx");
        AppendTextStyle(sb, "ROMAND",   "romand.shx");
        sb.AppendLine("0\nENDTAB");

        sb.AppendLine("0\nENDSEC");

        // ENTITIES section
        sb.AppendLine("0\nSECTION");
        sb.AppendLine("2\nENTITIES");
    }

    private static void AppendLayer(StringBuilder sb, string name, int color)
    {
        sb.AppendLine("0\nLAYER");
        sb.AppendLine($"2\n{name}");
        sb.AppendLine("70\n0");
        sb.AppendLine($"62\n{color}");
        sb.AppendLine("6\nCONTINUOUS");
    }

    private static void AppendTextStyle(StringBuilder sb, string name, string font)
    {
        sb.AppendLine("0\nSTYLE");
        sb.AppendLine($"2\n{name}");
        sb.AppendLine("70\n0");
        sb.AppendLine("40\n0.0");
        sb.AppendLine("41\n1.0");
        sb.AppendLine($"3\n{font}");
        sb.AppendLine("4\n");
    }

    private static void AppendFooter(StringBuilder sb)
    {
        sb.AppendLine("0\nENDSEC");
        sb.AppendLine("0\nEOF");
    }
}
