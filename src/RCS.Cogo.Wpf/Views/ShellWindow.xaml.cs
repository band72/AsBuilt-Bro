using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

public partial class ShellWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging;
    private bool _isZoomWindowMode;
    private bool _isZoomDragging;
    private Point _zoomStartPoint;

    public ShellWindow()
    {
        InitializeComponent();
        var vm = new ShellViewModel();
        DataContext = vm;
        
        vm.ZoomExtentsRequested += (s, e) => ZoomExtents();
        vm.ZoomInRequested += (s, e) => ZoomIn();
        vm.ZoomOutRequested += (s, e) => ZoomOut();
        vm.ZoomWindowRequested += (s, e) => {
            _isZoomWindowMode = true;
            Mouse.OverrideCursor = Cursors.Cross;
        };
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
        else if (_isZoomDragging)
        {
            var pos = e.GetPosition((UIElement)sender);
            var x = System.Math.Min(pos.X, _zoomStartPoint.X);
            var y = System.Math.Min(pos.Y, _zoomStartPoint.Y);
            var width = System.Math.Abs(pos.X - _zoomStartPoint.X);
            var height = System.Math.Abs(pos.Y - _zoomStartPoint.Y);
            
            ZoomRect.Margin = new Thickness(x, y, 0, 0);
            ZoomRect.Width = width;
            ZoomRect.Height = height;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isZoomWindowMode)
        {
            _isZoomDragging = true;
            _zoomStartPoint = e.GetPosition((UIElement)sender);
            
            ZoomRect.Margin = new Thickness(_zoomStartPoint.X, _zoomStartPoint.Y, 0, 0);
            ZoomRect.Width = 0;
            ZoomRect.Height = 0;
            ZoomRect.Visibility = Visibility.Visible;
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isZoomDragging)
        {
            _isZoomDragging = false;
            _isZoomWindowMode = false;
            Mouse.OverrideCursor = null;
            ZoomRect.Visibility = Visibility.Collapsed;
            ((UIElement)sender).ReleaseMouseCapture();

            var endPoint = e.GetPosition((UIElement)sender);
            
            // Convert rect to World constraints and calculate new matrix
            if (System.Math.Abs(endPoint.X - _zoomStartPoint.X) > 5 && System.Math.Abs(endPoint.Y - _zoomStartPoint.Y) > 5)
            {
                var matrix = WorldTransform.Matrix;
                matrix.Invert();
                var worldStart = matrix.Transform(_zoomStartPoint);
                var worldEnd = matrix.Transform(endPoint);
                
                double minX = System.Math.Min(worldStart.X, worldEnd.X);
                double maxX = System.Math.Max(worldStart.X, worldEnd.X);
                double minY = System.Math.Min(worldStart.Y, worldEnd.Y);
                double maxY = System.Math.Max(worldStart.Y, worldEnd.Y);
                
                double width = maxX - minX;
                double height = maxY - minY;
                
                // Viewport boundary scaling logic analogous to zoom extents 
                double vpWidth = ViewportCanvas.ActualWidth;
                double vpHeight = ViewportCanvas.ActualHeight;
                if (vpWidth > 0 && vpHeight > 0)
                {
                    double scaleX = vpWidth / width;
                    double scaleY = vpHeight / height;
                    double scale = System.Math.Min(scaleX, scaleY);
                    
                    double midX = (minX + maxX) / 2.0;
                    double midY = (minY + maxY) / 2.0;

                    var newMatrix = new Matrix();
                    newMatrix.Scale(scale, -scale);
                    newMatrix.Translate(vpWidth/2.0 - midX * scale, vpHeight/2.0 - midY * -scale);
                    WorldTransform.Matrix = newMatrix;
                    
                    if (DataContext is ShellViewModel vm) vm.CurrentViewScale = scale;
                }
            }
            e.Handled = true;
        }
    }

    private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Only trigger if the event source is the TabControl itself
        if (e.OriginalSource is System.Windows.Controls.TabControl)
        {
            if (DataContext is ShellViewModel vm)
            {
                // Index 1 corresponds to Tab 2: Cogo
                // Use a minor dispatch delay to let WPF fully swap the visual tree elements before focusing
                if (vm.SelectedTabIndex == 1)
                {
                    Dispatcher.BeginInvoke(new Action(() => CogoInputTextBox.Focus()), System.Windows.Threading.DispatcherPriority.Input);
                }
            }
        }
    }
}
