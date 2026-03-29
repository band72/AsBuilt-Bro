using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;
using RCS.Cogo.Core.Primitives;
using RCS.Cogo.Wpf.Commands;
using RCS.Cogo.App.Models;
using RCS.Cogo.App.Persistence;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RCS.Geo.Core;
using RCS.Geo.ProjNet;
using RCS.Geo.Abstractions;
using GeoWpf = RCS.Geo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.ViewModels;

public partial class ShellViewModel
{
    private void ValidateNetwork()
    {
        CommandLog.Add("--- Validating Pipe Network ---");
        
        List<string>? validTypes = null;
        if (CogoCodes.Count > 0)
        {
            validTypes = CogoCodes.Select(c => c.LocalCode)
                          .Concat(CogoCodes.Select(c => c.SystemCode))
                          .Where(s => !string.IsNullOrEmpty(s))
                          .Distinct()
                          .ToList();
            CommandLog.Add($"Validating against {CogoCodes.Count} imported codes.");
        }

        // Pass validTypes for both Structures and Pipes (assuming codes cover both)
        var issues = _pipelineRunner.ValidateNetwork(validStructureTypes: validTypes, validPipeTypes: validTypes);
        
        if (issues.Count == 0)
        {
            CommandLog.Add("Network is Valid.");
        }
        else
        {
            foreach(var issue in issues)
            {
                CommandLog.Add($"[ISSUE] {issue}");
            }
        }
        CommandLog.Add("-------------------------------");
    }

    private void LogToOutput(string msg)
    {
        CommandLog.Add(msg);
        _context.Log(msg);
    }

    private void ExecuteUtilConvert()
    {
        if (double.TryParse(UtilDecInput, out double d))
        {
            UtilDmsOutput = DegreeToDmsString(d);
            LogToOutput($"Converted Decimal to DMS: {d} -> {UtilDmsOutput}");
        }
        else
        {
            UtilDmsOutput = "Invalid Input";
            LogToOutput("Error: Invalid Decimal Input.");
        }
    }

    private void ExecuteUtilConvertDmsToDd()
    {
        try
        {
            if (double.TryParse(UtilDmsInput, out double dms))
            {
                double d = Angle.FromDMS(dms).Degrees;
                UtilDdOutput = $"{d:F6}°";
                LogToOutput($"Converted DMS to Decimal: {dms} -> {UtilDdOutput}");
            }
            else
            {
                UtilDdOutput = "Invalid Input";
                LogToOutput("Error: Invalid DMS Input.");
            }
        }
        catch
        {
            UtilDdOutput = "Invalid Input";
            LogToOutput("Error: Failed to process DMS Input.");
        }
    }

    private void ExecuteBearingMath(bool isAdd)
    {
        try
        {
            if (double.TryParse(Bearing1Input, out double b1) && double.TryParse(Bearing2Input, out double b2))
            {
                double d1 = Angle.FromDMS(b1).Degrees;
                double d2 = Angle.FromDMS(b2).Degrees;
                double res = isAdd ? (d1 + d2) : (d1 - d2);
                
                while(res < 0) res += 360;
                while(res >= 360) res -= 360;

                BearingMathDdOutput = $"{res:F6}°";
                BearingMathDmsOutput = DegreeToDmsString(res);
                string op = isAdd ? "+" : "-";
                LogToOutput($"Bearing Math ({op}): {b1} {op} {b2} -> {BearingMathDdOutput} / {BearingMathDmsOutput}");
            }
            else
            {
                BearingMathDdOutput = "Invalid Input";
                BearingMathDmsOutput = "";
                LogToOutput("Error: Invalid Bearing Input.");
            }
        }
        catch
        {
            BearingMathDdOutput = "Error";
            BearingMathDmsOutput = "";
            LogToOutput("Error: Failed to process Bearing Math.");
        }
    }

    private void ExecuteUtilSupplement()
    {
        if (double.TryParse(UtilSuppInput, out double d))
        {
            // Supplement = 180 - Angle.
            double supp = 180.0 - d;
            // Normalize? Usually 0-180 or 0-360.
            // If input is > 180, technically supplement implies geometrical construct, usually 180-x. 
            // Result can be negative if x > 180. Let's keep it raw.
            UtilSuppOutput = DegreeToDmsString(supp);
            LogToOutput($"Supplement Finder: 180 - {d} -> {UtilSuppOutput}");
        }
        else
        {
            UtilSuppOutput = "Invalid Input";
            LogToOutput("Error: Invalid Supplement Input.");
        }
    }

