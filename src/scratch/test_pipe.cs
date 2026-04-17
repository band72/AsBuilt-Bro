using System;
using System.Collections.Generic;
using RCS.Piping.Core.Scripting;
using System.Linq;

class Program {
    static void Main() {
        var compiler = new PipeScriptCompiler();
        var ms = new HashSet<string>();
        var cs = new HashSet<string>();
        var text = @"PT 1001 5025 1000 100 CORNER
PT 1002 5125 1000 100 CORNER
PT 1003 5125 1050 100 CORNER
PT 1004 5025 1050 100 CORNER
BEG LOT_E1
CONT 1001
CONT 1002
CONT 1003
CONT 1004
CLOSE
END";
        var result = compiler.Compile(text, id => true, ms, cs);
        foreach(var msg in result.Diagnostics) {
            Console.WriteLine(msg.Severity + ": " + msg.Message);
        }
    }
}
