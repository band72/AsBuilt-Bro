namespace RCS.Piping.Core.Scripting;

public class ScriptDiagnostic
{
    public int LineNumber { get; set; }
    public string Severity { get; set; } = "INFO"; // INFO, WARN, ERROR
    public string Message { get; set; } = string.Empty;

    public override string ToString() => $"[{Severity}] Line {LineNumber}: {Message}";
}
