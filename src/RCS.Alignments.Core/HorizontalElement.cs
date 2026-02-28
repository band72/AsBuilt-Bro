using System;
using RCS.Cogo.Core.Primitives;

namespace RCS.Alignments.Core;

public abstract class HorizontalElement
{
    public double StartStation { get; set; }
    public abstract double Length { get; }
    public double EndStation => StartStation + Length;

    public abstract Point3D GetCoordinateAt(double station, double offset = 0.0);
    public abstract (double Station, double Offset)? GetStationOffset(Point3D point);
    
    // Derived classes will store their own geometry (e.g. Start/End points, Arcs, etc.)
}
