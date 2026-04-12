using System.Windows;
using System.Windows.Controls;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

/// <summary>
/// DataTemplateSelector that swaps the center-pane content of the
/// AsBuiltWorkspaceView based on the currently active WorkflowPhase.
/// Register templates in the XAML Resources dictionary keyed by
/// phase name (e.g. x:Key="IntakePhaseTemplate").
/// </summary>
public class PhaseViewSelector : DataTemplateSelector
{
    // Each property receives a DataTemplate set in XAML via property element syntax.
    public DataTemplate? IntakeTemplate        { get; set; }
    public DataTemplate? PointsCleanupTemplate { get; set; }
    public DataTemplate? StructuresTemplate    { get; set; }
    public DataTemplate? PipeRunsTemplate      { get; set; }
    public DataTemplate? PartsMappingTemplate  { get; set; }
    public DataTemplate? ValidationTemplate    { get; set; }
    public DataTemplate? PreviewTemplate       { get; set; }
    public DataTemplate? DeliverablesTemplate  { get; set; }
    public DataTemplate? ExportPackageTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        // item is the AsBuiltWorkspaceViewModel; read its SelectedStep?.Phase
        var phase = item is ViewModels.AsBuiltWorkspaceViewModel vm
            ? vm.SelectedStep?.Phase
            : null;

        return phase switch
        {
            WorkflowPhase.Intake        => IntakeTemplate,
            WorkflowPhase.PointsCleanup => PointsCleanupTemplate,
            WorkflowPhase.Structures    => StructuresTemplate,
            WorkflowPhase.PipeRuns      => PipeRunsTemplate,
            WorkflowPhase.PartsMapping  => PartsMappingTemplate,
            WorkflowPhase.Validation    => ValidationTemplate,
            WorkflowPhase.Preview       => PreviewTemplate,
            WorkflowPhase.Deliverables  => DeliverablesTemplate,
            WorkflowPhase.ExportPackage => ExportPackageTemplate,
            _                           => DeliverablesTemplate   // safe default
        } ?? new DataTemplate();
    }
}
