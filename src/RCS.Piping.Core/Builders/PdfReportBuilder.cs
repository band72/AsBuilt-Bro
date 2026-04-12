using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

/// <summary>
/// Writes a minimal but spec-compliant binary PDF 1.4 document for an As-Built survey report.
/// Uses raw PDF object streams — no third-party library required.
///
/// Structure: single page, Helvetica-based, wrapped text, header/footer, full report body.
/// </summary>
public sealed class PdfReportBuilder
{
    private const float PageW   = 612f;  // US Letter width  (pt)
    private const float PageH   = 792f;  // US Letter height (pt)
    private const float MarginL = 54f;
    private const float MarginR = 558f;
    private const float MarginT = 738f;
    private const float MarginB = 54f;
    private const float LineH   = 13f;
    private const float FontSz  = 9f;
    private const float TitleSz = 13f;
    private const float HdrSz   = 10f;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Build(AsBuiltJob job, string outputPath)
    {
        var pageContents = BuildPages(job);
        int pageCount    = pageContents.Count;

        // Object IDs:
        // 1 = catalog, 2 = pages dictionary
        // 3 .. 2+pageCount          = page page-dictionary objects
        // 3+pageCount .. 2+2*pageCount = content stream objects
        // 3+2*pageCount = font F1 (Helvetica regular)
        // 4+2*pageCount = font F2 (Helvetica-Bold)
        int fontR      = 3 + 2 * pageCount;
        int fontB      = 4 + 2 * pageCount;
        int totalObjs  = fontB;

        using var ms = new MemoryStream();

        // helper – write raw string + flush
        void W(string s)
        {
            var b = Encoding.Latin1.GetBytes(s);
            ms.Write(b);
        }

        var xrefOffsets = new long[totalObjs];

        void StartObj(int id)
        {
            xrefOffsets[id - 1] = ms.Position;
            W($"{id} 0 obj\n");
        }

        void EndObj() => W("endobj\n");

        // Header
        W("%PDF-1.4\n%\xb5\xb6\n");

        // Obj 1 – catalog
        StartObj(1);
        W("<< /Type /Catalog /Pages 2 0 R >>");
        W("\n");
        EndObj();

        // Obj 2 – pages
        StartObj(2);
        var kids = string.Join(" ", Enumerable.Range(3, pageCount).Select(i => $"{i} 0 R"));
        W($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\n");
        EndObj();

        // Obj 3..2+pageCount – page dictionaries
        for (int p = 0; p < pageCount; p++)
        {
            int pgId       = 3 + p;
            int contentId  = 3 + pageCount + p;
            StartObj(pgId);
            W($"<< /Type /Page /Parent 2 0 R\n" +
              $"   /MediaBox [0 0 {(int)PageW} {(int)PageH}]\n" +
              $"   /Resources << /Font << /F1 {fontR} 0 R /F2 {fontB} 0 R >> >>\n" +
              $"   /Contents {contentId} 0 R >>\n");
            EndObj();
        }

        // Obj 3+pageCount..2+2*pageCount – content streams
        for (int p = 0; p < pageCount; p++)
        {
            int contentId = 3 + pageCount + p;
            var bytes     = Encoding.Latin1.GetBytes(pageContents[p]);
            StartObj(contentId);
            W($"<< /Length {bytes.Length} >>\nstream\n");
            ms.Write(bytes);
            W("\nendstream\n");
            EndObj();
        }

        // Font objects
        StartObj(fontR);
        W("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\n");
        EndObj();

        StartObj(fontB);
        W("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\n");
        EndObj();

        // xref
        long xrefPos = ms.Position;
        W($"xref\n0 {totalObjs + 1}\n");
        W("0000000000 65535 f \n");
        foreach (var o in xrefOffsets)
            W($"{o:D10} 00000 n \n");

        W($"trailer\n<< /Size {totalObjs + 1} /Root 1 0 R >>\n");
        W($"startxref\n{xrefPos}\n%%EOF\n");

        File.WriteAllBytes(outputPath, ms.ToArray());
    }

    // ── Page Content Builder ──────────────────────────────────────────────────

    private List<string> BuildPages(AsBuiltJob job)
    {
        var lines = new List<(string text, bool bold, float size)>();

        var id   = job.Identity;
        var runs = job.Network.Runs.Values.ToList();
        var strs = job.Network.Structures.Values.ToList();

        // Title block
        lines.Add(("AS-BUILT UTILITY SURVEY REPORT", true,  TitleSz));
        lines.Add(("",                                false, LineH));
        lines.Add(($"Job Number   : {id.JobNumber}",           false, FontSz));
        lines.Add(($"Client       : {id.ClientName}",          false, FontSz));
        lines.Add(($"County       : {id.County}",              false, FontSz));
        lines.Add(($"Utility Owner: {id.UtilityOwner}",        false, FontSz));
        lines.Add(($"Field Date   : {id.FieldDate:MM/dd/yyyy}",false, FontSz));
        lines.Add(($"Drafter      : {id.Drafter}",             false, FontSz));
        lines.Add(($"Checker      : {id.Checker}",             false, FontSz));
        lines.Add(($"Revision     : {id.RevisionNumber}",      false, FontSz));
        lines.Add(($"Exported     : {DateTime.Now:MM/dd/yyyy HH:mm}", false, FontSz));
        lines.Add(($"Coordinates  : {job.Environment}",        false, FontSz));
        lines.Add(("", false, LineH));

        // Structures
        lines.Add(($"STRUCTURES  ({strs.Count})", true, HdrSz));
        lines.Add(("Point    Type                  Rim Elev   Inv In    Inv Out", false, FontSz));
        foreach (var st in strs)
            lines.Add(($"{Pad(st.PointId, 8)}{Pad(st.Type ?? "", 22)}{Fmt(st.RimElevation),10}{Fmt(st.InvertIn),10}{Fmt(st.InvertOut),10}", false, FontSz));
        lines.Add(("", false, LineH));

        // Pipe runs
        lines.Add(($"PIPE RUNS  ({runs.Count})", true, HdrSz));
        lines.Add(("From  To    Type                Dia  Material      Length   Slope%  InvFr   InvTo", false, FontSz));
        foreach (var run in runs)
            lines.Add(($"{Pad(run.FromPointId, 6)}{Pad(run.ToPointId, 6)}{Pad(run.Type, 20)}{run.Diameter,5:F1}{Pad(run.Material, 14)}{run.ComputedLength,8:F2}{run.SlopePercent,8:F2}{Fmt(run.InvertStart),8}{Fmt(run.InvertEnd),8}", false, FontSz));
        lines.Add(("", false, LineH));

        // Summary
        lines.Add(("SUMMARY", true, HdrSz));
        lines.Add(($"  Total Structures : {strs.Count}",                                         false, FontSz));
        lines.Add(($"  Total Pipe Runs  : {runs.Count}",                                         false, FontSz));
        lines.Add(($"  Total Run Length : {runs.Sum(r => r.ComputedLength):F2} ft",              false, FontSz));
        lines.Add(($"  Unique Utilities : {runs.Select(r => r.Type).Distinct().Count()}",        false, FontSz));
        lines.Add(("END OF REPORT", true, FontSz));

        return Paginate(lines, id.JobNumber);
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    private static List<string> Paginate(
        List<(string text, bool bold, float size)> lines,
        string jobNum)
    {
        var pages = new List<string>();
        var sb    = new StringBuilder();
        float y   = MarginT;
        int   pgN = 1;

        void BeginPage()
        {
            sb.Clear();
            y = MarginT;
            // page header rule
            sb.Append($"BT /F2 8 Tf {MarginL:F0} {(MarginT + 16):F0} Td (RCS As-Built Pro  —  {EscPdf(jobNum)}) Tj ET ");
        }

        void EndPage()
        {
            // footer
            sb.Append($"BT /F1 7 Tf {(PageW / 2 - 30):F0} {(MarginB - 14):F0} Td (Page {pgN}) Tj ET ");
            pages.Add(sb.ToString());
            pgN++;
        }

        BeginPage();

        foreach (var (text, bold, sz) in lines)
        {
            if (y - (sz + 3f) < MarginB)
            {
                EndPage();
                BeginPage();
            }

            if (string.IsNullOrEmpty(text))
            {
                y -= LineH * 0.5f;
                continue;
            }

            string font = bold ? "F2" : "F1";
            sb.Append($"BT /{font} {sz:F1} Tf {MarginL:F1} {y:F1} Td ({EscPdf(text)}) Tj ET ");
            y -= sz + 4f;
        }

        EndPage();
        return pages;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string EscPdf(string s)
        => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string Pad(string? s, int w)
    {
        s ??= "";
        return s.Length >= w ? s[..w] : s.PadRight(w);
    }

    private static string Fmt(double? v) => v.HasValue ? $"{v:F2}" : "---";
}
