using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Alignments.Core;

public class Vpi
{
    public double Station { get; set; }
    public double Elevation { get; set; }
    public double CurveLength { get; set; } // 0 if it's just a grade break
}

public class Profile
{
    public string Name { get; set; } = string.Empty;
    public string ProfileType { get; set; } = "FG"; // e.g., EG or FG
    
    public List<Vpi> Intersections { get; } = new();

    public void AddVpi(Vpi vpi)
    {
        Intersections.Add(vpi);
        Intersections.Sort((a, b) => a.Station.CompareTo(b.Station));
    }

    public double? GetElevationAtStation(double station)
    {
        if (Intersections.Count == 0) return null;
        if (Intersections.Count == 1) return Intersections[0].Elevation;

        // Find applicable segment
        for (int i = 0; i < Intersections.Count - 1; i++)
        {
            var vpi1 = Intersections[i];
            var vpi2 = Intersections[i + 1];

            // If station is between these two VPIs
            if (station >= vpi1.Station && station <= vpi2.Station)
            {
                // Basic linear grade for now (no parabolic vertical curve implementation yet)
                double length = vpi2.Station - vpi1.Station;
                if (length == 0) return vpi1.Elevation;

                double grade = (vpi2.Elevation - vpi1.Elevation) / length;
                double dist = station - vpi1.Station;
                return vpi1.Elevation + grade * dist;
            }
        }

        return null;
    }
}
