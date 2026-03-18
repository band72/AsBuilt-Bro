using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RCS.Data.Entities;
using RCS.Cogo.Wpf.ViewModels;
using System.Collections.Generic;
using RCS.Data;

namespace RCS.Cogo.Wpf.Views
{
    public partial class AssetsMapControl : UserControl
    {
        private Point _dragStart;
        private Point _dragStartOffset;
        private double _currentScale = 1.0;
        private Point _currentTranslation = new Point(0, 0);
        
        // Cache calculated bounding box
        private double _minX, _minY, _maxX, _maxY;

        public AssetsMapControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshMap();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshMap();
            ZoomExtents();
        }

        public void RefreshMap()
        {
            MapCanvas.Children.Clear();

            var vm = DataContext as InstalledAssetsViewModel;
            if (vm == null || !vm.HasActiveProject) return;

            // Reset bounds
            _minX = double.MaxValue; _minY = double.MaxValue;
            _maxX = double.MinValue; _maxY = double.MinValue;

            // Figures
            foreach (var figure in vm.FigureAssets)
            {
                if (figure.Vertices == null || figure.Vertices.Count == 0) continue;
                
                var points = new PointCollection();
                foreach (var v in figure.Vertices.OrderBy(x => x.OrderIndex))
                {
                    if (v.Point == null) continue;
                    double x = v.Point.Easting;
                    double y = -v.Point.Northing; // Negate Y for screen coordinates
                    points.Add(new Point(x, y));
                    UpdateBounds(x, y);
                }

                if (points.Count > 1)
                {
                    var polyline = new Polyline
                    {
                        Points = points,
                        Stroke = new SolidColorBrush(Colors.LightGray),
                        StrokeThickness = 2.0 / _currentScale,
                        ToolTip = $"{figure.Name} ({figure.Layer})"
                    };
                    MapCanvas.Children.Add(polyline);
                }
            }

            // Pipes and Points
            DrawPipeCollection(vm.WaterPipes, Colors.Cyan);
            DrawPipeCollection(vm.WWGravityPipes, Colors.LimeGreen);
            DrawPipeCollection(vm.WWPressurePipes, Colors.LimeGreen);
            DrawPipeCollection(vm.ReclaimedPipes, Colors.Purple);
            DrawPipeCollection(vm.STGravityPipes, Colors.Orange); // Wait, storm is usually yellow or orange? Let's use Orange
            
            DrawPointCollection(vm.WaterPoints, Colors.Cyan, "Water Point");
            DrawPointCollection(vm.WWPoints, Colors.LimeGreen, "Sewer Point");
            DrawPointCollection(vm.Manholes, Colors.LimeGreen, "Manhole");
            DrawPointCollection(vm.WaterValves, Colors.Cyan, "Water Valve");

            // Re-apply scale to maintain line thickness relative to zoom
            UpdateLineThicknesses();
        }

        private void DrawPipeCollection<T>(IEnumerable<T> pipes, Color color) where T : InstalledAsset
        {
            var vm = DataContext as InstalledAssetsViewModel;
            if (vm == null) return;

            using var db = new AppDbContext();
            
            foreach (var pipe in pipes)
            {
                if (!string.IsNullOrEmpty(pipe.UpstreamPointId) && !string.IsNullOrEmpty(pipe.DownstreamPointId))
                {
                    var pStart = db.SurveyPoints.FirstOrDefault(p => p.Id == pipe.UpstreamPointId);
                    var pEnd = db.SurveyPoints.FirstOrDefault(p => p.Id == pipe.DownstreamPointId);

                    if (pStart != null && pEnd != null)
                    {
                        var points = new PointCollection();
                        double x1 = pStart.Easting; double y1 = -pStart.Northing;
                        double x2 = pEnd.Easting; double y2 = -pEnd.Northing;
                        
                        points.Add(new Point(x1, y1));
                        points.Add(new Point(x2, y2));
                        UpdateBounds(x1, y1); UpdateBounds(x2, y2);

                        var line = new Polyline
                        {
                            Points = points,
                            Stroke = new SolidColorBrush(color),
                            StrokeThickness = 2.0 / _currentScale,
                            ToolTip = $"Pipe: {pipe.PartKey} ({pipe.Size} {pipe.Material})"
                        };
                        MapCanvas.Children.Add(line);
                        continue;
                    }
                }

                // Fallback rendering
                if (pipe.Easting.HasValue && pipe.Northing.HasValue)
                {
                     DrawPoint(pipe.Easting.Value, -pipe.Northing.Value, color, pipe.PartKey ?? "");
                }
            }
        }

