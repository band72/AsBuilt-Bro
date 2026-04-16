using System.Windows.Controls;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class PartsMappingPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;
    private bool _showUnmappedOnly;
    private string? _preEditMappingState;

    public PartsMappingPhaseView() => InitializeComponent();

    public void Load(AsBuiltJob job)
    {
        RefreshGrid();
        RefreshCounters();
    }

    private void BtnAutoMap_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var oldState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
        
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
        
        var newState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
        Vm.UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
            "Auto-Map Parts",
            j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(oldState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); },
            j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(newState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); }
        ));

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
        if (Vm?.Job == null || MappingGrid.SelectedItems.Count == 0) return;
        var oldState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);

        foreach (PartMappingEntry entry in MappingGrid.SelectedItems)
            entry.Status = MappingStatus.Skipped;
        RefreshGrid();
        RefreshCounters();

        var newState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
        Vm.UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
            "Mark Parts as Skipped",
            j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(oldState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); },
            j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(newState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); }
        ));

        Vm.RequestRevalidation();
    }

    private void MappingGrid_PreparingCellForEdit(object s, DataGridPreparingCellForEditEventArgs e)
    {
        if (Vm?.Job == null) return;
        _preEditMappingState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
    }

    private void MappingGrid_CellEditEnding(object s, DataGridCellEditEndingEventArgs e)
    {
        if (Vm?.Job == null || _preEditMappingState == null) return;
        var oldState = _preEditMappingState;

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new System.Action(() =>
            {
                RefreshCounters();
                var newState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
                if (oldState != newState)
                {
                    Vm.UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
                        "Edit Part Mapping",
                        j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(oldState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); },
                        j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(newState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); }
                    ));
                }
                _preEditMappingState = null;
                Vm?.RequestRevalidation();
            }));
    }

    private void BtnExport_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var dialog = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = "CodesLibrary.csv" };
        if (dialog.ShowDialog() == true)
        {
            var lines = new System.Collections.Generic.List<string> { "AssetId,DetectedDesc,ProposedPartKey,Manufacturer,NominalDiameter,PartMaterial,SDRClass,Notes,Confidence,Status" };
            foreach (var p in Vm.Job.PartMappings)
            {
                lines.Add($"{p.AssetId},{p.DetectedDesc},{p.ProposedPartKey},{p.Manufacturer},{p.NominalDiameter},{p.PartMaterial},{p.SDRClass},{p.Notes.Replace(",", ";")},{p.Confidence},{p.Status}");
            }
            File.WriteAllLines(dialog.FileName, lines);
            Vm.ShowSnackbarRequested?.Invoke("Codes Module successfully exported.", false);
        }
    }

    private void BtnImport_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var dialog = new OpenFileDialog { Filter = "CSV File (*.csv)|*.csv" };
        if (dialog.ShowDialog() == true)
        {
            var oldState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
            var lines = File.ReadAllLines(dialog.FileName).Skip(1);
            
            int updated = 0;
            foreach (var line in lines)
            {
                var p = line.Split(',');
                if (p.Length >= 10)
                {
                    var match = Vm.Job.PartMappings.FirstOrDefault(x => x.AssetId == p[0] || x.DetectedDesc == p[1]);
                    if (match != null)
                    {
                        match.ProposedPartKey = p[2];
                        match.Manufacturer = p[3];
                        double.TryParse(p[4], out double dia);
                        match.NominalDiameter = dia;
                        match.PartMaterial = p[5];
                        match.SDRClass = p[6];
                        match.Notes = p[7];
                        match.Status = MappingStatus.Resolved;
                        updated++;
                    }
                }
            }
            
            RefreshGrid();
            RefreshCounters();
            Vm.RequestRevalidation();
            Vm.ShowSnackbarRequested?.Invoke($"Imported and resolved {updated} feature codes mapping elements.", false);

            var newState = System.Text.Json.JsonSerializer.Serialize(Vm.Job.PartMappings);
            Vm.UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
                "Import Codes Library",
                j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(oldState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); },
                j => { var items = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<PartMappingEntry>>(newState); j.PartMappings.Clear(); if (items != null) foreach (var i in items) j.PartMappings.Add(i); }
            ));
        }
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
