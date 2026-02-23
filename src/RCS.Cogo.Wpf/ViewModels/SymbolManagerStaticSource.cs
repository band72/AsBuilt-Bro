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
        "fitting", "pipe", "manhole", "valve" 
    };

    public static string[] AvailableSymbols { get; } = new[]
    {
        "Symbol_Manhole", "Symbol_Inlet", "Symbol_Meter", "Symbol_WaterValve", "Symbol_Hydrant", "Symbol_Fitting", "Symbol_Default"
    };
}