        private void DrawPointCollection<T>(IEnumerable<T> points, Color color, string label) where T : InstalledAsset
        {
            foreach (var pt in points)
            {
                if (pt.Easting.HasValue && pt.Northing.HasValue)
                {
                    double x = pt.Easting.Value;
                    double y = -pt.Northing.Value;
                    DrawPoint(x, y, color, $"{label}: {pt.PartKey}");
                }
            }
        }

        private void DrawPoint(double x, double y, Color color, string tooltip)
        {
            UpdateBounds(x, y);

            var ellipse = new Ellipse
            {
                Width = 10.0 / _currentScale,
                Height = 10.0 / _currentScale,
                Fill = new SolidColorBrush(color),
                ToolTip = tooltip
            };
            
            Canvas.SetLeft(ellipse, x - (ellipse.Width / 2));
            Canvas.SetTop(ellipse, y - (ellipse.Height / 2));
            MapCanvas.Children.Add(ellipse);
        }

        private void UpdateBounds(double x, double y)
        {
            if (x < _minX) _minX = x;
            if (x > _maxX) _maxX = x;
            if (y < _minY) _minY = y;
            if (y > _maxY) _maxY = y;
        }

        private void UpdateLineThicknesses()
        {
            foreach (UIElement child in MapCanvas.Children)
            {
                if (child is Polyline poly)
                {
                    poly.StrokeThickness = 2.0 / _currentScale;
                }
                else if (child is Ellipse ellipse)
                {
                    double size = 10.0 / _currentScale;
                    double oldLeft = Canvas.GetLeft(ellipse) + (ellipse.Width / 2);
                    double oldTop = Canvas.GetTop(ellipse) + (ellipse.Height / 2);
                    
                    ellipse.Width = size;
                    ellipse.Height = size;
                    
                    Canvas.SetLeft(ellipse, oldLeft - (size / 2));
                    Canvas.SetTop(ellipse, oldTop - (size / 2));
                }
            }
        }

        private void ZoomExtents()
        {
            if (_minX == double.MaxValue) return; // No points

            double paddingX = (_maxX - _minX) * 0.1;
            double paddingY = (_maxY - _minY) * 0.1;

            double width = _maxX - _minX + 2 * paddingX;
            double height = _maxY - _minY + 2 * paddingY;
            
            if (width < 0.1) width = 10;
            if (height < 0.1) height = 10;

            double scaleX = MapCanvas.ActualWidth / width;
            double scaleY = MapCanvas.ActualHeight / height;

            _currentScale = Math.Min(scaleX, scaleY);

            double centerX = _minX - paddingX + width / 2;
            double centerY = _minY - paddingY + height / 2;

            double viewCenterX = MapCanvas.ActualWidth / 2;
            double viewCenterY = MapCanvas.ActualHeight / 2;

            _currentTranslation = new Point(
                viewCenterX - (centerX * _currentScale),
                viewCenterY - (centerY * _currentScale)
            );

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            WorldTransform.Matrix = new Matrix(_currentScale, 0, 0, _currentScale, _currentTranslation.X, _currentTranslation.Y);
            UpdateLineThicknesses();
        }

        private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 1.2 : 1 / 1.2;
            Point mousePos = e.GetPosition(MapCanvas);

            double newScale = _currentScale * zoomFactor;

            _currentTranslation.X = mousePos.X - (mousePos.X - _currentTranslation.X) * zoomFactor;
            _currentTranslation.Y = mousePos.Y - (mousePos.Y - _currentTranslation.Y) * zoomFactor;

            _currentScale = newScale;
            ApplyTransform();
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(this);
            _dragStartOffset = _currentTranslation;
            MapCanvas.CaptureMouse();
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MapCanvas.ReleaseMouseCapture();
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MapCanvas.IsMouseCaptured)
            {
                Point currentPos = e.GetPosition(this);
                Vector diff = currentPos - _dragStart;

                _currentTranslation.X = _dragStartOffset.X + diff.X;
                _currentTranslation.Y = _dragStartOffset.Y + diff.Y;

                ApplyTransform();
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentScale *= 1.2;
            ApplyTransform();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _currentScale /= 1.2;
            ApplyTransform();
        }

        private void ZoomExtents_Click(object sender, RoutedEventArgs e)
        {
            ZoomExtents();
        }
    }
}
