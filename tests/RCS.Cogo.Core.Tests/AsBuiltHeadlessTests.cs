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
    public class AsBuiltHeadlessTests
    {
        [Fact]
        public void HeadlessOakwoodTest()
        {
            // 1. Arrange
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "70498-W1A HEADLESS TEST";

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

            // 2. Run Intake Engine
            var intake = new IntakeAnalysisEngine();
            var report = intake.Analyze(scriptPath, IntakeFileType.CogoScript, job);
            
            // 3. Run Validation Engine
            var validator = new ValidationEngine();
            var result = validator.Validate(job);

            // 4. Output Summary
            Console.WriteLine("--- HEADLESS WORKFLOW REPORT ---");
            Console.WriteLine($"Points Loaded: {report.PointsLoaded}");
            Console.WriteLine($"Runs Loaded: {report.RunsLoaded}");
            Console.WriteLine("--------------------------------");
            Console.WriteLine($"Validation Errors: {result.ErrorCount}");
            Console.WriteLine($"Validation Warnings: {result.WarningCount}");

            foreach(var issue in result.Issues) 
            {
                Console.WriteLine($"[{issue.Severity}] {issue.Category} | {issue.RuleName} - {issue.Message}");
            }

            Assert.True(result.Issues.Count > 0, "Expected the Oakwood script to generate some validation warnings/errors to test the logic.");
        }

        [Fact]
        public void AutoFixLandXmlExportTest()
        {
            // Arrange
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "123-EXPORT-TEST";
            
            // Add a mock structure with an unmapped part to simulate import state
            job.Network.Structures["S1"] = new PipeStructure { Id = "S1", PointId = "100", Type = "CB-1" };
            job.PointRows.Add(new PointRow { PointId = "100", Easting = 100, Northing = 100, Elevation = 5.0 });
            
            job.PartMappings.Add(new PartMappingEntry { AssetId = "S1", DetectedDesc = "CB-1", Status = MappingStatus.Pending });
            
            var validator = new ValidationEngine();
            var initialResult = validator.Validate(job);
            
            // Confirm we have an unmapped parts error
            Assert.Contains(initialResult.Issues, i => i.RuleName == "UNMAPPED_PARTS");
            
            // Act: Execute auto-mapping heuristic manually
            var unmapped = job.PartMappings.FirstOrDefault(m => m.AssetId == "S1");
            Assert.NotNull(unmapped);
            
            var dict = new System.Collections.Generic.Dictionary<string, string> {
                { "CB", "Catch Basin" }, { "MH", "Sewer Manhole" }
            };
            
            foreach (var kvp in dict) {
                if (unmapped.DetectedDesc.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0) {
                    unmapped.PartKey = kvp.Value;
                    unmapped.Status = MappingStatus.Resolved;
                    break;
                }
            }
            
            var finalResult = validator.Validate(job);
            
            // Assert error resolved and LandXML can be emitted
            Assert.DoesNotContain(finalResult.Issues, i => i.RuleName == "UNMAPPED_PARTS");
            Assert.Equal("Catch Basin", unmapped.PartKey);
            
            string outPath = Path.Combine(Path.GetTempPath(), "test_export.xml");
            RCS.Piping.Core.Builders.LandXmlBuilder.Export(job, outPath);
            Assert.True(File.Exists(outPath));
            
            // Cleanup
            if (File.Exists(outPath)) File.Delete(outPath);
        }

        [Fact]
        public void AdvancedTopographyDeviationTest()
        {
            // Arrange
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "TOLERANCE-TEST";

            // Stage an As-Built Run that is too shallow and deviated from design
            job.Network.Structures["MH-1"] = new PipeStructure { Id = "MH-1", PointId = "P1", Type = "Sanitary Manhole" };
            job.PointRows.Add(new PointRow { PointId = "P1", Easting = 5000, Northing = 5000, Elevation = 90.0 });
            
            job.Network.Runs["R-1"] = new PipeRun { Id = "R-1", FromPointId = "P1", ToPointId = "P2", Diameter = 8, InvertStart = 98.0 }; // 98.0 inverted

            // Create a TIN Surface where ground is at 100.0ft
            job.BaseSurface = new RCS.Piping.Core.Models.TopographicSurface();
            job.BaseSurface.Points.Add(new RCS.Piping.Core.Models.TopographicPoint { Easting = 5000, Northing = 5000, Elevation = 100.0 });

            // Create a Design Baseline where Invert Start was intended to be 94.0ft instead of 98.0
            job.DesignBaseline = new AsBuiltJob();
            job.DesignBaseline.Network.Runs["R-1"] = new PipeRun { Id = "R-1", InvertStart = 94.0 };

            // Act
            var validator = new ValidationEngine();
            var result = validator.Validate(job);

            // Assert Cover Violation (ground 100 - invert 98 = 2ft. < 3ft statutory cover)
            Assert.Contains(result.Issues, i => i.RuleName == "MINIMUM_COVER");
            
            // Assert Deviation Violation (As-built 98 - Design 94 = 4ft deviation > 0.5ft tolerance)
            Assert.Contains(result.Issues, i => i.RuleName == "DESIGN_DEVIATION");
        }

        [Fact]
        public void ExportBundleBuilderTest()
        {
            // Arrange
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "BUNDLE-TEST-70498";
            
            job.Network.Structures["S1"] = new PipeStructure { Id = "S1", PointId = "100", Type = "Catch Basin" };
            job.PointRows.Add(new PointRow { PointId = "100", Easting = 1000, Northing = 1000, Elevation = 10.0, Description = "CB-1" });
            
            var dxfBuilder = new RCS.Piping.Core.Builders.DxfBuilder();
            var pdfBuilder = new RCS.Piping.Core.Builders.PdfReportBuilder();
            var csvBuilder = new RCS.Piping.Core.Builders.PnezdExportBuilder();
            var bundleBuilder = new RCS.Piping.Core.Builders.ExportBundleBuilder(dxfBuilder, pdfBuilder, csvBuilder);

            string targetDir = Path.Combine(Path.GetTempPath(), "ExportBundleTest_" + Guid.NewGuid().ToString("N"));

            try
            {
                // Act
                var result = bundleBuilder.Build(job, targetDir);

                // Assert all 4 deliverable files exist
                Assert.True(File.Exists(result.DxfPath), "DXF file must exist");
                Assert.True(File.Exists(result.LandXmlPath), "LandXML file must exist");
                Assert.True(File.Exists(result.PdfReportPath), "PDF report file must exist");
                Assert.True(File.Exists(result.PnezdCsvPath), "PNEZD CSV file must exist");
            }
            finally
            {
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, recursive: true);
                }
            }
        }
    }
}
