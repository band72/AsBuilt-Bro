using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using RCS.Data;
using RCS.Data.Entities;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views
{
    public partial class FiguresWindow : Window
    {
        private AppDbContext _context;
        private string _projectId;
        private List<Figure> _allFigures = new();
        private Action _onClosedCallback;
        public FiguresWindow(string projectId, Action onClosedCallback = null)
        {
            InitializeComponent();
            _projectId = projectId;
            _context = new AppDbContext();
            _onClosedCallback = onClosedCallback;
            
            LoadFigures();
        }

        private void LoadFigures()
        {
            if (string.IsNullOrEmpty(_projectId)) return;

            _allFigures = _context.Figures
                .Include(f => f.Vertices)
                .ThenInclude(v => v.Point)
                .Where(f => f.ProjectId == _projectId)
                .ToList();

            UpdateFilterOptions();
        }

        private void UpdateFilterOptions()
        {
            var prefixes = _allFigures
                .Where(f => !string.IsNullOrEmpty(f.Name))
                .Select(f => f.Name.Length >= 2 ? f.Name.Substring(0, 2).ToUpper() : f.Name.ToUpper())
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            prefixes.Insert(0, "All");
            NamePrefixFilterComboBox.ItemsSource = prefixes;
            NamePrefixFilterComboBox.SelectedIndex = 0;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (NamePrefixFilterComboBox == null || FiguresDataGrid == null) return;

            string filter = NamePrefixFilterComboBox.SelectedItem as string ?? "All";

            var filtered = filter == "All" 
                ? _allFigures.ToList() 
                : _allFigures.Where(f => !string.IsNullOrEmpty(f.Name) && f.Name.ToUpper().StartsWith(filter)).ToList();

            FiguresDataGrid.ItemsSource = null;
            FiguresDataGrid.ItemsSource = filtered;
        }

        private void NamePrefixFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _context.SaveChanges(); // Persist any pending data grid edits
            }
            catch { }
            
            _onClosedCallback?.Invoke();
            MessageBox.Show("Figures synchronized with Installed Assets.", "Sync Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (FiguresDataGrid.SelectedItem is Figure figure)
            {
                var result = MessageBox.Show($"Are you sure you want to delete figure '{figure.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Use raw SQL string replacement directly to ensure SQLite binds without parameter mapping errors
                        string fid = figure.Id.Replace("'", "''"); // escape just in case
                        _context.Database.ExecuteSqlRaw($"DELETE FROM FigureVertices WHERE FigureId = '{fid}'");
                        _context.Database.ExecuteSqlRaw($"DELETE FROM Figures WHERE Id = '{fid}'");

                        try
                        {
                            _context.Entry(figure).State = EntityState.Detached;
                        } catch { }

                        _allFigures.Remove(figure);
                        
                        ApplyFilter(); // Make sure the window local Grid refreshes immediately natively
                        UpdateFilterOptions(); // Then update filter list
                        FigureCanvas.Children.Clear();
                        
                        // Finally trigger the callback to load exactly what's actually in SQLite into the main view
                        _onClosedCallback?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting figure: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a figure to delete.", "No Figure Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void FiguresDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawFigure();
        }

        private void FigureCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawFigure();
        }

        private void DrawFigure()
        {
            FigureCanvas.Children.Clear();
            if (FiguresDataGrid.SelectedItem is not Figure figure || figure.Vertices == null || figure.Vertices.Count == 0)
                return;

            var orderedVertices = figure.Vertices.OrderBy(v => v.OrderIndex).ToList();

            // Extract valid points (ignore vertices that somehow lost their Point reference)
            var points = orderedVertices.Where(v => v.Point != null).Select(v => new Point(v.Point.Easting, v.Point.Northing)).ToList();
            if (points.Count < 2) return; // Cannot draw line without at least 2 points
            if (figure.IsClosed && points.Count >= 3) points.Add(points.First()); // close polygon

            // Compute Bounding Box
            double minEasting = points.Min(p => p.X);
            double maxEasting = points.Max(p => p.X);
            double minNorthing = points.Min(p => p.Y);
            double maxNorthing = points.Max(p => p.Y);

            double widthRange = maxEasting - minEasting;
            double heightRange = maxNorthing - minNorthing;
            if (widthRange == 0) widthRange = 1;
            if (heightRange == 0) heightRange = 1;

            double padding = 20; // 20px padding
            double canvasWidth = FigureCanvas.ActualWidth - (2 * padding);
            if(canvasWidth < 0) canvasWidth = FigureCanvas.ActualWidth;
            double canvasHeight = FigureCanvas.ActualHeight - (2 * padding);
            if(canvasHeight < 0) canvasHeight = FigureCanvas.ActualHeight;

            double scaleX = canvasWidth / widthRange;
            double scaleY = canvasHeight / heightRange;
            double scale = Math.Min(scaleX, scaleY);
            if (double.IsNaN(scale) || double.IsInfinity(scale)) scale = 1;

            // Offset to center
            double scaledWidth = widthRange * scale;
            double scaledHeight = heightRange * scale;
            double offsetX = padding + (canvasWidth - scaledWidth) / 2.0;
            double offsetY = padding + (canvasHeight - scaledHeight) / 2.0;

            PathGeometry pathGeom = new PathGeometry();
            PathFigure pathFig = new PathFigure
            {
                StartPoint = new Point(
                    offsetX + (points[0].X - minEasting) * scale,
                    FigureCanvas.ActualHeight - (offsetY + (points[0].Y - minNorthing) * scale)) // Flip Y for WPF (Northing typically goes up mathematically)
            };

            for (int i = 1; i < points.Count; i++)
            {
                var pt = new Point(
                    offsetX + (points[i].X - minEasting) * scale,
                    FigureCanvas.ActualHeight - (offsetY + (points[i].Y - minNorthing) * scale)
                );
                // Currently draws straight lines. Could add bulge (arc) logic here if v.Bulge != 0.
                pathFig.Segments.Add(new LineSegment(pt, true));
            }

            pathGeom.Figures.Add(pathFig);

            Path path = new Path
            {
                Stroke = Brushes.Cyan,
                StrokeThickness = 2,
                Data = pathGeom
            };

            FigureCanvas.Children.Add(path);

            // Add Ellipses for Vertices
            foreach (var pt in points)
            {
                double px = offsetX + (pt.X - minEasting) * scale;
                double py = FigureCanvas.ActualHeight - (offsetY + (pt.Y - minNorthing) * scale);
                Ellipse el = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Yellow,
                    Margin = new Thickness(px - 3, py - 3, 0, 0)
                };
                FigureCanvas.Children.Add(el);
            }
        }

        private void FiguresDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is Figure figure)
                {
                    try 
                    {
                        if (_context.Entry(figure).State == EntityState.Detached)
                            return;

                        // Basic save
                        _context.Entry(figure).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                    catch { } // Ignore exceptions to prevent UI crashes if item is already deleted or detached
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            _onClosedCallback?.Invoke();
            base.OnClosed(e);
        }
    }
}
