using System;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.Core.Maths;

/// <summary>
/// Provides core coordinate geometry calculations.
/// </summary>
public static class GeometryEngine
{
    private const double TwoPi = 2 * Math.PI;

    /// <summary>
    /// Calculates the geodetic inverse (Distance and Azimuth) between two points.
    /// </summary>
    public static (double Distance, Angle Azimuth) Inverse(Point3D p1, Point3D p2)
    {
        double dN = p2.Northing - p1.Northing;
        double dE = p2.Easting - p1.Easting;
        double dist = Math.Sqrt(dN * dN + dE * dE);
        double az = Math.Atan2(dE, dN);
        
        if (az < 0) az += TwoPi;
        
        return (dist, Angle.FromRadians(az));
    }

    /// <summary>
    /// Calculates a new point given a starting point, azimuth, and distance.
    /// </summary>
    public static Point3D Forward(Point3D p, Angle az, double dist)
    {
        return new Point3D(
            p.Northing + dist * Math.Cos(az.Radians),
            p.Easting + dist * Math.Sin(az.Radians),
            p.Elevation
        );
    }
    
    /// <summary>
    /// Calculates the intersection point of two lines defined by points and bearings (Azimuths).
    /// </summary>
    /// <returns>The intersection point, or null if lines are parallel.</returns>
    public static Point3D? IntersectionBearingBearing(Point3D p1, Angle az1, Point3D p2, Angle az2)
    {
        double N1 = p1.Northing; 
        double E1 = p1.Easting;
        double N2 = p2.Northing; 
        double E2 = p2.Easting;

        double theta1 = az1.Radians;
        double theta2 = az2.Radians;

        // Check for parallel lines
        if (Math.Abs(Math.Sin(theta1 - theta2)) < 1e-9)
            return null;

        double dN = N2 - N1;
        double dE = E2 - E1;

        // Solution using Sine Rule or simultaneous equations
        // E = E1 + d1 * sin(theta1) = E2 + d2 * sin(theta2)
        // N = N1 + d1 * cos(theta1) = N2 + d2 * cos(theta2)
        
        // E2 - E1 = d1*sin(theta1) - d2*sin(theta2)
        // N2 - N1 = d1*cos(theta1) - d2*cos(theta2)

        // Solving for d1:
        // d1 = ( (N2-N1)*sin(theta2) - (E2-E1)*cos(theta2) ) / sin(theta2-theta1)
        // Note: sin(theta2-theta1) = sin(theta2)cos(theta1) - cos(theta2)sin(theta1)
        
        double denominator = Math.Sin(theta2 - theta1);
        
        // Wait, standard forumla:
        // x1, y1, theta1
        // x2, y2, theta2
        // d1 = ( (x2-x1) cos(theta2) - (y2-y1) sin(theta2) ) / sin(theta1 - theta2)
        // Let's derive or use standard Survey formula.
        
        // E = x, N = y
        // E = E1 + k1 sin1
        // N = N1 + k1 cos1
        
        // E = E2 + k2 sin2
        // N = N2 + k2 cos2
        
        // k1 sin1 - k2 sin2 = E2 - E1 = dE
        // k1 cos1 - k2 cos2 = N2 - N1 = dN
        
        // Multiply 1 by cos2, 2 by sin2
        // k1 sin1 cos2 - k2 sin2 cos2 = dE cos2
        // k1 cos1 sin2 - k2 cos2 sin2 = dN sin2
        
        // Subtract:
        // k1 (sin1 cos2 - cos1 sin2) = dE cos2 - dN sin2
        // k1 sin(theta1 - theta2) = dE cos2 - dN sin2
        
        double k1 = (dE * Math.Cos(theta2) - dN * Math.Sin(theta2)) / Math.Sin(theta1 - theta2);
        
        return Forward(p1, az1, k1);
    }

