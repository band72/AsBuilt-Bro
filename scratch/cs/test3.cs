using System;
using System.Linq;

class Prog {
    static void Main() {
        string t = "WATER WMET";
        string t2 = "WATER WM";
        string SymbolType = "Default";
        string SymbolType2 = "Default";

        if (t.Contains("MET") || t.Equals("WMET") || t.Equals("GMET") || t.Equals("EMET")) SymbolType = "Meter";
        if (t2.Contains("MH") || t2.EndsWith("M") || t2.EndsWith("MH") || t2.Contains("STM") || t2.Equals("WWM")) SymbolType2 = "Manhole";

        Console.WriteLine($"{t} -> {SymbolType}");
        Console.WriteLine($"{t2} -> {SymbolType2}");
    }
}
