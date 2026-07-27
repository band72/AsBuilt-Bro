using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCS.Cogo.App;
using RCS.Piping.Core.Engines;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;

namespace HeadlessValidator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- RCS COGO Enterprise Headless Validation Test ---");

            // 1. Setup DI services without WPF runtime dependencies
            var services = new ServiceCollection()
                .AddCogoServices()
                .BuildServiceProvider();

            // 2. Resolve components from DI container
            var intake = services.GetRequiredService<IntakeAnalysisEngine>();
            var validator = services.GetRequiredService<ValidationEngine>();

            // 3. Dynamically resolve script path relative to repo root
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
                Console.WriteLine($"ERROR: Script file not found at: {scriptPath}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Parsing: {scriptPath}");

            // 4. Run Intake & Analysis Engine
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "HEADLESS-TEST-OAKWOOD";

            var report = intake.Analyze(scriptPath, IntakeFileType.CogoScript, job);
            Console.WriteLine($"Intake Analysis Complete: {report.PointsLoaded} points, {report.RunsLoaded} runs parsed.");

            // 5. Run Validation Engine
            var result = validator.Validate(job);

            // 6. Print Report
            Console.WriteLine("========================================================");
            Console.WriteLine($"VALIDATION RESULT: {result.ErrorCount} Errors | {result.WarningCount} Warnings");
            Console.WriteLine("========================================================");
            
            foreach (var issue in result.Issues)
            {
                string tag = issue.Severity == IssueSeverity.Error ? "[ERROR]  " : "[WARNING] " ;
                Console.WriteLine($"{tag} {issue.Category} | {issue.RuleName}");
                Console.WriteLine($"     Msg: {issue.Message}");
                if (issue.TargetId != null) 
                    Console.WriteLine($"     Id:  {issue.TargetId}");
                Console.WriteLine();
            }

            Console.WriteLine("Headless Pipeline Test Complete.");
        }
    }
}
