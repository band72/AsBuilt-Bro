using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RCS.Cogo.Wpf.ViewModels;
// AsBuiltWorkspaceView and AsBuiltWorkspaceViewModel share this assembly;
// no additional using needed — they live in RCS.Cogo.Wpf.Views / .ViewModels.

namespace RCS.Cogo.Wpf.Views;

public partial class ShellWindow : Window
{
    private Point _lastMousePosition;
    private bool _isDragging;
    private bool _isZoomWindowMode;
    private bool _isZoomDragging;
    private Point _zoomStartPoint;

    // ── As-Built workspace (lazy-initialised on first tab activation) ─────────
    private AsBuiltWorkspaceView?         _asBuiltView;
    private AsBuiltWorkspaceViewModel?    _asBuiltVm;
    private const int AsBuiltTabIndex = 9;   // 0-based index of the 🏗 As-Built tab

    public ShellWindow()
    {
        InitializeComponent();
        var vm = new ShellViewModel();
        DataContext = vm;
        
        vm.ZoomExtentsRequested  += (s, e) => ZoomExtents();
        vm.ZoomInRequested        += (s, e) => ZoomIn();
        vm.ZoomOutRequested       += (s, e) => ZoomOut();
        vm.ZoomWindowRequested    += (s, e) => ActivateZoomWindow();
        vm.ZoomToPointRequested   += (s, target) => ZoomToPoint(target);
        vm.ViewRestoreRequested   += (s, matrixArray) => RestoreView(matrixArray);

        // Initialize Default View
        var matrix = new Matrix();
        matrix.Scale(1, -1);
        matrix.Translate(-4400, 5400);
        WorldTransform.Matrix = matrix;
        WorldTransform.Matrix = matrix;

        // ── Show the Welcome screen once the main window is ready ────────────
        Loaded += OnFirstLoad;
    }

