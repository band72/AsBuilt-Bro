using System;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Alignments.Core;

namespace RCS.Cogo.App.Commands;

public class AlignmentCommand : ICommand
{
    public string Name => "ALGN";
    public string Description => "Handles Horizontal Alignment definitions (BEG, TANGENT, CURVE, END)";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        Execute(args, context);
        return Task.CompletedTask;
    }

    public void Execute(string[] args, ICogoContext context)
    {
        if (args.Length < 2) throw new ArgumentException("ALGN requires a subcommand (BEG, TANGENT, CURVE, END).");

        string sub = args[1].ToUpper();
        switch (sub)
        {
            case "BEG":
                if (args.Length < 3) throw new ArgumentException("ALGN BEG requires a name.");
                
                int nameIdx = 2;
                if (args[nameIdx].Equals("FIG", StringComparison.OrdinalIgnoreCase))
                {
                    nameIdx++;
                    if (args.Length <= nameIdx) throw new ArgumentException("ALGN BEG requires a name.");
                }
                
                string name = args[nameIdx];
                double startStation = 0.0;
                
                int stIdx = nameIdx + 1;
                if (args.Length > stIdx)
                {
                    // Parse station like 10+00.00 -> 1000.0
                    string raw = args[stIdx].Replace("+", "");
                    if (double.TryParse(raw, out double st)) startStation = st;
                }
                
                context.CurrentAlignment = new Alignment { Name = name, StartStation = startStation };
                context.Log($"[INFO] Began Alignment '{name}' at station {startStation:F2}");

                // IMPLICIT FIGURE TO SYNC ALIGNMENT
                var existingFig = context.GetFigure(name);
                if (existingFig == null)
                {
                    var fig = new RCS.Cogo.App.State.Figure(name);
                    context.AddFigure(fig);
                    context.CurrentFigure = fig;
                }
                else
                {
                    existingFig.PointIds.Clear();
                    context.CurrentFigure = existingFig;
                }
                break;

            case "TANGENT":
                if (context.CurrentAlignment == null) throw new InvalidOperationException("No active alignment. Use ALGN BEG first.");
                if (args.Length < 4) throw new ArgumentException("ALGN TANGENT requires two point IDs.");
                
                var p1 = context.GetPoint(args[2]);
                var p2 = context.GetPoint(args[3]);
                if (p1 == null || p2 == null) throw new ArgumentException("One or both points not found for tangent.");
                
                var line = new LineElement { StartPoint = p1, EndPoint = p2 };
                context.CurrentAlignment.AddElement(line);
                context.Log($"[INFO] Added Tangent from Pt {args[2]} to Pt {args[3]} length {line.Length:F2}");

                // IMPLICIT FIGURE
                if (context.CurrentFigure != null && context.CurrentFigure.Name == context.CurrentAlignment.Name)
                {
                    if (context.CurrentFigure.PointIds.Count == 0 || context.CurrentFigure.PointIds.Last() != args[2])
                        context.CurrentFigure.PointIds.Add(args[2]);
                    
                    context.CurrentFigure.PointIds.Add(args[3]);
                }
                break;
                
            case "CURVE":
                if (context.CurrentAlignment == null) throw new InvalidOperationException("No active alignment.");
                if (args.Length < 5) throw new ArgumentException("ALGN CURVE requires PC, RP, and PT point IDs.");
                
                var pc = context.GetPoint(args[2]);
                var rp = context.GetPoint(args[3]);
                var pt = context.GetPoint(args[4]);
                if (pc == null || rp == null || pt == null) throw new ArgumentException("Curve points not found.");
                
                double dx = pc.Easting - rp.Easting;
                double dy = pc.Northing - rp.Northing;
                double radius = Math.Sqrt(dx*dx + dy*dy);
                
                // Determine azs
                double startAz = Math.Atan2(dx, dy) * 180.0 / Math.PI;
                if (startAz < 0) startAz += 360.0;
                
                double dx2 = pt.Easting - rp.Easting;
                double dy2 = pt.Northing - rp.Northing;
                double endAz = Math.Atan2(dx2, dy2) * 180.0 / Math.PI;
                if (endAz < 0) endAz += 360.0;
                
                // Extremely simple sweep direction for demo purposes:
                double sweep = endAz - startAz;
                if (sweep < 0) sweep += 360.0;
                bool cw = sweep <= 180.0;
                
                var arc = new ArcElement 
                { 
                    CenterPoint = rp, 
                    Radius = radius, 
                    StartAzimuth = startAz, 
                    EndAzimuth = endAz, 
                    IsClockwise = cw 
                };
                context.CurrentAlignment.AddElement(arc);
                context.Log($"[INFO] Added Curve: R={radius:F2}");

                // IMPLICIT FIGURE
                if (context.CurrentFigure != null && context.CurrentFigure.Name == context.CurrentAlignment.Name)
                {
                    if (context.CurrentFigure.PointIds.Count == 0 || context.CurrentFigure.PointIds.Last() != args[2])
                        context.CurrentFigure.PointIds.Add(args[2]);
                    
                    context.CurrentFigure.PointIds.Add(args[4]);
                }
                break;

            case "END":
                if (context.CurrentAlignment == null) throw new InvalidOperationException("No active alignment to end.");

                // RULE: Horizontal alignments may not be a closed figure
                if (context.CurrentAlignment.Elements.Count > 1)
                {
                    var firstElem = context.CurrentAlignment.Elements.First();
                    var lastElem = context.CurrentAlignment.Elements.Last();
                    
                    var firstPt = firstElem.GetCoordinateAt(firstElem.StartStation);
                    var lastPt = lastElem.GetCoordinateAt(lastElem.EndStation);

                    if (firstPt != null && lastPt != null)
                    {
                        double dE = lastPt.Easting - firstPt.Easting;
                        double dN = lastPt.Northing - firstPt.Northing;
                        double dist = Math.Sqrt(dE * dE + dN * dN);
                        
                        // If it wraps within a strict threshold, discard it
                        if (dist < 0.01) 
                        {
                            context.CurrentAlignment = null;
                            throw new InvalidOperationException("RULE VIOLATION: Horizontal alignments may not be a closed figure (starts and ends at the exact same coordinate).");
                        }
                    }
                }

                context.AddAlignment(context.CurrentAlignment);
                context.Log($"[INFO] Ended Alignment '{context.CurrentAlignment.Name}'. Total length {context.CurrentAlignment.Elements.Sum(e => e.Length):F2}");

                if (context.CurrentFigure != null && context.CurrentFigure.Name == context.CurrentAlignment.Name)
                {
                    context.CurrentFigure = null;
                }

                context.CurrentAlignment = null;
                break;
                
            default:
                throw new ArgumentException($"Unknown ALGN subcommand: {sub}");
        }
    }
}

