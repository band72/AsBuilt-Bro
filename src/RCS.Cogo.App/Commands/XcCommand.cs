using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class XcCommand : ICommand
{
    public string Name => "XC";
    public string Description => "Extend/Intersection Curve Commands. Usage: XC <SubCmd> ...";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Usage: XC <SubCmd> ...");
            return Task.CompletedTask;
        }

        // XC ZD (BULB) [radius] [chord-az] [chord-dis]
        // Example logic: Just create the end point for now to allow script flow.
        // We will ignore the curve geometry and treat as straight line for Pnt Creation.
        // This is a placeholder to stop errors.
        
        // Check if figure is active.
        if (context.CurrentFigure == null)
        {
            context.Log("Error: XC command requires an active figure.");
            return Task.CompletedTask;
        }

        // We assume the last point in the figure is the start.
        var lastPtId = context.CurrentFigure.PointIds.LastOrDefault();
        if (lastPtId == null)
        {
             context.Log("Error: Figure has no start point.");
             return Task.CompletedTask;
        }
        
        var startPt = context.GetPoint(lastPtId);
        if (startPt == null)
        {
            context.Log($"Error: Point {lastPtId} not found.");
            return Task.CompletedTask;
        }

        // Parsing "XC ZD" etc.
        // args[0] = "XC"
        // args[1] = "ZD" (or BD, etc)
        
        // This is complex because result might be a new point or just adding to figure.
        // In most Cogo scripts, XC usually *adds* a segment to the figure input.
        // It might not create a new named point, but adds to the figure geometry.
        // But our Figure class is List<string> (Point IDs).
        // So we MUST create a point ID to add it to our Figure.
        // Does the user provide a point ID? 
        // XC ZD (BULB) [radius] [chord-az] [chord-dis]
        // No point ID in syntax.
        // This implies we generate a temporary point or the script implies we just store the geometry.
        // Since our Figure is Point-ID based, we have a mismatch.
        // Workaround: Generate a synthetic point ID, e.g., "FIG_Pt_X" and add it.
        
        string subCmd = args[1].ToUpper();
        if (subCmd == "ZD" || subCmd == "BD" || subCmd == "AD" || subCmd == "DD")
        {
            // Try to parse Radius, Az/Brg, Dist from subsequent args.
            // Simplified: We look for the last argument as distance and 2nd to last as Azimuth?
            // XC ZD (BULB) [radius] [chord-az] [chord-dis]
            // Arg count varying.
            // Let's just try to find Distance.
            // If we can't parse, we just log warning and add a dummy point to keep continuity?
            // User put "Unknown command: XC", so we just need it to NOT error out completely.
            
            // Let's try to parse the last two arguments as Azimuth and Distance.
            if (args.Length >= 4 
                && double.TryParse(args[args.Length - 1], out double dist) 
                && double.TryParse(args[args.Length - 2], out double azDms))
            {
                 var az = Angle.FromDMS(azDms);
                 var newPt = GeometryEngine.Forward(startPt, az, dist);
                 
                 // Extract Radius
                 double R = 0;
                 for (int i = 2; i < args.Length - 2; i++)
                 {
                     if (args[i] != "(BULB)" && double.TryParse(args[i], out double r))
                     {
                         R = r;
                         break;
                     }
                 }

                 double absR = System.Math.Abs(R);
                 // If radius is valid and mathematically capable of spanning the chord
                 if (absR > 0 && absR >= (dist / 2.0) * 0.999)
                 {
                     if (absR < dist / 2.0) absR = dist / 2.0; // Clamp floating point precision errors
                     
                     // Central arc delta
                     double delta = 2 * System.Math.Asin(dist / (2 * absR));
                     
                     // Midpoint of the chord
                     var M = new Point3D((startPt.Northing + newPt.Northing) / 2, (startPt.Easting + newPt.Easting) / 2, startPt.Elevation);
                     
                     // Distance from midpoint to circle center
                     double d = absR * System.Math.Cos(delta / 2);
                     
                     // Bearing to center (Bulge Right means center is to the right of the chord path)
                     double dirToCenter = az.Radians + (R > 0 ? System.Math.PI / 2 : -System.Math.PI / 2);
                     var O = GeometryEngine.Forward(M, Angle.FromRadians(dirToCenter), d);
                     
                     // Bearing from Center to Start
                     var inv = GeometryEngine.Inverse(O, startPt);
                     double azO1 = inv.Azimuth.Radians;
                     
                     int segments = 12; // Enough to visualize a smooth curve
                     double sweep = (R > 0 ? delta : -delta);
                     
                     for (int i = 1; i <= segments; i++)
                     {
                         double fraction = (double)i / segments;
                         double currentAz = azO1 + fraction * sweep;
                         var pCurve = GeometryEngine.Forward(O, Angle.FromRadians(currentAz), absR);
                         
                         // Fix the final tie-in point perfectly
                         if (i == segments) pCurve = newPt;

                         string sId = "XC_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
                         context.AddPoint(sId, pCurve, "XC Segment");
                         context.CurrentFigure.PointIds.Add(sId);
                     }
                     context.Log($"Assimilated {segments}-segment synthesized radial curve.");
                     return Task.CompletedTask;
                 }
                 else
                 {
                     // Use a unique synthetic ID so standard PNT commands don't overwrite the curve chords
                     string synthId = "XC_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
                     
                     context.AddPoint(synthId, newPt, "XC Computed");
                     context.CurrentFigure.PointIds.Add(synthId);
                     context.Log($"Added linear chord (Radius invalid/omitted) to {synthId}");
                     return Task.CompletedTask;
                 }
            }
        }
        
        context.Log($"XC {subCmd} not fully implemented. Skipped.");
        return Task.CompletedTask;
    }
}
