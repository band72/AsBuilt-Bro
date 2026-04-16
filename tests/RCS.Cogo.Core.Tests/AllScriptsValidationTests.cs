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
            var baseDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..");
            var sampleDir = Path.Combine(baseDir, "SampleScripts");
            var docsDir = Path.Combine(baseDir, "docs", "examples");
            
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
