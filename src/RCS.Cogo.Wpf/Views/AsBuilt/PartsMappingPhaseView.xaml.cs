using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class PartsMappingPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;
    private bool _showUnmappedOnly;

    public PartsMappingPhaseView() => InitializeComponent();

    public void Load(AsBuiltJob job)
    {
        RefreshGrid();
        RefreshCounters();
    }

    private void BtnAutoMap_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        // Simple heuristic auto-mapper — match description keywords to known part keys
        var catalog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "manhole",     "MH-STD-48"   }, { "mh",          "MH-STD-48"   },
            { "inlet",       "INLET-STD"   }, { "catch basin",  "CB-STD"      },
            { "cleanout",    "CO-STD"      }, { "valve",        "VALVE-STD"   },
            { "meter",       "METER-STD"   }, { "fitting",      "FITTING-STD" },
            { "junction",    "JB-STD"      }, { "tee",          "TEE-STD"     },
            { "reducer",     "RED-STD"     }, { "plug",         "PLUG-STD"    },
        };

        int resolved = 0;
        foreach (var entry in Vm.Job.PartMappings.Where(p => p.Status == MappingStatus.Pending))
        {
            foreach (var kv in catalog)
            {
                if (entry.DetectedDesc.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                {
                    entry.ProposedPartKey = kv.Value;
                    entry.Confidence      = 0.80;
                    entry.Status          = MappingStatus.Resolved;
                    resolved++;
                    break;
                }
            }
        }
        RefreshGrid();
        RefreshCounters();
        Vm.RequestRevalidation();
    }

    private void BtnFilterUnmapped_Click(object s, System.Windows.RoutedEventArgs e)
    {
        _showUnmappedOnly = !_showUnmappedOnly;
        RefreshGrid();
        BtnFilterUnmapped.Content = _showUnmappedOnly ? "📋 Show All" : "🔍 Show Unmapped Only";
    }

    private void BtnMarkSkipped_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        foreach (PartMappingEntry entry in MappingGrid.SelectedItems)
            entry.Status = MappingStatus.Skipped;
        RefreshGrid();
        RefreshCounters();
        Vm.RequestRevalidation();
    }

    private void MappingGrid_CellEditEnding(object s, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new System.Action(() =>
            {
                RefreshCounters();
                Vm?.RequestRevalidation();
            }));
    }

    private void RefreshGrid()
    {
        if (Vm?.Job == null) return;
        var source = _showUnmappedOnly
            ? Vm.Job.PartMappings.Where(p => p.Status == MappingStatus.Pending || p.Status == MappingStatus.Error)
            : Vm.Job.PartMappings.AsEnumerable();
        MappingGrid.ItemsSource = source.ToList();
    }

    private void RefreshCounters()
    {
        if (Vm?.Job == null) return;
        int mapped  = Vm.Job.PartMappings.Count(p => p.Status == MappingStatus.Resolved || p.Status == MappingStatus.Skipped);
        int pending = Vm.Job.PartMappings.Count(p => p.Status == MappingStatus.Pending);
        int error   = Vm.Job.PartMappings.Count(p => p.Status == MappingStatus.Error);
        int total   = Vm.Job.PartMappings.Count;

        TxtMapped.Text  = $"{mapped} Mapped";
        TxtPending.Text = $"{pending} Pending";
        TxtError.Text   = $"{error} Error";

        double pct = total > 0 ? (double)mapped / total * 100.0 : 0;
        MappingProgress.Value = pct;
        TxtMappingPct.Text    = $"{pct:F0}%";
    }
}
