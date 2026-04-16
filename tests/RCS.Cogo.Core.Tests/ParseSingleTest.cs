using System;
using System.IO;
using System.Linq;
using Xunit;
using RCS.Piping.Core.Scripting;

namespace RCS.Cogo.Core.Tests
{
    public class ParseSingleTest
    {
        [Fact]
        public void Test02()
        {
            var file = @"C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\docs\examples\02_advanced_curves.cogo";
            var script = File.ReadAllText(file);
            var compiler = new PipeScriptCompiler();
            var result = compiler.Compile(script, _ => null, new(), new());
            foreach(var diag in result.Diagnostics.Where(d => d.Severity == "ERROR"))
                Console.WriteLine($"ERROR {diag.LineNumber}: {diag.Message}");
        }
    }
}