    private void ClearCurveSolver()
    {
        // Clear Curve Inputs/Outputs
        CurveRadius = "";
        CurveTangent = "";
        CurveChord = "";
        CurveArc = "";
        CurveDelta = "";
        CurveDeltaDms = "";

        // Clear Utility Inputs/Outputs
        UtilDecInput = "";
        UtilDmsOutput = "";
        UtilDmsInput = "";
        UtilDdOutput = "";
        UtilSuppInput = "";
        UtilSuppOutput = "";
        Bearing1Input = "";
        Bearing2Input = "";
        BearingMathDdOutput = "";
        BearingMathDmsOutput = "";
        
        LogToOutput("Curve Solver Reset.");
    }

    private string DegreeToDmsString(double decimalDegrees)
    {
        // Handle Negative
        bool isNeg = decimalDegrees < 0;
        decimalDegrees = Math.Abs(decimalDegrees);
        
        int d = (int)decimalDegrees;
        double rem = (decimalDegrees - d) * 60.0;
        int m = (int)rem;
        double s = (rem - m) * 60.0;
        
        // F1 gives one decimal place for seconds, e.g. 12.5"
        return $"{(isNeg ? "-" : "")}{d}° {m:00}' {s:00.0}\"";
    }

    private void SolveCurve()
    {
        // Collect inputs
        double? r = double.TryParse(CurveRadius, out double dr) ? dr : null;
        double? t = double.TryParse(CurveTangent, out double dt) ? dt : null;
        double? c = double.TryParse(CurveChord, out double dc) ? dc : null;
        double? l = double.TryParse(CurveArc, out double dl) ? dl : null;
        double? d = double.TryParse(CurveDelta, out double dd) ? dd : null; // Degrees

        // Need exactly 2 inputs
        int count = (r.HasValue ? 1 : 0) + (t.HasValue ? 1 : 0) + (c.HasValue ? 1 : 0) + (l.HasValue ? 1 : 0) + (d.HasValue ? 1 : 0);
        
        if (count != 2)
        {
            LogToOutput("Error: Please provide exactly two curve parameters.");
            return;
        }

        double deltaRad = 0;
        double R = 0;
        bool solved = false;

        // Convert Delta to Radians if provided
        if (d.HasValue) deltaRad = d.Value * (Math.PI / 180.0);

        try
        {
            // Case 1: R & Delta
            if (r.HasValue && d.HasValue)
            {
                R = r.Value;
                solved = true;
            }
            // Case 2: R & T
            else if (r.HasValue && t.HasValue)
            {
                R = r.Value;
                deltaRad = 2 * Math.Atan(t.Value / R);
                solved = true;
            }
            // Case 3: R & C
            else if (r.HasValue && c.HasValue)
            {
                R = r.Value;
                // sin(delta/2) = C/2R
                // Domain check: C <= 2R
                if (c.Value > 2 * R) throw new Exception("Chord cannot be larger than Diameter.");
                deltaRad = 2 * Math.Asin(c.Value / (2 * R));
                solved = true;
            }
            // Case 4: R & Arc
            else if (r.HasValue && l.HasValue)
            {
                R = r.Value;
                deltaRad = l.Value / R;
                solved = true;
            }
            // Case 5: T & Delta
            else if (t.HasValue && d.HasValue)
            {
                R = t.Value / Math.Tan(deltaRad / 2);
                solved = true;
            }
            // Case 6: C & Delta
            else if (c.HasValue && d.HasValue)
            {
                R = c.Value / (2 * Math.Sin(deltaRad / 2));
                solved = true;
            }
            // Case 7: Arc & Delta
            else if (l.HasValue && d.HasValue)
            {
                R = l.Value / deltaRad;
                solved = true;
            }
            // Case 8: T & C
            else if (t.HasValue && c.HasValue)
            {
                // cos(delta/2) = C/2T
                if (c.Value >= 2 * t.Value) throw new Exception("Chord must be less than 2*Tangent (for simple curve < 180).");
                 deltaRad = 2 * Math.Acos(c.Value / (2 * t.Value));
                 R = t.Value / Math.Tan(deltaRad / 2);
                 solved = true;
            }
            // Case 9: Arc & T (Transcendental)
            else if (l.HasValue && t.HasValue)
            {
                 // Iterate to find Delta
                 // T = R tan(d/2), L = R d => R = L/d
                 // T = (L/d) * tan(d/2) -> T/L = tan(d/2)/d
                 // f(d) = tan(d/2)/d - T/L = 0
                 double expectedRatio = t.Value / l.Value;
                 double estDelta = 2 * Math.Atan(expectedRatio); // Approximation? tan(x) ~ x for small x. tan(d/2)/d ~ (d/2)/d = 0.5. T/L ~ 0.5?
                 // Wait for small angles T ~ L/2.
                 // Actually for very small angles, T ~ L/2. 
                 // Simple Newton Method
                 deltaRad = SolveDeltaFromTangentArc(t.Value, l.Value);
                 R = l.Value / deltaRad;
                 solved = true;
            }
            // Case 10: Arc & C (Transcendental)
            else if (l.HasValue && c.HasValue)
            {
                 // C = 2R sin(d/2), L = R d => R = L/d
                 // C = 2(L/d) sin(d/2)
                 // C/L = sin(d/2) / (d/2) = sinc(d/2)
                 deltaRad = SolveDeltaFromChordArc(c.Value, l.Value);
                 R = l.Value / deltaRad;
                 solved = true;
            }

            if (solved)
            {
                double finalDeltaDeg = deltaRad * (180.0 / Math.PI);
                double finalT = R * Math.Tan(deltaRad / 2);
                double finalC = 2 * R * Math.Sin(deltaRad / 2);
                double finalL = R * deltaRad;

                // Update Fields (Check for NaN)
                CurveRadius = R.ToString("F3");
                CurveTangent = finalT.ToString("F3");
                CurveChord = finalC.ToString("F3");
                CurveArc = finalL.ToString("F3");
                CurveDelta = finalDeltaDeg.ToString("F6"); // High precision dec
                CurveDeltaDms = DegreeToDmsString(finalDeltaDeg); // DMS
                
                LogToOutput($"Curve Solved: R={CurveRadius}, T={CurveTangent}, L={CurveArc}, C={CurveChord}, D={CurveDelta} ({CurveDeltaDms})");
            }
        }
        catch (Exception ex)
        {
            LogToOutput($"Curve Solver Error: {ex.Message}");
        }
    }

