using System;
using System.Collections.Generic;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Services;

public interface IMaterialCatalogCache
{
    bool IsValidMaterial(string rawMaterial);
    PipeMaterial NormalizeMaterial(string rawMaterial);
    string GetCanonicalCode(string rawMaterial);
    IReadOnlySet<string> ValidCodes { get; }
}

public sealed class MaterialCatalogCache : IMaterialCatalogCache
{
    private static readonly HashSet<string> _knownCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PVC","DIP","PE","ALUM","HDPE","CMP","RCP","ABS","CI","GI","SS","CPVC","CONC","CLAY","STEEL",
        "NONE","UNKNOWN"
    };

    public IReadOnlySet<string> ValidCodes => _knownCodes;

    public bool IsValidMaterial(string rawMaterial)
    {
        if (string.IsNullOrWhiteSpace(rawMaterial)) return false;
        var mat = PipeMaterialParser.Parse(rawMaterial);
        return mat != PipeMaterial.Unknown || _knownCodes.Contains(rawMaterial.Trim());
    }

    public PipeMaterial NormalizeMaterial(string rawMaterial)
    {
        return PipeMaterialParser.Parse(rawMaterial);
    }

    public string GetCanonicalCode(string rawMaterial)
    {
        var mat = PipeMaterialParser.Parse(rawMaterial);
        if (mat != PipeMaterial.Unknown)
        {
            return PipeMaterialParser.ToStandardCode(mat);
        }
        return rawMaterial?.Trim().ToUpperInvariant() ?? "UNKNOWN";
    }
}
