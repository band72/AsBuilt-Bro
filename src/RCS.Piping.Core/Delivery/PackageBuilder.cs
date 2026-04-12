using System.IO;
using System.Text.Json;
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
                    DeliverableType.Dxf                   => WriteDxfStub(job, packageDir),
                    DeliverableType.PdfReport             => WritePdfStub(job, packageDir),
                    DeliverableType.Pnezd                 => WritePnezdStub(job, packageDir),
                    DeliverableType.PartsReport           => WritePartsReportStub(job, packageDir),
                    DeliverableType.LandXml               => WriteLandXmlStub(job, packageDir),
                    DeliverableType.CertificationPackage  => WriteCertStub(job, packageDir),
                    _                                     => null
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
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
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

    // ── File stub writers ─────────────────────────────────────────────────────
    // In production the real builders (DxfBuilder, PdfBuilder, etc.) are
    // invoked here. These stubs create empty files so the package folder and
    // manifest are always present even before builder integration is complete.

    private static string WriteDxfStub(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_AsBuilt.dxf");
        if (!File.Exists(path)) File.WriteAllText(path, $"; DXF placeholder — job {job.Identity.JobNumber}");
        return path;
    }

    private static string WritePdfStub(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_Report.pdf");
        if (!File.Exists(path)) File.WriteAllBytes(path, Array.Empty<byte>());
        return path;
    }

    private static string WritePnezdStub(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_PNEZD.csv");
        var lines = new List<string> { "Point,Northing,Easting,Elevation,Description" };
        foreach (var pt in job.PointRows)
            lines.Add($"{pt.PointId},{pt.Northing:F3},{pt.Easting:F3},{pt.Elevation:F3},{pt.Description}");
        File.WriteAllLines(path, lines);
        return path;
    }

    private static string WritePartsReportStub(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_Parts.csv");
        var lines = new List<string> { "AssetId,DisplayName,PartKey,Manufacturer,Status" };
        foreach (var m in job.PartMappings)
            lines.Add($"{m.AssetId},{m.DisplayName},{m.PartKey},{m.Manufacturer},{m.Status}");
        File.WriteAllLines(path, lines);
        return path;
    }

    private static string WriteLandXmlStub(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}.xml");
        if (!File.Exists(path)) File.WriteAllText(path, $"<?xml version=\"1.0\"?><LandXML/>");
        return path;
    }

    private static string WriteCertStub(AsBuiltJob job, string dir)
    {
        var path = Path.Combine(dir, $"{job.Identity.JobNumber}_Certification.txt");
        File.WriteAllText(path, $"Certification package — {job.Identity.JobNumber} Rev {job.Identity.RevisionNumber}");
        return path;
    }

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
