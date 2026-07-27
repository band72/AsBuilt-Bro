using System;
using System.IO;
using RCS.Piping.Core.Workflow;
using RCS.Piping.Core.Builders;
using RCS.Piping.Core.Engines;

namespace QuickDxfRunner {
    class Program {
        static void Main(string[] args) {
            static string FindRepoRoot(string start)
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "SampleScripts")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
                return start;
            }

            string repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            string scriptPath = Path.Combine(repoRoot, "SampleScripts", "JEA_Oakwood_WaterMain_70498-W1A.cogo");
            
            if (!File.Exists(scriptPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Sample script not found at: {scriptPath}");
                Console.ResetColor();
                return;
            }

            var job = new AsBuiltJob();
            job.Identity.JobNumber = "JEA-OAKWOOD-PROFILE-TEST";
            
            var intake = new IntakeAnalysisEngine();
            intake.Analyze(scriptPath, IntakeFileType.CogoScript, job);
            
            var validator = new ValidationEngine();
            var res = validator.Validate(job);
            foreach(var r in res.Issues) Console.WriteLine(r.Severity + " - " + r.Message);
            
            string outPath = Path.Combine(repoRoot, "Oakwood_Validation_Graph.dxf");
            var builder = new DxfBuilder();
            builder.Build(job, outPath);
            Console.WriteLine("Generated TEST DXF flawlessly at: " + outPath);
        }
    }
}
