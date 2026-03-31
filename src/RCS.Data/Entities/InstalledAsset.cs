namespace RCS.Data.Entities;
using System;

public abstract class InstalledAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string? SourceSheetRowIndex { get; set; }

    // Core ID & Internal
    public string? PartKey { get; set; }
    public string? Discipline { get; set; }
    public string? FeatureType { get; set; }
    public int? Quantity { get; set; }

    // Shared Attributes
    public string? Subtype { get; set; }
    public string? FacilityOwner { get; set; }
    public string? Size { get; set; }
    public string? SizeSecondary { get; set; }
    public string? Material { get; set; }
    public string? PipeClass { get; set; }
    public string? LiningManufacturer { get; set; }
    public string? LiningMaterial { get; set; }
    public string? Orientation { get; set; }
    public string? PipeRole { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNo { get; set; }
    public string? YearManufactured { get; set; }
    public string? RfidBarcode { get; set; }

    // Elevations & Depths 
    public double? GradeElevation { get; set; }
    public double? TopElevation { get; set; }
    public double? Depth { get; set; }
    public double? Cover { get; set; }

    // Pipe Runs
    public double? Length { get; set; }
    public double? DownstreamInvert { get; set; }
    public double? DownstreamGrade { get; set; }
    public double? UpstreamInvert { get; set; }
    public double? UpstreamGrade { get; set; }
    public double? Slope { get; set; }

    // Coordinates (Shared for points)
    public double? Easting { get; set; }
    public double? Northing { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? UpstreamPointId { get; set; }
    public string? DownstreamPointId { get; set; }

    // Pipe segment endpoints (for water/reclaimed/pressure pipes that have GPS start+end)
    public double? StartNorthing { get; set; }
    public double? StartEasting { get; set; }
    public double? EndNorthing { get; set; }
    public double? EndEasting { get; set; }

    // Valves
    public string? ValveType { get; set; }
    public string? OpenDirection { get; set; }
    public double? TurnsToOpen { get; set; }
    public double? NutElevation { get; set; }
    public double? DepthToNut { get; set; }

    // Manholes
    public string? ManholeType { get; set; }
    public string? DropType { get; set; }
    public double? RimElevation { get; set; }
    public string? InvertElevationsWithDirections { get; set; }
    public double? LowestInvertElevation { get; set; }
    public string? ExteriorJointTapeType { get; set; }
    public string? ExteriorJointTapeManufacturer { get; set; }

    }