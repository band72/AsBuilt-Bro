using System;
using System.IO;
using System.Linq;
using Xunit;
using RCS.Piping.Core.Scripting;

namespace RCS.Cogo.Core.Tests
{
    public class ParseSingleTest
    {
        // Walk up from the test binary to find the repo root (where docs/examples/ lives).
        private static string? FindRepoRoot(string start)
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "docs", "examples")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        [Fact]
        public void Test02()
        {
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            if (repoRoot == null)
                return;   // repo root not found — skip gracefully

            var file = Path.Combine(repoRoot, "docs", "examples", "02_advanced_curves.cogo");
            if (!File.Exists(file))
                return;   // file not present — skip gracefully

            var script   = File.ReadAllText(file);
            var compiler = new PipeScriptCompiler();
            var result   = compiler.Compile(script, _ => null, new(), new());
            foreach (var diag in result.Diagnostics.Where(d => d.Severity == "ERROR"))
                Console.WriteLine($"ERROR {diag.LineNumber}: {diag.Message}");
        }
    }
}
