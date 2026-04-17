with open(r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\ShellViewModel.cs', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('public System.Windows.Input.ICommand ExportDxfCommand { get; }', 
'''public System.Windows.Input.ICommand ExportDxfCommand { get; }
    public System.Windows.Input.ICommand CreateUploadPackageCommand { get; }''')

text = text.replace('ExportDxfCommand = new RelayCommand(_ => ExportDxf());',
'''ExportDxfCommand = new RelayCommand(_ => ExportDxf());
        CreateUploadPackageCommand = new RelayCommand(_ => CreateUploadPackage());''')

method_impl = '''
    private void CreateUploadPackage()
    {
        var proj = _context.CurrentProject;
        if (proj == null)
        {
            System.Windows.MessageBox.Show("Open a project first.", "No Project", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        string availNo = proj.AvailabilityNumber?.Trim() ?? "UNKNOWN";
        string projectName = proj.ProjectName?.Trim() ?? "UNKNOWN";
        string utility = "Water"; // Assuming Water by default per common template, or "Mixed"
        string units = "FT";
        string revisionLabel = "REV1";

        string lockedStem = RCS.Packaging.Naming.JeaNaming.LockedStem(availNo, projectName, utility, units);
        
        // Setup Save Folder Prompt using SaveFileDialog as Folder Picker
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select location to create Upload Package (Hit Save)",
            FileName = "Save_Here", // Dummy name
            Filter = "Directory|*.directory",
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false
        };

        if (dialog.ShowDialog() == true)
        {
            try 
            {
                string basePath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? "";
                if (string.IsNullOrWhiteSpace(basePath)) return;
                
                string packageDir = System.IO.Path.Combine(basePath, lockedStem);
                System.IO.Directory.CreateDirectory(packageDir);
                
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageDir, "01_LandXML"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageDir, "02_DXF"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageDir, "03_Points"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageDir, "04_Parts"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageDir, "05_Certification"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(packageDir, "99_Manifest"));
                
                string manifestDir = System.IO.Path.Combine(packageDir, "99_Manifest");
                string readmeContent = RCS.Packaging.Readme.UploadReadmeBuilder.Build(availNo, projectName, revisionLabel);
                
                System.IO.File.WriteAllText(System.IO.Path.Combine(packageDir, "README.txt"), readmeContent);
                System.IO.File.WriteAllText(System.IO.Path.Combine(manifestDir, "manifest.json"), "{}");
                
                System.Windows.MessageBox.Show($"Upload Package created at:\\n{packageDir}", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _context.Log($"[AUDIT] Created Upload Package {lockedStem}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating package: {ex.Message}");
                _context.Log($"[ERROR] Package Creation Error: {ex.Message}");
            }
        }
    }
'''

if 'CreateUploadPackage()' not in text:
    text = text.replace('private void ExportDxf()', method_impl + '\n    private void ExportDxf()')

with open(r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\ShellViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(text)
print("Patched successfully!")
