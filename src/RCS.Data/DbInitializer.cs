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
             // Seed Default Water Codes
             if (!context.CogoCodes.Any(c => c.LocalCode == "JEAWV"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "JEAWV", SystemCode = "W-VALVE", Description = "Water Valve" });
             }
             if (!context.CogoCodes.Any(c => c.LocalCode == "JEAWF"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "JEAWF", SystemCode = "W-FITTING", Description = "Water Fitting" });
             }
             if (!context.CogoCodes.Any(c => c.LocalCode == "JEAWH"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "JEAWH", SystemCode = "W-HYDRANT", Description = "Fire Hydrant" });
             }

             // Seed Default Storm & Sewer Codes
             if (!context.CogoCodes.Any(c => c.LocalCode == "STM"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "STM", SystemCode = "ST-MANHOLE", Description = "Storm Manhole" });
             }
             if (!context.CogoCodes.Any(c => c.LocalCode == "JEASTF"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "JEASTF", SystemCode = "ST-FITTING", Description = "Storm Fitting / Outfall" });
             }
             if (!context.CogoCodes.Any(c => c.LocalCode == "MH"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "MH", SystemCode = "WW-MANHOLE", Description = "Sanitary Manhole" });
             }
             if (!context.CogoCodes.Any(c => c.LocalCode == "WWF"))
             {
                 context.CogoCodes.Add(new Entities.CogoCodeEntity { LocalCode = "WWF", SystemCode = "WW-FITTING", Description = "Sanitary Fitting" });
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
