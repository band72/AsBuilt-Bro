import re

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\InstalledAssetsView.xaml'
with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

datagrids = re.findall(r'<DataGrid ItemsSource="\{Binding (.*?)\}".*?</DataGrid>', text, flags=re.DOTALL)
print(f'Total DataGrids with explicit ItemSource: {len(datagrids)}')
for b in datagrids:
    print(b)
