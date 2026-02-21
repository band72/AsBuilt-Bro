using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RCS.Data.Entities;

public class ValidationRuleEntity
{
    [Key]
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string RuleDescription { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class AppGlobalSetting
{
    [Key]
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
}
