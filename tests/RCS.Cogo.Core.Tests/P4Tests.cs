using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCS.Cogo.App.Commands;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;
using RCS.Piping.Core.Builders;
using RCS.Piping.Core.Delivery;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Core.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// PackageBuilderTests   (P4 Item 1 — real deliverables)
// ─────────────────────────────────────────────────────────────────────────────
public class PackageBuilderTests
{
    // ── Shared factory ────────────────────────────────────────────────────────

    private static AsBuiltJob MakeJob(string jobNum = "TEST-999") => new()
    {
        Identity = new ProjectIdentity
        {
            JobNumber      = jobNum,
            ClientName     = "Acme Corp",
            County         = "Demo County",
            UtilityOwner   = "City Water",
            Drafter        = "AI",
            Checker        = "QC",
            FieldDate      = new DateTime(2026, 1, 15),
            RevisionNumber = 1
        },
        Environment = CoordinateEnvironment.LocalGrid,
        PointRows   = new ObservableCollection<PointRow>
        {
            new() { PointId = "1", Northing = 10000.0, Easting = 10000.0, Elevation = 100.0, Description = "MH-1" },
            new() { PointId = "2", Northing = 10100.0, Easting = 10050.0, Elevation = 99.5,  Description = "MH-2" },
            new() { PointId = "3", Northing = 10200.0, Easting = 10100.0, Elevation = 99.0,  Description = "MH-3" },
        },
        Network     = BuildNetwork(),
        PartMappings = new ObservableCollection<PartMappingEntry>
        {
            new() { AssetId = "A1", DisplayName = "8\" DI Pipe", PartKey = "DI-8", Manufacturer = "ACME", Status = MappingStatus.Resolved },
            new() { AssetId = "A2", DisplayName = "MH Type 1",  PartKey = "MH-1", Manufacturer = "ACME", Status = MappingStatus.Resolved },
        }
    };

    private static PipeNetwork BuildNetwork()
    {
        var net = new PipeNetwork();
        net.AddRun(new PipeRun
        {
            Id = "R1", Type = "WASTEWATER", FromPointId = "1", ToPointId = "2",
            Diameter = 8, Material = "DI", SlopePercent = 0.5, ComputedLength = 111.8,
            InvertStart = 99.2, InvertEnd = 99.0
        });
        net.AddRun(new PipeRun
        {
            Id = "R2", Type = "WASTEWATER", FromPointId = "2", ToPointId = "3",
            Diameter = 8, Material = "DI", SlopePercent = 0.5, ComputedLength = 111.8,
            InvertStart = 99.0, InvertEnd = 98.8
        });
        net.AddStructure(new PipeStructure { Id = "S1", PointId = "1", Type = "MANHOLE", RimElevation = 100.0, InvertIn = 99.2, InvertOut = 99.2 });
        net.AddStructure(new PipeStructure { Id = "S2", PointId = "2", Type = "MANHOLE", RimElevation = 99.5,  InvertIn = 99.0, InvertOut = 99.0 });
        return net;
    }

    private static DeliverableCard Card(DeliverableType type) => new()
    {
        TypeEnum = type, IsEnabled = true, IsBlocked = false
    };

    // ── Tests: package folder ─────────────────────────────────────────────────