    /// <summary>
    /// Calculates the intersection of two lines defined by start/end points and offsets.
    /// Offset > 0 is Right, < 0 is Left.
    /// </summary>
    /// <summary>
    /// Calculates the intersection of two lines defined by start/end points and offsets.
    /// Offset > 0 is Right, < 0 is Left.
    /// </summary>
    public static Point3D? IntersectionLineLine(Point3D l1Start, Point3D l1End, double off1, 
                                              Point3D l2Start, Point3D l2End, double off2)
    {
        // 1. Calculate Azimuths of base lines
        var res1 = Inverse(l1Start, l1End);
        var res2 = Inverse(l2Start, l2End);
        
        // 2. Adjust Start Points by Offset (Perpendicular)
        // Perpendicular to Right is Az + 90
        var p1Shifted = Forward(l1Start, res1.Azimuth, off1); // Wait off1 is perp? Yes.
        // Wait, shifting Forward by Azimuth + 90 is correct for Offset Right.
         var p1ShiftedPerp = Forward(l1Start, res1.Azimuth + Angle.HalfPi, off1);
         var p2ShiftedPerp = Forward(l2Start, res2.Azimuth + Angle.HalfPi, off2);

        // 3. Intersect using base azimuths (parallel lines have same azimuth)
        return IntersectionBearingBearing(p1ShiftedPerp, res1.Azimuth, p2ShiftedPerp, res2.Azimuth);
    }

    /// <summary>
    /// Calculates the intersection(s) of two circles (Distance-Distance).
    /// Returns two potential points (Left and Right relative to P1->P2 vector).
    /// </summary>
    public static (Point3D? Left, Point3D? Right) IntersectionDistanceDistance(Point3D p1, double r1, Point3D p2, double r2)
    {
        var (dist12, az12) = Inverse(p1, p2);

        // Check for no solution
        if (dist12 > r1 + r2 || dist12 < Math.Abs(r1 - r2) || dist12 == 0)
        {
            return (null, null); // Separated, contained, or concentric
        }

        // Law of Cosines
        // r2^2 = r1^2 + dist12^2 - 2*r1*dist12*cos(alpha)
        double cosAlpha = (r1 * r1 + dist12 * dist12 - r2 * r2) / (2 * r1 * dist12);
        
        // Clamp for floating point errors
        if (cosAlpha > 1.0) cosAlpha = 1.0;
        if (cosAlpha < -1.0) cosAlpha = -1.0;

        double alpha = Math.Acos(cosAlpha); // Radians

        // Left Solution (Counter-Clockwise from Vector P1->P2)
        // Az12 - alpha
        var azLeft = az12.Radians - alpha;
        
        // Right Solution (Clockwise)
        // Az12 + alpha
        var azRight = az12.Radians + alpha;

        return (Forward(p1, Angle.FromRadians(azLeft), r1), Forward(p1, Angle.FromRadians(azRight), r1));
    }

    /// <summary>
    /// Calculates the intersection of two line segments, if they intersect within their bounds.
    /// </summary>
    public static Point3D? IntersectionSegmentSegment(Point3D p1, Point3D p2, Point3D p3, Point3D p4)
    {
        double E1 = p1.Easting, N1 = p1.Northing;
        double E2 = p2.Easting, N2 = p2.Northing;
        double E3 = p3.Easting, N3 = p3.Northing;
        double E4 = p4.Easting, N4 = p4.Northing;

        double denom = (N4 - N3) * (E2 - E1) - (E4 - E3) * (N2 - N1);
        if (Math.Abs(denom) < 1e-9) return null; // Parallel or collinear

        double uA = ((E4 - E3) * (N1 - N3) - (N4 - N3) * (E1 - E3)) / denom;
        double uB = ((E2 - E1) * (N1 - N3) - (N2 - N1) * (E1 - E3)) / denom;

        if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
        {
            double intE = E1 + (uA * (E2 - E1));
            double intN = N1 + (uA * (N2 - N1));
            double intElev = p1.Elevation + (uA * (p2.Elevation - p1.Elevation)); 
            return new Point3D(intN, intE, intElev);
        }

        return null;
    }
}
