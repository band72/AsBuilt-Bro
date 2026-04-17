import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Data\DbInitializer.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

target = """             var textColumns = new[] { 
                 "Discriminator", "Description", "PartKey", "Discipline", "FeatureType", "Size", "Material", 
                 "Manufacturer", "ManufacturerPartNo", "YearManufactured", "Confidence", "Source", "Warning", "Notes" 
             };"""

replacement_cols = """             var textColumns = new[] { 
                 "Discriminator", "PartKey", "Discipline", "FeatureType", "Subtype", "FacilityOwner",
                 "Size", "SizeSecondary", "Material", "PipeClass", "LiningManufacturer", "LiningMaterial",
                 "Orientation", "PipeRole", "DropType", "InvertElevationsWithDirections", "ExteriorJointTapeType",
                 "ExteriorJointTapeManufacturer", "Manufacturer", "ManufacturerPartNo", "YearManufactured", "RfidBarcode",
                 "ValveType", "OpenDirection", "ManholeType",
                 "CrossingNumber", "UpperPipeType", "UpperPipeSize", "LowerPipeType", "LowerPipeSize"
             };

             var realColumns = new[] {
                 "GradeElevation", "TopElevation", "Depth", "Cover", "Length", "DownstreamInvert", "DownstreamGrade",
                 "UpstreamInvert", "UpstreamGrade", "Slope", "Easting", "Northing", "Latitude", "Longitude",
                 "TurnsToOpen", "NutElevation", "DepthToNut", "RimElevation", "LowestInvertElevation",
                 "UpperPipeTopElevation", "UpperCover", "UpperPipeBottomElevation", "LowerPipeTopElevation", "LowerCover", "Separation"
             };"""

add_text_loop = """                     foreach (var col in textColumns)
                     {
                         try { context.Database.ExecuteSqlRaw($"ALTER TABLE \\"{tableName}\\" ADD COLUMN \\"{col}\\" TEXT NULL;"); } catch { }
                     }"""

add_all_loops = """                     foreach (var col in textColumns)
                     {
                         try { context.Database.ExecuteSqlRaw($"ALTER TABLE \\"{tableName}\\" ADD COLUMN \\"{col}\\" TEXT NULL;"); } catch { }
                     }
                     foreach (var col in realColumns)
                     {
                         try { context.Database.ExecuteSqlRaw($"ALTER TABLE \\"{tableName}\\" ADD COLUMN \\"{col}\\" REAL NULL;"); } catch { }
                     }"""

text = text.replace(target, replacement_cols).replace(add_text_loop, add_all_loops)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Patched DbInitializer successfully!")