    [Fact]
    public void Build_CreatesPackageFolder()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PKG-FOLDER");
        job.Deliverables.Clear();   // no deliverables — just folder + manifest
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            Assert.True(Directory.Exists(dir));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Build_WriteManifestJson()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PKG-MANIFEST");
        job.Deliverables.Clear();
        try
        {
            var dir      = new PackageBuilder(tmp).Build(job);
            var manifest = Path.Combine(dir, "manifest.json");
            Assert.True(File.Exists(manifest));
            Assert.Contains("PKG-MANIFEST", File.ReadAllText(manifest));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Build_AppendsExportHistoryEntry()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PKG-HISTORY");
        job.Deliverables.Clear();
        try
        {
            Assert.Empty(job.ExportHistory);
            new PackageBuilder(tmp).Build(job);
            Assert.Single(job.ExportHistory);
            Assert.Equal(1, job.ExportHistory[0].RevisionNumber);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Build_FolderName_ContainsJobNumber()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PROJ-42");
        job.Deliverables.Clear();
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            Assert.Contains("PROJ-42", Path.GetFileName(dir));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: DXF deliverable ────────────────────────────────────────────────

    [Fact]
    public void Dxf_Deliverable_CreatesRealDxfFile()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("DXF-001");
        job.Deliverables.Add(Card(DeliverableType.Dxf));
        try
        {
            var dir  = new PackageBuilder(tmp).Build(job);
            var dxf  = Path.Combine(dir, "DXF-001_AsBuilt.dxf");
            Assert.True(File.Exists(dxf));
            Assert.Contains("SECTION", File.ReadAllText(dxf));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Dxf_Deliverable_ContainsUtilityLayer()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("DXF-002");
        job.Deliverables.Add(Card(DeliverableType.Dxf));
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            var dxf = File.ReadAllText(Path.Combine(dir, "DXF-002_AsBuilt.dxf"));
            Assert.Contains("WW-MAIN", dxf);    // wastewater layer expected
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Dxf_Deliverable_StatusMessageIsExported()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("DXF-003");
        var card = Card(DeliverableType.Dxf);
        job.Deliverables.Add(card);
        try
        {
            new PackageBuilder(tmp).Build(job);
            Assert.Contains("✅", card.StatusMessage);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: Text Report deliverable ───────────────────────────────────────

    [Fact]
    public void TextReport_Deliverable_CreatesFile()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("RPT-001");
        job.Deliverables.Add(Card(DeliverableType.PdfReport));
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            Assert.True(File.Exists(Path.Combine(dir, "RPT-001_Report.txt")));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void TextReport_ContainsJobIdentity()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("RPT-002");
        job.Deliverables.Add(Card(DeliverableType.PdfReport));
        try
        {
            var dir     = new PackageBuilder(tmp).Build(job);
            var content = File.ReadAllText(Path.Combine(dir, "RPT-002_Report.txt"));
            Assert.Contains("RPT-002",    content);
            Assert.Contains("Acme Corp",  content);
            Assert.Contains("AI",         content);   // Drafter
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void TextReport_ContainsPipeRunsSection()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("RPT-003");
        job.Deliverables.Add(Card(DeliverableType.PdfReport));
        try
        {
            var dir     = new PackageBuilder(tmp).Build(job);
            var content = File.ReadAllText(Path.Combine(dir, "RPT-003_Report.txt"));
            Assert.Contains("PIPE RUNS",   content);
            Assert.Contains("STRUCTURES",  content);
            Assert.Contains("SUMMARY",     content);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void TextReport_ContainsTotalRunLength()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("RPT-004");
        job.Deliverables.Add(Card(DeliverableType.PdfReport));
        try
        {
            var dir   = new PackageBuilder(tmp).Build(job);
            var text  = File.ReadAllText(Path.Combine(dir, "RPT-004_Report.txt"));
            // 2 runs × 111.8 = 223.6
            Assert.Contains("223.60", text);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: PNEZD deliverable ──────────────────────────────────────────────

    [Fact]
    public void Pnezd_Deliverable_HasCorrectHeader()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PNZ-001");
        job.Deliverables.Add(Card(DeliverableType.Pnezd));
        try
        {
            var dir  = new PackageBuilder(tmp).Build(job);
            var csv  = File.ReadAllLines(Path.Combine(dir, "PNZ-001_PNEZD.csv"));
            Assert.Equal("Point,Northing,Easting,Elevation,Description", csv[0]);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Pnezd_Deliverable_RowCountMatchesPoints()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PNZ-002");
        job.Deliverables.Add(Card(DeliverableType.Pnezd));
        try
        {
            var dir  = new PackageBuilder(tmp).Build(job);
            var rows = File.ReadAllLines(Path.Combine(dir, "PNZ-002_PNEZD.csv"));
            Assert.Equal(job.PointRows.Count + 1, rows.Length);   // +1 for header
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Pnezd_Deliverable_RowContainsPointData()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PNZ-003");
        job.Deliverables.Add(Card(DeliverableType.Pnezd));
        try
        {
            var dir  = new PackageBuilder(tmp).Build(job);
            var csv  = File.ReadAllText(Path.Combine(dir, "PNZ-003_PNEZD.csv"));
            Assert.Contains("10000.000", csv);   // northing of point 1
            Assert.Contains("MH-1", csv);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: Parts Report deliverable ──────────────────────────────────────

    [Fact]
    public void PartsReport_Deliverable_HasCorrectHeader()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PRT-001");
        job.Deliverables.Add(Card(DeliverableType.PartsReport));
        try
        {
            var dir   = new PackageBuilder(tmp).Build(job);
            var csv   = File.ReadAllLines(Path.Combine(dir, "PRT-001_Parts.csv"));
            Assert.Equal("AssetId,DisplayName,PartKey,Manufacturer,Status", csv[0]);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void PartsReport_Deliverable_ContainsResolvedParts()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("PRT-002");
        job.Deliverables.Add(Card(DeliverableType.PartsReport));
        try
        {
            var dir  = new PackageBuilder(tmp).Build(job);
            var csv  = File.ReadAllText(Path.Combine(dir, "PRT-002_Parts.csv"));
            Assert.Contains("DI-8",   csv);
            Assert.Contains("Resolved", csv);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: LandXML deliverable ────────────────────────────────────────────

    [Fact]
    public void LandXml_Deliverable_CreatesXmlFile()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("LXM-001");
        job.Deliverables.Add(Card(DeliverableType.LandXml));
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            Assert.True(File.Exists(Path.Combine(dir, "LXM-001.xml")));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void LandXml_Deliverable_HasLandXmlRoot()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("LXM-002");
        job.Deliverables.Add(Card(DeliverableType.LandXml));
        try
        {
            var dir  = new PackageBuilder(tmp).Build(job);
            var xml  = File.ReadAllText(Path.Combine(dir, "LXM-002.xml"));
            Assert.StartsWith("<?xml", xml.TrimStart());
            Assert.Contains("<LandXML", xml);
            Assert.Contains("</LandXML>", xml);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void LandXml_Deliverable_ContainsCgPoints()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("LXM-003");
        job.Deliverables.Add(Card(DeliverableType.LandXml));
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            var xml = File.ReadAllText(Path.Combine(dir, "LXM-003.xml"));
            Assert.Contains("<CgPoints>", xml);
            Assert.Contains("CgPoint",   xml);
            Assert.Contains("MH-1",      xml);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void LandXml_Deliverable_ContainsPipeNetwork()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("LXM-004");
        job.Deliverables.Add(Card(DeliverableType.LandXml));
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            var xml = File.ReadAllText(Path.Combine(dir, "LXM-004.xml"));
            Assert.Contains("<PipeNetworks>", xml);
            Assert.Contains("<Pipe",          xml);
            Assert.Contains("<Struct",        xml);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: Certification deliverable ─────────────────────────────────────

    [Fact]
    public void Certification_Deliverable_CreatesFile()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("CERT-001");
        job.Deliverables.Add(Card(DeliverableType.CertificationPackage));
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            Assert.True(File.Exists(Path.Combine(dir, "CERT-001_Certification.txt")));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Certification_Deliverable_ContainsQcSignatures()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("CERT-002");
        job.Deliverables.Add(Card(DeliverableType.CertificationPackage));
        try
        {
            var dir   = new PackageBuilder(tmp).Build(job);
            var text  = File.ReadAllText(Path.Combine(dir, "CERT-002_Certification.txt"));
            Assert.Contains("Drafter Signature", text);
            Assert.Contains("Checker Signature", text);
            Assert.Contains("AI",   text);   // Drafter name
            Assert.Contains("QC",   text);   // Checker name
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    [Fact]
    public void Certification_BlockedDeliverable_IsSkipped()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("CERT-003");
        var blocked = Card(DeliverableType.CertificationPackage);
        blocked.IsBlocked = true;
        job.Deliverables.Add(blocked);
        try
        {
            var dir = new PackageBuilder(tmp).Build(job);
            // Blocked deliverable should NOT produce a file
            Assert.False(File.Exists(Path.Combine(dir, "CERT-003_Certification.txt")));
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }

    // ── Tests: multi-deliverable package ─────────────────────────────────────

    [Fact]
    public void AllDeliverables_ProduceCorrectFileCount()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var job = MakeJob("ALL-001");
        foreach (var t in Enum.GetValues<DeliverableType>())
            job.Deliverables.Add(Card(t));
        try
        {
            var dir   = new PackageBuilder(tmp).Build(job);
            var files = Directory.GetFiles(dir);
            // 6 deliverables + 1 manifest = 7
            Assert.Equal(7, files.Length);
        }
        finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// C3CommandTests   (P4 Item 2 — 3-point circumscribed circle)
// ─────────────────────────────────────────────────────────────────────────────
public class C3CommandTests
{
    // Use the existing StubCogoContext from RenumberAndClosureTests.cs
    private static StubCogoContext MakeCtx() => new();

    private static async Task<StubCogoContext> RunC3(string p1, string p2, string p3,
        double n1, double e1,
        double n2, double e2,
        double n3, double e3)
    {
        var ctx = MakeCtx();
        ctx.AddPoint(p1, new Point3D(n1, e1, 0));
        ctx.AddPoint(p2, new Point3D(n2, e2, 0));
        ctx.AddPoint(p3, new Point3D(n3, e3, 0));
        await new C3Command().ExecuteAsync(new[] { "C3", p1, p2, p3 }, ctx);
        return ctx;
    }

    [Fact]
    public async Task C3_WithValidPoints_LogsCenter()
    {
        // Right triangle with hypotenuse as diameter — circumcenter is midpoint of hypotenuse
        var ctx = await RunC3("A","B","C",  0,0,  0,4,  3,0);
        Assert.Contains(ctx.Messages, m => m.Contains("Center"));
    }

    [Fact]
    public async Task C3_WithValidPoints_LogsRadius()
    {
        var ctx = await RunC3("A","B","C",  0,0,  0,4,  3,0);
        Assert.Contains(ctx.Messages, m => m.Contains("Radius"));
    }

    [Fact]
    public async Task C3_WithValidPoints_LogsCircumference()
    {
        var ctx = await RunC3("A","B","C",  0,0,  0,4,  3,0);
        Assert.Contains(ctx.Messages, m => m.Contains("Circum"));
    }

    [Fact]
    public async Task C3_KnownCircle_RadiusIsCorrect()
    {
        // Equilateral triangle inscribed in circle of radius r=10
        // Vertices at angles 0°, 120°, 240°
        double r = 10.0;
        double n1 = r * Math.Cos(0),           e1 = r * Math.Sin(0);
        double n2 = r * Math.Cos(2*Math.PI/3), e2 = r * Math.Sin(2*Math.PI/3);
        double n3 = r * Math.Cos(4*Math.PI/3), e3 = r * Math.Sin(4*Math.PI/3);

        var ctx = await RunC3("A","B","C", n1,e1, n2,e2, n3,e3);
        var radiusLine = ctx.Messages.First(m => m.Contains("Radius"));
        // extract number from "  Radius  : 10.0000 ft"
        var numStr = radiusLine.Split(':').Last().Trim().Split(' ')[0];
        Assert.True(double.TryParse(numStr, out var computed));
        Assert.InRange(computed, r - 0.001, r + 0.001);
    }

    [Fact]
    public async Task C3_CollinearPoints_LogsError()
    {
        // Three collinear points on X axis
        var ctx = await RunC3("A","B","C",  0,0,  0,5,  0,10);
        Assert.Contains(ctx.Messages, m => m.Contains("collinear"));
    }

    [Fact]
    public async Task C3_MissingArgs_LogsUsage()
    {
        var ctx = MakeCtx();
        await new C3Command().ExecuteAsync(new[] { "C3" }, ctx);
        Assert.Contains(ctx.Messages, m => m.Contains("Usage"));
    }

    [Fact]
    public async Task C3_UnknownPoint_LogsError()
    {
        var ctx = MakeCtx();
        ctx.AddPoint("A", new Point3D(0, 0, 0));
        ctx.AddPoint("B", new Point3D(0, 5, 0));
        // "C" not added
        await new C3Command().ExecuteAsync(new[] { "C3", "A", "B", "C" }, ctx);
        Assert.Contains(ctx.Messages, m => m.Contains("not found"));
    }

    [Fact]
    public void C3_Description_MentionsThreePoint()
    {
        Assert.Contains("3-Point", new C3Command().Description);
    }

    [Fact]
    public void C3_Name_IsC3()
    {
        Assert.Equal("C3", new C3Command().Name);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GeometryEngineTests   (P4 — covering Inverse / Forward / Intersection)
// ─────────────────────────────────────────────────────────────────────────────
public class GeometryEngineTests
{
    [Fact]
    public void Inverse_DueNorth_ReturnsZeroAzimuth()
    {
        var p1 = new Point3D(0, 0, 0);
        var p2 = new Point3D(100, 0, 0);
        var (dist, az) = GeometryEngine.Inverse(p1, p2);
        Assert.InRange(dist, 99.999, 100.001);
        Assert.InRange(az.Degrees, -0.001, 0.001);
    }

    [Fact]
    public void Inverse_DueEast_Returns90Azimuth()
    {
        var p1 = new Point3D(0, 0, 0);
        var p2 = new Point3D(0, 100, 0);
        var (dist, az) = GeometryEngine.Inverse(p1, p2);
        Assert.InRange(dist,  99.999, 100.001);
        Assert.InRange(az.Degrees, 89.999, 90.001);
    }

    [Fact]
    public void Inverse_SamePoint_ReturnsZeroDistance()
    {
        var p = new Point3D(12345, 67890, 0);
        var (dist, _) = GeometryEngine.Inverse(p, p);
        Assert.InRange(dist, 0.0, 1e-9);
    }

    [Fact]
    public void Forward_DueNorthUnit_ReturnsCorrectPoint()
    {
        var p  = new Point3D(1000, 2000, 0);
        var az = Angle.FromDegrees(0);    // due north
        var q  = GeometryEngine.Forward(p, az, 100);
        Assert.InRange(q.Northing, 1099.999, 1100.001);
        Assert.InRange(q.Easting,  1999.999, 2000.001);
    }

    [Fact]
    public void Forward_DueEastUnit_ReturnsCorrectPoint()
    {
        var p  = new Point3D(1000, 2000, 0);
        var az = Angle.FromDegrees(90);
        var q  = GeometryEngine.Forward(p, az, 50);
        Assert.InRange(q.Easting,  2049.999, 2050.001);
        Assert.InRange(q.Northing,  999.999, 1000.001);
    }

    [Fact]
    public void Inverse_Forward_Roundtrip()
    {
        var p1 = new Point3D(10000, 10000, 0);
        var p2 = new Point3D(10300, 10400, 0);
        var (dist, az) = GeometryEngine.Inverse(p1, p2);
        var p3 = GeometryEngine.Forward(p1, az, dist);
        Assert.InRange(p3.Northing, p2.Northing - 0.001, p2.Northing + 0.001);
        Assert.InRange(p3.Easting,  p2.Easting  - 0.001, p2.Easting  + 0.001);
    }

    [Fact]
    public void IntersectionBearingBearing_Perpendicular_ReturnsCorrectPoint()
    {
        // Two lines meet at (100, 100):
        //   Line 1: from (0,100) bearing due North
        //   Line 2: from (100,0) bearing due East
        var p1 = new Point3D(0, 100, 0);
        var p2 = new Point3D(100, 0, 0);
        var az1 = Angle.FromDegrees(0);   // north
        var az2 = Angle.FromDegrees(90);  // east
        var result = GeometryEngine.IntersectionBearingBearing(p1, az1, p2, az2);
        Assert.NotNull(result);
        Assert.InRange(result!.Northing, 99.99, 100.01);
        Assert.InRange(result.Easting,  99.99, 100.01);
    }

    [Fact]
    public void IntersectionBearingBearing_ParallelLines_ReturnsNull()
    {
        var p1 = new Point3D(0, 0, 0);
        var p2 = new Point3D(0, 100, 0);
        var az  = Angle.FromDegrees(0);  // both due north
        var result = GeometryEngine.IntersectionBearingBearing(p1, az, p2, az);
        Assert.Null(result);
    }
}
