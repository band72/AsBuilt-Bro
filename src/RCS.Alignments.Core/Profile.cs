using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Alignments.Core;

public class Vpi
{
    public double Station { get; set; }
    public double Elevation { get; set; }
    public double CurveLength { get; set; } // 0 = grade break, >0 = parabolic VC length
}

public class Profile
{
    public string Name { get; set; } = string.Empty;
    public string ProfileType { get; set; } = "FG"; // EG or FG

    public List<Vpi> Intersections { get; } = new();

    public void AddVpi(Vpi vpi)
    {
        Intersections.Add(vpi);
        Intersections.Sort((a, b) => a.Station.CompareTo(b.Station));
    }

    /// <summary>
    /// Returns elevation at a given station using parabolic vertical curve math
    /// where CurveLength > 0, otherwise linear grade interpolation.
    /// Standard highway formula: y = y_bvc + g1*x + (g2-g1)/(2L) * x²
    /// </summary>
    public double? GetElevationAtStation(double station)
    {
        if (Intersections.Count == 0) return null;
        if (Intersections.Count == 1) return Intersections[0].Elevation;

        if (station <= Intersections[0].Station) return Intersections[0].Elevation;
        if (station >= Intersections[^1].Station) return Intersections[^1].Elevation;

        for (int i = 0; i < Intersections.Count - 1; i++)
        {
            var v1 = Intersections[i];
            var v2 = Intersections[i + 1];

            if (station > v2.Station) continue;

            double segLen = v2.Station - v1.Station;
            if (segLen <= 0) return v1.Elevation;

            double g1 = (v2.Elevation - v1.Elevation) / segLen;

            // Check if VPI i+1 has a parabolic VC
            double vcLen = v2.CurveLength;
            if (vcLen > 0 && i + 2 < Intersections.Count)
            {
                var v3 = Intersections[i + 2];
                double nextSegLen = v3.Station - v2.Station;
                double g2 = nextSegLen > 0 ? (v3.Elevation - v2.Elevation) / nextSegLen : g1;

                double bvc = v2.Station - vcLen / 2.0;
                double evc = v2.Station + vcLen / 2.0;
                double elevBvc = v2.Elevation - g1 * (vcLen / 2.0);

                if (station >= bvc && station <= evc)
                {
                    double x = station - bvc;
                    return elevBvc + g1 * x + (g2 - g1) / (2.0 * vcLen) * x * x;
                }
                else if (station < bvc)
                {
                    double distFromV1 = station - v1.Station;
                    return v1.Elevation + g1 * distFromV1;
                }
                else
                {
                    double elevEvc = elevBvc + g1 * vcLen + (g2 - g1) / (2.0 * vcLen) * vcLen * vcLen;
                    double distFromEvc = station - evc;
                    return elevEvc + g2 * distFromEvc;
                }
            }

            // No VC — simple linear grade
            double dist = station - v1.Station;
            return v1.Elevation + g1 * dist;
        }

        return null;
    }

    /// <summary>Returns grade at a station as a formatted percentage string e.g. "+2.50%"</summary>
    public string GetGradeStringAt(double station)
    {
        if (Intersections.Count < 2) return "0.00%";
        for (int i = 0; i < Intersections.Count - 1; i++)
        {
            var v1 = Intersections[i];
            var v2 = Intersections[i + 1];
            if (station >= v1.Station && station <= v2.Station)
            {
                double len = v2.Station - v1.Station;
                if (len <= 0) return "0.00%";
                double grade = (v2.Elevation - v1.Elevation) / len * 100.0;
                return $"{grade:+0.00;-0.00}%";
            }
        }
        return "0.00%";
    }
}
