using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RCS.Data;

class Program
{
    static void Main()
    {
        try
        {
            using (var db = new AppDbContext())
            {
                foreach (var entityType in db.Model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    if (!string.IsNullOrEmpty(tableName) && 
                        tableName != "CogoCodes" && tableName != "Materials" && 
                         tableName != "SymbolManager" && tableName != "GlobalSettings" && tableName != "ValidationRules")
                    {
                        try {
                            db.Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN \"IsVisible\" INTEGER NOT NULL DEFAULT 1;");
                            Console.WriteLine($"Added IsVisible to {tableName}");
                        }
                        catch {}
                    }
                }
                Console.WriteLine("All tables patched.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
