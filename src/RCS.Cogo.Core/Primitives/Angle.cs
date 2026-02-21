using System;

namespace RCS.Cogo.Core.Primitives;

/// <summary>
/// Represents an angle, providing conversions between Radians, Decimal Degrees, and DMS (DDD.MMSS).
/// </summary>
public readonly struct Angle : IEquatable<Angle>, IComparable<Angle>
{
    private const double Tolerance = 1e-9;
    
    /// <summary>
    /// The angle value in Radians.
    /// </summary>
    public double Radians { get; }

    /// <summary>
    /// The angle value in Decimal Degrees.
    /// </summary>
    public double Degrees => Radians * (180.0 / Math.PI);

    private Angle(double radians)
    {
        Radians = radians;
    }

    public static Angle FromRadians(double radians) => new(radians);
    
    public static Angle FromDegrees(double degrees) => new(degrees * (Math.PI / 180.0));

    /// <summary>
    /// Creates an angle from a DMS value in the format DDD.MMSS
    /// Example: 45.3030 => 45° 30' 30"
    /// </summary>
    public static Angle FromDMS(double dms)
    {
        int degrees = (int)dms;
        double fractional = Math.Abs(dms - degrees);
        
        // Extract minutes (first two decimal places)
        // 0.3030 * 100 = 30.30 => int 30
        int minutes = (int)(fractional * 100 + 1e-9); 
        
        // Extract seconds (remainder)
        // (30.30 - 30) = 0.30 * 100 = 30
        double seconds = (fractional * 100 - minutes) * 100;

        double decimalDegrees = Math.Abs(degrees) + (minutes / 60.0) + (seconds / 3600.0);
        
        if (dms < 0) decimalDegrees = -decimalDegrees;
        
        return FromDegrees(decimalDegrees);
    }
    
    /// <summary>
    /// Creates an Azimuth Angle from a Quadrant and Bearing (DMS).
    /// Quadrants: 1=NE, 2=SE, 3=SW, 4=NW
    /// </summary>
    public static Angle FromQuadrant(int quadrant, double bearingDms)
    {
        var bearing = FromDMS(bearingDms);
        double decDeg = bearing.Degrees;
        
        return quadrant switch
        {
            1 => FromDegrees(decDeg),            // NE: Az = Brg
            2 => FromDegrees(180.0 - decDeg),    // SE: Az = 180 - Brg
            3 => FromDegrees(180.0 + decDeg),    // SW: Az = 180 + Brg
            4 => FromDegrees(360.0 - decDeg),    // NW: Az = 360 - Brg
            _ => throw new ArgumentException("Quadrant must be 1-4")
        };
    }
    
    /// <summary>
    /// Returns the angle in DMS format (DDD.MMSS).
    /// </summary>
    public double ToDMS()
    {
        double totalDegrees = Degrees;
        int d = (int)Math.Abs(totalDegrees);
        double remainder = Math.Abs(totalDegrees) - d;
        
        double totalMinutes = remainder * 60.0;
        int m = (int)totalMinutes;
        
        double s = (totalMinutes - m) * 60.0;
        
        // Round seconds to avoid precision issues impacting the DMS representation
        // For standard survey output 4 decimals is enough (DDD.MMSS)
        // But internal representation might need more.
        
        double dms = d + (m / 100.0) + (s / 10000.0);
        
        return totalDegrees < 0 ? -dms : dms;
    }

    // Common survey directions
    public static Angle Zero => new(0);
    public static Angle HalfPi => new(Math.PI / 2); // 90 deg
    public static Angle Pi => new(Math.PI);         // 180 deg
    public static Angle TwoPi => new(2 * Math.PI);  // 360 deg

    // Operators
    public static Angle operator +(Angle a, Angle b) => new(a.Radians + b.Radians);
    public static Angle operator -(Angle a, Angle b) => new(a.Radians - b.Radians);
    public static Angle operator *(Angle a, double scalar) => new(a.Radians * scalar);
    public static Angle operator /(Angle a, double scalar) => new(a.Radians / scalar);
    public static bool operator ==(Angle a, Angle b) => Math.Abs(a.Radians - b.Radians) < Tolerance;
    public static bool operator !=(Angle a, Angle b) => !(a == b);
    
    public bool Equals(Angle other) => this == other;
    public override bool Equals(object? obj) => obj is Angle other && Equals(other);
    public override int GetHashCode() => Radians.GetHashCode();
    public int CompareTo(Angle other) => Radians.CompareTo(other.Radians);

    public override string ToString() => $"{ToDMS():F4}";
}
