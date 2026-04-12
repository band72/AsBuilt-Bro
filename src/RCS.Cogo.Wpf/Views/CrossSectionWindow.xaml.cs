using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using RCS.Alignments.Core;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.ViewModels
{
    /// <summary>Dims a brush to ~15% opacity for the CUT/FILL badge background.</summary>
    public class BrushAlphaConverter : IValueConverter
    {
        public static readonly BrushAlphaConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush scb)
            {
                var c = scb.Color;
                return new SolidColorBrush(Color.FromArgb(38, c.R, c.G, c.B));
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;   // one-way converter — ConvertBack is never used
    }
}

namespace RCS.Cogo.Wpf.Views
{
    public partial class CrossSectionWindow : Window
    {
        private readonly List<CrossSection> _sections;
        private CrossSectionViewModel _vm;

        public CrossSectionWindow(List<CrossSection> sections)
        {
            InitializeComponent();
            _sections = sections ?? new List<CrossSection>();
            _vm = new CrossSectionViewModel(_sections);
            DataContext = _vm;
        }

        // ── Navigation ─────────────────────────────────────────────────────
        private void OnPrev(object sender, RoutedEventArgs e) => _vm.GoPrev();
        private void OnNext(object sender, RoutedEventArgs e) => _vm.GoNext();

        // ── Canvas size propagation ────────────────────────────────────────
        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _vm.CanvasWidth  = e.NewSize.Width;
            _vm.CanvasHeight = e.NewSize.Height;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            XsCanvas.UpdateLayout();
        }

