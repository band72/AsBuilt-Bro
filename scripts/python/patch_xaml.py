import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

# I need to strip Description, Source, Confidence, Warning, Notes grids.
target_remove = """                    <!-- Manufacturer info -->
                    <StackPanel Margin="0,15,0,0">
                        <TextBlock Text="Manufacturer:" FontWeight="Bold" Foreground="CornflowerBlue"/>
                        <TextBox Text="{Binding Manufacturer}" Background="#252526" Foreground="White" Padding="5" Margin="0,5,0,0"/>
                    </StackPanel>

                    <StackPanel Margin="0,5,0,0">
                        <TextBlock Text="Model / Part No:"/>
                        <TextBox Text="{Binding ManufacturerPartNo}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel Margin="0,5,0,0">
                        <TextBlock Text="Year Manufactured:"/>
                        <TextBox Text="{Binding YearManufactured}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel Margin="0,15,0,0">
                        <TextBlock Text="Confidence/Source:" FontWeight="Bold" Foreground="CornflowerBlue"/>
                        <Grid Margin="0,5,0,0">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBox Grid.Column="0" Text="{Binding Confidence}" Background="#252526" Foreground="White" Padding="5" Margin="0,0,5,0"/>
                            <TextBox Grid.Column="1" Text="{Binding Source}" Background="#252526" Foreground="White" Padding="5" Margin="5,0,0,0"/>
                        </Grid>
                    </StackPanel>

                    <StackPanel Margin="0,5,0,0">
                        <TextBlock Text="Description (User):"/>
                        <TextBox Text="{Binding Description}" Background="#252526" Foreground="White" Padding="5" AcceptsReturn="False"/>
                    </StackPanel>
                    
                    <StackPanel Margin="0,5,0,0">
                        <TextBlock Text="Notes:"/>
                        <TextBox Text="{Binding Notes}" IsReadOnly="True" Background="#252526" Foreground="White" Padding="5" AcceptsReturn="True" Height="60" TextWrapping="Wrap"/>
                    </StackPanel>"""

replacement_add = """                    <!-- Manufacturer info -->
                    <StackPanel Margin="0,15,0,0">
                        <TextBlock Text="Manufacturer:" FontWeight="Bold" Foreground="CornflowerBlue"/>
                        <TextBox Text="{Binding Manufacturer}" Background="#252526" Foreground="White" Padding="5" Margin="0,5,0,0"/>
                    </StackPanel>

                    <StackPanel Margin="0,5,0,0">
                        <TextBlock Text="Model / Part No:"/>
                        <TextBox Text="{Binding ManufacturerPartNo}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel Margin="0,5,0,0">
                        <TextBlock Text="Year Manufactured:"/>
                        <TextBox Text="{Binding YearManufactured}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>"""

text = text.replace(target_remove, replacement_add)

target_elev_nodes = """                            <TextBlock Text="Elevation/Invert:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                <TextBox Text="{Binding Elevation, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                                <Button Grid.Column="1" Content="..." Width="25" Margin="5,0,0,0" Tag="Elevation" Click="CalculateInvert_Click"/>
                            </Grid>"""

replacement_elev_nodes = """                            <TextBlock Text="Top/Grade Elev:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <TextBox Grid.Column="0" Text="{Binding TopElevation, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5" Margin="0,0,2,0"/>
                                <TextBox Grid.Column="1" Text="{Binding GradeElevation, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5" Margin="2,0,0,0"/>
                            </Grid>"""

text = text.replace(target_elev_nodes, replacement_elev_nodes)

target_pipe_start = """                            <TextBlock Text="Start Invert:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                <TextBox Text="{Binding InvertStart, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                                <Button Grid.Column="1" Content="..." Width="25" Margin="5,0,0,0" Tag="InvertStart" Click="CalculateInvert_Click"/>
                            </Grid>"""

replacement_pipe_start = """                            <TextBlock Text="Length / Slope:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="*"/>
                                </Grid.ColumnDefinitions>
                                <TextBox Grid.Column="0" Text="{Binding Length, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5" Margin="0,0,2,0"/>
                                <TextBox Grid.Column="1" Text="{Binding Slope, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5" Margin="2,0,0,0"/>
                            </Grid>"""

text = text.replace(target_pipe_start, replacement_pipe_start)

# We also need to strip bindings to NorthingStart/EastingStart which we removed and just hide that row since pipes don't use X/Y
target_pipe_geom_1 = """                        <StackPanel Grid.Column="0" Margin="0,0,5,0">
                            <TextBlock Text="Start Northing:"/>
                            <TextBox Text="{Binding NorthingStart, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="5,0,5,0">
                            <TextBlock Text="Start Easting:"/>
                            <TextBox Text="{Binding EastingStart, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>"""
replacement_pipe_geom_1 = """                        <StackPanel Grid.Column="0" Margin="0,0,5,0">
                            <TextBlock Text="Upstream Invert:"/>
                            <TextBox Text="{Binding UpstreamInvert, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="5,0,5,0">
                            <TextBlock Text="Upstream Grade:"/>
                            <TextBox Text="{Binding UpstreamGrade, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>"""

text = text.replace(target_pipe_geom_1, replacement_pipe_geom_1)

target_pipe_geom_2 = """                        <StackPanel Grid.Column="0" Margin="0,0,5,0">
                            <TextBlock Text="End Northing:"/>
                            <TextBox Text="{Binding NorthingEnd, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="5,0,5,0">
                            <TextBlock Text="End Easting:"/>
                            <TextBox Text="{Binding EastingEnd, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>
                        <StackPanel Grid.Column="2" Margin="5,0,0,0">
                            <TextBlock Text="End Invert:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="*"/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                <TextBox Text="{Binding InvertEnd, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                                <Button Grid.Column="1" Content="..." Width="25" Margin="5,0,0,0" Tag="InvertEnd" Click="CalculateInvert_Click"/>
                            </Grid>
                        </StackPanel>"""

replacement_pipe_geom_2 = """                        <StackPanel Grid.Column="0" Margin="0,0,5,0">
                            <TextBlock Text="Downstream Invert:"/>
                            <TextBox Text="{Binding DownstreamInvert, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>
                        <StackPanel Grid.Column="1" Margin="5,0,5,0">
                            <TextBlock Text="Downstream Grade:"/>
                            <TextBox Text="{Binding DownstreamGrade, StringFormat=\{0:F3\}}" Background="#252526" Foreground="White" Padding="5"/>
                        </StackPanel>
                        <StackPanel Grid.Column="2" Margin="5,0,0,0">
                        </StackPanel>"""

text = text.replace(target_pipe_geom_2, replacement_pipe_geom_2)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

cs_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml.cs'
with open(cs_path, 'r', encoding='utf-8') as cs_f:
    cs_text = cs_f.read()

# Fix the universal copy mapping since I've removed Description, Source, Confidence
cs_text = cs_text.replace("asset.Confidence = _editingAsset.Confidence;\n", "")
cs_text = cs_text.replace("asset.Source = _editingAsset.Source;\n", "")

with open(cs_path, 'w', encoding='utf-8') as cs_f:
    cs_f.write(cs_text)

print("Patched XAML UI successfully!")
