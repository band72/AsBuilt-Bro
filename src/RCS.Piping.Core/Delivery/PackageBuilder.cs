using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RCS.Piping.Core.Builders;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Delivery;

/// <summary>
/// Assembles a time-stamped, revisioned export package folder.
/// Folder name pattern: {JobNumber}_Rev{Revision}_{MMddyyyy}
/// </summary>
public class PackageBuilder
{
    private readonly string _baseOutputDir;

    public PackageBuilder(string baseOutputDir)
    {
        _baseOutputDir = baseOutputDir;
    }

    /// <summary>
    /// Creates the package folder, runs each enabled deliverable builder,
    /// writes a JSON manifest, and appends an ExportRecord to the job.
    /// Returns the full path of the created package folder.
    /// </summary>
    public string Build(AsBuiltJob job)
    {
        var folderName = BuildFolderName(job);
        var packageDir = Path.Combine(_baseOutputDir, folderName);
        Directory.CreateDirectory(packageDir);

        var filesGenerated = new List<string>();

        foreach (var card in job.Deliverables.Where(c => c.IsEnabled && !c.IsBlocked))
        {
            try
            {
                var path = card.TypeEnum switch
                {
                    DeliverableType.Dxf                  => WriteDxf(job, packageDir),
                    DeliverableType.PdfReport            => WriteTextReport(job, packageDir),
                    DeliverableType.Pnezd                => WritePnezd(job, packageDir),
                    DeliverableType.PartsReport          => WritePartsReport(job, packageDir),
                    DeliverableType.LandXml              => WriteLandXml(job, packageDir),
                    DeliverableType.CertificationPackage => WriteCertification(job, packageDir),
                    _                                    => null
                };
                if (path != null) filesGenerated.Add(path);
                card.StatusMessage = "✅ Exported";
            }
            catch (Exception ex)
            {
                card.StatusMessage = $"❌ {ex.Message}";
            }
        }

        // ── Manifest ──────────────────────────────────────────────────────────
        var manifest = new PackageManifest
        {
            JobNumber      = job.Identity.JobNumber,
            ClientName     = job.Identity.ClientName,
            RevisionNumber = job.Identity.RevisionNumber,
            ExportedAt     = DateTime.UtcNow,
            FilesGenerated = filesGenerated
        };
        var manifestPath = Path.Combine(packageDir, "manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest,
            new JsonSerializerOptions { WriteIndented = true }));
        filesGenerated.Add(manifestPath);

        // ── Append history ────────────────────────────────────────────────────
        job.ExportHistory.Add(new ExportRecord
        {
            ExportedAt     = DateTime.UtcNow,
            PackagePath    = packageDir,
            RevisionNumber = job.Identity.RevisionNumber,
            FilesGenerated = filesGenerated
        });

