import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Fix comboboxes 
text = text.replace(
    '<ComboBox Text="{Binding Material}" ItemsSource="{Binding MaterialsList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    '<ComboBox Text="{Binding Material}" ItemsSource="{Binding MaterialsList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="White" Foreground="Black" Padding="5"/>'
)

text = text.replace(
    '<ComboBox Text="{Binding Subtype}" ItemsSource="{Binding SubtypesList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    '<ComboBox Text="{Binding Subtype}" ItemsSource="{Binding SubtypesList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="White" Foreground="Black" Padding="5"/>'
)

text = text.replace(
    '<ComboBox Text="{Binding FacilityOwner}" ItemsSource="{Binding FacilityOwnersList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    '<ComboBox Text="{Binding FacilityOwner}" ItemsSource="{Binding FacilityOwnersList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="White" Foreground="Black" Padding="5"/>'
)

text = text.replace(
    '<ComboBox Text="{Binding PipeClass}" ItemsSource="{Binding PipeClassList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    '<ComboBox Text="{Binding PipeClass}" ItemsSource="{Binding PipeClassList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="White" Foreground="Black" Padding="5"/>'
)

text = text.replace(
    '<ComboBox Text="{Binding Orientation}" ItemsSource="{Binding OrientationsList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    '<ComboBox Text="{Binding Orientation}" ItemsSource="{Binding OrientationsList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="White" Foreground="Black" Padding="5"/>'
)

button_target = """        <Grid Grid.Row="4">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <StackPanel Grid.Column="0" Orientation="Horizontal">
                <Button Content="Materials" Click="Materials_Click" Padding="10,5" Background="#6B8E23" Foreground="White" FontWeight="Bold"/>
                <Button Content="Universal" Click="Universal_Click" Padding="10,5" Margin="5,0,0,0" Background="#9B59B6" ToolTip="Apply these metadata settings to all other records in the current grid" Foreground="White" FontWeight="Bold"/>
            </StackPanel>

            <StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="Delete" Click="Delete_Click" Padding="10,5" Margin="0,0,10,0" Background="#A52A2A" Foreground="White" FontWeight="Bold"/>
                <Button Content="Cancel" Click="Cancel_Click" Padding="10,5" Margin="0,0,10,0" Background="#2D2D30" Foreground="White"/>
                <Button Content="Save" Click="Save_Click" Padding="15,5" Background="#007ACC" Foreground="White" FontWeight="Bold"/>
            </StackPanel>
        </Grid>"""

button_replacement = """        <Grid Grid.Row="4">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                <Button Content="Materials" Click="Materials_Click" Padding="10,5" Margin="0,0,10,0" Background="#2D2D30" Foreground="White"/>
                <Button Content="Universal" Click="Universal_Click" Padding="10,5" Margin="0,0,10,0" Background="#2D2D30" ToolTip="Apply these metadata settings to all other records in the current grid" Foreground="White"/>
                <Button Content="Delete" Click="Delete_Click" Padding="10,5" Margin="0,0,10,0" Background="#2D2D30" Foreground="White"/>
                <Button Content="Cancel" Click="Cancel_Click" Padding="10,5" Margin="0,0,10,0" Background="#2D2D30" Foreground="White"/>
                <Button Content="Save" Click="Save_Click" Padding="10,5" Background="#2D2D30" Foreground="White"/>
            </StackPanel>
        </Grid>"""

text = text.replace(button_target, button_replacement)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("UI patches applied!")
