using System.Collections.Generic;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Scripting;

public class ScriptCompileResult
{
    public List<PipeRun> Runs { get; } = new();
    public List<PipeStructure> Structures { get; } = new();
    public List<ScriptDiagnostic> Diagnostics { get; } = new();
}
