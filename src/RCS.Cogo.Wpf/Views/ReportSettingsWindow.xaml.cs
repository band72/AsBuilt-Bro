using System;
using System.Windows;
using RCS.Cogo.App.Models;

namespace RCS.Cogo.Wpf.Views;

public partial class ReportSettingsWindow : Window
{
    private readonly ReportConfiguration _config;
    private readonly ReportSettingsViewModel _viewModel;

    public ReportSettingsWindow(ReportConfiguration config)
    {
        InitializeComponent();
        _config = config;
        
        // Ensure not null
        if (_config == null) _config = new ReportConfiguration();

        _viewModel = new ReportSettingsViewModel(_config);
        DataContext = _viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Commit();
        DialogResult = true;
        Close();
    }
}

public class ReportSettingsViewModel
{
    private readonly ReportConfiguration _model;

    // View Properties
    public bool ExportWater { get; set; }
    public string WaterSheetName { get; set; }
    
    public bool ExportSewer { get; set; }
    public string SewerSheetName { get; set; }

    public bool ExportStorm { get; set; }
    public string StormSheetName { get; set; }

    public bool ExportGas { get; set; }
    public string GasSheetName { get; set; }
    
    public bool IncludeNullValues { get; set; }
    
    // Bearing Format Logic
    public bool IsDMS { get; set; }
    public bool IsDD { get; set; }

    public ReportSettingsViewModel(ReportConfiguration model)
    {
        _model = model;
        
        // Load
        ExportWater = model.ExportWater;
        WaterSheetName = model.WaterSheetName;
        ExportSewer = model.ExportSewer;
        SewerSheetName = model.SewerSheetName;
        ExportStorm = model.ExportStorm;
        StormSheetName = model.StormSheetName;
        ExportGas = model.ExportGas;
        GasSheetName = model.GasSheetName;
        IncludeNullValues = model.IncludeNullValues;
        
        // Default to DMS if null
        if (string.IsNullOrWhiteSpace(model.BearingFormat)) model.BearingFormat = "DMS";
        
        IsDMS = model.BearingFormat == "DMS";
        IsDD = model.BearingFormat == "DD";
    }

    public void Commit()
    {
        _model.ExportWater = ExportWater;
        _model.WaterSheetName = WaterSheetName;
        _model.ExportSewer = ExportSewer;
        _model.SewerSheetName = SewerSheetName;
        _model.ExportStorm = ExportStorm;
        _model.StormSheetName = StormSheetName;
        _model.ExportGas = ExportGas;
        _model.GasSheetName = GasSheetName;
        _model.IncludeNullValues = IncludeNullValues;
        
        _model.BearingFormat = IsDD ? "DD" : "DMS";
    }
}
