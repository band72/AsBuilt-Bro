using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCS.Cogo.App;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Wpf;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;
using RCS.Cogo.Wpf.Services;

namespace HeadlessValidator
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            Console.WriteLine("--- RCS COGO Enterprise Headless Validation Test ---");

            // 1. Build host just like App.xaml.cs to resolve dependencies
            var host = App.CreateHostBuilder(Array.Empty<string>()).Build();
            var services = host.Services;

            // 2. Get the ViewModel and Script Engine
            var engine = services.GetRequiredService<ScriptEngine>();
            var context = services.GetRequiredService<ICogoContext>();

            // 3. Define mapping
            string scriptPath = @"C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\SampleScripts\JEA_Oakwood_WaterMain_70498-W1A.cogo";
            
            Console.WriteLine($"Parsing: {scriptPath}");
            string scriptCode = File.ReadAllText(scriptPath);

            // 4. Execute script batch
            await engine.ExecuteBatchAsync(scriptCode, context);

            // 5. Run the Validation Phase logic
            var job = new AsBuiltJob();
            job.Identity.JobNumber = "HEADLESS-TEST-OAKWOOD";
            
            if (context.PipingSystem != null)
            {
                foreach(var s in context.PipingSystem.Structures)
                    job.Network.AddStructure(s);
                foreach(var r in context.PipingSystem.Runs)
                    job.Network.AddRun(r);
            }

            var validator = new ValidationEngine();
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
