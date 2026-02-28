namespace RCS.Cogo.App.State;

public class CogoCode
{
    public string LocalCode { get; set; } = string.Empty;
    public string SystemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string SymbolImagePath
    {
        get
        {
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            // The executable runs in src\RCS.Cogo.Wpf\bin\Debug\net8.0-windows\
            // We want to point to c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\SymbolsLibrary
            var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var libraryPath = System.IO.Path.Combine(repoRoot, "SymbolsLibrary");
            
            var expectedPath = System.IO.Path.Combine(libraryPath, $"{LocalCode}_{SystemCode}.png");
            if (System.IO.File.Exists(expectedPath))
            {
                return expectedPath;
            }
            return "";
        }
    }

    public CogoCode() { }

    public CogoCode(string local, string system, string desc)
    {
        LocalCode = local;
        SystemCode = system;
        Description = desc;
    }
}
