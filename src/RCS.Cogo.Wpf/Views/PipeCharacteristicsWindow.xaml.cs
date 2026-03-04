using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Cogo.Wpf.Views
{
    public partial class PipeCharacteristicsWindow : Window
    {
        public ObservableCollection<PartSpecificationEntity> Specifications { get; set; } = new();

        public PartSpecificationEntity? SelectedPart { get; private set; }

        public PipeCharacteristicsWindow(bool isSelectMode = false)
        {
            InitializeComponent();
            DataContext = this;
            LoadFromDB();

            if (isSelectMode)
            {
                BtnSelect.Visibility = Visibility.Visible;
            }
        }

        private void LoadFromDB()
        {
            try
            {
                using var db = new AppDbContext();
                var items = db.PartSpecifications.ToList();
                Specifications.Clear();
                foreach (var i in items)
                {
                    Specifications.Add(i);
                }
                TxtStatus.Text = $"Database loaded. Total records: {Specifications.Count}";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Error loading DB: " + ex.Message;
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            Specifications.Add(new PartSpecificationEntity { PartNumber = "New Part" });
            TxtStatus.Text = "New blank row added. Remember to enter a unique Part Number and Save.";
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (SpecsGrid.SelectedItem is PartSpecificationEntity spec)
            {
                Specifications.Remove(spec);
                TxtStatus.Text = $"Removed row for Part Number '{spec.PartNumber}'. Remember to Save.";
            }
        }

        private void SelectRow_Click(object sender, RoutedEventArgs e)
        {
            if (SpecsGrid.SelectedItem is PartSpecificationEntity spec)
            {
                SelectedPart = spec;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a valid row first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                
                // Truncate and replace style or upsert. Let's do a complete replace to easily manage deletions.
                var existing = db.PartSpecifications.ToList();
                db.PartSpecifications.RemoveRange(existing);
                db.SaveChanges(); // wipe clean
                
                db.PartSpecifications.AddRange(Specifications.Where(s => !string.IsNullOrWhiteSpace(s.PartNumber)));
                db.SaveChanges(); // populate modified list
                
                TxtStatus.Text = "Saved successfully to database.";
                TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Error saving to database: " + ex.Message;
                TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Export Pipe Characteristics",
                FileName = "PipeCharacteristics.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var sw = new StreamWriter(dialog.FileName);
                    sw.WriteLine("PartNumber,NominalDiameter,OuterDiameter,PipeThickness,InnerDiameter,Deflection,Note");
                    foreach (var s in Specifications)
                    {
                        sw.WriteLine($"{s.PartNumber},{s.NominalDiameter},{s.OuterDiameter},{s.PipeThickness},{s.InnerDiameter},{s.Deflection},\"{s.Note?.Replace("\"", "\"\"")}\"");
                    }
                    TxtStatus.Text = $"Exported to {dialog.SafeFileName} successfully.";
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = "Export Error: " + ex.Message;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Import Pipe Characteristics"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var lines = File.ReadAllLines(dialog.FileName);
                    if (lines.Length <= 1) return; // Note: Header is expected

                    Specifications.Clear();
                    
                    // Start at index 1 to skip header
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Very basic CSV split
                        var parts = line.Split(',');
                        if (parts.Length >= 7)
                        {
                            var spec = new PartSpecificationEntity();
                            spec.PartNumber = parts[0].Trim();
                            
                            if (double.TryParse(parts[1], out double nd)) spec.NominalDiameter = nd;
                            if (double.TryParse(parts[2], out double od)) spec.OuterDiameter = od;
                            if (double.TryParse(parts[3], out double pt)) spec.PipeThickness = pt;
                            if (double.TryParse(parts[4], out double id)) spec.InnerDiameter = id;
                            if (double.TryParse(parts[5], out double def)) spec.Deflection = def;
                            
                            spec.Note = parts[6].Trim().Trim('"');

                            if (!string.IsNullOrWhiteSpace(spec.PartNumber))
                            {
                                Specifications.Add(spec);
                            }
                        }
                    }

                    TxtStatus.Text = $"Imported {Specifications.Count} rows successfully. Click Save to persist to DB.";
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = "Import Error: " + ex.Message;
                    TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }
    }
}
