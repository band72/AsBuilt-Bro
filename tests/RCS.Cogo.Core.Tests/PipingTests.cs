using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RCS.Piping.Core.Scripting;
using RCS.Piping.Core.Builders;
using RCS.Piping.Core.Engines;
using RCS.Piping.Core.Workflow;
using RCS.Piping.Core.Models;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.Core.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// PipeScriptCompilerTests   (Item 3a)
// ─────────────────────────────────────────────────────────────────────────────
public class PipeScriptCompilerTests
{
    private static ScriptCompileResult Compile(string script,
        HashSet<string>? materials = null,
        HashSet<string>? codes = null)
    {
        var compiler = new PipeScriptCompiler();
        return compiler.Compile(
            script,
            _ => null,
            materials ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase),
            codes    ?? new HashSet<string>(System.StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void EmptyScript_ReturnsInfoDiagnostic()
    {
        var result = Compile("");
        Assert.Contains(result.Diagnostics, d => d.Severity == "INFO" && d.LineNumber == 1);
    }

    [Fact]
    public void CommentOnly_ReturnsNoErrors()
    {
        var result = Compile("// This is a comment\n; another comment");
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == "ERROR");
    }

    [Fact]
    public void PipeEnginePause_ReturnsInfoDiagnostic()
    {
        var result = Compile("PIPE-ENGINE-OFF");
        Assert.Contains(result.Diagnostics, d => d.Severity == "INFO" && d.Message.Contains("PAUSED"));
    }

    [Fact]
    public void PipeEngineResume_ReturnsInfoDiagnostic()
    {
        var result = Compile("PIPE-ENGINE-OFF\nPIPE-ENGINE-ON");
        Assert.Contains(result.Diagnostics, d => d.Severity == "INFO" && d.Message.Contains("RESUMED"));
    }

    [Fact]
    public void UnknownCommand_ReturnsError()
    {
        var result = Compile("ZZZUNKNOWNCOMMAND 1 2 3");
        Assert.Contains(result.Diagnostics, d => d.Severity == "ERROR");
    }

    [Fact]
    public void SsCommand_MissingArgs_ReturnsError()
    {
        var result = Compile("SS-C");   // requires PointID and Code
        Assert.Contains(result.Diagnostics, d => d.Severity == "ERROR" && d.Message.Contains("SS-C"));
    }

    [Fact]
    public void PrunMissingVerb_ReturnsError()
    {
        var result = Compile("PRUN");   // needs START or END
        Assert.Contains(result.Diagnostics, d => d.Severity == "ERROR");
    }

    [Fact]
    public void NumericTokenOutsidePrun_ReturnsError()
    {
        var result = Compile("123");
        Assert.Contains(result.Diagnostics, d => d.Severity == "ERROR");
    }

    [Fact]
    public void MultiLineScript_LineNumbersAreAccurate()
    {
        var script = "// good line\n// good line\nZZZBAD 1 2";
        var result = Compile(script);
        var err = result.Diagnostics.First(d => d.Severity == "ERROR");
        Assert.Equal(3, err.LineNumber);
    }

    [Fact]
    public void DiagnosticToString_ContainsExpectedParts()
    {
        var diag = new ScriptDiagnostic { LineNumber = 7, Severity = "WARN", Message = "Test message" };
        var str = diag.ToString();
        Assert.Contains("WARN", str);
        Assert.Contains("7", str);
        Assert.Contains("Test message", str);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// DxfBuilderLayerTests   (Item 3b — utility layer mapping)
// ─────────────────────────────────────────────────────────────────────────────
public class DxfBuilderLayerTests
{
    private static AsBuiltJob MakeJob(string runType, string structType = "Generic") =>
        new()
        {
            Identity  = new ProjectIdentity { JobNumber = "TEST-001", ClientName = "Unit Test", Drafter = "AI", Checker = "AI", FieldDate = System.DateTime.Today, RevisionNumber = 0 },
            PointRows = new ObservableCollection<PointRow>
            {
                new() { PointId = "1", Northing = 10000, Easting = 10000, Elevation = 0 },
                new() { PointId = "2", Northing = 10100, Easting = 10100, Elevation = 1 },
            },
            Network   = BuildNetwork(runType, structType)
        };

    private static RCS.Piping.Core.Models.PipeNetwork BuildNetwork(string runType, string structType)
    {
        var n = new RCS.Piping.Core.Models.PipeNetwork();
        n.AddRun(new RCS.Piping.Core.Models.PipeRun
        {
            Id = System.Guid.NewGuid().ToString(), Type = runType,
            FromPointId = "1", ToPointId = "2", Diameter = 8, Material = "DI"
        });
        n.AddStructure(new RCS.Piping.Core.Models.PipeStructure
        {
            Id = System.Guid.NewGuid().ToString(), Type = structType, PointId = "1"
        });
        return n;
    }

    [Theory]
    [InlineData("WATER",       "W-MAIN")]
    [InlineData("POTABLE",     "W-MAIN")]
    [InlineData("WASTEWATER",  "WW-MAIN")]
    [InlineData("SEWER",       "WW-MAIN")]
    [InlineData("FORCE MAIN",  "WW-FORCE-MAIN")]
    [InlineData("RECLAIM",     "RCL-MAIN")]
    [InlineData("STORM",       "ST-MAIN")]
    [InlineData("ELECTRIC",    "E-CONDUIT")]
    [InlineData("GAS",         "G-MAIN")]
    [InlineData("TELECOM",     "TEL-MAIN")]
    [InlineData("UNKNOWN",     "AS-BUILT-PIPES")]
    public void PipeLayer_MapsCorrectly(string runType, string expectedLayer)
    {
        var job = MakeJob(runType);
        var tmp = Path.GetTempFileName();
        try
        {
            new DxfBuilder().Build(job, tmp);
            var dxf = File.ReadAllText(tmp);
            Assert.Contains(expectedLayer, dxf);
        }
        finally { File.Delete(tmp); }
    }

    [Theory]
    [InlineData("MANHOLE",  "WW-MH")]
    [InlineData("VALVE",    "W-VALVE")]
    [InlineData("HYDRANT",  "W-HYDRANT")]
    [InlineData("JUNCTION", "ST-MH")]
    [InlineData("VAULT",    "E-VAULT")]
    [InlineData("Unknown",  "AS-BUILT-STRUCTURES")]
    public void StructureLayer_MapsCorrectly(string structType, string expectedLayer)
    {
        var job = MakeJob("WATER", structType);
        var tmp = Path.GetTempFileName();
        try
        {
            new DxfBuilder().Build(job, tmp);
            var dxf = File.ReadAllText(tmp);
            Assert.Contains(expectedLayer, dxf);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void DxfOutput_BeginsWithSectionHeader()
    {
        var job = MakeJob("WATER");
        var tmp = Path.GetTempFileName();
        try
        {
            new DxfBuilder().Build(job, tmp);
            var lines = File.ReadAllLines(tmp);
            Assert.Contains("SECTION", lines[1]);   // Group 0 = "SECTION"
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void DxfOutput_EndsWithEof()
    {
        var job = MakeJob("STORM");
        var tmp = Path.GetTempFileName();
        try
        {
            new DxfBuilder().Build(job, tmp);
            var content = File.ReadAllText(tmp).TrimEnd();
            Assert.EndsWith("EOF", content);
        }
        finally { File.Delete(tmp); }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// IntakeReportDiffTests   (Item 3c)
// ─────────────────────────────────────────────────────────────────────────────
public class IntakeReportDiffTests
{
    [Fact]
    public void IntakeReport_DefaultState_IsNotSuccess()
    {
        var r = new IntakeReport();
        Assert.False(r.Success);
        Assert.Equal("No file imported yet.", r.Summary);
    }

    [Fact]
    public void IntakeReport_DiffFields_InitializeToZero()
    {
        var r = new IntakeReport();
        Assert.Equal(0, r.RowsAdded);
        Assert.Equal(0, r.RowsUpdated);
        Assert.Equal(0, r.RowsSkipped);
        Assert.Empty(r.ValidationErrors);
    }

    [Fact]
    public void IntakeReport_CanSetDiffFields()
    {
        var r = new IntakeReport
        {
            RowsAdded      = 10,
            RowsUpdated    = 3,
            RowsSkipped    = 1,
            ValidationErrors = new List<string> { "Row 5: bad coordinate" }
        };
        Assert.Equal(10,  r.RowsAdded);
        Assert.Equal(3,   r.RowsUpdated);
        Assert.Equal(1,   r.RowsSkipped);
        Assert.Single(r.ValidationErrors);
    }

    [Fact]
    public void PnezdParser_PopulatesDiffFields_OnFreshImport()
    {
        // Build a temp PNEZD file with 5 unique points
        var csv = "1,10000.00,10000.00,0.0,BM1\n2,10100.00,10000.00,0.0,BM2\n3,10200.00,10000.00,0.0,BM3\n" +
                  "bad line\n5,10500.00,10000.00,0.0,BM5";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, csv);
        try
        {
            var job    = new AsBuiltJob { Identity = new ProjectIdentity() };
            var engine = new IntakeAnalysisEngine();
            var report = engine.Analyze(tmp, IntakeFileType.Pnezd, job);

            Assert.True(report.Success);
            Assert.Equal(4, report.RowsAdded);      // 4 parseable rows (line 4 skipped)
            Assert.Equal(0, report.RowsUpdated);    // no duplicates
            Assert.Equal(1, report.RowsSkipped);    // "bad line"
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void PnezdParser_UpdatesExistingPoint_IncreasesRowsUpdated()
    {
        var csv1 = "1,10000.00,10000.00,0.0,Original";
        var csv2 = "1,10001.00,10001.00,5.0,Updated";       // same point, different coords

        var tmp1 = Path.GetTempFileName();
        var tmp2 = Path.GetTempFileName();
        File.WriteAllText(tmp1, csv1);
        File.WriteAllText(tmp2, csv2);
        try
        {
            var job    = new AsBuiltJob { Identity = new ProjectIdentity() };
            var engine = new IntakeAnalysisEngine();
            engine.Analyze(tmp1, IntakeFileType.Pnezd, job);   // first import
            var report = engine.Analyze(tmp2, IntakeFileType.Pnezd, job);   // overwrite

            Assert.Equal(1, report.RowsUpdated);
            Assert.Equal(0, report.RowsAdded);
        }
        finally { File.Delete(tmp1); File.Delete(tmp2); }
    }
}