        // ── P3: CSV Export ─────────────────────────────────────────────────
        private void OnExportCsv(object sender, RoutedEventArgs e)
        {
            if (_sections.Count == 0)
            {
                MessageBox.Show("No cross sections to export.", "Nothing to Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter      = "CSV Files|*.csv",
                FileName    = "CrossSections.csv",
                Title       = "Export Cross Sections to CSV"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Station,FG_Elevation,EG_Elevation,CutFill,Status,Tmpl_Width_L,Tmpl_Width_R,Slope_L,Slope_R,Shot_Offset,Shot_EG_Elev");

                foreach (var xs in _sections)
                {
                    string status = Math.Abs(xs.CutFill) < 0.01 ? "AT_GRADE"
                                  : xs.CutFill < 0           ? "CUT"
                                                              : "FILL";

                    if (xs.Shots.Count == 0)
                    {
                        sb.AppendLine($"{xs.StationLabel},{xs.FGElevation:F4},{xs.EGElevationCL:F4}," +
                                      $"{xs.CutFill:F4},{status},{xs.TemplateWidthLeft:F2},{xs.TemplateWidthRight:F2}," +
                                      $"{xs.ForeslopeLeft:F2},{xs.ForeslopeRight:F2},,");
                    }
                    else
                    {
                        bool first = true;
                        foreach (var shot in xs.Shots.OrderBy(s => s.Offset))
                        {
                            if (first)
                            {
                                sb.AppendLine($"{xs.StationLabel},{xs.FGElevation:F4},{xs.EGElevationCL:F4}," +
                                              $"{xs.CutFill:F4},{status},{xs.TemplateWidthLeft:F2},{xs.TemplateWidthRight:F2}," +
                                              $"{xs.ForeslopeLeft:F2},{xs.ForeslopeRight:F2},{shot.Offset:F2},{shot.EGElevation:F4}");
                                first = false;
                            }
                            else
                            {
                                sb.AppendLine($",,,,,,,,," +
                                              $"{shot.Offset:F2},{shot.EGElevation:F4}");
                            }
                        }
                    }
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Exported {_sections.Count} sections to:\n{dlg.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── P3: DXF Export (current section) ───────────────────────────────
        private void OnExportDxf(object sender, RoutedEventArgs e)
        {
            if (_sections.Count == 0)
            {
                MessageBox.Show("No cross sections to export.", "Nothing to Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter   = "DXF Files|*.dxf",
                FileName = $"XS_{_sections[_vm.CurrentIndex].StationLabel.Replace("+","_")}.dxf",
                Title    = "Export Current Section to DXF"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                ExportSectionToDxf(dlg.FileName, _sections[_vm.CurrentIndex]);
                MessageBox.Show($"Section {_sections[_vm.CurrentIndex].StationLabel} exported to:\n{dlg.FileName}",
                    "DXF Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DXF export failed:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── P3: Export All sections to DXF ─────────────────────────────────
        private void OnExportAll(object sender, RoutedEventArgs e)
        {
            if (_sections.Count == 0)
            {
                MessageBox.Show("No cross sections to export.", "Nothing to Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter   = "DXF Files|*.dxf",
                FileName = "CrossSections_All.dxf",
                Title    = "Export All Sections — Multi-sheet DXF"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                ExportAllSectionsToDxf(dlg.FileName, _sections);
                MessageBox.Show($"All {_sections.Count} sections tiled in:\n{dlg.FileName}",
                    "DXF Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── DXF helpers ────────────────────────────────────────────────────

        private static void ExportSectionToDxf(string path, CrossSection xs)
        {
            using var w = new StreamWriter(path, false, Encoding.UTF8);
            WriteDxfHeader(w);
            w.WriteLine("  0\nSECTION\n  2\nENTITIES");

            double originX = 0, originY = 0;           // section placed at (0,0) in model space
            WriteSectionEntities(w, xs, originX, originY);

            w.WriteLine("  0\nENDSEC\n  0\nEOF");
        }

        private static void ExportAllSectionsToDxf(string path, List<CrossSection> sections)
        {
            using var w = new StreamWriter(path, false, Encoding.UTF8);
            WriteDxfHeader(w);
            w.WriteLine("  0\nSECTION\n  2\nENTITIES");

            // Tile vertically: each section 50 units tall, 120 units wide
            double tileH = 50.0;
            for (int i = 0; i < sections.Count; i++)
            {
                double originX = 0;
                double originY = -i * (tileH + 10);    // stack downward
                WriteSectionEntities(w, sections[i], originX, originY);
            }

            w.WriteLine("  0\nENDSEC\n  0\nEOF");
        }

        private static void WriteSectionEntities(StreamWriter w, CrossSection xs, double ox, double oy)
        {
            // Scale: 1 DXF unit = 1 ft.  Map offset→X, elevation delta→Y from FGElev.
            double fgElev = xs.FGElevation;

            // ── EG Polyline (blue: colour 5) ──────────────────────────────────
            var shots = xs.Shots.OrderBy(s => s.Offset).ToList();
            if (shots.Count >= 2)
            {
                w.WriteLine("  0\nPOLYLINE");
                w.WriteLine("  8\nXS_EG");
                w.WriteLine(" 62\n     5");    // blue
                w.WriteLine(" 66\n     1");    // vertices follow
                w.WriteLine(" 10\n0.0\n 20\n0.0\n 30\n0.0");
                foreach (var shot in shots)
                {
                    w.WriteLine("  0\nVERTEX");
                    w.WriteLine("  8\nXS_EG");
                    w.WriteLine($" 10\n{(ox + shot.Offset):F4}");
                    w.WriteLine($" 20\n{(oy + shot.EGElevation - fgElev):F4}");
                    w.WriteLine("  0");
                }
                w.WriteLine("SEQEND");
            }

            // ── Resolve actual daylight points ─────────────────────────────────
            double lEdge = -xs.TemplateWidthLeft;
            double rEdge =  xs.TemplateWidthRight;

            var ldp = xs.GetDaylightPoint(rightSide: false);
            var rdp = xs.GetDaylightPoint(rightSide: true);

            double lDayOff  = ldp?.Offset    ?? lEdge * 2.5;
            double lDayElev = ldp?.Elevation  ?? (xs.GetGroundElevAt(lDayOff) ?? fgElev);
            double rDayOff  = rdp?.Offset    ?? rEdge * 2.5;
            double rDayElev = rdp?.Elevation  ?? (xs.GetGroundElevAt(rDayOff) ?? fgElev);

            // ── FG Template (yellow: colour 2) ────────────────────────────────
            WritePolylineFG(w, ox, oy, fgElev, lDayOff, lDayElev, lEdge, 0, rEdge, rDayOff, rDayElev);

            // ── CUT zone outline (red: colour 1) ─────────────────────────────
            // CL-relative cut area: paved surface sits above EG → shade below FG
            if (xs.CutFill < -0.01 && shots.Count >= 2)
            {
                w.WriteLine("  0\nPOLYLINE\n  8\nXS_CUT\n 62\n     1\n 66\n     1");
                w.WriteLine(" 10\n0.0\n 20\n0.0\n 30\n0.0");
                // FG edge left → CL → FG edge right
                void VC(double offX, double elevY) =>
                    w.WriteLine($"  0\nVERTEX\n  8\nXS_CUT\n 10\n{(ox + offX):F4}\n 20\n{(oy + elevY - fgElev):F4}\n  0");
                VC(lEdge, fgElev);
                VC(0,     fgElev);
                VC(rEdge, fgElev);
                // trace EG shots back left (in reverse)
                foreach (var shot in shots.AsEnumerable().Reverse()
                             .Where(s => s.Offset >= lEdge && s.Offset <= rEdge))
                    VC(shot.Offset, shot.EGElevation);
                w.WriteLine("SEQEND");
            }

            // ── FILL zone outline (green: colour 3) ───────────────────────────
            if (xs.CutFill > 0.01 && shots.Count >= 2)
            {
                w.WriteLine("  0\nPOLYLINE\n  8\nXS_FILL\n 62\n     3\n 66\n     1");
                w.WriteLine(" 10\n0.0\n 20\n0.0\n 30\n0.0");
                void VF(double offX, double elevY) =>
                    w.WriteLine($"  0\nVERTEX\n  8\nXS_FILL\n 10\n{(ox + offX):F4}\n 20\n{(oy + elevY - fgElev):F4}\n  0");
                VF(lEdge, fgElev);
                VF(0,     fgElev);
                VF(rEdge, fgElev);
                foreach (var shot in shots.AsEnumerable().Reverse()
                             .Where(s => s.Offset >= lEdge && s.Offset <= rEdge))
                    VF(shot.Offset, shot.EGElevation);
                w.WriteLine("SEQEND");
            }

            // ── CL tick ───────────────────────────────────────────────────────
            WriteDxfLine(w, "XS_CL", 3, ox, oy - 1.5, ox, oy + 1.5);

            // ── Horizontal datum line (offset axis) ───────────────────────────
            double axisExtent = Math.Max(Math.Abs(lDayOff), Math.Abs(rDayOff)) + 5;
            WriteDxfLine(w, "XS_GRID", 8, ox - axisExtent, oy, ox + axisExtent, oy);

            // ── Offset tick marks every 10 ft ─────────────────────────────────
            for (double off = -Math.Floor(axisExtent / 10) * 10; off <= axisExtent; off += 10)
            {
                WriteDxfLine(w, "XS_GRID", 8, ox + off, oy - 0.5, ox + off, oy + 0.5);
                WriteDxfText(w, "XS_GRID", 8, ox + off - 2, oy - 2.5, 1.0, $"{(int)off}");
            }

            // ── Title block border ────────────────────────────────────────────
            double tbX1 = ox - axisExtent;
            double tbX2 = ox + axisExtent;
            double tbY1 = oy - 8;
            double tbY2 = oy + Math.Abs(lDayElev - fgElev > 0 ? lDayElev - fgElev : 5) + 10;

            // outer frame
            WriteDxfLine(w, "XS_BORDER", 7, tbX1, tbY1, tbX2, tbY1);
            WriteDxfLine(w, "XS_BORDER", 7, tbX2, tbY1, tbX2, tbY2);
            WriteDxfLine(w, "XS_BORDER", 7, tbX2, tbY2, tbX1, tbY2);
            WriteDxfLine(w, "XS_BORDER", 7, tbX1, tbY2, tbX1, tbY1);
            // title separator line
            WriteDxfLine(w, "XS_BORDER", 7, tbX1, tbY1 + 7, tbX2, tbY1 + 7);

            // ── Title block text ──────────────────────────────────────────────
            string cfStatus = xs.CutFill < -0.01 ? $"CUT {Math.Abs(xs.CutFill):F2} ft"
                            : xs.CutFill >  0.01 ? $"FILL {xs.CutFill:F2} ft"
                            : "AT GRADE";

            WriteDxfText(w, "XS_LABEL", 7, tbX1 + 1, tbY1 + 5.0, 2.5,
                $"STA: {xs.StationLabel}");

            WriteDxfText(w, "XS_LABEL", 7, tbX1 + 1, tbY1 + 2.5, 1.8,
                $"FG: {fgElev:F3}  EG: {xs.EGElevationCL:F3}  {cfStatus}");

            WriteDxfText(w, "XS_LABEL", 7, tbX1 + 1, tbY1 + 0.8, 1.4,
                $"Tmpl: {xs.TemplateWidthLeft:F0}L + {xs.TemplateWidthRight:F0}R ft  " +
                $"Slopes {xs.ForeslopeLeft:F1}:1 L / {xs.ForeslopeRight:F1}:1 R");
        }

        private static void WritePolylineFG(StreamWriter w, double ox, double oy, double fgElev,
            double lDayOff, double lDayElev,
            double lEdge, double clOff, double rEdge,
            double rDayOff, double rDayElev)
        {
            w.WriteLine("  0\nPOLYLINE");
            w.WriteLine("  8\nXS_FG");
            w.WriteLine(" 62\n     2");    // yellow
            w.WriteLine(" 66\n     1");
            w.WriteLine(" 10\n0.0\n 20\n0.0\n 30\n0.0");

            void V(double offX, double elevY)
            {
                w.WriteLine($"  0\nVERTEX\n  8\nXS_FG\n 10\n{(ox + offX):F4}\n 20\n{(oy + elevY - fgElev):F4}");
                w.WriteLine("  0");
            }

            V(lDayOff, lDayElev);
            V(lEdge,   fgElev);
            V(clOff,   fgElev);
            V(rEdge,   fgElev);
            V(rDayOff, rDayElev);

            w.WriteLine("SEQEND");
        }

        private static void WriteDxfLine(StreamWriter w, string layer, int color,
            double x1, double y1, double x2, double y2)
        {
            w.WriteLine($"  0\nLINE\n  8\n{layer}\n 62\n{color,6}");
            w.WriteLine($" 10\n{x1:F4}\n 20\n{y1:F4}\n 30\n0.0");
            w.WriteLine($" 11\n{x2:F4}\n 21\n{y2:F4}\n 31\n0.0");
        }

        private static void WriteDxfText(StreamWriter w, string layer, int color,
            double x, double y, double h, string text)
        {
            w.WriteLine($"  0\nTEXT\n  8\n{layer}\n 62\n{color,6}");
            w.WriteLine($" 10\n{x:F4}\n 20\n{y:F4}\n 30\n0.0");
            w.WriteLine($" 40\n{h:F4}");
            w.WriteLine($"  1\n{text}");
        }

        private static void WriteDxfHeader(StreamWriter w)
        {
            w.WriteLine("  0\nSECTION\n  2\nHEADER");
            w.WriteLine("  9\n$ACADVER\n  1\nAC1015");
            w.WriteLine("  9\n$INSUNITS\n 70\n      2");  // feet
            w.WriteLine("  0\nENDSEC");
        }
    }
}
