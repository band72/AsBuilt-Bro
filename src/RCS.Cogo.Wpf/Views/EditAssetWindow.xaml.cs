using System;
using System.Linq;
using System.Windows;
using RCS.Data;
using RCS.Data.Entities;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views
{
    public partial class EditAssetWindow : Window
    {
        private InstalledAsset _editingAsset;
        private InstalledAssetsViewModel _viewModel;
        private bool _isNewAsset;
        
        // Constructor for Editing
        public EditAssetWindow(InstalledAsset asset, InstalledAssetsViewModel vm)
        {
            InitializeComponent();
            _editingAsset = asset;
            _viewModel = vm;
            _isNewAsset = false;
            DataContext = _editingAsset;
        }
        
        // Constructor for adding a new asset (not directly supported for base class unless we specify type)
        // We will just let users add via the DataGrid's new row.

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Re-sync row edit
                await _viewModel.SaveItemAsync(_editingAsset);
                
                // Update UI collections to correctly display updated fields on DataGrid
                await _viewModel.ReloadAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var msg = $"SQLite Error details: {ex.Message}\nInner: {ex.InnerException?.Message}";
                _viewModel.LogAction?.Invoke($"[DB_ERROR_POPUP] {msg}");
                MessageBox.Show(msg + $"\n\nStack: {ex.StackTrace}", "Popup Auto-Save DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Materials_Click(object sender, RoutedEventArgs e)
        {
            MsgTxt.Text = "";
            
            // Search criteria hierarchy:
            // 1. Description Property (if it exists on child type)
            // 2. PartKey natively on base class.
            
            string partName = "";
            var type = _editingAsset.GetType();
            var descProp = type.GetProperty("Description", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (descProp != null)
            {
                partName = descProp.GetValue(_editingAsset) as string ?? "";
            }

            if (string.IsNullOrWhiteSpace(partName))
            {
                partName = _editingAsset.PartKey ?? "";
            }

            if (string.IsNullOrWhiteSpace(partName))
            {
                // Just pass empty string so we can browse all materials
                partName = "";
            }

            try
            {
                var selectionWindow = new MaterialSelectionWindow(partName);
                selectionWindow.Owner = this;
                
                if (selectionWindow.ShowDialog() == true)
                {
                    var mat = selectionWindow.SelectedMaterial;
                    if (mat != null)
                    {
                        // Fill remaining empty fields
                        if (string.IsNullOrWhiteSpace(_editingAsset.Discipline) && !string.IsNullOrWhiteSpace(mat.Discipline)) _editingAsset.Discipline = mat.Discipline;
                        if (string.IsNullOrWhiteSpace(_editingAsset.FeatureType) && !string.IsNullOrWhiteSpace(mat.FeatureType)) _editingAsset.FeatureType = mat.FeatureType;
                        if (string.IsNullOrWhiteSpace(_editingAsset.Size) && !string.IsNullOrWhiteSpace(mat.Size)) _editingAsset.Size = mat.Size;
                        if (string.IsNullOrWhiteSpace(_editingAsset.Material) && !string.IsNullOrWhiteSpace(mat.Material)) _editingAsset.Material = mat.Material;
                        if (string.IsNullOrWhiteSpace(_editingAsset.Manufacturer) && !string.IsNullOrWhiteSpace(mat.Manufacturer)) _editingAsset.Manufacturer = mat.Manufacturer;
                        if (string.IsNullOrWhiteSpace(_editingAsset.ManufacturerPartNo) && !string.IsNullOrWhiteSpace(mat.Model)) _editingAsset.ManufacturerPartNo = mat.Model;
                        if (string.IsNullOrWhiteSpace(_editingAsset.YearManufactured) && !string.IsNullOrWhiteSpace(mat.Year)) _editingAsset.YearManufactured = mat.Year;
                        
                        // User requests note field to be readonly, and likely wants material note filled into asset note
                        if (string.IsNullOrWhiteSpace(_editingAsset.Notes) && !string.IsNullOrWhiteSpace(mat.Notes)) _editingAsset.Notes = mat.Notes;

                        // Rebind to update UI
                        DataContext = null;
                        DataContext = _editingAsset;

                        MsgTxt.Text = "Missing fields populated from selected material.";
                        MsgTxt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    }
                }
            }
            catch (Exception ex)
            {
                MsgTxt.Text = $"Lookup Error: {ex.Message}";
                MsgTxt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show($"Are you sure you want to delete {_editingAsset.PartKey}?", "Delete Asset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                await _viewModel.DeleteAssetAsync(_editingAsset);
                await _viewModel.ReloadAsync(); // refresh
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CalculateInvert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                var calc = new InvertCalculatorWindow(_editingAsset);
                calc.Owner = this;
                if (calc.ShowDialog() == true && calc.ComputedValue.HasValue)
                {
                    var val = calc.ComputedValue.Value;
                    
                    // Assign via reflection using the Tag
                    var prop = _editingAsset.GetType().GetProperty(tag, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (prop != null)
                    {
                        prop.SetValue(_editingAsset, val);
                        
                        // Force UI refresh for the binding
                        DataContext = null;
                        DataContext = _editingAsset;
                        MsgTxt.Text = $"Computed invert applied to {tag}.";
                        MsgTxt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    }
                }
            }
        }
    }
}
