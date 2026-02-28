using System;
using System.Linq;
using RCS.Data;
using RCS.Data.Entities;

namespace RCS.Services;

public static class GlobalSettingsService
{
    public static string GetSetting(string key, string defaultValue)
    {
        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            var setting = db.GlobalSettings.FirstOrDefault(s => s.SettingKey == key);
            return setting != null ? setting.SettingValue : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public static void SaveSetting(string key, string value)
    {
        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            var setting = db.GlobalSettings.FirstOrDefault(s => s.SettingKey == key);
            
            if (setting == null)
            {
                db.GlobalSettings.Add(new AppGlobalSetting { SettingKey = key, SettingValue = value });
            }
            else
            {
                setting.SettingValue = value;
            }
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Saving Setting {key}: {ex.Message}");
        }
    }
}
