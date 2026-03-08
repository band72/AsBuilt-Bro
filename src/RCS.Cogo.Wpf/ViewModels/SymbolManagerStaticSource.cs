namespace RCS.Cogo.Wpf.ViewModels;

public static class SymbolManagerStaticSource
{
    public static string[] Disciplines { get; } = new[] 
    { 
        "gas", "electric", "water", "sewer", "waste water", "reclaimed", "chilled",
        "g", "st", "d", "e", "el"
    };

    public static string[] Types { get; } = new[] 
    { 
        "fitting", "pipe", "manhole", "valve", "meter"
    };

    public static string[] AvailableSymbols { get; } = GetAvailableSymbols();

    private static string[] GetAvailableSymbols()
    {
        var list = new System.Collections.Generic.List<string>
        {
            "Symbol_Manhole", "Symbol_Inlet", "Symbol_Meter", "Symbol_WaterValve", "Symbol_Hydrant", "Symbol_Fitting", "Symbol_Default"
        };
        
        string dir = @"C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\SymbolsLibrary";
        if (System.IO.Directory.Exists(dir))
        {
            foreach(var file in System.IO.Directory.GetFiles(dir, "*.png"))
            {
                list.Add(file);
            }
        }
        return list.ToArray();
    }
}