    private double SolveDeltaFromTangentArc(double T, double L)
    {
        // f(x) = tan(x/2) - (T/L)x = 0
        // Find x (delta)
        // Derivative f'(x) = 0.5 * sec^2(x/2) - T/L
        
        double targetRatio = T / L;
        double x = 2.0 * Math.Atan(targetRatio); // Initial guess
        
        for(int i=0; i<20; i++)
        {
            double fx = Math.Tan(x/2) - targetRatio * x;
            double dfx = 0.5 * Math.Pow(1/Math.Cos(x/2), 2) - targetRatio;
            
            double xNew = x - fx/dfx;
            if (Math.Abs(xNew - x) < 1e-6) return xNew;
            x = xNew;
        }
        return x;
    }

    private double SolveDeltaFromChordArc(double C, double L)
    {
        // f(x) = 2 sin(x/2) - (C/L)x = 0
        // Derivative f'(x) = cos(x/2) - C/L
        
        double targetRatio = C / L;
        // sinc(x/2) = targetRatio.
        // For small x, sinc(x) ~ 1 - x^2/6.
        // 1 - (x/2)^2/6 = ratio => (x/2)^2 = 6(1-ratio) => x/2 = sqrt(6(1-ratio)) => x = 2*sqrt...
        
        double x = Math.Sqrt(24 * (1 - targetRatio)); 
        if (double.IsNaN(x)) x = 0.1;

        for(int i=0; i<20; i++)
        {
            double fx = 2 * Math.Sin(x/2) - targetRatio * x;
            double dfx = Math.Cos(x/2) - targetRatio;
            
            double xNew = x - fx/dfx;
            if (Math.Abs(xNew - x) < 1e-6) return xNew;
            x = xNew;
        }
        return x;
    }


    
}
