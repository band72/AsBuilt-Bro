import re

with open(r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\ShellViewModel.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# First, let's remove the badly injected block at the very end.
bad_injection_pattern = re.compile(r'^\s*private void CreateUploadPackage\(\)[\s\S]*?\}\s*\}\s*$', re.MULTILINE)
text = bad_injection_pattern.sub('}\n', text)

# Just to make absolutely sure it's gone:
method_marker = "private void CreateUploadPackage()"
if method_marker in text:
    text = text[:text.find(method_marker)] + "}\n" # truncate trailing stuff if it failed

method_impl = '''
    private void CreateUploadPackage()
    {
        var proj = _currentProject;
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
        
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select location to create Upload Package (Hit Save)",
            FileName = "Save_Here", 
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
                // Logging
                CommandLog.Add($"[AUDIT] Created Upload Package {lockedStem}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating package: {ex.Message}");
                CommandLog.Add($"[ERROR] Package Creation Error: {ex.Message}");
            }
        }
    }
'''

# Find a good place to inject INSIDE the class. Right before `private void ExecuteTestNativeSecurity()` or `private void ExportDxf()`
if "private void ExportDxf()" in text:
    text = text.replace("private void ExportDxf()", method_impl + "\n    private void ExportDxf()")
elif "private void ExecuteTestNativeSecurity()" in text:
    text = text.replace("private void ExecuteTestNativeSecurity()", method_impl + "\n    private void ExecuteTestNativeSecurity()")
else:
    # Append right before the last closing braces of the class. Let's find "}" at the end.
    idx = text.rfind("}")
    idx = text.rfind("}", 0, idx) # second to last `}` is likely the class 
    text = text[:idx] + method_impl + "\n" + text[idx:]
    
with open(r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\ShellViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(text)
print('Injected successfully!')
