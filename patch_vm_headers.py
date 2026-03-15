import re

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\InstalledAssetsViewModel.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Fix FPoint blank entry
text = re.sub(
    r'object\?\[\] FPoint\(InstalledAsset i\) => new object\?\[\] \{ i.PartKey, "", i.Subtype,',
    r'object?[] FPoint(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype,',
    text
)

# New Header Maps
hPipeCross = r'string[] hPipeCross = new[] { "CrossingNumber", "UpperPipeType", "UpperPipeSize", "GradeElevation", "UpperPipeTopElevation", "UpperCover", "UpperPipeBottomElevation", "LowerPipeType", "LowerPipeSize", "LowerPipeTopElevation", "LowerCover", "Separation", "Easting", "Northing", "Latitude", "Longitude" };'
hPipeGen = r'string[] hPipeGen = new[] { "PartKey", "Subtype", "FacilityOwner", "Size", "PipeClass", "Manufacturer", "Material", "LiningManufacturer", "LiningMaterial", "Length" };'
hPointGen = r'string[] hPointGen = new[] { "PartKey", "Subtype", "FacilityOwner", "Size", "Orientation", "PipeClass", "Manufacturer", "Material", "LiningManufacturer", "LiningMaterial", "GradeElevation", "TopElevation", "Cover", "Easting", "Northing", "Latitude", "Longitude" };'
hFittingGen = r'string[] hFittingGen = new[] { "PartKey", "Subtype", "FacilityOwner", "Size", "SizeSecondary", "Manufacturer", "Material", "LiningManufacturer", "LiningMaterial", "TopElevation", "GradeElevation", "Depth", "Easting", "Northing", "Latitude", "Longitude" };'
hValveGen = r'string[] hValveGen = new[] { "PartKey", "Subtype", "ValveType", "FacilityOwner", "Size", "Orientation", "OpenDirection", "TurnsToOpen", "NutElevation", "GradeElevation", "DepthToNut", "Manufacturer", "Easting", "Northing", "Latitude", "Longitude" };'
hHydrantGen = r'string[] hHydrantGen = new[] { "PartKey", "FacilityOwner", "YearManufactured", "Manufacturer", "Easting", "Northing", "Latitude", "Longitude", "RfidBarcode" };'
hMeterGen = r'string[] hMeterGen = new[] { "PartKey", "Size", "Subtype", "FacilityOwner", "Orientation", "Manufacturer", "Material", "Easting", "Northing", "Latitude", "Longitude" };'
hLocateGen = r'string[] hLocateGen = new[] { "PartKey", "Subtype", "Easting", "Northing", "Latitude", "Longitude" };'

hGravityPipe = r'string[] hGravityPipe = new[] { "PartKey", "Subtype", "FacilityOwner", "Size", "PipeClass", "Manufacturer", "Material", "LiningManufacturer", "LiningMaterial", "Length", "DownstreamInvert", "DownstreamGrade", "UpstreamInvert", "UpstreamGrade", "Slope" };'
hManhole = r'string[] hManhole = new[] { "PartKey", "Subtype", "FacilityOwner", "ManholeType", "DropType", "Manufacturer", "Size", "Material", "LiningMaterial", "LiningManufacturer", "RimElevation", "InvertElevationsWithDirections", "LowestInvertElevation", "ExteriorJointTapeType", "ExteriorJointTapeManufacturer", "Easting", "Northing", "Latitude", "Longitude", "RfidBarcode" };'
hServicePoint = r'string[] hServicePoint = new[] { "PartKey", "Subtype", "GradeElevation", "TopElevation", "Cover", "Easting", "Northing", "Latitude", "Longitude" };'

# Replace them inside the file
text = re.sub(r'string\[\] hPipeCross = new\[\] \{.*?\};', hPipeCross, text)
text = re.sub(r'string\[\] hPipeGen = new\[\] \{.*?\};', hPipeGen, text)
text = re.sub(r'string\[\] hPointGen = new\[\] \{.*?\};', hPointGen, text)
text = re.sub(r'string\[\] hFittingGen = new\[\] \{.*?\};', hFittingGen, text)
text = re.sub(r'string\[\] hValveGen = new\[\] \{.*?\};', hValveGen, text)
text = re.sub(r'string\[\] hHydrantGen = new\[\] \{.*?\};', hHydrantGen, text)
text = re.sub(r'string\[\] hMeterGen = new\[\] \{.*?\};', hMeterGen, text)
text = re.sub(r'string\[\] hLocateGen = new\[\] \{.*?\};', hLocateGen, text)
text = re.sub(r'string\[\] hGravityPipe = new\[\] \{.*?\};', hGravityPipe, text)
text = re.sub(r'string\[\] hManhole = new\[\] \{.*?\};', hManhole, text)
text = re.sub(r'string\[\] hServicePoint = new\[\] \{.*?\};', hServicePoint, text)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)
