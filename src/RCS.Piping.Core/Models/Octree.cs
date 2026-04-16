using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Piping.Core.Models;

/// <summary>
/// A lightweight 3D Octree implementation for rapid spatial indexing 
/// and K-Nearest-Neighbor (KNN) operations across massive LiDAR point clouds.
/// </summary>
public class OctreeNode
{
    // Bounding Box
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
    public double MinZ { get; set; }
    public double MaxZ { get; set; }

    public int MaxCapacity { get; set; } = 64;
    public List<TopographicPoint> Points { get; set; } = new();

    public OctreeNode[]? Children { get; set; }

    public OctreeNode(double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
    {
        MinX = minX; MaxX = maxX;
        MinY = minY; MaxY = maxY;
        MinZ = minZ; MaxZ = maxZ;
    }

    public void Insert(TopographicPoint p)
    {
        if (!Contains(p)) return;

        if (Children == null && Points.Count < MaxCapacity)
        {
            Points.Add(p);
            return;
        }

        if (Children == null)
            Subdivide();

        foreach (var child in Children!)
            child.Insert(p);
    }

    private void Subdivide()
    {
        double midX = (MinX + MaxX) / 2;
        double midY = (MinY + MaxY) / 2;
        double midZ = (MinZ + MaxZ) / 2;

        Children = new OctreeNode[8];
        Children[0] = new OctreeNode(MinX, midX, MinY, midY, MinZ, midZ);
        Children[1] = new OctreeNode(midX, MaxX, MinY, midY, MinZ, midZ);
        Children[2] = new OctreeNode(MinX, midX, midY, MaxY, MinZ, midZ);
        Children[3] = new OctreeNode(midX, MaxX, midY, MaxY, MinZ, midZ);
        Children[4] = new OctreeNode(MinX, midX, MinY, midY, midZ, MaxZ);
        Children[5] = new OctreeNode(midX, MaxX, MinY, midY, midZ, MaxZ);
        Children[6] = new OctreeNode(MinX, midX, midY, MaxY, midZ, MaxZ);
        Children[7] = new OctreeNode(midX, MaxX, midY, MaxY, midZ, MaxZ);

        foreach (var pt in Points)
            foreach (var child in Children)
                child.Insert(pt);

        Points.Clear();
    }

    public bool Contains(TopographicPoint p) =>
        p.Easting >= MinX && p.Easting <= MaxX &&
        p.Northing >= MinY && p.Northing <= MaxY &&
        p.Elevation >= MinZ && p.Elevation <= MaxZ;

    public void GetNearestNeighbors(double x, double y, double z, int count, PriorityQueue<TopographicPoint, double> pq)
    {
        // Compute distance to bounding box
        double dx = Math.Max(0, Math.Max(MinX - x, x - MaxX));
        double dy = Math.Max(0, Math.Max(MinY - y, y - MaxY));
        double dz = Math.Max(0, Math.Max(MinZ - z, z - MaxZ));
        double distSqToBox = dx * dx + dy * dy + dz * dz;

        // If worst point in PQ is closer than the box itself, ignore this node.
        if (pq.Count == count && pq.TryPeek(out _, out double worstDistSq) && distSqToBox > worstDistSq)
            return;

        if (Children == null)
        {
            foreach (var pt in Points)
            {
                double pdx = pt.Easting - x;
                double pdy = pt.Northing - y;
                double pdz = pt.Elevation - z;
                double pDistSq = pdx * pdx + pdy * pdy + pdz * pdz;

                if (pq.Count < count)
                {
                    pq.Enqueue(pt, -pDistSq); // Invert distance so top is worst (max-heap behavior for removal)
                }
                else if (pq.TryPeek(out _, out double worst) && -pDistSq > worst)
                {
                    pq.Dequeue();
                    pq.Enqueue(pt, -pDistSq);
                }
            }
        }
        else
        {
            foreach (var child in Children.OrderBy(c => c.DistanceSqToCenter(x, y, z)))
            {
                child.GetNearestNeighbors(x, y, z, count, pq);
            }
        }
    }

    private double DistanceSqToCenter(double x, double y, double z)
    {
        double cx = (MinX + MaxX) / 2;
        double cy = (MinY + MaxY) / 2;
        double cz = (MinZ + MaxZ) / 2;
        double dx = cx - x;
        double dy = cy - y;
        double dz = cz - z;
        return dx * dx + dy * dy + dz * dz;
    }
}
