using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

// ─────────────────────────────────────────────────────────────────────────────
// PdfReportBuilder
// Produces a plain-text paginated report styled for PDF printing.
// Using raw UTF-8 text (no external PDF library dependency).
// To get a real PDF: pass this output to a WebBrowser print dialog, or wrap
// in Windows.Graphics.Printing when/if a print dialog is needed.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class PdfReportBuilder
{
    private const int PageWidth = 96;
    private const char Sep      = '─';

    public void Build(AsBuiltJob job, ValidationResult validation, string outputPath)
    {
        var sb = new StringBuilder(8192);

        WritePage(sb, job, validation);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private void WritePage(StringBuilder sb, AsBuiltJob job, ValidationResult v)
    {
        var now = DateTime.Now;
        var id  = job.Identity;

        // ── Cover ────────────────────────────────────────────────────────────
        Line(sb);
        Center(sb, "AS-BUILT SURVEY REPORT");
        Center(sb, $"Revision {id.RevisionNumber}  |  {now:MMMM d, yyyy}");
        Line(sb);
        sb.AppendLine();

        // ── Project Identity ─────────────────────────────────────────────────
        Section(sb, "PROJECT IDENTITY");
        Field(sb, "Job Number",    id.JobNumber);
        Field(sb, "Client",        id.ClientName);
        Field(sb, "Contractor",    id.Contractor);
        Field(sb, "Utility Owner", id.UtilityOwner);
        Field(sb, "County",        id.County);
        Field(sb, "Field Date",    id.FieldDate?.ToString("MM/dd/yyyy") ?? "—");
        Field(sb, "Drafter",       id.Drafter);
        Field(sb, "Checker",       id.Checker);
        Field(sb, "Description",   id.Description);
        sb.AppendLine();

        // ── Network Summary ──────────────────────────────────────────────────
        Section(sb, "NETWORK SUMMARY");
        Field(sb, "Survey Points",    job.PointRows.Count.ToString());
        Field(sb, "Pipe Runs",        job.Network.Runs.Count.ToString());
        Field(sb, "Structures",       job.Network.Structures.Count.ToString());
        Field(sb, "Parts Mapped",     job.PartMappings.Count(p => p.Status == MappingStatus.Resolved).ToString());
        Field(sb, "Parts Pending",    job.PartMappings.Count(p => p.Status == MappingStatus.Pending).ToString());
        sb.AppendLine();

        // ── QC Gate ──────────────────────────────────────────────────────────
        Section(sb, "QC VALIDATION GATE");
        Field(sb, "Errors",   v.ErrorCount.ToString());
        Field(sb, "Warnings", v.WarningCount.ToString());
        Field(sb, "Status",   v.IsExportReady ? "✅  CLEARED FOR EXPORT" : "🔴  BLOCKED — ISSUES PRESENT");
        sb.AppendLine();

        // ── Issue Log ────────────────────────────────────────────────────────
        if (v.Issues.Any())
        {
            Section(sb, "VALIDATION ISSUES");
            foreach (var issue in v.Issues.OrderByDescending(i => i.Severity))
                sb.AppendLine($"  [{issue.Severity,-7}] {issue.RuleName,-20} {issue.Message}");
            sb.AppendLine();
        }

        // ── Point Coordinate Table ───────────────────────────────────────────
        Section(sb, "SURVEY POINT COORDINATES");
        sb.AppendLine($"  {"PT#",-8} {"NORTHING",14} {"EASTING",14} {"ELEVATION",12} DESCRIPTION");
        sb.AppendLine(new string(Sep, PageWidth));
        foreach (var pt in job.PointRows.OrderBy(r => r.PointId))
        {
            sb.AppendLine($"  {pt.PointId,-8} {pt.Northing,14:F4} {pt.Easting,14:F4}" +
                          $" {pt.Elevation,12:F4} {pt.Description}");
        }
        sb.AppendLine();

        // ── Pipe Run Table ───────────────────────────────────────────────────
        Section(sb, "PIPE RUN SCHEDULE");
        sb.AppendLine($"  {"FROM",-8} {"TO",-8} {"DIAM",6} {"MAT",-8} {"INV-UP",10} {"INV-DN",10} {"SLOPE%",8}");
        sb.AppendLine(new string(Sep, PageWidth));
        foreach (var run in job.Network.Runs.Values)
        {
            double slope = double.NaN;
            var fromPt = job.PointRows.FirstOrDefault(r => r.PointId == run.FromPointId);
            var toPt   = job.PointRows.FirstOrDefault(r => r.PointId == run.ToPointId);
            if (fromPt != null && toPt != null && run.InvertStart.HasValue && run.InvertEnd.HasValue)
            {
                double dx = toPt.Easting  - fromPt.Easting;
                double dy = toPt.Northing - fromPt.Northing;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0.01)
                    slope = (run.InvertStart.Value - run.InvertEnd.Value) / len * 100.0;
            }

            sb.AppendLine($"  {run.FromPointId,-8} {run.ToPointId,-8} {run.Diameter,6:F1}\" {run.Material,-8}" +
                          $" {(run.InvertStart.HasValue ? run.InvertStart.Value.ToString("F4") : "—"),10}" +
                          $" {(run.InvertEnd.HasValue   ? run.InvertEnd.Value.ToString("F4")   : "—"),10}" +
                          $" {(double.IsNaN(slope) ? "N/A" : $"{slope:F2}%"),8}");

        }
        sb.AppendLine();

        // ── Structure Table ──────────────────────────────────────────────────
        Section(sb, "STRUCTURE SCHEDULE");
        sb.AppendLine($"  {"ID",-10} {"PT#",-8} {"TYPE",-16} {"RIM ELEV",12}");
        sb.AppendLine(new string(Sep, PageWidth));
        foreach (var st in job.Network.Structures.Values)
        {
            sb.AppendLine($"  {st.Id,-10} {st.PointId,-8} {st.Type,-16}" +
                          $" {(st.RimElevation.HasValue ? st.RimElevation.Value.ToString("F4") : "—"),12}");
        }
        sb.AppendLine();

        // ── Footer ───────────────────────────────────────────────────────────
        Line(sb);
        Center(sb, $"Generated by RCS Cogo Enterprise  |  {now:yyyy-MM-dd HH:mm}");
        Line(sb);
    }

    // ── Formatting Helpers ────────────────────────────────────────────────────

    private static void Line(StringBuilder sb)     => sb.AppendLine(new string(Sep, PageWidth));
    private static void Section(StringBuilder sb, string title)
    {
        sb.AppendLine($"▌ {title}");
        sb.AppendLine(new string('─', PageWidth));
    }

    private static void Center(StringBuilder sb, string text)
    {
        int pad = Math.Max(0, (PageWidth - text.Length) / 2);
        sb.AppendLine(new string(' ', pad) + text);
    }

    private static void Field(StringBuilder sb, string label, string value)
        => sb.AppendLine($"  {label,-20}: {value}");
}
