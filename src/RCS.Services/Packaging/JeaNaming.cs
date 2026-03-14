using System.Text.RegularExpressions;

namespace RCS.Packaging.Naming;

public static class JeaNaming
{
    public static string RevSuffix(int revision) => revision <= 0 ? "" : $"_REV{revision}";

    public static string Clean(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "UNNAMED";
        var s = input.Trim();
        s = Regex.Replace(s, @"\s+", "_");
        s = Regex.Replace(s, @"[^A-Za-z0-9_\-]+", "_");
        s = Regex.Replace(s, @"_+", "_");
        return s.Trim('_');
    }

    public static string LockedStem(string availNo, string projectName, string utility, string units)
        => Clean($"JEA_{availNo}_{projectName}_{utility}_{units}");
}
