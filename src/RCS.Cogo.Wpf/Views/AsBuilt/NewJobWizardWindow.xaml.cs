using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class NewJobWizardWindow : Window
{
    private int _step = 1;
    private const int TotalSteps = 4;

    /// <summary>The configured job produced by this wizard. Null = cancelled.</summary>
    public AsBuiltJob? ResultJob { get; private set; }

    public NewJobWizardWindow() => InitializeComponent();

    // ── Window chrome ─────────────────────────────────────────────────────────
    private void TitleBar_MouseDown(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
    private void BtnClose_Click(object s, RoutedEventArgs e) { ResultJob = null; Close(); }

    // ── Navigation ────────────────────────────────────────────────────────────
    private void BtnNext_Click(object s, RoutedEventArgs e)
    {
        if (!ValidateCurrentStep()) return;
        if (_step < TotalSteps) { _step++; ShowStep(); }
        else { BuildJob(); Close(); }
    }

    private void BtnBack_Click(object s, RoutedEventArgs e)
    {
        if (_step > 1) { _step--; ShowStep(); }
    }

    private void ShowStep()
    {
        Step1.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        Step4.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;

        var labels = new[] { "Job Identity", "Coordinate Environment", "Import Sources", "Expected Deliverables" };
        TxtStepTitle.Text = $"Step {_step} of {TotalSteps} — {labels[_step - 1]}";

        BtnBack.IsEnabled = _step > 1;
        BtnNext.Content   = _step == TotalSteps ? "✅ Create Job" : "Next →";

        var pips = new[] { Pip1, Pip2, Pip3, Pip4 };
        for (int i = 0; i < pips.Length; i++)
            pips[i].Background = new SolidColorBrush(
                i < _step
                    ? Color.FromRgb(0x4A, 0x90, 0xE2)
                    : Color.FromRgb(0x2A, 0x2A, 0x45));
    }

    private bool ValidateCurrentStep()
    {
        if (_step == 1 && string.IsNullOrWhiteSpace(TxtJobNumber.Text))
        {
            MessageBox.Show("Please enter a Job Number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    // ── Import buttons ────────────────────────────────────────────────────────
    private void BtnImportPnezd_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "CSV/PNEZD|*.csv;*.txt|All|*.*", Title = "Select PNEZD File" };
        if (dlg.ShowDialog() == true) TxtPnezdPath.Text = dlg.FileName;
    }
    private void BtnImportJea_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Excel|*.xlsx;*.xls|All|*.*", Title = "Select JEA Excel File" };
        if (dlg.ShowDialog() == true) TxtJeaPath.Text = dlg.FileName;
    }
    private void BtnImportCogo_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "COGO Script|*.cogo;*.txt|All|*.*", Title = "Select COGO Script" };
        if (dlg.ShowDialog() == true) TxtCogoPath.Text = dlg.FileName;
    }

    // ── Build the job from wizard answers ─────────────────────────────────────
    private void BuildJob()
    {
        CoordinateEnvironment coordEnv = RbStatePlane.IsChecked == true ? CoordinateEnvironment.StatePlane
                                       : RbLocalGrid.IsChecked  == true ? CoordinateEnvironment.LocalGrid
                                       : RbGps.IsChecked        == true ? CoordinateEnvironment.Gps
                                       : CoordinateEnvironment.Unknown;

        var job = new AsBuiltJob
        {
            Identity = new ProjectIdentity
            {
                JobNumber     = TxtJobNumber.Text.Trim(),
                ClientName    = TxtClientName.Text.Trim(),
                UtilityOwner  = TxtUtilityOwner.Text.Trim(),
                County        = TxtCounty.Text.Trim(),
                Drafter       = TxtDrafter.Text.Trim(),
                Checker       = TxtChecker.Text.Trim(),
                FieldDate     = DtpFieldDate.SelectedDate,
            },
            Environment = coordEnv,
        };

        // Deliverables
        job.Deliverables.Clear();
        if (ChkDxf.IsChecked     == true) job.Deliverables.Add(new DeliverableCard { Type = "DXF Drawing" });
        if (ChkPdf.IsChecked     == true) job.Deliverables.Add(new DeliverableCard { Type = "PDF Report" });
        if (ChkPnezd.IsChecked   == true) job.Deliverables.Add(new DeliverableCard { Type = "PNEZD" });
        if (ChkLandXml.IsChecked == true) job.Deliverables.Add(new DeliverableCard { Type = "LandXML" });
        if (ChkParts.IsChecked   == true) job.Deliverables.Add(new DeliverableCard { Type = "Parts Report" });
        if (ChkCert.IsChecked    == true) job.Deliverables.Add(new DeliverableCard { Type = "Certification Package" });

        // Pending imports (loaded in the background by IntakeAnalysisEngine)
        if (!string.IsNullOrEmpty(TxtPnezdPath.Text))  job.PendingImportPaths.Add(TxtPnezdPath.Text);
        if (!string.IsNullOrEmpty(TxtJeaPath.Text))    job.PendingImportPaths.Add(TxtJeaPath.Text);
        if (!string.IsNullOrEmpty(TxtCogoPath.Text))   job.PendingImportPaths.Add(TxtCogoPath.Text);

        ResultJob = job;
    }
}
