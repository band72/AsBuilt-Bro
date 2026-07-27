using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RCS.Data;
using RCS.Data.Entities;
using RCS.Piping.Core.Models;

namespace RCS.Cogo.Wpf.Views
{
    public partial class MaterialSelectionWindow : Window
    {
        public MaterialEntity? SelectedMaterial { get; private set; }
        private List<MaterialEntity> _allMaterials = new();
        private string _initialQuery = string.Empty;

        public MaterialSelectionWindow(string query)
        {
            InitializeComponent();
            _initialQuery = query;
            Loaded += MaterialSelectionWindow_Loaded;
        }

        private void MaterialSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                _allMaterials = db.Materials.ToList();

                if (!string.IsNullOrWhiteSpace(_initialQuery))
                {
                    var cleanQuery = _initialQuery.Replace("-", " ").Replace("|", " ");
                    SearchBox.Text = cleanQuery;
                }
                else
                {
                    FilterMaterials();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading materials: {ex.Message}");
            }
        }

        private void FilterMaterials()
        {
            if (_allMaterials == null) return;
            
            var query = SearchBox.Text.ToLower().Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                MaterialsGrid.ItemsSource = _allMaterials.Take(100).ToList();
                return;
            }

            var tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var matchedMaterials = _allMaterials
                .Select(m => new
                {
                    Material = m,
                    Score = tokens.Count(t => 
                        (m.PartKey?.ToLower().Contains(t) == true) ||
                        (m.Material?.ToLower().Contains(t) == true) ||
                        (m.FeatureType?.ToLower().Contains(t) == true) ||
                        (m.Discipline?.ToLower().Contains(t) == true) ||
                        (m.Size?.ToLower().Contains(t) == true) ||
                        (m.Notes?.ToLower().Contains(t) == true) ||
                        (m.Manufacturer?.ToLower().Contains(t) == true) ||
                        (PipeMaterialParser.Parse(t) != PipeMaterial.Unknown && PipeMaterialParser.Parse(m.Material) == PipeMaterialParser.Parse(t)))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Material)
                .Take(100)
                .ToList();

            MaterialsGrid.ItemsSource = matchedMaterials;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterMaterials();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            ConfirmSelection();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MaterialsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MaterialsGrid.SelectedItem != null && MaterialsGrid.SelectedItem is MaterialEntity)
            {
                ConfirmSelection();
            }
        }

        private void ConfirmSelection()
        {
            if (MaterialsGrid.SelectedItem is MaterialEntity mat)
            {
                SelectedMaterial = mat;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a material first.");
            }
        }
    }
}
