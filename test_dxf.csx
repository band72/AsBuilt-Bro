using System;
using System.IO;
using System.Linq;
using RCS.Piping.Core.Workflow;
using RCS.Piping.Core.Builders;
using RCS.Cogo.Core;
using RCS.Cogo.App.Scripting;

// We fake a stub ICogoContext just to satisfy the AsBuiltJob
class StubCtx : ICogoContext {
    public System.Collections.Generic.IEnumerable<(string, RCS.Cogo.Core.Primitives.Point3D, string)> GetAllPoints() => null;
    public void AddPoint(string id, RCS.Cogo.Core.Primitives.Point3D pt, string desc) {}
    public RCS.Cogo.Core.Primitives.Point3D? GetPoint(string id) => null;
    public bool RemovePoint(string id) => false;
    public bool RenamePoint(string oldId, string newId) => false;
    public int GetNextPointId() => 1;
    public void AddFigure(Figure f) {}
    public Figure GetFigure(string name) => null;
    public bool DeleteFigure(string name) => false;
    public System.Collections.Generic.IEnumerable<Figure> GetAllFigures() => null;
    public void Log(string m) {}
    public void ClearLog() {}
    public RCS.Cogo.Core.Primitives.Point3D? CurrentStation { get; set; }
    public RCS.Cogo.Core.Primitives.Point3D? CurrentBacksight { get; set; }
    public bool TraverseMode { get; set; }
    public string Units { get; set; }
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double ScaleFactor { get; set; }
    public bool AtmosCorrection { get; set; }
    public bool CurvatureRefraction { get; set; }
    public bool AutoPoint { get; set; }
    public string AngleFormat { get; set; }
    public string VerticalFormat { get; set; }
    public string EdmMode { get; set; }
    public string PrismMode { get; set; }
    public double MapCheckClosureTolerance { get; set; }
    public bool ShowAlignmentLabels { get; set; }
    public bool ShowVerticalAlignmentLabels { get; set; }
    public bool ShowVPIs { get; set; }
    public bool ShowGradePercent { get; set; }
    public bool OutputEnabled { get; set; }
    public Figure CurrentFigure { get; set; }
    public (RCS.Cogo.Core.Primitives.Point3D? Left, RCS.Cogo.Core.Primitives.Point3D? Right) LastIntersections { get; set; }
}

var job = new AsBuiltJob(new StubCtx());
string scriptPath = @""tests\RCS.Cogo.Core.Tests\TestData\JEA_Oakwood_WaterMain_70498-W1A.cogo"";
var lines = File.ReadAllLines(scriptPath);

var pointMapper = new ScriptPointMapper();
job.PointRows = pointMapper.ExtractPoints(lines);

var networkBuilder = new NetworkBuilder();
job.Network = networkBuilder.BuildFromScript(lines);

var valEngine = new ValidationEngine();
var result = valEngine.Validate(job);

Console.WriteLine($""Validation Issues Found: {result.Issues.Count}"");
foreach (var issue in result.Issues) {
    Console.WriteLine($""[{issue.Severity}] {issue.RuleName}: {issue.Message}"");
}

string dxfPath = @""Validation_Profile_Test.dxf"";
var dxfBuilder = new DxfBuilder();
dxfBuilder.Build(job, dxfPath);

Console.WriteLine($""DXF Created: {dxfPath}"");
