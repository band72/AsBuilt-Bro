using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using RCS.Data;
using RCS.Data.Entities;
using RCS.Cogo.Wpf.Commands;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RCS.Cogo.Wpf.ViewModels;

public class ValidationRuleModel : ViewModelBase
{
    private readonly ValidationRuleEntity _entity;
    
    public int Id => _entity.Id;
    public string Category => _entity.Category;
    public string FieldName => _entity.FieldName;
    public string RuleDescription
    {
        get => _entity.RuleDescription;
        set {
            if (_entity.RuleDescription != value)
            {
                _entity.RuleDescription = value;
                OnPropertyChanged(nameof(RuleDescription));
            }
        }
    }

    public bool IsEnabled
    {
        get => _entity.IsEnabled;
        set { 
            if (_entity.IsEnabled != value)
            {
                _entity.IsEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    public ValidationRuleEntity Entity => _entity;

    public ValidationRuleModel(ValidationRuleEntity entity)
    {
        _entity = entity;
    }
}

public class ValidationCategoryGroup
{
    public string CategoryName { get; set; } = string.Empty;
    public ObservableCollection<ValidationRuleModel> Rules { get; set; } = new();
}

public class ValidationSettingsViewModel : ViewModelBase
{
    // Bound property for global disable
    private bool _noExplicitValidation;
    public bool NoExplicitValidation
    {
        get => _noExplicitValidation;
        set => SetField(ref _noExplicitValidation, value);
    }
    
    // Grouped by Category for display
    public ObservableCollection<ValidationCategoryGroup> CategoryGroups { get; } = new();

    private List<ValidationRuleModel> _allRules = new();

    public ICommand CheckAllCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand SaveChangesCommand { get; }

    public ValidationSettingsViewModel()
    {
        CheckAllCommand = new RelayCommand(_ => SetAll(true));
        ClearAllCommand = new RelayCommand(_ => SetAll(false));
        SaveChangesCommand = new RelayCommand(_ => SaveSettings());
        
        LoadData();
    }

    private void SetAll(bool state)
    {
        foreach (var rule in _allRules)
        {
            rule.IsEnabled = state;
        }
    }

    private void LoadData()
    {
        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            
            // Explicitly create tables if EnsureCreated skipped them
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ValidationRules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Category TEXT,
                    FieldName TEXT,
                    RuleDescription TEXT,
                    IsEnabled INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS GlobalSettings (
                    SettingKey TEXT PRIMARY KEY,
                    SettingValue TEXT
                );
            ");
            
            // Handle Global setting
            var g = db.GlobalSettings.FirstOrDefault(s => s.SettingKey == "NoExplicitValidation");
            if (g != null)
            {
                _noExplicitValidation = g.SettingValue == "True";
            }
            
            // Handle rules, seed if empty or partially seeded
            if (db.ValidationRules.Count() < 100)
            {
                db.Database.ExecuteSqlRaw("DELETE FROM ValidationRules");
                SeedRules(db);
            }

            var entities = db.ValidationRules.ToList();
            _allRules = entities.Select(e => new ValidationRuleModel(e)).ToList();

            var groups = _allRules.GroupBy(r => r.Category)
                .Select(g => new ValidationCategoryGroup 
                { 
                    CategoryName = string.IsNullOrWhiteSpace(g.Key) ? "General" : g.Key, 
                    Rules = new ObservableCollection<ValidationRuleModel>(g) 
                })
                .OrderBy(g => g.CategoryName)
                .ToList();

            CategoryGroups.Clear();
            foreach (var group in groups)
            {
                CategoryGroups.Add(group);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading validation rules: {ex.Message}");
        }
    }

    private void SeedRules(AppDbContext db)
    {
        string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "validation_rules.json");
        if (!File.Exists(jsonPath)) return;

        string json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            try 
            {
                // The JSON format from powershell
                string category = element.TryGetProperty("Pipe Crossing Table", out var cObj) ? cObj.GetString() ?? "" : "";
                string fieldName = element.TryGetProperty("Crossing Number", out var fObj) ? fObj.GetString() ?? "" : "";
                string ruleDesc = element.TryGetProperty("Open Text", out var rObj) ? rObj.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(fieldName)) continue;

                db.ValidationRules.Add(new ValidationRuleEntity
                {
                    Category = category,
                    FieldName = fieldName,
                    RuleDescription = ruleDesc,
                    IsEnabled = true
                });
            }
            catch { }
        }
        db.SaveChanges();
    }

    public void SaveSettings()
    {
        try
        {
            using var db = new AppDbContext();
            
            // Save Global
            var g = db.GlobalSettings.FirstOrDefault(s => s.SettingKey == "NoExplicitValidation");
            if (g == null)
            {
                g = new AppGlobalSetting { SettingKey = "NoExplicitValidation" };
                db.GlobalSettings.Add(g);
            }
            g.SettingValue = NoExplicitValidation.ToString();
            
            // Save Rules
            foreach (var r in _allRules)
            {
                db.ValidationRules.Update(r.Entity);
            }
            
            db.SaveChanges();
            System.Windows.MessageBox.Show("Validation Settings Saved Successfully.");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving validation rules: {ex.Message}");
        }
    }
}
