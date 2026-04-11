using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class MapCheckCommand : ICommand
{
    private string FormatBearing(double azimuthDegrees)
    {
        double az = azimuthDegrees % 360;
        if (az < 0) az += 360;

        string FormatDMS(double deg)
        {
            int d = (int)deg;
            double minutesRaw = (deg - d) * 60.0;
            int m = (int)minutesRaw;
            double s = Math.Round((minutesRaw - m) * 60.0);
            if (s >= 60)
            {
                s -= 60;
                m++;
            }
            if (m >= 60)
            {
                m -= 60;
                d++;
            }
            return $"{d}° {m:00}' {s:00}\"";
        }

        if (az < 90) return $"N {FormatDMS(az)} E";
        if (az <= 180) return $"S {FormatDMS(180 - az)} E";
        if (az < 270) return $"S {FormatDMS(az - 180)} W";
        return $"N {FormatDMS(360 - az)} W";
    }

    public string Name => "MAPCHECK";
    public string Description => "Calculates Area and Closure for a Figure. Usage: MAPCHECK <FigureName>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Usage: MAPCHECK <FigureName>");
            return Task.CompletedTask;
        }

        string figName = args[1];
        var figure = context.GetFigure(figName);

        if (figure == null)
        {
            context.Log($"Error: Figure {figName} not found.");
            return Task.CompletedTask;
        }

        if (figure.PointIds.Count < 3)
        {
            context.Log("Error: Figure must have at least 3 points for MapCheck.");
            return Task.CompletedTask;
        }

        var points = new List<Point3D>();
        foreach (var id in figure.PointIds)
        {
            var p = context.GetPoint(id);
            if (p == null)
            {
                context.Log($"Error: Point {id} in figure not found.");
                return Task.CompletedTask;
            }
            points.Add(p);
        }

        context.Log($"======================================================================");
        context.Log($"                 SURVEY MAPCHECK: {(string.IsNullOrEmpty(figName) ? "FIGURE" : figName.ToUpper())}");
        context.Log($"======================================================================");
        context.Log($"Start Point: {figure.PointIds[0]}    \tN: {points[0].Northing:F4}   \tE: {points[0].Easting:F4}");

        figure.Labels.Clear();
        for (int i = 0; i < points.Count - 1; )
        {
            var p1 = points[i];
            string id1 = figure.PointIds[i];

            int nextIdx = i + 1;
            string nextId = figure.PointIds[nextIdx];
            
            if (nextId.StartsWith("XC_"))
            {
                int endCurveIdx = nextIdx;
                while (endCurveIdx < points.Count && figure.PointIds[endCurveIdx].StartsWith("XC_"))
                {
                    endCurveIdx++;
                }

                if (endCurveIdx < points.Count)
                {
                    var pEnd = points[endCurveIdx];
                    double arcLength = 0;
                    for (int j = i; j < endCurveIdx; j++)
                    {
                        arcLength += GeometryEngine.Inverse(points[j], points[j+1]).Distance;
                    }
                    var chord = GeometryEngine.Inverse(p1, pEnd);
                    double chordLength = chord.Distance;

                    var pMid = points[i + (endCurveIdx - i) / 2];
                    double a = GeometryEngine.Inverse(p1, pMid).Distance;
                    double b = GeometryEngine.Inverse(pMid, pEnd).Distance;
                    double c = chordLength;
                    double s = (a + b + c) / 2;
                    double areaT = Math.Sqrt(Math.Max(0, s * (s - a) * (s - b) * (s - c)));
                    double r = areaT < 0.001 ? 0 : (a * b * c) / (4 * areaT);

                    double rot = -chord.Azimuth.Degrees + 90;
                    if (rot < -90) rot += 180;
                    if (rot > 90) rot -= 180;
                    
                    var midX = (p1.Easting + pEnd.Easting) / 2;
                    var midY = (p1.Northing + pEnd.Northing) / 2;
                    
                    string labelTxt = $"C: L={arcLength:F2} R={r:F2}\nChd: {FormatBearing(chord.Azimuth.Degrees)}  {chordLength:F2}";
                    figure.Labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = labelTxt, Easting = midX, Northing = midY, RotationDegrees = rot });
                    
                    context.Log($"Curve \tChd: {FormatBearing(chord.Azimuth.Degrees)} \tDist: {chordLength:F4}");
                    context.Log($"      \tRadius: {r:F4} \tLength: {arcLength:F4}");
                    context.Log($"End Point:   {figure.PointIds[endCurveIdx]}    \tN: {pEnd.Northing:F4}   \tE: {pEnd.Easting:F4}");

                    i = endCurveIdx;
                }
                else
                {
                    i++;
                }
            }
            else
            {
                var p2 = points[nextIdx];
                var inv = GeometryEngine.Inverse(p1, p2);
                
                var midX = (p1.Easting + p2.Easting) / 2;
                var midY = (p1.Northing + p2.Northing) / 2;
                
                double rot = -inv.Azimuth.Degrees + 90;
                if (rot < -90) rot += 180;
                if (rot > 90) rot -= 180;

                string labelTxt = $"{FormatBearing(inv.Azimuth.Degrees)}\n{inv.Distance:F2}";
                figure.Labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = labelTxt, Easting = midX, Northing = midY, RotationDegrees = rot });
                
                context.Log($"Line  \tBrg: {FormatBearing(inv.Azimuth.Degrees)} \tDist: {inv.Distance:F4}");
                context.Log($"End Point:   {nextId}    \tN: {p2.Northing:F4}   \tE: {p2.Easting:F4}");
                
                i++;
            }
        }

        // Perimeter
        double perimeter = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            perimeter += GeometryEngine.Inverse(points[i], points[i + 1]).Distance;
        }

        // Closure (Last to First)
        var last = points[points.Count - 1];
        var first = points[0];
        var closure = GeometryEngine.Inverse(last, first);
        
        bool isClosed = closure.Distance <= context.MapCheckClosureTolerance;
        figure.MapCheckFailed = !isClosed;
        
        if (!isClosed)
        {
            perimeter += closure.Distance; // Add the closing distance to perimeter
        }

        // Area (Shoelace)
        // A = 0.5 * | Sum(x_i * y_i+1 - x_i+1 * y_i) |
        double areaSum = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count]; // Wrap around
            areaSum += (p1.Easting * p2.Northing) - (p2.Easting * p1.Northing);
        }
        double area = Math.Abs(areaSum) * 0.5;
        double acres = area / 43560.0;

        // Precision
        // 1 : (Perimeter / ClosureDist)
        double precision = 0;
        if (closure.Distance > 1e-9)
        {
            precision = perimeter / closure.Distance;
        }

        context.Log($"----------------------------------------------------------------------");
        context.Log($"Perimeter: {perimeter:F3}");
        context.Log($"Area: {area:F2} sq.ft, {acres:F4} acres");
        context.Log($"Closure Error: {closure.Distance:F4}, Brg: {FormatBearing(closure.Azimuth.Degrees)}");
        
        if (closure.Distance > 1e-9)
            context.Log($"Precision: 1:{precision:F0}");
        else
            context.Log("Precision: Perfect Closure");

        // ── Persist results to Figure for session-to-session QC badge ─────────
        figure.QcStatus      = isClosed ? RCS.Cogo.App.State.FigureQcStatus.Passed
                                        : RCS.Cogo.App.State.FigureQcStatus.Failed;
        figure.ClosureError  = closure.Distance;
        figure.ClosureBearing= closure.Azimuth.Degrees;
        figure.AreaSqFt      = area;
        figure.Acres         = acres;
        figure.Perimeter     = perimeter;
        figure.PrecisionRatio= precision > 1e-9 ? precision : null;
        figure.LastQcRun     = DateTime.UtcNow;
            
        return Task.CompletedTask;
    }
}

