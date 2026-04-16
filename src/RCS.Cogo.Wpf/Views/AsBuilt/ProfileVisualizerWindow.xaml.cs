using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class ProfileVisualizerWindow : Window
{
    public string RunTitle { get; set; } = "";

    public ProfileVisualizerWindow(AsBuiltJob job, RCS.Piping.Core.Models.PipeRun targetRun)
    {
        InitializeComponent();
        
        var p1 = job.PointRows.FirstOrDefault(r => r.PointId == targetRun.FromPointId);
        var p2 = job.PointRows.FirstOrDefault(r => r.PointId == targetRun.ToPointId);
        
        if (p1 == null || p2 == null)
        {
            RunTitle = $"Invalid Run {targetRun.Id}";
            DataContext = this;
            return;
        }

        double len = targetRun.ComputedLength;
        if (len == 0) len = Math.Sqrt(Math.Pow(p2.Easting - p1.Easting, 2) + Math.Pow(p2.Northing - p1.Northing, 2));

        double startZ = targetRun.InvertStart ?? p1.Elevation;
        double endZ   = targetRun.InvertEnd ?? p2.Elevation;

        RunTitle = $"Profile: Run {targetRun.Id} ({targetRun.Type})   L={len:F2}'\nInvert Start: {startZ:F2}'   Invert End: {endZ:F2}'";
        DataContext = this;

        Loaded += (s, e) => DrawProfile(job, targetRun, len, startZ, endZ, p1, p2);
    }

    private void DrawProfile(AsBuiltJob job, RCS.Piping.Core.Models.PipeRun targetRun, double len, double startZ, double endZ, RCS.Piping.Core.Workflow.PointRow p1, RCS.Piping.Core.Workflow.PointRow p2)
    {
        ProfileCanvas.Children.Clear();
        
        double canvasW = ProfileCanvas.ActualWidth;
        double canvasH = ProfileCanvas.ActualHeight;
        
        if (canvasW == 0 || canvasH == 0) return;

        double baseZ = Math.Min(startZ, endZ) - 2;
        double topZ = Math.Max(startZ, endZ) + 5;
        
        // Find max crossing elevation
        foreach (var r2 in job.Network.Runs.Values.Where(r => r.Id != targetRun.Id))
        {
            var p3 = job.PointRows.FirstOrDefault(r => r.PointId == r2.FromPointId);
            var p4 = job.PointRows.FirstOrDefault(r => r.PointId == r2.ToPointId);
            if (p3 == null || p4 == null) continue;
            
            double denom = (p4.Northing - p3.Northing) * (p2.Easting - p1.Easting) - (p4.Easting - p3.Easting) * (p2.Northing - p1.Northing);
            if (Math.Abs(denom) < 1e-9) continue;
            
            double uA = ((p4.Easting - p3.Easting) * (p1.Northing - p3.Northing) - (p4.Northing - p3.Northing) * (p1.Easting - p3.Easting)) / denom;
            double uB = ((p2.Easting - p1.Easting) * (p1.Northing - p3.Northing) - (p2.Northing - p1.Northing) * (p1.Easting - p3.Easting)) / denom;
            
            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            {
                double z2Cross = (r2.InvertStart ?? p3.Elevation) + uB * ((r2.InvertEnd ?? p4.Elevation) - (r2.InvertStart ?? p3.Elevation));
                if (z2Cross - 2 < baseZ) baseZ = z2Cross - 2;
                if (z2Cross + 2 > topZ) topZ = z2Cross + 2;
            }
        }
        
        double zRange = topZ - baseZ;
        if (zRange < 1) zRange = 10;
        
        double xScale = canvasW / Math.Max(len, 10);
        double yScale = canvasH / zRange; // Native scaling based on canvas

        // Y-axis inversion (WPF is top-down Y)
        double GetY(double z) => canvasH - ((z - baseZ) * yScale);

        // Draw Base Pipe
        var pipeLine = new Line {
            X1 = 0, Y1 = GetY(startZ),
            X2 = len * xScale, Y2 = GetY(endZ),
            Stroke = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
            StrokeThickness = Math.Max(2, (targetRun.Diameter / 12.0) * yScale)
        };
        ProfileCanvas.Children.Add(pipeLine);
        
        // Draw Intersections
        foreach (var r2 in job.Network.Runs.Values)
        {
            if (r2.Id == targetRun.Id) continue;
            var p3 = job.PointRows.FirstOrDefault(r => r.PointId == r2.FromPointId);
            var p4 = job.PointRows.FirstOrDefault(r => r.PointId == r2.ToPointId);
            if (p3 == null || p4 == null) continue;

            double denom = (p4.Northing - p3.Northing) * (p2.Easting - p1.Easting) - (p4.Easting - p3.Easting) * (p2.Northing - p1.Northing);
            if (Math.Abs(denom) < 1e-9) continue;

            double uA = ((p4.Easting - p3.Easting) * (p1.Northing - p3.Northing) - (p4.Northing - p3.Northing) * (p1.Easting - p3.Easting)) / denom;
            double uB = ((p2.Easting - p1.Easting) * (p1.Northing - p3.Northing) - (p2.Northing - p1.Northing) * (p1.Easting - p3.Easting)) / denom;

            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            {
                double z2Cross = (r2.InvertStart ?? p3.Elevation) + uB * ((r2.InvertEnd ?? p4.Elevation) - (r2.InvertStart ?? p3.Elevation));
                
                double xPlot = uA * len * xScale;
                double yPlot = GetY(z2Cross);
                double diamY = (r2.Diameter / 12.0) * yScale;
                if (diamY < 10) diamY = 10;
                
                var marker = new Ellipse {
                    Width = diamY, Height = diamY,
                    Fill = Brushes.Red, Stroke = Brushes.White,
                    ToolTip = $"X-ing {r2.Type} Inv: {z2Cross:F2}'\nClearance: {Math.Abs(z2Cross - (startZ + uA * (endZ - startZ))):F2}'"
                };
                Canvas.SetLeft(marker, xPlot - diamY / 2);
                Canvas.SetTop(marker, yPlot - diamY / 2);
                ProfileCanvas.Children.Add(marker);
                
                var lb = new TextBlock { Text = $"X-ing {r2.Type}", Foreground=Brushes.IndianRed, FontSize=10, FontWeight=FontWeights.Bold };
                Canvas.SetLeft(lb, xPlot + 10);
                Canvas.SetTop(lb, yPlot - 10);
                ProfileCanvas.Children.Add(lb);
            }
        }
        
        // Add Draggable Nodes
        AddDraggableNode(job, targetRun, true, 0, GetY(startZ), yScale, baseZ, canvasH, p1, p2, len);
        AddDraggableNode(job, targetRun, false, len * xScale, GetY(endZ), yScale, baseZ, canvasH, p1, p2, len);
    }

    private void AddDraggableNode(AsBuiltJob job, RCS.Piping.Core.Models.PipeRun run, bool isStart, double x, double y, double yScale, double baseZ, double canvasH, RCS.Piping.Core.Workflow.PointRow p1, RCS.Piping.Core.Workflow.PointRow p2, double len)
    {
        var node = new Ellipse {
            Width = 16, Height = 16,
            Fill = Brushes.LightGreen, Stroke = Brushes.Black, StrokeThickness = 2,
            Cursor = System.Windows.Input.Cursors.SizeNS,
            ToolTip = isStart ? "Drag to adjust Invert Start" : "Drag to adjust Invert End"
        };
        Canvas.SetLeft(node, x - 8);
        Canvas.SetTop(node, y - 8);
        ProfileCanvas.Children.Add(node);

        bool isDragging = false;
        node.MouseLeftButtonDown += (s, e) => {
            isDragging = true;
            node.CaptureMouse();
            e.Handled = true;
        };
        node.MouseMove += (s, e) => {
            if (!isDragging) return;
            double curY = e.GetPosition(ProfileCanvas).Y;
            if (curY < 0) curY = 0; if (curY > canvasH) curY = canvasH;
            double curZ = baseZ + ((canvasH - curY) / yScale);
            
            if (isStart) run.InvertStart = curZ;
            else run.InvertEnd = curZ;
            
            Canvas.SetTop(node, curY - 8);
        };
        node.MouseLeftButtonUp += (s, e) => {
            isDragging = false;
            node.ReleaseMouseCapture();
            // Redraw profile mathematically
            double startZAct = run.InvertStart ?? p1.Elevation;
            double endZAct   = run.InvertEnd ?? p2.Elevation;
            DrawProfile(job, run, len, startZAct, endZAct, p1, p2);
        };
    }
}
