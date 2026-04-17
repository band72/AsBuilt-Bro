import sys

# 1. Patch XAML
xaml_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml'
with open(xaml_path, 'r', encoding='utf-8') as f:
    text = f.read()

target = '<Button Grid.Column="0" Content="Materials" Click="Materials_Click" Padding="15,5" Background="#6B8E23" Foreground="White" FontWeight="Bold"/>'
replacement = """<StackPanel Grid.Column="0" Orientation="Horizontal">
                <Button Content="Materials" Click="Materials_Click" Padding="15,5" Background="#6B8E23" Foreground="White" FontWeight="Bold"/>
                <Button Content="Universal" Click="Universal_Click" Padding="15,5" Margin="5,0,0,0" Background="#9B59B6" ToolTip="Apply these metadata settings to all other records in the current grid" Foreground="White" FontWeight="Bold"/>
            </StackPanel>"""

text = text.replace(target, replacement)
with open(xaml_path, 'w', encoding='utf-8') as f:
    f.write(text)

# 2. Patch CS
cs_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml.cs'
with open(cs_path, 'r', encoding='utf-8') as f:
    cs_text = f.read()

target_cs = """        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }"""

replacement_cs = """        private async void Universal_Click(object sender, RoutedEventArgs e)
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

                                    asset.Confidence = _editingAsset.Confidence;
                                    asset.Source = _editingAsset.Source;
                                    asset.Description = _editingAsset.Description;

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

""" + target_cs

cs_text = cs_text.replace(target_cs, replacement_cs)

with open(cs_path, 'w', encoding='utf-8') as f:
    f.write(cs_text)

print("Added Universal Apply button and logic successfully.")
