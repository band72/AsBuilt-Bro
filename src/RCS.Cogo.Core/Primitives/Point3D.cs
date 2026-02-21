namespace RCS.Cogo.Core.Primitives;

/// <summary>
/// Represents an immutable 3D point in a Cartesian coordinate system.
/// </summary>
/// <param name="Northing">The Y-coordinate (North).</param>
/// <param name="Easting">The X-coordinate (East).</param>
/// <param name="Elevation">The Z-coordinate (Height).</param>
public record Point3D(double Northing, double Easting, double Elevation = 0)
{
    /// <summary>
    /// Gets the origin point (0,0,0).
    /// </summary>
    public static Point3D Origin => new(0, 0, 0);

    /// <summary>
    /// Returns a string representation of the point.
    /// </summary>
    public override string ToString() => $"N:{Northing:F4}, E:{Easting:F4}, Z:{Elevation:F4}";
    
    /// <summary>
    /// Checks if the point is essentially 2D (Elevation is 0 or ignored).
    /// </summary>
    public bool Is2D => Elevation == 0;
}
