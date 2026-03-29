using System;
using System.Collections.Generic;
using System.Windows;
using RCS.Cogo.App.Models;

namespace RCS.Cogo.Wpf.Views;

public partial class ProjectDetailsWindow : Window
{
    private Project _project;

    public ProjectDetailsWindow(Project project)
    {
        InitializeComponent();
        _project = project;

        // Simple binding context
        this.DataContext = new ProjectDetailsViewModel(_project);
    }
    
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Simple validation
        var vm = (ProjectDetailsViewModel)DataContext;
        if (string.IsNullOrWhiteSpace(vm.ProjectName))
        {
            MessageBox.Show("Project Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Commit changes back to model (if not bound directly, but here we bound directly to VM which wraps model)
        vm.Commit();
        
        DialogResult = true;
        Close();
    }
    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var vm = (ProjectDetailsViewModel)DataContext;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Base Project Folder",
            FileName = "Save_Here", 
            Filter = "Directory|*.directory",
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false
        };

        if (dialog.ShowDialog() == true)
        {
            string basePath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? "";
            
            // Build the requested JEA name string using the packaging format
            if (string.IsNullOrWhiteSpace(vm.AvailNo))
            {
                vm.AvailNo = DateTime.Now.ToString("MMddyyyyffff");
            }
            string availNo = vm.AvailNo;
            string projectName = string.IsNullOrWhiteSpace(vm.ProjectName) ? "UNKNOWN" : vm.ProjectName;
            string utility = string.IsNullOrWhiteSpace(vm.Utility) ? "Mixed" : vm.Utility;
            string units = string.IsNullOrWhiteSpace(vm.Units) ? "FT" : vm.Units;
            
            string lockedStem = RCS.Packaging.Naming.JeaNaming.LockedStem(availNo, projectName, utility, units);
            
            vm.SaveLocation = System.IO.Path.Combine(basePath, lockedStem);
        }
    }
}

public class ProjectDetailsViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private Project _model;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    private string _saveLocation = string.Empty;
    public string SaveLocation 
    { 
        get => _saveLocation; 
        set { _saveLocation = value; OnPropertyChanged(); } 
    }

    public string ProjectName { get; set; } = string.Empty;
    public string AvailNo    { get; set; } = string.Empty;
    public string Utility    { get; set; } = string.Empty;
    public string Units      { get; set; } = string.Empty;
    public int Revision { get; set; }
    public ProjectSettings Settings { get; set; } // Reference directly for now

    public List<string> UtilityTypes { get; } = new List<string> { "Sewer", "Water", "Electric", "Gas", "Telecom" };
    public List<string> UnitTypes { get; } = new List<string> { "USFT", "Meters" };

    public ProjectDetailsViewModel(Project project)
    {
        _model = project;
        ProjectName = project.ProjectName;
        AvailNo = project.AvailNo;
        Utility = project.Utility;
        Units = project.Units;
        Revision = project.Revision;
        SaveLocation = project.SaveLocation;
        Settings = project.Settings ?? new ProjectSettings();
    }

    public void Commit()
    {
        if (string.IsNullOrWhiteSpace(AvailNo))
        {
            AvailNo = DateTime.Now.ToString("MMddyyyyffff");
        }

        _model.ProjectName = ProjectName;
        _model.SaveLocation = SaveLocation;
        _model.AvailNo = AvailNo;
        _model.Utility = Utility;
        _model.Units = Units;
        _model.Revision = Revision;
        _model.Settings = Settings;
        
        // Update deliverables list based on settings
        _model.Deliverables.Clear();
        if (Settings.RequirePdfReport) _model.Deliverables.Add(new Deliverable { Name = "Certification Report (PDF)" });
        if (Settings.RequireLandXml) _model.Deliverables.Add(new Deliverable { Name = "LandXML Export" });
        if (Settings.RequireCsv) _model.Deliverables.Add(new Deliverable { Name = "CSV Point Export" });
        
        // Actually physically create the directory!
        if (!string.IsNullOrWhiteSpace(_model.SaveLocation))
        {
            try 
            {
                System.IO.Directory.CreateDirectory(_model.SaveLocation);
            }
            catch (Exception) { /* Ignoring file io exceptions directly here for now */ }
        }
    }
}
