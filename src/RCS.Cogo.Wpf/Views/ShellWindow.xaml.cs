using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

public partial class ShellWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging;

    public ShellWindow()
    {
        InitializeComponent();
        var vm = new ShellViewModel();
        DataContext = vm;
        
        vm.ZoomExtentsRequested += (s, e) => ZoomExtents();
        vm.ZoomInRequested += (s, e) => ZoomIn();
        vm.ZoomOutRequested += (s, e) => ZoomOut();
        vm.ZoomToPointRequested += (s, target) => ZoomToPoint(target);
        
        // Initialize Default View (Center 5000,5000, Scale 1, Y flipped)
        // Matrix: M11=Scale, M22=-Scale, OffsetX, OffsetY
        // Initial setup to see coordinate 0,0 at bottom left?
        var matrix = new Matrix();
        matrix.Scale(1, -1); // Flip Y
        // Translate to map 5000,5000 to center of screen (approx 600, 400)
        // Screen X = (World X * M11) + OffsetX => 600 = 5000 * 1 + OffsetX => OffsetX = -4400
        // Screen Y = (World Y * M22) + OffsetY => 400 = 5000 * -1 + OffsetY => OffsetY = 5400
        matrix.Translate(-4400, 5400); 
        WorldTransform.Matrix = matrix;
    }

    private void ZoomIn()
    {
        var matrix = WorldTransform.Matrix;
        var center = new Point(ViewportCanvas.ActualWidth / 2, ViewportCanvas.ActualHeight / 2);
        matrix.ScaleAt(1.05, 1.05, center.X, center.Y);
        WorldTransform.Matrix = matrix;
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = matrix.M11;
    }

    private void ZoomOut()
    {
        var matrix = WorldTransform.Matrix;
        var center = new Point(ViewportCanvas.ActualWidth / 2, ViewportCanvas.ActualHeight / 2);
        matrix.ScaleAt(1.0 / 1.05, 1.0 / 1.05, center.X, center.Y);
        WorldTransform.Matrix = matrix;
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = matrix.M11;
    }

    private void ZoomToPoint(Point target)
    {
        double targetScale = 1.5;
        
        var matrix = new Matrix();
        matrix.M11 = targetScale;
        matrix.M22 = -targetScale; // Flip Y
        
        double screenCenterX = ViewportCanvas.ActualWidth / 2;
        double screenCenterY = ViewportCanvas.ActualHeight / 2;
        
        // Offset = Center - (World * Scale)
        matrix.OffsetX = screenCenterX - (target.X * targetScale);
        matrix.OffsetY = screenCenterY - (target.Y * -targetScale); // Using flipped Y
        
        WorldTransform.Matrix = matrix;
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = targetScale;
    }

    private void ZoomExtents()
    {
        var vm = DataContext as ShellViewModel;
        if (vm == null || vm.Points.Count == 0) return;
        
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        
        // Include Points
        foreach (var p in vm.Points)
        {
            if (p.Easting < minX) minX = p.Easting;
            if (p.Easting > maxX) maxX = p.Easting;
            if (p.Northing < minY) minY = p.Northing;
            if (p.Northing > maxY) maxY = p.Northing;
        }
        
        // Include Figures
        foreach (var f in vm.Figures)
        {
            foreach (var pt in f.Points)
            {
                 if (pt.X < minX) minX = pt.X;
                 if (pt.X > maxX) maxX = pt.X;
                 if (pt.Y < minY) minY = pt.Y;
                 if (pt.Y > maxY) maxY = pt.Y;
            }
        }
        
        if (minX == double.MaxValue) return;
        
        // Add 10% margin
        double width = maxX - minX;
        double height = maxY - minY;
        
        if (width < 10) width = 10; // Min bounds
        if (height < 10) height = 10;
        
        minX -= width * 0.1;
        maxX += width * 0.1;
        minY -= height * 0.1;
        maxY += height * 0.1;
        
        width = maxX - minX;
        height = maxY - minY;

        // Viewport Size
        double vpWidth = ViewportCanvas.ActualWidth;
        double vpHeight = ViewportCanvas.ActualHeight;
        
        if (vpWidth == 0 || vpHeight == 0) return;
        
        // Calculate Scale
        double scaleX = vpWidth / width;
        double scaleY = vpHeight / height;
        double scale = Math.Min(scaleX, scaleY);
        
        // Center
        double midX = (minX + maxX) / 2.0;
        double midY = (minY + maxY) / 2.0;
        
        // Apply Transform
        // Screen X = (World X - midX) * scale + vpWidth/2
        // Screen Y = (World Y - midY) * -scale + vpHeight/2  (Flip Y)
        
        var matrix = new Matrix();
        matrix.Scale(scale, -scale);
        matrix.Translate(vpWidth/2.0 - midX * scale, vpHeight/2.0 - midY * -scale);
        
        WorldTransform.Matrix = matrix;
        vm.CurrentViewScale = scale;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(ViewportCanvas);
        var matrix = WorldTransform.Matrix;
        
        // Very smooth steps for mouse wheel per user request
        double scale = e.Delta > 0 ? 1.03 : (1.0 / 1.03);
        
        matrix.ScaleAt(scale, scale, pos.X, pos.Y);
        WorldTransform.Matrix = matrix;
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = matrix.M11;
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastMousePosition = e.GetPosition(this);
        _isDragging = true;
        ((UIElement)sender).CaptureMouse();
        e.Handled = true; // Prevent context menu
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ((UIElement)sender).ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var currentPos = e.GetPosition(this);
            var delta = currentPos - _lastMousePosition;
            
            var matrix = WorldTransform.Matrix;
            matrix.Translate(delta.X, delta.Y);
            WorldTransform.Matrix = matrix;
            
            _lastMousePosition = currentPos;
            e.Handled = true; // Consuming the event while dragging
        }
    }
}
