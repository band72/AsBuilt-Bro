using System;
using System.Collections.Generic;
using System.Linq;
using RCS.Cogo.Core.Primitives;

namespace RCS.Alignments.Core;

public class Alignment
{
    public string Name { get; set; } = string.Empty;
    public double StartStation { get; set; } = 0.0;
    
    public List<HorizontalElement> Elements { get; } = new();
    public List<Profile> Profiles { get; } = new();

    public void AddElement(HorizontalElement element)
    {
        if (Elements.Count == 0)
        {
            element.StartStation = StartStation;
        }
        else
        {
            element.StartStation = Elements.Last().EndStation;
        }
        Elements.Add(element);
    }
    
    public Point3D? GetCoordinateAt(double station, double offset = 0.0)
    {
        var element = Elements.FirstOrDefault(e => station >= e.StartStation && station <= e.EndStation);
        return element?.GetCoordinateAt(station, offset);
    }

    public (double Station, double Offset)? GetStationOffset(Point3D point)
    {
        // Simple projection: just find the first element where the point projects perfectly
        foreach (var element in Elements)
        {
            var result = element.GetStationOffset(point);
            if (result.HasValue) return result;
        }
        return null;
    }
}
