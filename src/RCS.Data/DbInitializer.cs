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
            // Drop unique constraint on ProjectNumber which breaks generic 0000 fallback projects
            try { context.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS \"IX_Projects_ProjectNumber\";"); } catch { }

            // Upgrade Schema: Add TopOutsideWallElev, OuterWallThicknessTop, InnerDiameter, AdjustedInvert to legacy tables
            var tablesToUpgrade = new[] { "Pipes", "Structures", "Valves", "Fittings", "Meters", 
                "PipeCrossings", "WaterHydrants", "WaterLocateBoxes", "ReclaimedHydrants", 
                "ReclaimedLocateBoxes", "GLocateBoxes", "ELocateBoxes", "ChilledLocateBoxes", 
                "STLocateBoxes", "WWLocateBoxes" };
            foreach (var table in tablesToUpgrade)
            {
                var newCols = new[] { "TopOutsideWallElev", "OuterWallThicknessTop", "InnerDiameter", "AdjustedInvert" };
                foreach (var col in newCols)
                    try { context.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{col}\" REAL NULL;"); } catch { }
            }
            // Cogo Codes
            try { context.Database.ExecuteSqlRaw("ALTER TABLE \"CogoCodes\" ADD COLUMN \"Block\" TEXT NULL;"); } catch { }
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""CogoCodes"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CogoCodes"" PRIMARY KEY AUTOINCREMENT,
                    ""LocalCode"" TEXT NOT NULL,
                    ""SystemCode"" TEXT NULL,
                    ""Description"" TEXT NULL,
                    ""Block"" TEXT NULL
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

             // Seed Materials from JEA_Validation_List.csv
             var matCsvPath = System.IO.Path.Combine(repoRoot, "JEA_Validation_List.csv");
             if (System.IO.File.Exists(matCsvPath))
             {
                 var lines = System.IO.File.ReadAllLines(matCsvPath);
                 foreach (var line in lines.Skip(1)) // Skip header
                 {
                     if (string.IsNullOrWhiteSpace(line)) continue;
                     var parts = line.Split(',');
                     if (parts.Length >= 5)
                     {
                         string pKey = parts[0].Trim();
                         string disc = parts[1].Trim();
                         string feat = parts[2].Trim();
                         string siz = parts[3].Trim();
                         string mat = parts[4].Trim();
                         string mfg = parts.Length > 6 ? parts[6].Trim() : "";
                         string mod = parts.Length > 7 ? parts[7].Trim() : "";
                         string yr = parts.Length > 8 ? parts[8].Trim() : "";
                         string nts = parts.Length > 12 ? parts[12].Trim() : "";

                         if (!context.Materials.Any(m => m.PartKey == pKey))
                         {
                             context.Materials.Add(new Entities.MaterialEntity 
                             { 
                                 PartKey = pKey, 
                                 Discipline = disc, 
                                 FeatureType = feat, 
                                 Size = siz, 
                                 Material = mat, 
                                 Manufacturer = mfg, 
                                 Model = mod, 
                                 Year = yr, 
                                 Notes = nts 
                             });
                         }
                     }
                 }
             }

             // Seed additional generic testing materials
             if (!context.Materials.Any(m => m.Material == "PE"))
                 context.Materials.Add(new Entities.MaterialEntity { Material = "PE", Discipline = "Gas", FeatureType = "Pipe", Notes = "Polyethylene" });

             if (!context.Materials.Any(m => m.Material == "PVC"))
                 context.Materials.Add(new Entities.MaterialEntity { Material = "PVC", Discipline = "Water", FeatureType = "Pipe", Notes = "Polyvinyl Chloride" });

             // Seed Electric Materials
             if (!context.Materials.Any(m => m.Material == "ALUM"))
             {
                 context.Materials.Add(new Entities.MaterialEntity { Material = "ALUM", Discipline = "Electric", FeatureType = "Wire", Notes = "Aluminum Wire" });
             }

             context.SaveChanges();

             // Force creation of newly added tables (e.g. WaterPipes, WWValves, etc.) 
             // since EnsureCreated() skips them if DB file already exists.
             var createScript = context.Database.GenerateCreateScript();
             var sqlCommands = createScript.Split(';', StringSplitOptions.RemoveEmptyEntries);
             
             foreach (var sqlCmd in sqlCommands)
             {
                 if (string.IsNullOrWhiteSpace(sqlCmd)) continue;
                 
                 // Optionally convert "CREATE TABLE" -> "CREATE TABLE IF NOT EXISTS" for peace of mind in SQLite
                 var safeCmd = sqlCmd;
                 safeCmd = safeCmd.Replace("CREATE TABLE \"", "CREATE TABLE IF NOT EXISTS \"");
                 safeCmd = safeCmd.Replace("CREATE UNIQUE INDEX \"", "CREATE UNIQUE INDEX IF NOT EXISTS \"");
                 safeCmd = safeCmd.Replace("CREATE INDEX \"", "CREATE INDEX IF NOT EXISTS \"");

                 try
                 {
                     context.Database.ExecuteSqlRaw(safeCmd);
                 }
                 catch
                 {
                     // Typically fails if a column constraint already exists, just ignore.
                 }
             }

             // Schema Backfill: Add newly introduced columns to all Asset Tables
             // This prevents "no such column: Description" (or descriptor) crashes
             var textColumns = new[] { 
                 "Discriminator", "Description", "PartKey", "Discipline", "FeatureType", "Size", "Material", 
                 "Manufacturer", "ManufacturerPartNo", "YearManufactured", "Confidence", "Source", "Warning", "Notes" 
             };
             
             foreach (var entityType in context.Model.GetEntityTypes())
             {
                 var tableName = entityType.GetTableName();
                 if (!string.IsNullOrEmpty(tableName) && 
                     tableName != "CogoCodes" && tableName != "Materials" && 
                     tableName != "SymbolManager" && tableName != "GlobalSettings" && tableName != "ValidationRules")
                 {
                     foreach (var col in textColumns)
                     {
                         try { context.Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{col}\" TEXT NULL;"); } catch { }
                     }
                     try { context.Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN \"Quantity\" INTEGER NULL;"); } catch { }
                 }
             }
        }
        catch (Exception ex)
        {
             // Log or rethrow? For now, we assume this is safe.
             System.Diagnostics.Debug.WriteLine($"Error verifying schema: {ex.Message}");
        }
    }
}
