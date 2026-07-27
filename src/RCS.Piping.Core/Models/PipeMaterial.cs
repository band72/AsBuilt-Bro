using System;

namespace RCS.Piping.Core.Models;

/// <summary>
/// Strongly-typed enumeration representing canonical pipe material classifications.
/// </summary>
public enum PipeMaterial
{
    Pvc,
    DuctileIron,
    Hdpe,
    Aluminum,
    CorrugatedMetal,
    ReinforcedConcrete,
    Abs,
    CastIron,
    GalvanizedIron,
    StainlessSteel,
    Cpvc,
    Concrete,
    Clay,
    Steel,
    None,
    Unknown
}

/// <summary>
/// Normalization utility to parse raw string material inputs (e.g. "p.v.c.", "DIP", "hdpe")
/// into standardized PipeMaterial enum values and uniform display labels.
/// </summary>
public static class PipeMaterialParser
{
    public static PipeMaterial Parse(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput)) return PipeMaterial.Unknown;

        string clean = rawInput.Trim().ToUpperInvariant()
            .Replace(".", "")
            .Replace("-", "")
            .Replace(" ", "");

        return clean switch
        {
            "PVC" or "POLYVINYLCHLORIDE" => PipeMaterial.Pvc,
            "DIP" or "DUCTILEIRON" or "DI" => PipeMaterial.DuctileIron,
            "HDPE" or "PE" or "POLYETHYLENE" => PipeMaterial.Hdpe,
            "ALUM" or "ALUMINUM" => PipeMaterial.Aluminum,
            "CMP" or "CORRUGATEDMETAL" => PipeMaterial.CorrugatedMetal,
            "RCP" or "REINFORCEDCONCRETE" => PipeMaterial.ReinforcedConcrete,
            "ABS" => PipeMaterial.Abs,
            "CI" or "CASTIRON" => PipeMaterial.CastIron,
            "GI" or "GALVANIZEDIRON" => PipeMaterial.GalvanizedIron,
            "SS" or "STAINLESSSTEEL" or "STAINLESS" => PipeMaterial.StainlessSteel,
            "CPVC" => PipeMaterial.Cpvc,
            "CONC" or "CONCRETE" => PipeMaterial.Concrete,
            "CLAY" or "VITRIFIEDCLAY" => PipeMaterial.Clay,
            "STEEL" or "CARBONSTEEL" => PipeMaterial.Steel,
            "NONE" => PipeMaterial.None,
            _ => PipeMaterial.Unknown
        };
    }

    public static string ToStandardCode(PipeMaterial material) => material switch
    {
        PipeMaterial.Pvc => "PVC",
        PipeMaterial.DuctileIron => "DIP",
        PipeMaterial.Hdpe => "HDPE",
        PipeMaterial.Aluminum => "ALUM",
        PipeMaterial.CorrugatedMetal => "CMP",
        PipeMaterial.ReinforcedConcrete => "RCP",
        PipeMaterial.Abs => "ABS",
        PipeMaterial.CastIron => "CI",
        PipeMaterial.GalvanizedIron => "GI",
        PipeMaterial.StainlessSteel => "SS",
        PipeMaterial.Cpvc => "CPVC",
        PipeMaterial.Concrete => "CONC",
        PipeMaterial.Clay => "CLAY",
        PipeMaterial.Steel => "STEEL",
        PipeMaterial.None => "NONE",
        _ => "UNKNOWN"
    };
}
