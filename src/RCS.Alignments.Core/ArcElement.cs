using System;
using RCS.Cogo.Core.Primitives;

namespace RCS.Alignments.Core;

public class ArcElement : HorizontalElement
{
    public Point3D CenterPoint { get; set; }
    public double Radius { get; set; }
    public double StartAzimuth { get; set; }
    public double EndAzimuth { get; set; }
    public bool IsClockwise { get; set; }

    public override double Length
    {
        get
        {
            double sweep = IsClockwise ? EndAzimuth - StartAzimuth : StartAzimuth - EndAzimuth;
            if (sweep < 0) sweep += 360.0;
            return (sweep * Math.PI / 180.0) * Radius;
        }
    }

    public override Point3D GetCoordinateAt(double station, double offset = 0.0)
    {
        if (station < StartStation || station > EndStation)
            throw new ArgumentOutOfRangeException(nameof(station), "Station is outside the bounds of this arc element.");

        double distance = station - StartStation;
        
        // Convert distance to angle
        double angleRad = distance / Radius;
        double angleDeg = angleRad * 180.0 / Math.PI;

        double currentAzimuth = IsClockwise 
            ? StartAzimuth + angleDeg 
            : StartAzimuth - angleDeg;

        // Azimuth from Center to the point on the arc
        double radialAzimuth = IsClockwise ? currentAzimuth - 90 : currentAzimuth + 90;
        
        double radialAzRad = radialAzimuth * Math.PI / 180.0;
        
        double effRadius = IsClockwise ? Radius + offset : Radius - offset;

        double northing = CenterPoint.Northing + effRadius * Math.Cos(radialAzRad);
        double easting = CenterPoint.Easting + effRadius * Math.Sin(radialAzRad);

        return new Point3D(northing, easting, 0);
    }

    public override (double Station, double Offset)? GetStationOffset(Point3D point)
    {
        double dx = point.Easting - CenterPoint.Easting;
        double dy = point.Northing - CenterPoint.Northing;
        double distFromCenter = Math.Sqrt(dx * dx + dy * dy);

        double azToPoint = Math.Atan2(dx, dy) * 180.0 / Math.PI;
        if (azToPoint < 0) azToPoint += 360.0;

        // Check if azimuth is within arc bounds
        double startRadialAz = IsClockwise ? StartAzimuth - 90 : StartAzimuth + 90;
        if (startRadialAz < 0) startRadialAz += 360.0;
        if (startRadialAz > 360) startRadialAz -= 360.0;

        double endRadialAz = IsClockwise ? EndAzimuth - 90 : EndAzimuth + 90;
        if (endRadialAz < 0) endRadialAz += 360.0;
        if (endRadialAz > 360) endRadialAz -= 360.0;

        // Simplify bound checking logic (assuming small arcs)
        // More robust checking needed for production
        
        double angleDiff = IsClockwise 
            ? azToPoint - startRadialAz 
            : startRadialAz - azToPoint;

        if (angleDiff < 0) angleDiff += 360.0;

        double arcAngle = IsClockwise ? endRadialAz - startRadialAz : startRadialAz - endRadialAz;
        if (arcAngle < 0) arcAngle += 360.0;

        if (angleDiff > arcAngle) return null; // Outside segment

        double distAlongArc = (angleDiff * Math.PI / 180.0) * Radius;
        double station = StartStation + distAlongArc;
        
        double offset = IsClockwise 
            ? distFromCenter - Radius 
            : Radius - distFromCenter;

        return (station, offset);
    }
}
