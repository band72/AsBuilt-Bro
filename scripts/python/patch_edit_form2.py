import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\EditAssetWindow.xaml'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

replacements = {
    '<TextBox Text="{Binding Material}" Background="#252526" Foreground="White" Padding="5"/>':
    '<ComboBox Text="{Binding Material}" ItemsSource="{Binding MaterialsList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    
    '<TextBox Text="{Binding Subtype}" Background="#252526" Foreground="White" Padding="5"/>':
    '<ComboBox Text="{Binding Subtype}" ItemsSource="{Binding SubtypesList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    
    '<TextBox Text="{Binding FacilityOwner}" Background="#252526" Foreground="White" Padding="5"/>':
    '<ComboBox Text="{Binding FacilityOwner}" ItemsSource="{Binding FacilityOwnersList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    
    '<TextBox Text="{Binding PipeClass}" Background="#252526" Foreground="White" Padding="5"/>':
    '<ComboBox Text="{Binding PipeClass}" ItemsSource="{Binding PipeClassList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>',
    
    '<TextBox Text="{Binding Orientation}" Background="#252526" Foreground="White" Padding="5"/>':
    '<ComboBox Text="{Binding Orientation}" ItemsSource="{Binding OrientationsList, RelativeSource={RelativeSource AncestorType=Window}}" IsEditable="True" Background="#252526" Foreground="White" Padding="5"/>'
}

for k, v in replacements.items():
    text = text.replace(k, v)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("ComboBoxes updated successfully!")
