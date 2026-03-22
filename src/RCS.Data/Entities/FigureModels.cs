using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RCS.Data.Entities;

// The underlying COGO point stored in EF Core
public class SurveyPoint
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public string PointNumber { get; set; } = string.Empty;

    public double Northing { get; set; }
    public double Easting { get; set; }
    public double Elevation { get; set; }
    public string Description { get; set; } = string.Empty;

    // Navigation Property: What figures use this point?
    public ICollection<FigureVertex> FigureVertices { get; set; } = new List<FigureVertex>();
}

// The linework parent object inheriting from InstalledAsset to appear in the Assets UI
public class Figure : InstalledAsset
{
    // Inherits Id (string), ProjectId, PartKey, Discipline, etc. from InstalledAsset

    [Required]
    public string Name { get; set; } = string.Empty;
    
    // Will be "Horizontal_Align", "Vertical_Align", or "Parcel"
    public string Layer { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public bool IsVisible { get; set; } = true;

    public string? DescriptionText { get; set; } // So user can input a custom description
    
    // To retain backward compatibility with old alignment tabs allowing manual script entry
    public string? ScriptContent { get; set; }

    // Navigation Property: The ordered list of vertices that construct the line
    public List<FigureVertex> Vertices { get; set; } = new();
}

// The ordered map connecting points to construct the figure
public class FigureVertex
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string FigureId { get; set; } = string.Empty;
    [ForeignKey(nameof(FigureId))]
    public Figure Figure { get; set; } = null!;

    public string PointId { get; set; } = string.Empty;
    [ForeignKey(nameof(PointId))]
    public SurveyPoint Point { get; set; } = null!;

    // The order this vertex appears in the trace
    public int OrderIndex { get; set; }

    // AutoCAD Standard: Tangent of 1/4 of the included angle. 0 = Straight line.
    public double Bulge { get; set; } = 0.0;
}
