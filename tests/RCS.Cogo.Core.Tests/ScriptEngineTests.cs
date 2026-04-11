using System;
using System.Linq;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;
using Xunit;

namespace RCS.Cogo.Core.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GeometryEngine — Known-Answer Tests
// ─────────────────────────────────────────────────────────────────────────────

public class GeometryEngineInverseTests
{
    private const double Tol = 0.0001;

    /// <summary>
    /// Pythagoras: points at (N=0,E=0) and (N=3,E=4) → distance=5.
    /// Azimuth from p1→p2 = arctan(4/3) ≈ 53.13° (NE quadrant).
    /// </summary>
    [Fact]
    public void Inverse_PythagoreanTriple_CorrectDistanceAndAzimuth()
    {
        var p1 = new Point3D(0, 0, 0);   // Northing=0, Easting=0
        var p2 = new Point3D(3, 4, 0);   // Northing=3, Easting=4
        var (dist, az) = GeometryEngine.Inverse(p1, p2);

        Assert.InRange(dist, 5.0 - Tol, 5.0 + Tol);
        // arctan(ΔE/ΔN) = arctan(4/3) ≈ 53.13°
        Assert.InRange(az.Degrees, 53.13 - 0.01, 53.13 + 0.01);
    }

    [Fact]
    public void Inverse_DueNorth_AzimuthIsZero()
    {
        var p1 = new Point3D(0,   0, 0);
        var p2 = new Point3D(100, 0, 0);
        var (dist, az) = GeometryEngine.Inverse(p1, p2);

        Assert.InRange(dist, 100 - Tol, 100 + Tol);
        Assert.InRange(az.Degrees, 0 - Tol, 0 + Tol);
    }

    [Fact]
    public void Inverse_DueEast_AzimuthIs90()
    {
        var p1 = new Point3D(0, 0,   0);
        var p2 = new Point3D(0, 100, 0);
        var (dist, az) = GeometryEngine.Inverse(p1, p2);

        Assert.InRange(dist, 100 - Tol, 100 + Tol);
        Assert.InRange(az.Degrees, 90 - Tol, 90 + Tol);
    }
}

public class GeometryEngineForwardTests
{
    private const double Tol = 0.0001;

    /// <summary>
    /// Forward 100 ft due north from (10000, 10000) must arrive at N=10100, E=10000.
    /// </summary>
    [Fact]
    public void Forward_DueNorth_MovesNorthing()
    {
        var origin = new Point3D(10000, 10000, 0);
        var dest   = GeometryEngine.Forward(origin, Angle.FromDegrees(0), 100);

        Assert.InRange(dest.Northing, 10100 - Tol, 10100 + Tol);
        Assert.InRange(dest.Easting,  10000 - Tol, 10000 + Tol);
    }

    [Fact]
    public void Forward_DueEast_MovesEasting()
    {
        var origin = new Point3D(10000, 10000, 0);
        var dest   = GeometryEngine.Forward(origin, Angle.FromDegrees(90), 100);

        Assert.InRange(dest.Northing, 10000 - Tol, 10000 + Tol);
        Assert.InRange(dest.Easting,  10100 - Tol, 10100 + Tol);
    }

    /// <summary>
    /// Round-trip: Forward + Inverse must recover the same azimuth and distance.
    /// </summary>
    [Fact]
    public void ForwardInverseRoundTrip()
    {
        var origin  = new Point3D(10000, 10000, 0);
        double bearing = 127.4567;
        double dist    = 456.78;

        var dest       = GeometryEngine.Forward(origin, Angle.FromDegrees(bearing), dist);
        var (d2, az2)  = GeometryEngine.Inverse(origin, dest);

        Assert.InRange(d2,          dist    - 0.0001, dist    + 0.0001);
        Assert.InRange(az2.Degrees, bearing - 0.001,  bearing + 0.001);
    }

