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
        public System.Collections.Generic.List<string> FacilityOwnersList { get; set; } = new() { "JEA", "Private", "Other", "Unknown" };
        public System.Collections.Generic.List<string> YesNoList { get; } = new() { "Yes", "No", "Unknown" };
        public System.Collections.Generic.List<string> MaterialsList { get; } = new() { "PVC", "DIP", "RCP", "HDPE", "Steel", "Copper", "Other" };
        public System.Collections.Generic.List<string> SubtypesList { get; set; } = new() { "Chilled Fitting", "Locate Box", "Manhole", "Reclaimed Fitting", "Reclaimed Meter", "Reclaimed Pipe", "Reclaimed Valve", "Sewer Customer Point", "Sewer Fitting", "Sewer Gravity Pipe", "Sewer Pressure Pipe", "Sewer Valve", "Water Fitting", "Water Meter", "Water Pipe", "Water Valve" };
        public System.Collections.Generic.List<string> PipeClassList { get; set; } = new() { "Class 52", "Class 51", "Class 150", "Class 200", "SDR-35", "SDR-26", "Sch 40", "Sch 80", "Other" };
        public System.Collections.Generic.List<string> TrueFalseList { get; } = new() { "True", "False" };
        public System.Collections.Generic.List<string> OrientationsList { get; } = new() { "Horizontal", "Vertical", "Diagonal", "Unknown" };

        public System.Collections.Generic.List<string> PipeRoleList { get; set; } = new() { "Return", "Supply", "Unknown", "N/A" };
        public System.Collections.Generic.List<string> ManufacturerList { get; set; } = new();
        public System.Collections.Generic.List<string> HydrantModelList { get; set; } = new();
        public System.Collections.Generic.List<string> DropTypeList { get; set; } = new() { "Inside", "Outside", "Unknown", "None" };
        public System.Collections.Generic.List<string> ExteriorJointTapeManufacturerList { get; set; } = new() { "Con Seal", "Rub-R-Nek/Henry Company", "Wrapid Seal", "Other", "Unknown", "None" };
        public System.Collections.Generic.List<string> TapeTypeList { get; set; } = new() { "Con Seal", "Rub-R-Nek/Henry Company", "Wrapid Seal", "Other", "Unknown", "None" };
        public System.Collections.Generic.List<string> PipeSizeList { get; set; } = new() { "0.625", "0.75", "1", "1.5", "2", "2.5", "3", "4", "6", "8", "10", "12", "14", "16", "18", "20", "24", "30", "36", "42", "48", "60", "72" };
        public System.Collections.Generic.List<string> LiningManufacturerList { get; set; } = new() { "Induron", "Tnemec", "Protecto 401", "Sherwin Williams", "Other", "Unknown", "None" };
        public System.Collections.Generic.List<string> LiningMaterialList { get; set; } = new() { "Ceramic Epoxy", "Cement", "Polyurethane", "Epoxy", "HDPE", "Glass", "Other", "Unknown", "None" };
        public System.Collections.Generic.List<string> DisciplineList { get; } = new() { "WA", "WW", "DR", "GS", "EL", "RC", "CH" };
        public InstalledAsset EditingAsset => _editingAsset;
        public bool IsPipeCrossing => _editingAsset is PipeCrossing || _editingAsset.GetType().Name.Contains("Crossing");
        public bool IsFigure => _editingAsset is Figure;
        public bool IsPipe => !IsPipeCrossing && (_editingAsset is Pipe || _editingAsset.GetType().Name.Contains("Pipe"));
        public bool IsPointGeometry => !IsFigure && !IsPipe && !IsPipeCrossing;
        
        public System.Collections.ObjectModel.ObservableCollection<FigureVertex>? FigureVertices 
        {
            get
            {
                if (_editingAsset is Figure f)
                {
                    return new System.Collections.ObjectModel.ObservableCollection<FigureVertex>(f.Vertices.OrderBy(v => v.OrderIndex));
                }
                return null;
            }
        }

        private InstalledAsset _editingAsset;
        private InstalledAssetsViewModel _viewModel;
        // Constructor for Editing
        public EditAssetWindow(InstalledAsset asset, InstalledAssetsViewModel vm)
        {
            _editingAsset = asset;

            try
            {
                using var db = new AppDbContext();
                
                var allSubtypes = db.AssetSubtypes.ToList();
                
                string typeName = asset.GetType().Name;
                string searchCategory = typeName;
                
                // Formally Map Entity Classes to user's defined "Category" Database strings
                if (typeName.Contains("LocateBox") || typeName == "LocateBox") searchCategory = "Locate Box";
                else if (typeName.Contains("Manhole") || typeName == "Manhole") searchCategory = "Manhole";
                else if (typeName.Contains("Crossing") || typeName == "PipeCrossing") searchCategory = "Crossing Pipe Type";
                else if (typeName == "WWPoint" || typeName == "WWServicePoint") searchCategory = "Sewer Customer Point";
                else 
                {
                    // Map generic prefix abstractions
                    if (typeName.StartsWith("WW")) searchCategory = searchCategory.Replace("WW", "Sewer");
                    if (typeName.StartsWith("ST")) searchCategory = searchCategory.Replace("ST", "Storm");
                    if (typeName.StartsWith("G") && !typeName.StartsWith("Ga")) searchCategory = "Gas" + searchCategory.Substring(1);
                    if (typeName.StartsWith("E") && !typeName.StartsWith("El")) searchCategory = "Electric" + searchCategory.Substring(1);

                    // Break class camel-casing visually (e.g. SewerGravityPipe -> Sewer Gravity Pipe)
                    searchCategory = System.Text.RegularExpressions.Regex.Replace(searchCategory, "([a-z])([A-Z])", "$1 $2");
                    searchCategory = searchCategory.Replace("  ", " ").Trim();
                }

                var dynamicOptions = allSubtypes.Where(s => string.Equals(s.Category, searchCategory, StringComparison.OrdinalIgnoreCase)).Select(s => s.SubtypeName).ToList();
                if (dynamicOptions.Any()) SubtypesList = dynamicOptions;

                var foOptions = allSubtypes.Where(s => s.Category == "Facility Owner").Select(s => s.SubtypeName).ToList();
                if (foOptions.Any()) FacilityOwnersList = foOptions;

                var pcOptions = allSubtypes.Where(s => s.Category == "Chilled Pipe Class").Select(s => s.SubtypeName).ToList();
                if (pcOptions.Any()) PipeClassList = pcOptions;

                var prOptions = allSubtypes.Where(s => s.Category == "Chilled Pipe Role" || s.Category == "Pipe Role" || s.Category == "Pipe Type").Select(s => s.SubtypeName).ToList();
                if (prOptions.Any()) PipeRoleList = prOptions;
                
                ManufacturerList = allSubtypes.Where(s => s.Category == "Fitting Manufacturers").Select(s => s.SubtypeName).ToList();
                HydrantModelList = allSubtypes.Where(s => s.Category == "Hydrant Model").Select(s => s.SubtypeName).ToList();
                
                var dtOptions = allSubtypes.Where(s => s.Category == "Manhole Drop Type" || s.Category == "Drop Type").Select(s => s.SubtypeName).ToList();
                if (dtOptions.Any()) DropTypeList = dtOptions;
                
                var ejtmOptions = allSubtypes.Where(s => s.Category == "Manhole Exterior Joint Tape Manufacturer" || s.Category == "Tape Type").Select(s => s.SubtypeName).ToList();
                if (ejtmOptions.Any()) {
                    ExteriorJointTapeManufacturerList = ejtmOptions;
                    TapeTypeList = ejtmOptions;
                }
                
                var sizeOptions = allSubtypes.Where(s => s.Category == "Pipe Size").Select(s => s.SubtypeName).ToList();
                if (sizeOptions.Any()) PipeSizeList = sizeOptions;

                var lmOptions = allSubtypes.Where(s => s.Category == "Lining Manufacturer").Select(s => s.SubtypeName).ToList();
                if (lmOptions.Any()) LiningManufacturerList = lmOptions;

                var lmatOptions = allSubtypes.Where(s => s.Category == "Lining Material").Select(s => s.SubtypeName).ToList();
                if (lmatOptions.Any()) LiningMaterialList = lmatOptions;
            }
            catch { }
            
            InitializeComponent();
            
            // Remove pipe delimiters to match visual formatting and prevent binding issues
            if (!string.IsNullOrEmpty(_editingAsset.PartKey) && _editingAsset.PartKey.Contains("|"))
            {
                _editingAsset.PartKey = _editingAsset.PartKey.Replace("|", "-");
            }

            if (IsPipeCrossing && string.IsNullOrEmpty(_editingAsset.FeatureType))
            {
                _editingAsset.FeatureType = "Pipe";
            }

            _viewModel = vm;
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

        private async void Universal_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show($"Are you sure you want to continuously apply the metadata settings from this asset ({_editingAsset.PartKey}) to ALL other {_editingAsset.GetType().Name} assets in the current grid view?", "Universal Apply", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    // Update all other assets in the matching ViewModel collection with the current asset's properties
                    var collProp = _viewModel.GetType().GetProperties()
                        .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                             p.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.ObjectModel.ObservableCollection<>) &&
                                             p.PropertyType.GenericTypeArguments[0] == _editingAsset.GetType());

                    if (collProp != null)
                    {
                        var collection = collProp.GetValue(_viewModel) as System.Collections.IEnumerable;
                        if (collection != null)
                        {
                            foreach (var item in collection)
                            {
                                var asset = item as InstalledAsset;
                                // DO NOT override unique spatial fields like coords, partkeys, or notes
                                if (asset != null && asset.Id != _editingAsset.Id)
                                {
                                    asset.Discipline = _editingAsset.Discipline;
                                    asset.FeatureType = _editingAsset.FeatureType;
                                    asset.Size = _editingAsset.Size;
                                    asset.SizeSecondary = _editingAsset.SizeSecondary;
                                    asset.Material = _editingAsset.Material;
                                    asset.Subtype = _editingAsset.Subtype;
                                    asset.FacilityOwner = _editingAsset.FacilityOwner;
                                    asset.PipeClass = _editingAsset.PipeClass;
                                    asset.Orientation = _editingAsset.Orientation;
                                    asset.PipeRole = _editingAsset.PipeRole;
                                    asset.DropType = _editingAsset.DropType;
                                    asset.LiningManufacturer = _editingAsset.LiningManufacturer;
                                    asset.LiningMaterial = _editingAsset.LiningMaterial;
                                    asset.ExteriorJointTapeType = _editingAsset.ExteriorJointTapeType;
                                    asset.ExteriorJointTapeManufacturer = _editingAsset.ExteriorJointTapeManufacturer;
                                    
                                    asset.Manufacturer = _editingAsset.Manufacturer;
                                    asset.ManufacturerPartNo = _editingAsset.ManufacturerPartNo;
                                    asset.YearManufactured = _editingAsset.YearManufactured;

                                                                        
                                    await _viewModel.SaveItemAsync(asset);
                                }
                            }
                        }
                    }

                    // Save the current editing asset too since it might have unsaved edits from the UI
                    await _viewModel.SaveItemAsync(_editingAsset);
                    await _viewModel.ReloadAsync();

                    MsgTxt.Text = "Universal settings applied matching assets!";
                    MsgTxt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
                    
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MsgTxt.Text = $"Universal Apply Error: {ex.Message}";
                    MsgTxt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                }
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