        return packageDir;
    }

    // ── Deliverable Writers ───────────────────────────────────────────────────

    /// <summary>
    /// Produces a real DXF file via <see cref="DxfBuilder"/> with utility-layer coloring.
    /// </summary>
    private static string WriteDxf(AsBuiltJob job, string dir)
    {
        var path    = Path.Combine(dir, $"{job.Identity.JobNumber}_AsBuilt.dxf");
        var builder = new DxfBuilder();
        builder.Build(job, path);
        return path;
    }

    /// <summary>
    /// Produces a human-readable plain-text as-built survey report.
    /// (PDF generation requires a third-party library; this text file is the
    ///  primary deliverable until a PDF renderer is integrated.)
    /// </summary>
    private static string WriteTextReport(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_Report.txt");
        var id   = job.Identity;
        var runs = job.Network.Runs.Values;
        var strs = job.Network.Structures.Values;

        var sb = new StringBuilder();
        sb.AppendLine("========================================================");
        sb.AppendLine("         AS-BUILT UTILITY SURVEY REPORT");
        sb.AppendLine("========================================================");
        sb.AppendLine($"Job Number   : {id.JobNumber}");
        sb.AppendLine($"Client       : {id.ClientName}");
        sb.AppendLine($"County       : {id.County}");
        sb.AppendLine($"Utility Owner: {id.UtilityOwner}");
        sb.AppendLine($"Field Date   : {id.FieldDate:MM/dd/yyyy}");
        sb.AppendLine($"Drafter      : {id.Drafter}");
        sb.AppendLine($"Checker      : {id.Checker}");
        sb.AppendLine($"Revision     : {id.RevisionNumber}");
        sb.AppendLine($"Exported     : {DateTime.Now:MM/dd/yyyy HH:mm}");
        sb.AppendLine($"Coordinate   : {job.Environment}");
        sb.AppendLine();

        // ── Structures ────────────────────────────────────────────────────────
        sb.AppendLine($"STRUCTURES ({strs.Count})");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"{"Point",-8}{"Type",-22}{"Rim Elev",10}{"Inv In",10}{"Inv Out",10}");
        sb.AppendLine(new string('-', 60));
        foreach (var st in strs)
        {
            sb.AppendLine($"{st.PointId,-8}{st.Type,-22}{Fmt(st.RimElevation),10}{Fmt(st.InvertIn),10}{Fmt(st.InvertOut),10}");
        }
        sb.AppendLine();

        // ── Pipe Runs ─────────────────────────────────────────────────────────
        sb.AppendLine($"PIPE RUNS ({runs.Count})");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{"From",-6}{"To",-6}{"Type",-20}{"Dia",5}{"Mat",-14}{"Length",8}{"Slope%",8}{"InvFr",8}{"InvTo",8}");
        sb.AppendLine(new string('-', 80));
        foreach (var run in runs)
        {
            sb.AppendLine($"{run.FromPointId,-6}{run.ToPointId,-6}{run.Type,-20}{run.Diameter,5:F1}{run.Material,-14}{run.ComputedLength,8:F2}{run.SlopePercent,8:F2}{Fmt(run.InvertStart),8}{Fmt(run.InvertEnd),8}");
        }
        sb.AppendLine();

        // ── Parts Mapping ─────────────────────────────────────────────────────
        if (job.PartMappings.Any())
        {
            sb.AppendLine($"PARTS MAPPING ({job.PartMappings.Count})");
            sb.AppendLine(new string('-', 70));
            sb.AppendLine($"{"AssetId",-14}{"Part",-20}{"Manufacturer",-20}{"Status",-12}");
            sb.AppendLine(new string('-', 70));
            foreach (var m in job.PartMappings)
                sb.AppendLine($"{m.AssetId,-14}{m.PartKey,-20}{m.Manufacturer,-20}{m.Status,-12}");
            sb.AppendLine();
        }

        // ── Summary ───────────────────────────────────────────────────────────
        sb.AppendLine("SUMMARY");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"  Total Structures : {strs.Count}");
        sb.AppendLine($"  Total Pipe Runs  : {runs.Count}");
        sb.AppendLine($"  Total Run Length : {runs.Sum(r => r.ComputedLength):F2} ft");
        sb.AppendLine($"  Unique Utilities : {runs.Select(r => r.Type).Distinct().Count()}");
        sb.AppendLine("========================================================");
        sb.AppendLine("END OF REPORT");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    /// <summary>
    /// Exports all survey points in PNEZD CSV format (Point, Northing, Easting, Elevation, Description).
    /// </summary>
    private static string WritePnezd(AsBuiltJob job, string dir)
    {
        var path  = Path.Combine(dir, $"{job.Identity.JobNumber}_PNEZD.csv");
        var lines = new List<string> { "Point,Northing,Easting,Elevation,Description" };
        foreach (var pt in job.PointRows)
            lines.Add($"{pt.PointId},{pt.Northing:F3},{pt.Easting:F3},{pt.Elevation:F3},{pt.Description}");
        File.WriteAllLines(path, lines, Encoding.UTF8);
        return path;
    }

    /// <summary>
    /// Exports a resolved parts manifest CSV with asset→catalog mapping details.
    /// </summary>
    private static string WritePartsReport(AsBuiltJob job, string dir)
    {
        var path  = Path.Combine(dir, $"{job.Identity.JobNumber}_Parts.csv");
        var lines = new List<string> { "AssetId,DisplayName,PartKey,Manufacturer,Status" };
        foreach (var m in job.PartMappings)
            lines.Add($"{m.AssetId},{m.DisplayName},{m.PartKey},{m.Manufacturer},{m.Status}");
        File.WriteAllLines(path, lines, Encoding.UTF8);
        return path;
    }

    /// <summary>
    /// Produces a LandXML 1.2 document with CgPoints and PipeNetworks elements.
    /// </summary>
    private static string WriteLandXml(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}.xml");
        var sb   = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<LandXML version=\"1.2\" xmlns=\"http://www.landxml.org/schema/LandXML-1.2\"");
        sb.AppendLine($"  date=\"{DateTime.UtcNow:yyyy-MM-dd}\" time=\"{DateTime.UtcNow:HH:mm:ss}\">");
        sb.AppendLine($"  <Project name=\"{EscXml(job.Identity.JobNumber)}\" desc=\"{EscXml(job.Identity.ClientName)}\"/>");

        // CgPoints
        if (job.PointRows.Any())
        {
            sb.AppendLine("  <CgPoints>");
            foreach (var pt in job.PointRows)
                sb.AppendLine($"    <CgPoint name=\"{EscXml(pt.PointId)}\" desc=\"{EscXml(pt.Description)}\">{pt.Northing:F4} {pt.Easting:F4} {pt.Elevation:F4}</CgPoint>");
            sb.AppendLine("  </CgPoints>");
        }

        // PipeNetworks
        if (job.Network.Runs.Any() || job.Network.Structures.Any())
        {
            sb.AppendLine($"  <PipeNetworks>");
            sb.AppendLine($"    <PipeNetwork name=\"{EscXml(job.Identity.JobNumber)}\" pipeNetType=\"Storm\">");

            foreach (var st in job.Network.Structures.Values)
            {
                sb.AppendLine($"      <Struct name=\"{EscXml(st.PointId)}\" desc=\"{EscXml(st.Type ?? string.Empty)}\"");
                if (st.RimElevation.HasValue)
                    sb.Append($"             rimElev=\"{st.RimElevation:F3}\"");
                if (st.InvertIn.HasValue)
                    sb.Append($" invertElev=\"{st.InvertIn:F3}\"");
                sb.AppendLine("/>");
            }

            foreach (var run in job.Network.Runs.Values)
            {
                sb.AppendLine($"      <Pipe name=\"{EscXml(run.Id)}\" desc=\"{EscXml(run.Type)}\"");
                sb.AppendLine($"            fromSsRef=\"{EscXml(run.FromPointId)}\" toSsRef=\"{EscXml(run.ToPointId)}\"");
                sb.AppendLine($"            size=\"{run.Diameter:F1}\" slope=\"{run.SlopePercent:F4}\">");
                sb.AppendLine($"        <CircPipe diameter=\"{run.Diameter:F4}\"/>");
                sb.AppendLine($"      </Pipe>");
            }

            sb.AppendLine("    </PipeNetwork>");
            sb.AppendLine("  </PipeNetworks>");
        }

        sb.AppendLine("</LandXML>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    /// <summary>
    /// Produces a plain-text certification cover sheet with job identity,
    /// quality control signatories, and a summary of deliverables.
    /// </summary>
    private static string WriteCertification(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_Certification.txt");
        var id   = job.Identity;
        var sb   = new StringBuilder();
        sb.AppendLine("========================================================");
        sb.AppendLine("     AS-BUILT UTILITY SURVEY — CERTIFICATION PACKAGE");
        sb.AppendLine("========================================================");
        sb.AppendLine();
        sb.AppendLine($"Job Number   : {id.JobNumber}");
        sb.AppendLine($"Client       : {id.ClientName}");
        sb.AppendLine($"Utility Owner: {id.UtilityOwner}");
        sb.AppendLine($"County       : {id.County}");
        sb.AppendLine($"Field Date   : {id.FieldDate:MM/dd/yyyy}");
        sb.AppendLine($"Revision     : {id.RevisionNumber}");
        sb.AppendLine();
        sb.AppendLine("DELIVERABLES INCLUDED IN THIS PACKAGE:");
        foreach (var d in job.Deliverables.Where(c => c.IsEnabled))
            sb.AppendLine($"  [{(d.IsBlocked ? "BLOCKED" : "✓")}] {d.TypeEnum,-25} {d.StatusMessage}");
        sb.AppendLine();
        sb.AppendLine("QC SIGN-OFF:");
        sb.AppendLine($"  Drafter  : {id.Drafter}");
        sb.AppendLine($"  Checker  : {id.Checker}");
        sb.AppendLine();
        sb.AppendLine("I hereby certify that these as-built records accurately represent");
        sb.AppendLine("conditions in the field as observed at the time of survey.");
        sb.AppendLine();
        sb.AppendLine($"Drafter Signature:  ____________________________  Date: {DateTime.Now:MM/dd/yyyy}");
        sb.AppendLine();
        sb.AppendLine($"Checker Signature:  ____________________________  Date: {DateTime.Now:MM/dd/yyyy}");
        sb.AppendLine();
        sb.AppendLine("========================================================");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Fmt(double? val) => val.HasValue ? $"{val:F2}" : "---";
    private static string EscXml(string s) => s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string BuildFolderName(AsBuiltJob job)
    {
        var safe = string.Concat((job.Identity.JobNumber.Length > 0 ? job.Identity.JobNumber : "JOB")
                                 .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        return $"{safe}_Rev{job.Identity.RevisionNumber}_{DateTime.Now:MMddyyyy}";
    }
}

/// <summary>JSON manifest written into every package folder.</summary>
public class PackageManifest
{
    public string       JobNumber       { get; set; } = string.Empty;
    public string       ClientName      { get; set; } = string.Empty;
    public int          RevisionNumber  { get; set; }
    public DateTime     ExportedAt      { get; set; }
    public List<string> FilesGenerated  { get; set; } = new();
}
