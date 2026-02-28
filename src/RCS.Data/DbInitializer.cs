using Microsoft.EntityFrameworkCore;

namespace RCS.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Ensure Database Exists (Creates if not present)
        context.Database.EnsureCreated();
        
        // Manual Schema Updates (Running scripts if needed for new tables on existing DB)
        try
        {
            // Cogo Codes
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""CogoCodes"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CogoCodes"" PRIMARY KEY AUTOINCREMENT,
                    ""LocalCode"" TEXT NOT NULL,
                    ""SystemCode"" TEXT NULL,
                    ""Description"" TEXT NULL
                );
            ");

            // Materials
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Materials"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Materials"" PRIMARY KEY AUTOINCREMENT,
                    ""PartKey"" TEXT NULL,
                    ""Discipline"" TEXT NULL,
                    ""FeatureType"" TEXT NULL,
                    ""Size"" TEXT NULL,
                    ""Material"" TEXT NULL,
                    ""Manufacturer"" TEXT NULL,
                    ""Model"" TEXT NULL,
                    ""Year"" TEXT NULL,
                    ""Notes"" TEXT NULL
                );
            ");

            // Symbols
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""SymbolManager"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SymbolManager"" PRIMARY KEY AUTOINCREMENT,
                    ""ClientCode"" TEXT NULL,
                    ""SystemCode"" TEXT NULL,
                    ""Symbol"" TEXT NULL,
                    ""Type"" TEXT NULL,
                    ""Discipline"" TEXT NULL
                );
            ");
             // Clean up old bugged default seed data (which conflicted with symbol file names)
             var buggedCodes = new[] { "JEAWV", "JEAWF", "JEAWH", "STM", "JEASTF", "MH", "WWF", "GASV", "GASF", "GMET", "EPOLE", "EMH", "EBOX", "EMETER" };
             var toRemove = context.CogoCodes.Where(c => buggedCodes.Contains(c.LocalCode)).ToList();
             if (toRemove.Any()) context.CogoCodes.RemoveRange(toRemove);

             // Auto-seed from MasterUtilityCodes.csv to ensure all 43 valid codes exist
             var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
             var repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", ".."));
             var csvPath = System.IO.Path.Combine(repoRoot, "MasterUtilityCodes.csv");
             
             if (System.IO.File.Exists(csvPath))
             {
                 var lines = System.IO.File.ReadAllLines(csvPath);
                 foreach (var line in lines.Skip(1)) // Skip header
                 {
                     if (string.IsNullOrWhiteSpace(line)) continue;
                     var parts = line.Split(',');
                     if (parts.Length >= 2)
                     {
                         string local = parts[0].Trim();
                         string sys = parts[1].Trim();
                         string desc = parts.Length >= 3 ? parts[2].Trim() : sys;
                         
                         // Insert if the exact local/sys mapping doesn't already exist
                         if (!context.CogoCodes.Any(c => c.LocalCode == local && c.SystemCode == sys))
                         {
                             context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = local, SystemCode = sys, Description = desc });
                         }
                     }
                 }
             }

             // Seed Electric Materials
             if (!context.Materials.Any(m => m.Material == "ALUM"))
             {
                 context.Materials.Add(new Entities.MaterialEntity { Material = "ALUM", Discipline = "Electric", FeatureType = "Wire", Notes = "Aluminum Wire" });
             }

             context.SaveChanges();
        }
        catch (Exception ex)
        {
             // Log or rethrow? For now, we assume this is safe.
             System.Diagnostics.Debug.WriteLine($"Error verifying schema: {ex.Message}");
        }
    }
}
