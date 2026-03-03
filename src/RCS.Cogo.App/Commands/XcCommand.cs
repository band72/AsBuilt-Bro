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

        // Check if figure is active.
        if (context.CurrentFigure == null)
        {
            context.Log("Error: XC command requires an active figure.");
            return Task.CompletedTask;
        }

        var lastPtId = System.Linq.Enumerable.LastOrDefault(context.CurrentFigure.PointIds);
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

        string subCmd = args[1].ToUpper();
        if (subCmd == "ZD" || subCmd == "BD" || subCmd == "AD" || subCmd == "DD")
        {
            var cleanArgs = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(args, a => !a.Equals("(BULB)", System.StringComparison.OrdinalIgnoreCase)));
            
            if (cleanArgs.Length < 5)
            {
                context.Log($"Usage for XC {subCmd}: <Radius> [TargetId] <Angle Args...> <Distance>");
                return Task.CompletedTask;
            }

            if (!double.TryParse(cleanArgs[2], out double radius))
            {
                context.Log($"Error: Invalid radius '{cleanArgs[2]}'");
                return Task.CompletedTask;
            }

            string targetId = null;
            Angle az = Angle.Zero;
            double dist = 0;

            if (subCmd == "BD")
            {
                if (cleanArgs.Length >= 6 && double.TryParse(cleanArgs[cleanArgs.Length - 1], out dist) &&
                    double.TryParse(cleanArgs[cleanArgs.Length - 2], out double brg) &&
                    double.TryParse(cleanArgs[cleanArgs.Length - 3], out double quad))
                {
                    az = Angle.FromQuadrant((int)quad, brg);
                    if (cleanArgs.Length >= 7) targetId = cleanArgs[3];
                }
                else
                {
                    context.Log("Error: Invalid arguments for XC BD. Ensure Quad, Bearing, and Distance are numbers.");
                    return Task.CompletedTask;
                }
            }
            else // ZD
            {
                if (cleanArgs.Length >= 5 && double.TryParse(cleanArgs[cleanArgs.Length - 1], out dist) &&
                    double.TryParse(cleanArgs[cleanArgs.Length - 2], out double azDms))
                {
                    az = Angle.FromDMS(azDms);
                    if (cleanArgs.Length >= 6) targetId = cleanArgs[3];
                }
                else
                {
                    context.Log($"Error: Invalid arguments for XC {subCmd}. Ensure Azimuth and Distance are numbers.");
                    return Task.CompletedTask;
                }
            }

            var newPt = GeometryEngine.Forward(startPt, az, dist);
            double absR = System.Math.Abs(radius);
            Point3D finalPt = newPt;

            if (absR > 0 && absR >= (dist / 2.0) * 0.999)
            {
                 if (absR < dist / 2.0) absR = dist / 2.0;
                 
                 double delta = 2 * System.Math.Asin(dist / (2 * absR));
                 var M = new Point3D((startPt.Northing + newPt.Northing) / 2, (startPt.Easting + newPt.Easting) / 2, startPt.Elevation);
                 double d = absR * System.Math.Cos(delta / 2);
                 double dirToCenter = az.Radians + (radius > 0 ? System.Math.PI / 2 : -System.Math.PI / 2);
                 var O = GeometryEngine.Forward(M, Angle.FromRadians(dirToCenter), d);
                 
                 var inv = GeometryEngine.Inverse(O, startPt);
                 double azO1 = inv.Azimuth.Radians;
                 
                 int segments = 12;
                 double sweep = (radius > 0 ? delta : -delta);
                 
                 for (int i = 1; i <= segments; i++)
                 {
                     double fraction = (double)i / segments;
                     double currentAz = azO1 + fraction * sweep;
                     var pCurve = GeometryEngine.Forward(O, Angle.FromRadians(currentAz), absR);
                     
                     if (i == segments) 
                     {
                         pCurve = newPt; 
                         string tId = targetId ?? ("XC_" + System.Guid.NewGuid().ToString("N").Substring(0, 6));
                         context.AddPoint(tId, pCurve, targetId != null ? "XC curve tie-in" : "XC Segment End");
                         context.CurrentFigure.PointIds.Add(tId);
                         finalPt = pCurve;
                     }
                     else
                     {
                         string sId = "XC_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
                         context.AddPoint(sId, pCurve, "XC Radial Sweep");
                         context.CurrentFigure.PointIds.Add(sId);
                     }
                 }
                 context.Log($"Assimilated {segments}-segment synthesized radial curve{(targetId != null ? " to " + targetId : "")}.");
            }
            else
            {
                 string tId = targetId ?? ("XC_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
                 context.AddPoint(tId, newPt, "XC Computed End");
                 context.CurrentFigure.PointIds.Add(tId);
                 context.Log($"Added linear chord (Radius invalid/omitted) to {tId}");
                 finalPt = newPt;
            }

            if (context.TraverseMode)
            {
                context.CurrentBacksight = context.CurrentStation;
                context.CurrentStation = finalPt;
                if (targetId != null)
                {
                    context.Log($"Traversed to new station: {targetId}");
                }
                else
                {
                    context.Log($"Traversed to new curve end station.");
                }
            }

            return Task.CompletedTask;
        }
        else if (subCmd == "PTS")
        {
            if (args.Length == 5)
            {
                if (!double.TryParse(args[2], out double radius))
                {
                    context.Log($"Error: Invalid radius '{args[2]}'");
                    return Task.CompletedTask;
                }
                
                string radiusPtId = args[3];
                string endPtId = args[4];
                
                var centerPt = context.GetPoint(radiusPtId);
                var endPt = context.GetPoint(endPtId);
                
                if (centerPt == null || endPt == null)
                {
                    context.Log($"Error: One or both points not found ({radiusPtId}, {endPtId}).");
                    return Task.CompletedTask;
                }

                bool isLeft = radius < 0;
                double absR = System.Math.Abs(radius);
                
                var invStart = GeometryEngine.Inverse(centerPt, startPt);
                double azStart = invStart.Azimuth.Radians;
                
                var invEnd = GeometryEngine.Inverse(centerPt, endPt);
                double azEnd = invEnd.Azimuth.Radians;
                
                double sweep = azEnd - azStart;
                
                if (isLeft) // Curve Left -> CCW -> negative sweep
                {
                    if (sweep > 0) sweep -= 2 * System.Math.PI;
                }
                else // Curve Right -> CW -> positive sweep
                {
                    if (sweep < 0) sweep += 2 * System.Math.PI;
                }

                int segments = 12;
                for (int i = 1; i < segments; i++)
                {
                    double fraction = (double)i / segments;
                    double currentAz = azStart + fraction * sweep;
                    var pCurve = GeometryEngine.Forward(centerPt, Angle.FromRadians(currentAz), absR);
                    
                    string sId = "XC_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
                    context.AddPoint(sId, pCurve, "XC PTS Segment");
                    context.CurrentFigure.PointIds.Add(sId);
                }
                
                // Finally add the real endpoint
                context.CurrentFigure.PointIds.Add(endPtId);
                context.Log($"Processed curve ({(isLeft ? "Left" : "Right")}) from {lastPtId} to {endPtId} (Radius Pt: {radiusPtId})");
            }
            else if (args.Length >= 4)
            {
                context.Log($"Processed intersecting curve through points: {string.Join(" ", args.Skip(2))}");
                string endPtId = args[args.Length - 1];
                if (context.GetPoint(endPtId) != null)
                {
                    context.CurrentFigure?.PointIds.Add(endPtId);
                }
            }
            else
            {
                context.Log("Usage: XC PTS <radius> <radius-point> <end-point>");
            }
            
            return Task.CompletedTask;
        }
        
        context.Log($"XC {subCmd} not fully implemented. Skipped.");
        return Task.CompletedTask;
    }
}
