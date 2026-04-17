import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\InstalledAssetsViewModel.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Define the start and end of strings to replace
start_marker = '        string[] hCrossing = new[] { "PartKey", "Description"'
end_marker = 'object?[] { i.PartKey, i.Name, i.Layer, i.DescriptionText, i.ScriptContent });'

if start_marker not in text or end_marker not in text:
    print("Markers not found!")
    sys.exit(1)

start_index = text.find(start_marker)
end_index = text.find(end_marker) + len(end_marker)

replacement = """        string[] hGlobal = new[] { "PartKey", "Discipline", "FeatureType", "Subtype", "FacilityOwner", "Size", "SizeSecondary", "Material", "PipeClass", "LiningManufacturer", "LiningMaterial", "Orientation", "PipeRole", "RfidBarcode", "DropType", "InvertElevationsWithDirections", "ExteriorJointTapeType", "ExteriorJointTapeManufacturer", "Quantity", "Manufacturer", "ManufacturerPartNo", "YearManufactured", "Confidence", "Source", "Warning", "Notes" };

        string[] hCrossing = hGlobal.Concat(new[] { "Description", "Northing", "Easting", "CrossingNumber", "UpperPipeType", "UpperPipeSize", "FinishedGradeElevation", "UpperPipeTopElevation", "UpperCover", "UpperPipeBottomElevation", "LowerPipeType", "LowerPipeSize", "LowerPipeTopElevation", "LowerCover", "Separation" }).ToArray();
        string[] hFigure = new[] { "AssetId", "Name", "Layer", "Description", "ScriptContent" };
        string[] hPipe = hGlobal.Concat(new[] { "Description", "Diameter", "NorthingStart", "EastingStart", "NorthingEnd", "EastingEnd", "InvertStart", "InvertEnd", "GradeElevationAtInvertStart", "GradeElevationAtInvertEnd" }).ToArray();
        string[] hPoint = hGlobal.Concat(new[] { "Description", "Northing", "Easting", "Elevation" }).ToArray();
        string[] hFitting = hGlobal.Concat(new[] { "Description", "Type", "Northing", "Easting", "Elevation" }).ToArray();
        string[] hValve = hGlobal.Concat(new[] { "Description", "Type", "Northing", "Easting", "Elevation", "OpenDirection", "TurnsToOpen", "NutElevation" }).ToArray();
        string[] hMeter = hGlobal.Concat(new[] { "Description", "Northing", "Easting", "Elevation" }).ToArray();

        object?[] GetGlobals(InstalledAsset i) => new object?[] { i.PartKey, i.Discipline, i.FeatureType, i.Subtype, i.FacilityOwner, i.Size, i.SizeSecondary, i.Material, i.PipeClass, i.LiningManufacturer, i.LiningMaterial, i.Orientation, i.PipeRole, i.RfidBarcode, i.DropType, i.InvertElevationsWithDirections, i.ExteriorJointTapeType, i.ExteriorJointTapeManufacturer, i.Quantity, i.Manufacturer, i.ManufacturerPartNo, i.YearManufactured, i.Confidence, i.Source, i.Warning, i.Notes };

        object?[] FormatCrossing(PipeCrossing i) => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.CrossingNumber, i.UpperPipeType, i.UpperPipeSize, i.FinishedGradeElevation, i.UpperPipeTopElevation, i.UpperCover, i.UpperPipeBottomElevation, i.LowerPipeType, i.LowerPipeSize, i.LowerPipeTopElevation, i.LowerCover, i.Separation }).ToArray();
        object?[] FormatPipe<T>(T i) where T : Pipe => GetGlobals(i).Concat(new object?[] { i.Description, i.Diameter, i.NorthingStart, i.EastingStart, i.NorthingEnd, i.EastingEnd, i.InvertStart, i.InvertEnd, i.GradeElevationAtInvertStart, i.GradeElevationAtInvertEnd }).ToArray();
        object?[] FormatPoint<T>(T i) where T : Structure => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatFitting<T>(T i) where T : Fitting => GetGlobals(i).Concat(new object?[] { i.Description, i.Type, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatValve<T>(T i) where T : Valve => GetGlobals(i).Concat(new object?[] { i.Description, i.Type, i.Northing, i.Easting, i.Elevation, i.OpenDirection, i.TurnsToOpen, i.NutElevation }).ToArray();
        object?[] FormatMeter<T>(T i) where T : Meter => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatHydrant<T>(T i) where T : Hydrant => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();
        object?[] FormatLocateBox<T>(T i) where T : LocateBox => GetGlobals(i).Concat(new object?[] { i.Description, i.Northing, i.Easting, i.Elevation }).ToArray();

        Write("General", "PipeCrossings", hCrossing, PipeCrossings, FormatCrossing);
        Write("General", "FigureAssets", hFigure, FigureAssets, i => new object?[] { i.PartKey, i.Name, i.Layer, i.DescriptionText, i.ScriptContent });"""

new_text = text[:start_index] + replacement + text[end_index:]

# Also fix the Write calls further down. For valves we should use FormatValve instead of FormatFitting inside the subsequent Write calls.
# Notice: Write("Water", "WaterValve", hFitting, WaterValves, FormatValve); was used, but it was using hFitting! We need to change hFitting to hValve!
new_text = new_text.replace('hFitting, WaterValves, FormatValve', 'hValve, WaterValves, FormatValve')
new_text = new_text.replace('hFitting, WWValves, FormatValve', 'hValve, WWValves, FormatValve')
new_text = new_text.replace('hFitting, ReclaimedValves, FormatValve', 'hValve, ReclaimedValves, FormatValve')
new_text = new_text.replace('hFitting, ChilledValves, FormatValve', 'hValve, ChilledValves, FormatValve')
new_text = new_text.replace('hFitting, GValves, FormatValve', 'hValve, GValves, FormatValve')
new_text = new_text.replace('hFitting, EValves, FormatValve', 'hValve, EValves, FormatValve')
new_text = new_text.replace('hFitting, STValves, FormatValve', 'hValve, STValves, FormatValve')

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_text)

print("Replaced properties successfully!")
