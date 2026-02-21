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
}

public class ProjectDetailsViewModel
{
    private Project _model;

    public string ProjectName { get; set; }
    public string AvailNo { get; set; }
    public string Utility { get; set; }
    public string Units { get; set; }
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
        Settings = project.Settings ?? new ProjectSettings();
    }

    public void Commit()
    {
        // Create timestamp suffix if AvailNo is empty
        if (string.IsNullOrWhiteSpace(AvailNo))
        {
            // Format: -MMddyyyyffff (Month Day Year 4-digit Milliseconds)
            string suffix = DateTime.Now.ToString("-MMddyyyyffff");
            
            // Avoid duplicate appending if the suffix already exists (simple heuristic: ends with 13 chars of digits)
            // But 'ffff' changes every time. Just append if not already looking like it.
            // Actually, user requested "if the availability number is empty, append...".
            // Let's modify the Property directly so the Model reflects it.
            ProjectName += suffix;
        }

        _model.ProjectName = ProjectName;
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
    }
}
