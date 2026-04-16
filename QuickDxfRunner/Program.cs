using System;
using System.IO;
using RCS.Piping.Core.Workflow;
using RCS.Piping.Core.Builders;
using RCS.Piping.Core.Engines;

namespace QuickDxfRunner {
    class Program {
        static void Main(string[] args) {
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "JEA-OAKWOOD-PROFILE-TEST";
            string scriptPath = @"C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\SampleScripts\JEA_Oakwood_WaterMain_70498-W1A.cogo";
            
            var intake = new IntakeAnalysisEngine();
            intake.Analyze(scriptPath, IntakeFileType.CogoScript, job);
            
            var validator = new ValidationEngine();
            var res = validator.Validate(job);
            foreach(var r in res.Issues) Console.WriteLine(r.Severity + " - " + r.Message);
            
            string outPath = @"C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\Oakwood_Validation_Graph.dxf";
            var builder = new DxfBuilder();
            builder.Build(job, outPath);
            Console.WriteLine("Generated TEST DXF flawlessly at: " + outPath);
        }
    }
}
