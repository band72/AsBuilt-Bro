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
            
            // Drop HorizontalAlignments to allow recreation with new ScriptContent layout
            try { context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"HorizontalAlignments\";"); } catch { }

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

            // Part Specifications
            try { context.Database.ExecuteSqlRaw("ALTER TABLE \"PartSpecifications\" ADD COLUMN \"OuterDiameter\" REAL NULL;"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE \"PartSpecifications\" ADD COLUMN \"NominalDiameter\" REAL NULL;"); } catch { }
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""PartSpecifications"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PartSpecifications"" PRIMARY KEY AUTOINCREMENT,
                    ""PartNumber"" TEXT NOT NULL,
                    ""OuterDiameter"" REAL NULL,
                    ""NominalDiameter"" REAL NULL,
                    ""PipeThickness"" REAL NULL,
                    ""InnerDiameter"" REAL NULL,
                    ""Deflection"" REAL NULL,
                    ""Note"" TEXT NULL
                );
            ");

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

             // REMOVED
             if (!context.AssetSubtypes.Any(s => s.Category == "Chilled Fitting"))
             {
                 var chilledFittings = new[] {
                     "Cross", "Elbow 11.25", "Elbow 22.5", "Elbow 45", "Elbow 90", "Plug", "Reducer", 
                     "Repair Coupling", "Sleeve", "Tapping Sleeve", "Tee", "Transition Coupling", "Other", 
                     "Unknown Fitting", "Vertical"
                 };

                 foreach (var cf in chilledFittings)
                 {
                     context.AssetSubtypes.Add(new Entities.AssetSubtypeEntity { Category = "Chilled Fitting", SubtypeName = cf });
                 }
                 context.SaveChanges();
             }

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

             // Seed Asset Subtypes Dynamically
             if (!context.AssetSubtypes.Any(s => s.Category == "Chilled Pipe Class"))
             {
                 var seedCategories = new System.Collections.Generic.Dictionary<string, string[]> {
                     { "Chilled Fitting", new[] { "Cross", "Elbow 11.25", "Elbow 22.5", "Elbow 45", "Elbow 90", "Plug", "Reducer", "Repair Coupling", "Sleeve", "Tapping Sleeve", "Tee", "Transition Coupling", "Other", "Unknown Fitting", "Vertical" } },
                     { "Locate Box", new[] { "Marker Ball", "Locate Wire Box" } },
                     { "Manhole", new[] { "Collection", "Effluent", "Force Main", "Low Pressure", "Trunk" } },
                     { "Reclaimed Fitting", new[] { "Cross", "Elbow 11.25", "Elbow 22.5", "Elbow 45", "Elbow 90", "Lateral Main Connection", "Plug", "Reducer", "Repair Coupling", "Service Lateral Fitting", "Sleeve", "Tapping Sleeve", "Tapping Saddle", "Tee", "Transition Coupling", "WYE", "Other", "Unknown Fitting", "Vertical", "Cap, Tapped", "Stub", "Cap" } },
                     { "Reclaimed Meter", new[] { "Control Meter", "Major Meter", "Minor Meter", "Plant Meter" } },
                     { "Reclaimed Pipe", new[] { "Augmentation Main", "Hydrant Lateral", "Reclaimed Main", "Service Lateral" } },
                     { "Reclaimed Valve", new[] { "Valve", "Backflow Preventor", "Hydrant Valve" } },
                     { "Sewer Customer Point", new[] { "Customer Point", "Sewer Flow Meter" } },
                     { "Sewer Fitting", new[] { "Cleanout", "Cross", "Elbow 11.25", "Elbow 22.5", "Elbow 45", "Elbow 90", "Lateral Main Connection", "Other", "Plug", "Reducer", "Repair Coupling", "Service Lateral Fitting", "Sleeve", "Stub", "Tapping Sleeve", "Tapping Saddle", "Tee", "Transition Coupling", "Unknown Fitting", "Vertical", "WYE", "Cap, Tapped", "Stub", "Cap" } },
                     { "Sewer Gravity Pipe", new[] { "Collection Main", "Trunk Main", "Collection Lateral" } },
                     { "Sewer Valve", new[] { "Valve", "Pump Out", "Air Release Valve" } },
                     { "Water Fitting", new[] { "Cross", "Elbow 11.25", "Elbow 22.5", "Elbow 45", "Elbow 90", "Lateral Main Connection", "Plug", "Reducer", "Repair Coupling", "Service Lateral Fitting", "Sleeve", "Tapping Sleeve", "Tapping Saddle", "Tee", "Transition Coupling", "Vertical", "WYE", "Other", "Unknown Fitting", "Cap, Tapped", "Stub", "Cap" } },
                     { "Water Meter", new[] { "Interconnect", "Major Meter", "Minor Meter", "Plant Meter", "Irrigation Meter", "Fire Meter" } },
                     { "Water Pipe", new[] { "Distribution Main", "Fire Line Main", "Raw Water Main", "Transmission Main", "Service Lateral", "Hydrant Lateral" } },
                     { "Water Valve", new[] { "Valve", "Backflow Preventor", "Hydrant Valve", "Air Release Valve" } },
                     { "Chilled Pipe Class", new[] { "CL50", "CL51", "DR11", "DR14", "DR17", "DR18", "DR25", "PC150", "PC250", "N/A", "Other", "Unknown" } },
                     { "Chilled Pipe Role", new[] { "Return", "Supply" } },
                     { "County", new[] { "Clay", "Duval", "Nassau", "St Johns" } },
                     { "Crossing Pipe Type", new[] { "Potable Water", "Gravity Sewer", "Force Main", "Vacuum Sewer", "Reclaimed", "Storm" } },
                     { "Facility Owner", new[] { "JEA", "Private", "Unknown" } },
                     { "Fitting Manufacturers", new[] { "American Cast Iron Pipe Company", "Cascade Waterworks Mfg", "Charlotte Pipe and Foundry Co", "Chemtrol/NIBCO", "Clow Valve", "Dresser Inc/GE", "FERNCO", "Ford Meter Box", "Galaxy Plastics", "Georg Fisher Sloane Manufacturing", "GPK Products Inc", "Harco Inc", "Harrington Corporation (HARCO)", "Ipex", "JCM Industries Inc", "Lasco Fittings Inc", "M&H Valve Company", "Mueller", "Mueller Aqua Grip", "Mueller Company", "Multi-Fittings", "Other", "Plastic Trends (Royal Building Projects)", "Power Seal", "Romac", "Romac Industries Inc", "Sigma Corp (Russell Pipe)", "SIP Industries", "Smith-Blair", "Spears Manufacturing", "Star Pipe Products", "TigreADS USA", "TPS Hymax", "Tyler Union", "Unknown", "US Pipe" } },
                     { "Hydrant Model", new[] { "American Darling", "American Flow", "AVK", "Clow", "Kennedy", "M&H", "Matthews", "Mueller", "US Pipe", "Waterous", "Other", "Unknown" } },
                     { "Manhole Drop Type", new[] { "Outside", "Inside", "Unknown" } },
                     { "Manhole Exterior Joint Tape Manufacturer", new[] { "Con Seal", "Rub-R-Nek/Henry Company", "Wrapid Seal (CCI Pipeline systems)", "Other", "Unknown" } }
                 };

                 foreach (var kvp in seedCategories)
                 {
                     string cat = kvp.Key;
                     foreach (var subName in kvp.Value)
                     {
                         if (!context.AssetSubtypes.Any(s => s.Category == cat && s.SubtypeName == subName))
                         {
                             context.AssetSubtypes.Add(new Entities.AssetSubtypeEntity { Category = cat, SubtypeName = subName });
                         }
                     }
                 }
                 context.SaveChanges();
             }

             var textColumns = new[] { 
                 "Discriminator", "PartKey", "Discipline", "FeatureType", "Subtype", "FacilityOwner",
                 "Size", "SizeSecondary", "Material", "PipeClass", "LiningManufacturer", "LiningMaterial",
                 "Orientation", "PipeRole", "DropType", "InvertElevationsWithDirections", "ExteriorJointTapeType",
                 "ExteriorJointTapeManufacturer", "Manufacturer", "ManufacturerPartNo", "YearManufactured", "RfidBarcode",
                 "ValveType", "OpenDirection", "ManholeType",
                 "CrossingNumber", "UpperPipeType", "UpperPipeSize", "LowerPipeType", "LowerPipeSize",
                 "UpstreamPointId", "DownstreamPointId"
             };

             var realColumns = new[] {
                 "GradeElevation", "TopElevation", "Depth", "Cover", "Length", "DownstreamInvert", "DownstreamGrade",
                 "UpstreamInvert", "UpstreamGrade", "Slope", "Easting", "Northing", "Latitude", "Longitude",
                 "TurnsToOpen", "NutElevation", "DepthToNut", "RimElevation", "LowestInvertElevation",
                 "UpperPipeTopElevation", "UpperCover", "UpperPipeBottomElevation", "LowerPipeTopElevation", "LowerCover", "Separation"
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
                     foreach (var col in realColumns)
                     {
                         try { context.Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{col}\" REAL NULL;"); } catch { }
                     }
                     try { context.Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN \"Quantity\" INTEGER NULL;"); } catch { }
                     try { context.Database.ExecuteSqlRaw($"ALTER TABLE \"{tableName}\" ADD COLUMN \"IsVisible\" INTEGER NOT NULL DEFAULT 1;"); } catch (Exception e) { System.Console.WriteLine($"DB INIT FAIL {tableName}: {e.Message}"); }
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