public class ProfileCommand : ICommand
{
    public string Name => "PROF";
    public string Description => "Handles Vertical Profile definitions (BEG, END)";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        Execute(args, context);
        return Task.CompletedTask;
    }

    public void Execute(string[] args, ICogoContext context)
    {
        if (args.Length < 2) throw new ArgumentException("PROF requires BEG or END.");
        
        string sub = args[1].ToUpper();
        if (sub == "BEG")
        {
            int algnIdx = 2;
            if (args.Length > 2 && args[algnIdx].Equals("FIG", StringComparison.OrdinalIgnoreCase))
            {
                algnIdx++;
            }
            if (args.Length < algnIdx + 2) throw new ArgumentException("PROF BEG requires alignment_name and type (EG/FG).");
            
            string algnName = args[algnIdx];
            string pType = args[algnIdx + 1];
            
            var algn = context.GetAlignment(algnName);
            if (algn == null) throw new ArgumentException($"Alignment '{algnName}' not found. Create it first.");
            
            context.CurrentProfile = new Profile { Name = algnName + "_" + pType, ProfileType = pType };
            context.Log($"[INFO] Began Profile '{context.CurrentProfile.Name}'");
        }
        else if (sub == "END")
        {
            if (context.CurrentProfile == null) throw new InvalidOperationException("No active profile to end.");
            
            // Attach to its alignment
            string algnName = context.CurrentProfile.Name.Split('_')[0];
            var algn = context.GetAlignment(algnName);
            if (algn != null)
            {
                algn.Profiles.Add(context.CurrentProfile);
                context.Log($"[INFO] Ended Profile '{context.CurrentProfile.Name}' with {context.CurrentProfile.Intersections.Count} VPIs.");
            }
            context.CurrentProfile = null;
        }
        else throw new ArgumentException($"Unknown PROF subcommand: {sub}");
    }
}

public class VpiCommand : ICommand
{
    public string Name => "VPI";
    public string Description => "Adds Vertical Point of Intersection to active profile";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        Execute(args, context);
        return Task.CompletedTask;
    }

    public void Execute(string[] args, ICogoContext context)
    {
        if (context.CurrentProfile == null) throw new InvalidOperationException("No active profile. Use PROF BEG first.");
        if (args.Length < 3) throw new ArgumentException("VPI requires Station and Elevation [and optional CurveLength].");

        string rawStation = args[1].Replace("+", "");
        if (!double.TryParse(rawStation, out double station)) throw new ArgumentException("Invalid VPI Station format.");
        
        if (!double.TryParse(args[2], out double elev)) throw new ArgumentException("Invalid VPI Elevation format.");
        
        double cl = 0;
        if (args.Length >= 4)
        {
            double.TryParse(args[3], out cl);
        }

        context.CurrentProfile.AddVpi(new Vpi { Station = station, Elevation = elev, CurveLength = cl });
        context.Log($"[INFO] Added VPI at {args[1]} Elev: {elev:F2} VC: {cl}");
    }
}

public class HaLblCommand : ICommand
{
    public string Name { get; }
    public string Description => "Toggles Horizontal Alignment Labels";

    public HaLblCommand(string name)
    {
        Name = name;
    }

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        Execute(args, context);
        return Task.CompletedTask;
    }

    public void Execute(string[] args, ICogoContext context)
    {
        if (Name == "HALBL-ON")
        {
            context.ShowAlignmentLabels = true;
            context.Log("[INFO] Horizontal Alignment labels turned ON.");
        }
        else if (Name == "HALBL-OFF")
        {
            context.ShowAlignmentLabels = false;
            context.Log("[INFO] Horizontal Alignment labels turned OFF.");
        }
    }
}