    private void OnFirstLoad(object sender, RoutedEventArgs e)
    {
        Loaded -= OnFirstLoad;   // fire once only
        if (DataContext is not ShellViewModel vm) return;

        var welcome = new WelcomeWindow(vm) { Owner = this };
        if (welcome.ShowDialog() != true) return;

        switch (welcome.SelectedAction)
        {
            case WelcomeWindow.WelcomeAction.New:
                vm.NewProjectCommand?.Execute(null);
                break;
            case WelcomeWindow.WelcomeAction.Open:
                vm.OpenProjectCommand?.Execute(null);
                break;
            case WelcomeWindow.WelcomeAction.ImportData:
                vm.ImportBatchCommand?.Execute(null);
                break;
            case WelcomeWindow.WelcomeAction.OpenRecent:
                if (welcome.SelectedRecentFile != null)
                    vm.OpenRecentFileCommand?.Execute(welcome.SelectedRecentFile);
                break;
            case WelcomeWindow.WelcomeAction.NewAsBuilt:
                // Switch directly to the As-Built tab index; the user then clicks New Job in the workspace
                NavigateToAsBuilt();
                return; // skip SyncProjectToAsBuiltJob — wizard creates its own job


        }

        // ── Push new/opened project into the As-Built workspace ──────────────
        // When the user creates or opens a project from the Welcome screen,
        // we construct a fresh AsBuiltJob seeded with the project identity so
        // the workflow navigator starts from the correct state.
        SyncProjectToAsBuiltJob();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is ShellViewModel vm && vm.IsDirty)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                vm.SaveProjectCommand?.Execute(null);
                // If still dirty after save (e.g. save cancelled), abort close
                if (vm.IsDirty) { e.Cancel = true; return; }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            // No → fall through and allow close
        }
        base.OnClosing(e);
    }

    private void RestoreView(double[] m)
    {
        if (m == null || m.Length != 6) return;
        WorldTransform.Matrix = new Matrix(m[0], m[1], m[2], m[3], m[4], m[5]);
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = m[0];
    }

    private void SaveCurrentView()
    {
        if (DataContext is ShellViewModel vm && vm.CurrentProject != null)
        {
            var m = WorldTransform.Matrix;
            vm.CurrentProject.SavedViewMatrix = new double[] { m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY };
        }
    }

    // ── Zoom Window (activated by WIN button) ────────────────────────────────
    private void ActivateZoomWindow()
    {
        _isZoomWindowMode = true;
        _isZoomDragging   = false;
        Mouse.OverrideCursor = Cursors.Cross;
        Log("[ZOOM] WIN mode activated");
    }

    private void ViewportGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isZoomWindowMode) return;

        _zoomStartPoint = e.GetPosition(ViewportGrid);
        _isZoomDragging = true;

        ZoomRect.Margin     = new Thickness(_zoomStartPoint.X, _zoomStartPoint.Y, 0, 0);
        ZoomRect.Width      = 0;
        ZoomRect.Height     = 0;
        ZoomRect.Visibility = Visibility.Visible;

        ViewportGrid.CaptureMouse();
        e.Handled = true;
        Log($"[ZOOM] Mouse down at {_zoomStartPoint}");
    }

    private void ViewportGrid_PreviewMouseMove(object sender, MouseEventArgs e)
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
            SaveCurrentView();
        }
        else if (_isZoomDragging)
        {
            var pos    = e.GetPosition(ViewportGrid);
            var x      = System.Math.Min(pos.X, _zoomStartPoint.X);
            var y      = System.Math.Min(pos.Y, _zoomStartPoint.Y);
            var width  = System.Math.Abs(pos.X - _zoomStartPoint.X);
            var height = System.Math.Abs(pos.Y - _zoomStartPoint.Y);

            ZoomRect.Margin = new Thickness(x, y, 0, 0);
            ZoomRect.Width  = width;
            ZoomRect.Height = height;
            e.Handled = true;
        }
    }

    private void ViewportGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isZoomDragging) return;

        _isZoomDragging = false;
        
        var endPoint = e.GetPosition(ViewportGrid);

        ViewportGrid.ReleaseMouseCapture();
        ZoomRect.Visibility     = Visibility.Collapsed;
        
        // Only exit WIN mode once the zoom completes successfully or is aborted
        _isZoomWindowMode       = false;
        Mouse.OverrideCursor    = null;

        double dx = System.Math.Abs(endPoint.X - _zoomStartPoint.X);
        double dy = System.Math.Abs(endPoint.Y - _zoomStartPoint.Y);

        Log($"[ZOOM] Mouse up at {endPoint}, dx={dx:F1}, dy={dy:F1}");

        if (dx > 5 && dy > 5)
            ApplyZoomWindow(_zoomStartPoint, endPoint);

        e.Handled = true;
    }

    private void ApplyZoomWindow(Point screenStart, Point screenEnd)
    {
        // Screen → World
        Matrix s2w;
        var currentMatrix = WorldTransform.Matrix;
        
        Log($"[ZOOM] Matrix before invert: M11={currentMatrix.M11:F4}, M12={currentMatrix.M12:F4}, M21={currentMatrix.M21:F4}, M22={currentMatrix.M22:F4}, Ox={currentMatrix.OffsetX:F1}, Oy={currentMatrix.OffsetY:F1}");
        
        if (!currentMatrix.HasInverse)
        {
            Log("[ZOOM] Matrix is singular! Resetting to default state.");
            currentMatrix = new Matrix(1, 0, 0, -1, 0, 0); // fallback identity with Y-flip
            WorldTransform.Matrix = currentMatrix;
        }

        try   
        { 
            s2w = currentMatrix;
            s2w.Invert(); 
        }
        catch (Exception ex) { Log($"[ZOOM] Invert failed: {ex.Message}"); return; }

        var w0 = s2w.Transform(screenStart);
        var w1 = s2w.Transform(screenEnd);

        double minX = System.Math.Min(w0.X, w1.X);
        double maxX = System.Math.Max(w0.X, w1.X);
        double minY = System.Math.Min(w0.Y, w1.Y);
        double maxY = System.Math.Max(w0.Y, w1.Y);

        double worldW = maxX - minX;
        double worldH = maxY - minY;
        
        Log($"[ZOOM] world bounds ({minX:F1},{minY:F1}) → ({maxX:F1},{maxY:F1})  size={worldW:F2}x{worldH:F2}");

        if (worldW < 1e-9 || worldH < 1e-9) 
        {
            Log("[ZOOM] skipped — degenerate world rect");
            return;
        }

        double vpW = ViewportGrid.ActualWidth;
        double vpH = ViewportGrid.ActualHeight;
        
        Log($"[ZOOM] viewport size {vpW:F1} x {vpH:F1}");
        
        if (vpW <= 0 || vpH <= 0)
        {
            Log("[ZOOM] skipped — zero viewport size");
            return;
        }

        double scale = System.Math.Min(vpW / worldW, vpH / worldH);
        if (scale < 1e-6) scale = 1e-6; // Prevent singular matrix
        
        double midX  = (minX + maxX) / 2.0;
        double midY  = (minY + maxY) / 2.0;

        // Set matrix elements directly
        var m = new Matrix(
            scale,
            0,
            0,
            -scale,
            vpW / 2.0 - midX * scale,
            vpH / 2.0 + midY * scale);
            
        WorldTransform.Matrix = m;
        Log($"[ZOOM] applied scale={scale:F4} mid=({midX:F1},{midY:F1})");

        if (DataContext is ShellViewModel vm)
            vm.CurrentViewScale = scale;
            
        SaveCurrentView();
    }

    // Helper — writes to the ViewModel output log (visible in the app)
    private void Log(string msg)
    {
        if (DataContext is ShellViewModel vm)
            vm.CommandLog.Add(msg);
            
        try
        {
            System.IO.File.AppendAllText("zoomlog.txt", $"{System.DateTime.Now:HH:mm:ss.fff} {msg}\n");
        }
        catch { }
    }

    private void ZoomIn()
    {
        var matrix = WorldTransform.Matrix;
        var center = new Point(ViewportGrid.ActualWidth / 2, ViewportGrid.ActualHeight / 2);
        matrix.ScaleAt(1.05, 1.05, center.X, center.Y);
        WorldTransform.Matrix = matrix;
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = matrix.M11;
        SaveCurrentView();
    }

    private void ZoomOut()
    {
        var matrix = WorldTransform.Matrix;
        var center = new Point(ViewportCanvas.ActualWidth / 2, ViewportCanvas.ActualHeight / 2);
        matrix.ScaleAt(1.0 / 1.05, 1.0 / 1.05, center.X, center.Y);
        WorldTransform.Matrix = matrix;
        if (DataContext is ShellViewModel vm) vm.CurrentViewScale = matrix.M11;
        SaveCurrentView();
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
        SaveCurrentView();
    }

    private void ZoomExtents()
    {
        var vm = DataContext as ShellViewModel;
        if (vm == null || (vm.Points.Count == 0 && vm.Figures.Count == 0 && vm.StructureGraphics.Count == 0)) return;
        
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        
        bool IsValidCoord(double v) => !double.IsNaN(v) && !double.IsInfinity(v) && System.Math.Abs(v) < 1e9;

        // Include Points
        foreach (var p in vm.Points)
        {
            if (IsValidCoord(p.Easting))
            {
                if (p.Easting < minX) minX = p.Easting;
                if (p.Easting > maxX) maxX = p.Easting;
            }
            if (IsValidCoord(p.Northing))
            {
                if (p.Northing < minY) minY = p.Northing;
                if (p.Northing > maxY) maxY = p.Northing;
            }
        }
        
        // Include Figures
        foreach (var f in vm.Figures)
        {
            foreach (var pt in f.Points)
            {
                if (IsValidCoord(pt.X))
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                }
                if (IsValidCoord(pt.Y))
                {
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
            }
        }

        // Include JEA Structure Graphics (manholes, valves, hydrants, fittings, etc.)
        foreach (var s in vm.StructureGraphics)
        {
            if (IsValidCoord(s.Easting))
            {
                if (s.Easting < minX) minX = s.Easting;
                if (s.Easting > maxX) maxX = s.Easting;
            }
            if (IsValidCoord(s.Northing))
            {
                if (s.Northing < minY) minY = s.Northing;
                if (s.Northing > maxY) maxY = s.Northing;
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

        // Use ViewportGrid — ViewportCanvas has no layout size (it's transformed/infinite)
        double vpWidth  = ViewportGrid.ActualWidth;
        double vpHeight = ViewportGrid.ActualHeight;
        
        if (vpWidth <= 0 || vpHeight <= 0) return;
        
        // Calculate Scale
        double scaleX = vpWidth / width;
        double scaleY = vpHeight / height;
        double scale = Math.Min(scaleX, scaleY);
        if (scale < 1e-6) scale = 1e-6; // Prevent singular matrix
        
        // Center
        double midX = (minX + maxX) / 2.0;
        double midY = (minY + maxY) / 2.0;
        
        // Apply Transform
        // Screen X = (World X - midX) * scale + vpWidth/2
        // Screen Y = (World Y - midY) * -scale + vpHeight/2  (Flip Y)
        
        // Build matrix directly — Translate() after Scale() multiplies offsets by M11/M22
        var matrixToApply = new Matrix(
            scale,
            0,
            0,
            -scale,                       // Y-flip
            vpWidth  / 2.0 - midX * scale,
            vpHeight / 2.0 + midY * scale); // +midY because M22 = -scale
            
        WorldTransform.Matrix = matrixToApply;
        Log($"[ZOOM EXTENTS] vpW={vpWidth:F1}, vpH={vpHeight:F1}, width={width:F1}, height={height:F1}, scale={scale:F4}, M11={matrixToApply.M11:F4}, M22={matrixToApply.M22:F4}");
        
        vm.CurrentViewScale = scale;
        SaveCurrentView();
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
        SaveCurrentView();
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





    private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Only trigger if the event source is the TabControl itself
        if (e.OriginalSource is not System.Windows.Controls.TabControl) return;
        if (DataContext is not ShellViewModel vm) return;

        // ── Auto-focus Cogo input box ─────────────────────────────────────────
        if (vm.SelectedTabIndex == 1)
        {
            Dispatcher.BeginInvoke(new System.Action(() => CogoInputTextBox.Focus()),
                System.Windows.Threading.DispatcherPriority.Input);
        }

        // ── As-Built workspace overlay ────────────────────────────────────────
        bool isAsBuilt = vm.SelectedTabIndex == AsBuiltTabIndex;
        AsBuiltTabOverlay.Visibility = isAsBuilt ? Visibility.Visible : Visibility.Collapsed;

        if (isAsBuilt)
            EnsureAsBuiltViewInitialized(vm);
    }

    /// <summary>
    /// Lazily creates the <see cref="AsBuiltWorkspaceView"/> and its ViewModel
    /// on the first time the As-Built tab is activated. Injects the existing
    /// <see cref="ViewportCanvas"/> so the workspace right-pane can render into
    /// the same canvas as the rest of the application.
    /// </summary>
    private void EnsureAsBuiltViewInitialized(ShellViewModel shellVm)
    {
        if (_asBuiltView != null) return;   // already built

        _asBuiltVm   = new AsBuiltWorkspaceViewModel();
        _asBuiltView = new AsBuiltWorkspaceView { DataContext = _asBuiltVm };

        // ── Canvas injection ──────────────────────────────────────────────────
        _asBuiltView.InjectCanvas(ViewportCanvas);

        // Put the view into the overlay slot
        AsBuiltOverlayContent.Content = _asBuiltView;

        // ── Production UX: show navigator + diagnostics when workspace is active ──
        WorkflowNavigatorSidebar.Visibility = Visibility.Visible;
        DiagnosticsPaneOverlay.Visibility   = Visibility.Visible;

        // Seed job identity from the active project
        SyncProjectToAsBuiltJob();
    }

    private void SyncProjectToAsBuiltJob()
    {
        if (_asBuiltVm == null || DataContext is not ShellViewModel shell) return;

        var job = new RCS.Piping.Core.Workflow.AsBuiltJob();

        var proj = shell.CurrentProject;
        job.Identity.JobNumber    = proj?.AvailNo      ?? string.Empty;
        job.Identity.ClientName   = proj?.ProjectName  ?? string.Empty;
        job.Identity.UtilityOwner = proj?.Utility      ?? string.Empty;

        _asBuiltVm.LoadJob(job);

        // ── Wire the job to ShellViewModel for navigator/diagnostics refresh ──
        shell.ActiveJob = job;
        shell.ShowDashboard = false;           // hide dashboard, show workspace
        shell.RefreshWorkflowState();
        shell.Diagnostics.Audit($"Job loaded: {job.Identity.JobNumber} — Rev {job.Identity.RevisionNumber}");
    }


    private void NavigateToAsBuilt()
    {
        if (DataContext is ShellViewModel vm)
            vm.SelectedTabIndex = AsBuiltTabIndex;
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

    // ── As-Built Demo menu handlers ───────────────────────────────────────────

    private static readonly string _demoFolder = ResolveDemoFolder();

    private static string ResolveDemoFolder()
    {
        // Installed path: {app}\AsBuiltDemo  (bundled by Inno Setup)
        var installed = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "AsBuiltDemo");
        if (System.IO.Directory.Exists(installed)) return installed;

        // Dev / manual fallback: Desktop\AsBuilt_Demo\JEA-2024-W-04471_Rev1_04122025
        return System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "AsBuilt_Demo", "JEA-2024-W-04471_Rev1_04122025");
    }

    private void OpenAsBuiltDemoFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!System.IO.Directory.Exists(_demoFolder))
        {
            MessageBox.Show(
                $"Demo folder not found:\n{_demoFolder}\n\nRe-run the 'Give me a prebuilt as-built project' command to regenerate it.",
                "AsBuilt Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        System.Diagnostics.Process.Start("explorer.exe", $"\"{_demoFolder}\"");
    }

    private void OpenAsBuiltDemoReadme_Click(object sender, RoutedEventArgs e)
    {
        var file = System.IO.Path.Combine(_demoFolder, "README_LOAD_ORDER.txt");
        LaunchDemoFile(file);
    }

    private void LoadAsBuiltDemoPnezd_Click(object sender, RoutedEventArgs e)
    {
        var file = System.IO.Path.Combine(_demoFolder, "W-04471_PNEZD.csv");
        LoadDemoFileIntoEditor(file);
    }

    private void LoadAsBuiltDemoCogo_Click(object sender, RoutedEventArgs e)
    {
        var file = System.IO.Path.Combine(_demoFolder, "W-04471_COGO.cogo");
        LoadDemoFileIntoEditor(file);
    }

    /// <summary>Opens the file with the OS default application (Notepad, etc.).</summary>
    private void LaunchDemoFile(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            MessageBox.Show($"File not found:\n{path}", "AsBuilt Demo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    /// <summary>Loads a text file into the Script Editor TextBox directly.</summary>
    private void LoadDemoFileIntoEditor(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            MessageBox.Show($"File not found:\n{path}", "AsBuilt Demo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var content = System.IO.File.ReadAllText(path);

        // Prefer the Piping/COGO script TextBox if visible; fall back to any named editor.
        if (txtPipingScript != null)
        {
            txtPipingScript.Text = content;
            txtPipingScript.Focus();
            txtPipingScript.CaretIndex = 0;

            // Switch VM to tab 0 (Script Editor) so the user sees the loaded content.
            if (DataContext is ShellViewModel vm)
                vm.SelectedTabIndex = 0;
        }
        else
        {
            MessageBox.Show("Script editor not available on the current tab.\n\n" +
                            "Navigate to the Script Editor tab first, then try again.",
                            "AsBuilt Demo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
