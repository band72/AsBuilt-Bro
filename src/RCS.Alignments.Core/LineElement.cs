using System;
using RCS.Cogo.Core.Primitives;

namespace RCS.Alignments.Core;

public class LineElement : HorizontalElement
{
    public Point3D StartPoint { get; set; }
    public Point3D EndPoint { get; set; }
    
    public override double Length 
    {
        get
        {
            double dE = EndPoint.Easting - StartPoint.Easting;
            double dN = EndPoint.Northing - StartPoint.Northing;
            return Math.Sqrt(dE*dE + dN*dN);
        }
    }
    
    public double Azimuth 
    {
        get
        {
            double dE = EndPoint.Easting - StartPoint.Easting;
            double dN = EndPoint.Northing - StartPoint.Northing;
            double az = Math.Atan2(dE, dN) * 180.0 / Math.PI;
            if (az < 0) az += 360.0;
            return az;
        }
    }

    public override Point3D GetCoordinateAt(double station, double offset = 0.0)
    {
        if (station < StartStation || station > EndStation)
            throw new ArgumentOutOfRangeException(nameof(station), "Station is outside the bounds of this line element.");

        double distance = station - StartStation;
        double azRad = Azimuth * Math.PI / 180.0;
        
        // Base coordinate along the line
        double northing = StartPoint.Northing + distance * Math.Cos(azRad);
        double easting = StartPoint.Easting + distance * Math.Sin(azRad);

        // Apply Offset
        if (offset != 0.0)
        {
            // Right is positive offset, Left is negative
            double offsetAzRad = (Azimuth + 90.0) * Math.PI / 180.0;
            northing += offset * Math.Cos(offsetAzRad);
            easting += offset * Math.Sin(offsetAzRad);
        }

        return new Point3D(northing, easting, 0); // Elev handled by Profile
    }

    public override (double Station, double Offset)? GetStationOffset(Point3D point)
    {
        // Vector pointing to Line End
        double dxLine = EndPoint.Easting - StartPoint.Easting;
        double dyLine = EndPoint.Northing - StartPoint.Northing;
        double lineLengthSq = dxLine * dxLine + dyLine * dyLine;
        
        if (lineLengthSq == 0) return null;

        // Vector pointing to point
        double dxPoint = point.Easting - StartPoint.Easting;
        double dyPoint = point.Northing - StartPoint.Northing;

        // Project Point onto Line
        double dotProduct = dxPoint * dxLine + dyPoint * dyLine;
        double lineLen = Math.Sqrt(lineLengthSq);
        double distAlongLine = dotProduct / lineLen;

        if (distAlongLine < 0 || distAlongLine > lineLen)
            return null; // Not perpendicular to this specific segment

        double station = StartStation + distAlongLine;

        // Calculate offset (Cross product in 2D)
        double crossProduct = dxPoint * dyLine - dyPoint * dxLine;
        // if Line goes North, point on East (right) produces crossProduct < 0 or > 0 based on signs.
        // dxLine = 0, dyLine = 100
        // Point: dxPoint = 10 (East/Right), dyPoint = 0
        // Cross = 10*100 - 0*0 = 1000. So Positive cross product = offset Right.
        double offset = -crossProduct / lineLen; 

        return (station, offset);
    }
}
