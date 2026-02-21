using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class MapCheckCommand : ICommand
{
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
        
        // Add closing segment to perimeter for purpose of precision if we treat strictly?
        // Usually MapCheck assumes the last point IS the first point if closed.
        // If they are distinct but conceptually same, closure applies.
        
        bool isClosed = closure.Distance < 0.001; // Tolerance
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

        context.Log($"--- MapCheck Report: {figName} ---");
        context.Log($"Points: {points.Count}");
        context.Log($"Perimeter: {perimeter:F3}");
        context.Log($"Area: {area:F2} sq.ft, {acres:F4} acres");
        context.Log($"Closure Error: {closure.Distance:F4}, Az: {closure.Azimuth.ToDMS():F4}");
        
        if (closure.Distance > 1e-9)
            context.Log($"Precision: 1:{precision:F0}");
        else
            context.Log("Precision: Perfect Closure");
            
        return Task.CompletedTask;
    }
}