    /// <summary>
    /// Four-leg traverse forming a square must close back to the start.
    /// </summary>
    [Fact]
    public void SquareTraverse_Closes()
    {
        var start = new Point3D(10000, 10000, 0);
        var p1 = GeometryEngine.Forward(start, Angle.FromDegrees(0),   100); // N
        var p2 = GeometryEngine.Forward(p1,    Angle.FromDegrees(90),  100); // E
        var p3 = GeometryEngine.Forward(p2,    Angle.FromDegrees(180), 100); // S
        var p4 = GeometryEngine.Forward(p3,    Angle.FromDegrees(270), 100); // W

        var (closure, _) = GeometryEngine.Inverse(p4, start);
        Assert.InRange(closure, 0, 0.001);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Shoelace area
// ─────────────────────────────────────────────────────────────────────────────
public class ShoelaceAreaTests
{
    private static double ComputeArea(Point3D[] pts)
    {
        double sum = 0;
        for (int i = 0; i < pts.Length; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Length];
            sum += a.Easting * b.Northing - b.Easting * a.Northing;
        }
        return Math.Abs(sum) * 0.5;
    }

    [Fact]
    public void Square100x100_Area10000()
    {
        // Northing=Y, Easting=X
        var pts = new[]
        {
            new Point3D(0,   0,   0),
            new Point3D(0,   100, 0),
            new Point3D(100, 100, 0),
            new Point3D(100, 0,   0),
        };
        Assert.InRange(ComputeArea(pts), 9999.99, 10000.01);
    }

    [Fact]
    public void RightTriangle3x4x5_Area6()
    {
        var pts = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(0, 3, 0),
            new Point3D(4, 0, 0),
        };
        Assert.InRange(ComputeArea(pts), 5.9999, 6.0001);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Segment-segment intersection
// ─────────────────────────────────────────────────────────────────────────────
public class IntersectionTests
{
    [Fact]
    public void CrossingDiagonals_FindsCenter()
    {
        // Diagonals of a 10N×10E square — should meet at center (N=5, E=5)
        var p1 = new Point3D(0,  0,  0);
        var p2 = new Point3D(10, 10, 0);
        var p3 = new Point3D(0,  10, 0);
        var p4 = new Point3D(10, 0,  0);

        var hit = GeometryEngine.IntersectionSegmentSegment(p1, p2, p3, p4);
        Assert.NotNull(hit);
        Assert.InRange(hit!.Northing, 4.999, 5.001);
        Assert.InRange(hit!.Easting,  4.999, 5.001);
    }

    [Fact]
    public void ParallelLines_ReturnsNull()
    {
        var p1 = new Point3D(0,  0, 0); var p2 = new Point3D(0,  10, 0);
        var p3 = new Point3D(5,  0, 0); var p4 = new Point3D(5,  10, 0);
        Assert.Null(GeometryEngine.IntersectionSegmentSegment(p1, p2, p3, p4));
    }

    [Fact]
    public void NonOverlappingCollinear_ReturnsNull()
    {
        var p1 = new Point3D(0,  0, 0); var p2 = new Point3D(4,  0, 0);
        var p3 = new Point3D(6,  0, 0); var p4 = new Point3D(10, 0, 0);
        Assert.Null(GeometryEngine.IntersectionSegmentSegment(p1, p2, p3, p4));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Angle struct
// ─────────────────────────────────────────────────────────────────────────────
public class AngleTests
{
    [Theory]
    [InlineData(0,   0,  0,   0.0)]
    [InlineData(90,  0,  0,  90.0)]
    [InlineData(45, 30,  0,  45.5)]
    [InlineData(36, 55, 54,  36.93166667)]
    public void DmsToDecimalDegrees(int deg, int min, int sec, double expected)
    {
        double actual = deg + min / 60.0 + sec / 3600.0;
        Assert.InRange(actual, expected - 0.000001, expected + 0.000001);
    }

    [Fact]
    public void FromDMS_RoundTrip()
    {
        // 36°55'54" written as DMS notation 36.5554
        var a = Angle.FromDMS(36.5554);
        Assert.InRange(a.Degrees, 36.93 - 0.01, 36.93 + 0.01);
    }

    [Fact]
    public void Quadrant1_NE_AzimuthEqualsBaseAngle()
    {
        var a = Angle.FromQuadrant(1, 45.00); // N45°E → az = 45°
        Assert.InRange(a.Degrees, 44.999, 45.001);
    }

    [Fact]
    public void Quadrant2_SE_AzimuthIs180MinusBearing()
    {
        var a = Angle.FromQuadrant(2, 45.00); // S45°E → az = 135°
        Assert.InRange(a.Degrees, 134.999, 135.001);
    }

    [Fact]
    public void Quadrant3_SW_AzimuthIs180PlusBearing()
    {
        var a = Angle.FromQuadrant(3, 45.00); // S45°W → az = 225°
        Assert.InRange(a.Degrees, 224.999, 225.001);
    }

    [Fact]
    public void Quadrant4_NW_AzimuthIs360MinusBearing()
    {
        var a = Angle.FromQuadrant(4, 45.00); // N45°W → az = 315°
        Assert.InRange(a.Degrees, 314.999, 315.001);
    }
}
