import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

target = """                    <StackPanel>
                        <TextBlock Text="Material:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding Material}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>"""

if target not in text:
    print("Could not find target block to replace!")
    sys.exit(1)

replacement = target + """

                    <!-- Core Added JEA Common Fields -->
                    <StackPanel>
                        <TextBlock Text="Subtype:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding Subtype}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Facility Owner:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding FacilityOwner}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Size Secondary:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding SizeSecondary}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Pipe Class:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding PipeClass}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Lining Manufacturer:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding LiningManufacturer}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Lining Material:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding LiningMaterial}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Orientation:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding Orientation}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Pipe Role:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding PipeRole}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Drop Type:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding DropType}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>

                    <StackPanel>
                        <TextBlock Text="Invert Elevations w/ Directions:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding InvertElevationsWithDirections}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>
                    
                    <StackPanel>
                        <TextBlock Text="Exterior Joint Tape Type:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding ExteriorJointTapeType}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>
                    
                    <StackPanel>
                        <TextBlock Text="Exterior Joint Tape Mfr:" Margin="0,5,0,0"/>
                        <TextBox Text="{Binding ExteriorJointTapeManufacturer}" Background="#252526" Foreground="White" Padding="5"/>
                    </StackPanel>"""

new_text = text.replace(target, replacement)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_text)

print("Patched EditAssetWindow successfully!")
