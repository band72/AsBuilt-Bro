using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Cogo.Core.Primitives;
using System.Threading.Tasks;

namespace RCS.Cogo.Wpf.Views
{
    public partial class PointsAlongPipeWindow : Window
    {
        private ShellViewModel _shellVm;
        private InstalledAssetsViewModel _assetsVm;

        public PointsAlongPipeWindow(ShellViewModel shellVm)
        {
            InitializeComponent();
            _shellVm = shellVm;
            _assetsVm = shellVm.InstalledAssets;
        }

        private async void Compute_Click(object sender, RoutedEventArgs e)
        {
            if (!_shellVm.EnsureActiveProject())
            {
                Close();
                return;
            }

            // Get selected disciplines
            var selectedDisciplines = new List<string>();
            if (CbWA.IsChecked == true) selectedDisciplines.Add("WA");
            if (CbWW.IsChecked == true) selectedDisciplines.Add("WW");
            if (CbRC.IsChecked == true) selectedDisciplines.Add("RC");
            if (CbGS.IsChecked == true) selectedDisciplines.Add("GS");
            if (CbEL.IsChecked == true) selectedDisciplines.Add("EL");
            if (CbCH.IsChecked == true) selectedDisciplines.Add("CH");
            if (CbDR.IsChecked == true) selectedDisciplines.Add("DR");

            // Get selected distance
            double distance = 50;
            switch (CmbDistance.SelectedIndex)
            {
                case 0: distance = 25; break;
                case 1: distance = 50; break;
                case 2: distance = 100; break;
                case 3: distance = 250; break;
                case 4: distance = 500; break;
            }

            int generatedCount = 0;

            foreach (var fig in _shellVm.Figures.ToList())
            {
                if (fig.Points == null || fig.Points.Count < 2) continue;
                
                // Usually Name determines discipline natively
                string nameLayer = (fig.Name ?? "").ToUpperInvariant();

                string? disc = null;
                foreach (var d in selectedDisciplines)
                {
                    if (nameLayer.Contains(d))
                    {
                        disc = d;
                        break;
                    }
                }
                
                if (disc == null) continue; // Not matching

                // Interpolate along the polyline figure
                double walkDist = 0;
                var polyPoints = fig.Points;
                
                // Add start point exactly
                await AddPointAsset(disc, polyPoints[0].Y, polyPoints[0].X);
                generatedCount++;

                for (int i = 0; i < polyPoints.Count - 1; i++)
                {
                    var p1 = polyPoints[i];
                    var p2 = polyPoints[i + 1];

                    double dx = p2.X - p1.X; // Easting
                    double dy = p2.Y - p1.Y; // Northing

                    double segmentLen = Math.Sqrt(dx * dx + dy * dy);
                    if (segmentLen == 0) continue;

                    double remainingSegment = segmentLen;
                    
                    while (walkDist + remainingSegment >= distance)
                    {
                        double needed = distance - walkDist;
                        double ratio = needed / segmentLen;
                        
                        double interpX = p1.X + dx * ratio;
                        double interpY = p1.Y + dy * ratio;

                        await AddPointAsset(disc, interpY, interpX);
                        generatedCount++;

                        walkDist = 0;
                        remainingSegment -= needed;
                        
                        // Advance p1 virtually to the new point
                        p1 = new System.Windows.Point(interpX, interpY);
                        dx = p2.X - p1.X;
                        dy = p2.Y - p1.Y;
                        segmentLen = remainingSegment;
                    }
                    walkDist += remainingSegment;
                }

                // Append the final endpoint if it's not super close to the last walked point
                if (walkDist > 1.0)
                {
                    var lastPt = polyPoints[polyPoints.Count - 1];
                    await AddPointAsset(disc, lastPt.Y, lastPt.X);
                    generatedCount++;
                }
            }

            _shellVm.CommandLog.Add($"[SYSTEM] Generated {generatedCount} points along pipes.");
            MessageBox.Show($"Successfully generated {generatedCount} points along selected pipelines.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        
        private async Task AddPointAsset(string discipline, double northing, double easting)
        {
            var pId = _shellVm.CurrentProject.Id.ToString();
            string pk = "P-" + Guid.NewGuid().ToString().Substring(0, 5);

            switch (discipline)
            {
                case "WA":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.WaterPoint { PartKey = pk, ProjectId = pId, Subtype = "Water Point Along Pipe", Discipline = "WA", Northing = northing, Easting = easting });
                    break;
                case "WW":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.WWPoint { PartKey = pk, ProjectId = pId, Subtype = "WW Point Along Pipe", Discipline = "WW", Northing = northing, Easting = easting });
                    break;
                case "RC":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.ReclaimedPoint { PartKey = pk, ProjectId = pId, Subtype = "RC Point Along Pipe", Discipline = "RC", Northing = northing, Easting = easting });
                    break;
                case "GS":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.GPoint { PartKey = pk, ProjectId = pId, Subtype = "Gas Point Along Pipe", Discipline = "GS", Northing = northing, Easting = easting });
                    break;
                case "EL":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.EPoint { PartKey = pk, ProjectId = pId, Subtype = "Electric Point Along Pipe", Discipline = "EL", Northing = northing, Easting = easting });
                    break;
                case "CH":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.ChilledPoint { PartKey = pk, ProjectId = pId, Subtype = "Chilled Point Along Pipe", Discipline = "CH", Northing = northing, Easting = easting });
                    break;
                case "DR":
                    await _assetsVm.AddItemAsync(new RCS.Data.Entities.STPoint { PartKey = pk, ProjectId = pId, Subtype = "Storm Point Along Pipe", Discipline = "DR", Northing = northing, Easting = easting });
                    break;
            }

            // optionally add a local UI marker
            _shellVm.StructureGraphics.Add(new StructureViewModel(
                pk, 
                new Point3D(northing, easting, 0), 
                $"{discipline} Point"
            ));
        }
    }
}
