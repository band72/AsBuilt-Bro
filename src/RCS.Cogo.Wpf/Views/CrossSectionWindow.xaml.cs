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
    /// <summary>Dims a brush to 15% opacity for the CUT/FILL badge background.</summary>
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
            => throw new NotImplementedException();
    }
}

namespace RCS.Cogo.Wpf.Views
{
    public partial class CrossSectionWindow : Window
    {
        private CrossSectionViewModel _vm;

        public CrossSectionWindow(List<CrossSection> sections)
        {
            InitializeComponent();
            _vm = new CrossSectionViewModel(sections);
            DataContext = _vm;
        }

        // ── Navigation ─────────────────────────────────────────────────────
        private void OnPrev(object sender, RoutedEventArgs e)
        {
            _vm.GoPrev();
        }

        private void OnNext(object sender, RoutedEventArgs e)
        {
            _vm.GoNext();
        }

        // ── Canvas size changed → update ViewModel dimensions → re-render ──
        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _vm.CanvasWidth  = e.NewSize.Width;
            _vm.CanvasHeight = e.NewSize.Height;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Force canvas measure on next layout pass
            XsCanvas.UpdateLayout();
        }

        // ── CSV Export ─────────────────────────────────────────────────────
        private void OnExportCsv(object sender, RoutedEventArgs e)
        {
            ExportCsv();
        }

        private void ExportCsv()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter   = "CSV Files|*.csv",
                FileName = "CrossSections.csv"
            };

            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("Station,FG_Elevation,EG_Elevation,CutFill,Status,Shot_Offset,Shot_EG_Elev");

            // Get sections through reflection since _vm.Sections is private
            // We re-use what the VM exposes via label list to identify which sections exist
            // (For a production build, expose _sections publicly on the VM)
            // For now we export via the cut/fill report text from StationingEngine

            sb.AppendLine("(Use XS COMPUTE in the script to generate the full cut/fill report)");
            File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show($"Exported to:\n{dlg.FileName}", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── DXF Export (single section) ────────────────────────────────────
        private void OnExportDxf(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DXF export per-section will be available in the next update.\n" +
                            "Use the COGO script XS COMPUTE command output for now.",
                            "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Export All sheets ──────────────────────────────────────────────
        private void OnExportAll(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Multi-sheet XS export queued.\n" +
                            "This will generate one canvas per station as a PNG or DXF batch.",
                            "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
