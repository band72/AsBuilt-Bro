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
        
        vm.ZoomExtentsRequested  += (s, e) => ZoomExtents();
        vm.ZoomInRequested        += (s, e) => ZoomIn();
        vm.ZoomOutRequested       += (s, e) => ZoomOut();
        vm.ZoomWindowRequested    += (s, e) => ActivateZoomOverlay();
        vm.ZoomToPointRequested   += (s, target) => ZoomToPoint(target);

        // Initialize Default View
        var matrix = new Matrix();
        matrix.Scale(1, -1);
        matrix.Translate(-4400, 5400);
        WorldTransform.Matrix = matrix;
    }

    // ── Zoom Overlay (activated by WIN button) ────────────────────────────────
    private void ActivateZoomOverlay()
    {
        ZoomOverlay.Visibility = Visibility.Visible;
        Mouse.OverrideCursor   = Cursors.Cross;
    }

    private void ZoomOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _zoomStartPoint = e.GetPosition(ZoomOverlay);
        _isZoomDragging = true;

        ZoomRect.Margin     = new Thickness(_zoomStartPoint.X, _zoomStartPoint.Y, 0, 0);
        ZoomRect.Width      = 0;
        ZoomRect.Height     = 0;
        ZoomRect.Visibility = Visibility.Visible;

        ZoomOverlay.CaptureMouse();
        e.Handled = true;
    }

    private void ZoomOverlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isZoomDragging) return;

        var pos    = e.GetPosition(ZoomOverlay);
        var x      = System.Math.Min(pos.X, _zoomStartPoint.X);
        var y      = System.Math.Min(pos.Y, _zoomStartPoint.Y);
        var width  = System.Math.Abs(pos.X - _zoomStartPoint.X);
        var height = System.Math.Abs(pos.Y - _zoomStartPoint.Y);

        ZoomRect.Margin = new Thickness(x, y, 0, 0);
        ZoomRect.Width  = width;
        ZoomRect.Height = height;
        e.Handled = true;
    }

    private void ZoomOverlay_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isZoomDragging) return;

        _isZoomDragging = false;
        ZoomOverlay.ReleaseMouseCapture();
        ZoomOverlay.Visibility  = Visibility.Collapsed;
        ZoomRect.Visibility     = Visibility.Collapsed;
        Mouse.OverrideCursor    = null;

        var endPoint = e.GetPosition(ZoomOverlay);
        double dx = System.Math.Abs(endPoint.X - _zoomStartPoint.X);
        double dy = System.Math.Abs(endPoint.Y - _zoomStartPoint.Y);

        if (dx > 5 && dy > 5)
            ApplyZoomWindow(_zoomStartPoint, endPoint);

        e.Handled = true;
    }

    private void ApplyZoomWindow(Point screenStart, Point screenEnd)
    {
        // Screen → World
        Matrix s2w;
        try   { s2w = WorldTransform.Matrix; s2w.Invert(); }
        catch { return; }

        var w0 = s2w.Transform(screenStart);
        var w1 = s2w.Transform(screenEnd);

        double minX = System.Math.Min(w0.X, w1.X);
        double maxX = System.Math.Max(w0.X, w1.X);
        double minY = System.Math.Min(w0.Y, w1.Y);
        double maxY = System.Math.Max(w0.Y, w1.Y);

        double worldW = maxX - minX;
        double worldH = maxY - minY;
        if (worldW < 1e-9 || worldH < 1e-9) return;

        double vpW = ViewportGrid.ActualWidth;
        double vpH = ViewportGrid.ActualHeight;
        if (vpW <= 0 || vpH <= 0) return;

        double scale = System.Math.Min(vpW / worldW, vpH / worldH);
        double midX  = (minX + maxX) / 2.0;
        double midY  = (minY + maxY) / 2.0;

        // Set matrix elements directly — Translate() after Scale() would scale the offsets
        WorldTransform.Matrix = new Matrix(
            scale,
            0,
            0,
            -scale,
            vpW / 2.0 - midX * scale,
            vpH / 2.0 + midY * scale);

        if (DataContext is ShellViewModel vm)
            vm.CurrentViewScale = scale;
    }

    private void ZoomIn()
    {
        var matrix = WorldTransform.Matrix;
        var center = new Point(ViewportGrid.ActualWidth / 2, ViewportGrid.ActualHeight / 2);
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
        double targetScale = 10.0;
        
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
        if (vm == null || (vm.Points.Count == 0 && vm.Figures.Count == 0 && vm.StructureGraphics.Count == 0)) return;
        
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

        // Include JEA Structure Graphics (manholes, valves, hydrants, fittings, etc.)
        foreach (var s in vm.StructureGraphics)
        {
            if (s.Easting < minX) minX = s.Easting;
            if (s.Easting > maxX) maxX = s.Easting;
            if (s.Northing < minY) minY = s.Northing;
            if (s.Northing > maxY) maxY = s.Northing;
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

        // Use ViewportGrid — ViewportCanvas has no layout size (it's transformed/infinite)
        double vpWidth  = ViewportGrid.ActualWidth;
        double vpHeight = ViewportGrid.ActualHeight;
        
        if (vpWidth <= 0 || vpHeight <= 0) return;
        
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
        
        // Build matrix directly — Translate() after Scale() multiplies offsets by M11/M22
        WorldTransform.Matrix = new Matrix(
            scale,
            0,
            0,
            -scale,                       // Y-flip
            vpWidth  / 2.0 - midX * scale,
            vpHeight / 2.0 + midY * scale); // +midY because M22 = -scale
        vm.CurrentViewScale = scale;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(ViewportGrid);
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

    private void btnToggleLineNumbers_Click(object sender, RoutedEventArgs e)
    {
        if (btnToggleLineNumbers.IsChecked == true)
        {
            txtPipingLineNumbers.Visibility = Visibility.Visible;
            UpdateLineNumbers();
        }
        else
        {
            txtPipingLineNumbers.Visibility = Visibility.Collapsed;
        }
    }

    private void txtPipingScript_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (txtPipingLineNumbers.Visibility == Visibility.Visible)
        {
            UpdateLineNumbers();
        }
    }

    private void txtPipingScript_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
    {
        if (txtPipingLineNumbers.Visibility == Visibility.Visible)
        {
            txtPipingLineNumbers.ScrollToVerticalOffset(e.VerticalOffset);
        }
    }

    private void UpdateLineNumbers()
    {
        int lineCount = txtPipingScript.LineCount;
        if (lineCount == 0) lineCount = 1;
        var sb = new System.Text.StringBuilder();
        for (int i = 1; i <= lineCount; i++)
        {
            sb.AppendLine(i.ToString());
        }
        txtPipingLineNumbers.Text = sb.ToString();
    }

    private void ExtendSelectionToFullLines(System.Windows.Controls.TextBox txt)
    {
        int start = txt.SelectionStart;
        int length = txt.SelectionLength;
        int end = start + length;
        
        int startOfSelectionLine = 0;
        for (int i = start - 1; i >= 0; i--)
        {
            if (txt.Text[i] == '\n')
            {
                startOfSelectionLine = i + 1;
                break;
            }
        }

        int endOfSelectionLine = txt.Text.Length;
        int checkEnd = end > 0 && txt.Text[end - 1] == '\n' ? end - 1 : end;
        
        for (int i = checkEnd; i < txt.Text.Length; i++)
        {
            if (txt.Text[i] == '\r' || txt.Text[i] == '\n')
            {
                endOfSelectionLine = i;
                break;
            }
        }
        
        txt.Select(startOfSelectionLine, endOfSelectionLine - startOfSelectionLine);
    }

    private void btnCommentPipingScript_Click(object sender, RoutedEventArgs e)
    {
        ExtendSelectionToFullLines(txtPipingScript);
        
        int selStart = txtPipingScript.SelectionStart;
        string selectedText = txtPipingScript.SelectedText;
        
        if (string.IsNullOrEmpty(selectedText)) return;
        
        var lines = selectedText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        var sb = new System.Text.StringBuilder();
        for(int i = 0; i < lines.Length; i++)
        {
            sb.Append("//" + lines[i]);
            if (i < lines.Length - 1) sb.AppendLine();
        }
        
        txtPipingScript.SelectedText = sb.ToString();
        txtPipingScript.Select(selStart, txtPipingScript.SelectedText.Length);
    }

    private void btnUncommentPipingScript_Click(object sender, RoutedEventArgs e)
    {
        ExtendSelectionToFullLines(txtPipingScript);
        
        int selStart = txtPipingScript.SelectionStart;
        string selectedText = txtPipingScript.SelectedText;
        
        if (string.IsNullOrEmpty(selectedText)) return;
        
        var lines = selectedText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        var sb = new System.Text.StringBuilder();
        for(int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("//"))
            {
                sb.Append(lines[i].Substring(2));
            }
            else
            {
                sb.Append(lines[i]);
            }
            if (i < lines.Length - 1) sb.AppendLine();
        }

        txtPipingScript.SelectedText = sb.ToString();
        txtPipingScript.Select(selStart, txtPipingScript.SelectedText.Length);
    }

    private void btnMixPipingScript_Click(object sender, RoutedEventArgs e)
    {
        string header = "COGO-ENGINE-OFF\r\nPIPE-ENGINE-ON\r\n";
        
        string currentText = txtPipingScript.Text ?? "";
        
        if (currentText.TrimStart().StartsWith("COGO-ENGINE-OFF", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        txtPipingScript.Text = header + currentText;
        txtPipingScript.Focus();
        txtPipingScript.CaretIndex = header.Length;
    }

    // ── Symbol click-to-inspect ───────────────────────────────────────────────
    private void StructureSymbol_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.FrameworkElement fe &&
            fe.Tag is StructureViewModel sym)
        {
            sym.SelectCommand?.Execute(null);
            e.Handled = true;
        }
    }
}
