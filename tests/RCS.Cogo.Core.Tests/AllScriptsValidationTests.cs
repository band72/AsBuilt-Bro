using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Engines;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Core.Tests
{
    public class AllScriptsValidationTests
    {
        [Fact]
        public void ValidateAllSampleScripts()
        {
            // Walk up from the test binary directory until we find the repo root
            // (identified by the presence of the SampleScripts folder).
            // This works on both local dev boxes and CI runners regardless of
            // how many levels deep the test output directory is.
            static string? FindRepoRoot(string start)
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "SampleScripts")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
                return null;
            }

            var repoRoot = FindRepoRoot(AppContext.BaseDirectory)
                ?? throw new DirectoryNotFoundException(
                    $"Could not locate repo root containing SampleScripts/ from: {AppContext.BaseDirectory}");

            var sampleDir = Path.Combine(repoRoot, "SampleScripts");
            var docsDir   = Path.Combine(repoRoot, "docs", "examples");

            var files = Directory.GetFiles(sampleDir, "*.txt", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(sampleDir, "*.cogo", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(docsDir, "*.cogo", SearchOption.AllDirectories))
                .ToArray();

            int totalErrors = 0;
            Console.WriteLine($"Found {files.Length} test scripts.");

            // Exclude CogoSeeder generated lists, output reports, and legacy mixed scripts
            files = files.Where(f => !f.EndsWith(".cs") 
                                  && !f.Contains("FileListAbsolute")
                                  && !f.Contains("AllAssets_Report")
                                  && !f.Contains("JEA_Mix_Script")).ToArray();

            foreach(var file in files)
            {
                var job = new AsBuiltJob();
                try
                {
                    var intake = new IntakeAnalysisEngine();
                    var report = intake.Analyze(file, IntakeFileType.CogoScript, job);
                    
                    var validator = new ValidationEngine();
                    var result = validator.Validate(job);

                    if (!report.Success) {
                        Console.WriteLine($"FAILED INTAKE: {Path.GetFileName(file)} - {report.Summary}");
                        totalErrors++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CRASH IN {Path.GetFileName(file)}: {ex.Message}");
                    totalErrors++;
                }
            }

            Assert.True(totalErrors == 0, $"Found {totalErrors} scripts that failed intake analysis parser!");
        }
    }
}
