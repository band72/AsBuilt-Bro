using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Piping.Core.Models;

public class TopographicPoint
{
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double Elevation { get; set; }
}

public class TopographicSurface
{
    public List<TopographicPoint> Points { get; set; } = new();
    private OctreeNode? _octreeRoot;

    public void BuildSpatialIndex()
    {
        if (Points.Count == 0) return;
        
        double minX = Points.Min(p => p.Easting) - 1;
        double maxX = Points.Max(p => p.Easting) + 1;
        double minY = Points.Min(p => p.Northing) - 1;
        double maxY = Points.Max(p => p.Northing) + 1;
        double minZ = Points.Min(p => p.Elevation) - 1;
        double maxZ = Points.Max(p => p.Elevation) + 1;

        _octreeRoot = new OctreeNode(minX, maxX, minY, maxY, minZ, maxZ);
        foreach (var p in Points) _octreeRoot.Insert(p);
    }

    /// <summary>
    /// Implements an Inverse Distance Weighting (IDW) interpolation
    /// to determine surface elevation at any arbitrary X, Y coordinate.
    /// </summary>
    public double InterpolateElevation(double easting, double northing)
    {
        if (Points.Count == 0) return 0;
        
        // Find exact match
        var exact = Points.FirstOrDefault(p => Math.Abs(p.Easting - easting) < 0.01 && Math.Abs(p.Northing - northing) < 0.01);
        if (exact != null) return exact.Elevation;

        double numerator = 0;
        double denominator = 0;
        double power = 2; // IDW power parameter

        if (_octreeRoot == null) BuildSpatialIndex();
        
        var pq = new PriorityQueue<TopographicPoint, double>();
        _octreeRoot?.GetNearestNeighbors(easting, northing, 0 /* 2D approx */, 6, pq);
        
        var neighbors = new List<(TopographicPoint Point, double DistSq)>();
        while (pq.Count > 0)
        {
            // The priority is -distSq
            if (pq.TryDequeue(out var pt, out double negDistSq) && pt != null)
            {
                neighbors.Add((pt, -negDistSq));
            }
        }

        foreach (var n in neighbors)
        {
            double dist = Math.Sqrt(n.DistSq);
            double w = 1.0 / Math.Pow(dist, power);
            numerator += w * n.Point.Elevation;
            denominator += w;
        }

        return denominator == 0 ? 0 : (numerator / denominator);
    }
}
