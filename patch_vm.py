import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\InstalledAssetsViewModel.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

start_marker = '        string[] hGlobal = new[]'
end_marker = 'Write("STLocateBox", STLocateBoxes, hPoint, FormatLocateBox);\n    }'

if start_marker not in text or end_marker not in text:
    print("Export formatting bloc not found!")
    sys.exit(1)

start_index = text.find(start_marker)
end_index = text.find(end_marker) + len(end_marker)

replacement = """        string[] hFigure = new[] { "AssetId", "Name", "Layer", "Description", "ScriptContent" };
        string[] hPipeCross = new[] { "Crossing Number", "Upper Pipe Type", "Upper Pipe Size (Inches)", "Finished Grade Elevation (Feet)", "Upper Pipe Top Elevation (Feet)", "Cover to Top of Upper Pipe (Feet)", "Upper Pipe Bottom Elevation (Feet)", "Lower Pipe Type", "Lower Pipe Size (Inches)", "Lower Pipe Top Elevation (Feet)", "Cover to Top of Lower Pipe (Feet)", "Separation Between Pipes (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        
        string[] hPipeGen = new[] { "Pipe Run Number", "Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Measured Length (Feet)" };
        string[] hPointGen = new[] { "Pipe Location Number", "Pipe Location", "Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Orientation", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Finished Grade Elevation (Feet)", "Pipe Top Elevation (Feet)", "Pipe Cover (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        string[] hFittingGen = new[] { "Fitting Number", "Fitting Subtype", "Facility Owner", "Fitting Size Primary (Inches)", "Fitting Size Secondary (Inches)", "Manufacturer", "Fitting Material", "Lining Manufacturer", "Lining Material", "Fitting Top Elevation (Feet)", "Finished Grade Elevation (Feet)", "Fitting Depth (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        string[] hValveGen = new[] { "Valve Number", "Valve Subtype", "Valve Type", "Facility Owner", "Valve Size", "Valve Orientation", "Valve Open Direction", "Turns to Open", "Valve Nut Elevation (Feet)", "Finished Grade Elevation (Feet)", "Depth to Nut (Feet)", "Valve Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        string[] hHydrantGen = new[] { "Hydrant Number", "Facility Owner", "Hydrant Manufacture Date (Year)", "Hydrant Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)", "RFID/Barcode Number" };
        string[] hMeterGen = new[] { "Meter Box Number", "Proposed Meter Size", "Meter Box Subtype", "Facility Owner", "Meter Box Orientation", "Meter Box Manufacturer/Supplier", "Meter Box Material", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        string[] hLocateGen = new[] { "Locate Box Number", "Locate Box Subtype", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };
        
        string[] hGravityPipe = new[] { "Sewer Pipe Run Number (GM#)", "Sewer Pipe Subtype", "Facility Owner", "Pipe Size (Inches)", "Pipe Class", "Pipe Manufacturer", "Pipe Material", "Pipe Lining Manufacturer", "Pipe Lining Material", "Pipe Run Length (feet)", "Downstream Pipe Invert Elevation (feet)", "Downstream Grade Elevation at Invert (feet)", "Upstream Pipe Invert Elevation (feet)", "Upstream Grade Elevation at Invert (feet)", "Slope (percent)" };
        string[] hManhole = new[] { "Manhole Number (MH#)", "Manhole Subtype", "Facility Owner", "Manhole Type", "Manhole Drop Type", "Manufacturer or Supplier", "Manhole Size (Feet)", "Manhole Material", "Manhole Lining Material", "Manhole Lining Manufacturer", "Rim Elevation (Feet)", "Invert Elevations (Feet) with Directions", "Lowest Invert Elevation (feet)", "Exterior Joint Tape Type", "Exterior Joint Tape Manufacturer", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)", "RFID/Barcode Number" };
        string[] hServicePoint = new[] { "Wastewater Service Point Number", "Service Point Subtype", "Finished Grade Elevation at Service Point", "Top of Pipe Elevation at Service Point (Feet)", "Depth of Cover (Feet)", "X Coord (State Plane Easting Feet)", "Y Coord (State Plane Northing Feet)", "Latitude (Decimal Degrees)", "Longitude (Decimal Degrees)" };

        object?[] FCross(PipeCrossing i) => new object?[] { i.CrossingNumber, i.UpperPipeType, i.UpperPipeSize, i.GradeElevation, i.UpperPipeTopElevation, i.UpperCover, i.UpperPipeBottomElevation, i.LowerPipeType, i.LowerPipeSize, i.LowerPipeTopElevation, i.LowerCover, i.Separation, i.Easting, i.Northing, i.Latitude, i.Longitude };
        object?[] FPipe(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.FacilityOwner, i.Size, i.PipeClass, i.Manufacturer, i.Material, i.LiningManufacturer, i.LiningMaterial, i.Length };
        object?[] FPoint(InstalledAsset i) => new object?[] { i.PartKey, "", i.Subtype, i.FacilityOwner, i.Size, i.Orientation, i.PipeClass, i.Manufacturer, i.Material, i.LiningManufacturer, i.LiningMaterial, i.GradeElevation, i.TopElevation, i.Cover, i.Easting, i.Northing, i.Latitude, i.Longitude };
        object?[] FFitting(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.FacilityOwner, i.Size, i.SizeSecondary, i.Manufacturer, i.Material, i.LiningManufacturer, i.LiningMaterial, i.TopElevation, i.GradeElevation, i.Depth, i.Easting, i.Northing, i.Latitude, i.Longitude };
        object?[] FValve(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.ValveType, i.FacilityOwner, i.Size, i.Orientation, i.OpenDirection, i.TurnsToOpen, i.NutElevation, i.GradeElevation, i.DepthToNut, i.Manufacturer, i.Easting, i.Northing, i.Latitude, i.Longitude };
        object?[] FHydrant(InstalledAsset i) => new object?[] { i.PartKey, i.FacilityOwner, i.YearManufactured, i.Manufacturer, i.Easting, i.Northing, i.Latitude, i.Longitude, i.RfidBarcode };
        object?[] FMeter(InstalledAsset i) => new object?[] { i.PartKey, i.Size, i.Subtype, i.FacilityOwner, i.Orientation, i.Manufacturer, i.Material, i.Easting, i.Northing, i.Latitude, i.Longitude };
        object?[] FLocate(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.Easting, i.Northing, i.Latitude, i.Longitude };
        
        object?[] FWWGravity(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.FacilityOwner, i.Size, i.PipeClass, i.Manufacturer, i.Material, i.LiningManufacturer, i.LiningMaterial, i.Length, i.DownstreamInvert, i.DownstreamGrade, i.UpstreamInvert, i.UpstreamGrade, i.Slope };
        object?[] FManhole(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.FacilityOwner, i.ManholeType, i.DropType, i.Manufacturer, i.Size, i.Material, i.LiningMaterial, i.LiningManufacturer, i.RimElevation, i.InvertElevationsWithDirections, i.LowestInvertElevation, i.ExteriorJointTapeType, i.ExteriorJointTapeManufacturer, i.Easting, i.Northing, i.Latitude, i.Longitude, i.RfidBarcode };
        object?[] FServicePoint(InstalledAsset i) => new object?[] { i.PartKey, i.Subtype, i.GradeElevation, i.TopElevation, i.Cover, i.Easting, i.Northing, i.Latitude, i.Longitude };

        Write("General", "PipeCrossings", hPipeCross, PipeCrossings, FCross);
        Write("General", "FigureAssets", hFigure, FigureAssets, i => new object?[] { i.Id, i.Name, i.Layer, "Description Removed", i.ScriptContent });

        Write("Water", "WaterPipeRun", hPipeGen, WaterPipes, FPipe);
        Write("Water", "WaterPointsAlongPipe", hPointGen, WaterPoints, FPoint);
        Write("Water", "WaterFitting", hFittingGen, WaterFittings, FFitting);
        Write("Water", "WaterValve", hValveGen, WaterValves, FValve);
        Write("Water", "WaterHydrant", hHydrantGen, WaterHydrants, FHydrant);
        Write("Water", "WaterMeter", hMeterGen, WaterMeters, FMeter);
        Write("Water", "WaterLocateBox", hLocateGen, WaterLocateBoxes, FLocate);

        Write("WW", "WWGravityPipeRun", hGravityPipe, WWGravityPipes, FWWGravity);
        Write("WW", "WWPressurePipeRun", hPipeGen, WWPressurePipes, FPipe);
        Write("WW", "WWPointsAlongPipe", hPointGen, WWPoints, FPoint);
        Write("WW", "WWFitting", hFittingGen, WWFittings, FFitting);
        Write("WW", "Manhole", hManhole, Manholes, FManhole);
        Write("WW", "WWServicePointMeter", hServicePoint, WWServicePoints, FServicePoint);
        Write("WW", "WWValve", hValveGen, WWValves, FValve);
        Write("WW", "WWLocateBox", hLocateGen, WWLocateBoxes, FLocate);

        Write("Reclaimed", "ReclaimedPipeRun", hPipeGen, ReclaimedPipes, FPipe);
        Write("Reclaimed", "ReclaimedPointsAlongPipe", hPointGen, ReclaimedPoints, FPoint);
        Write("Reclaimed", "ReclaimedFitting", hFittingGen, ReclaimedFittings, FFitting);
        Write("Reclaimed", "ReclaimedValve", hValveGen, ReclaimedValves, FValve);
        Write("Reclaimed", "ReclaimedHydrant", hHydrantGen, ReclaimedHydrants, FHydrant);
        Write("Reclaimed", "ReclaimedMeter", hMeterGen, ReclaimedMeters, FMeter);
        Write("Reclaimed", "ReclaimedLocateBox", hLocateGen, ReclaimedLocateBoxes, FLocate);

        Write("Chilled", "ChilledPipeRun", hPipeGen, ChilledPipes, FPipe);
        Write("Chilled", "ChilledPointsAlongPipe", hPointGen, ChilledPoints, FPoint);
        Write("Chilled", "ChilledFitting", hFittingGen, ChilledFittings, FFitting);
        Write("Chilled", "ChilledValve", hValveGen, ChilledValves, FValve);
        Write("Chilled", "ChilledMeter", hMeterGen, ChilledMeters, FMeter);
        Write("Chilled", "ChilledLocateBox", hLocateGen, ChilledLocateBoxes, FLocate);

        Write("Gas", "GasGravityPipeRun", hGravityPipe, GGravityPipes, FWWGravity);
        Write("Gas", "GasPressurePipeRun", hPipeGen, GPressurePipes, FPipe);
        Write("Gas", "GasPointsAlongPipe", hPointGen, GPoints, FPoint);
        Write("Gas", "GasFitting", hFittingGen, GFittings, FFitting);
        Write("Gas", "GasManhole", hManhole, GManholes, FManhole);
        Write("Gas", "GasServicePointMeter", hServicePoint, GServicePoints, FServicePoint);
        Write("Gas", "GasValve", hValveGen, GValves, FValve);
        Write("Gas", "GasLocateBox", hLocateGen, GLocateBoxes, FLocate);

        Write("Electric", "ElectricGravityPipeRun", hGravityPipe, EGravityPipes, FWWGravity);
        Write("Electric", "ElectricPressurePipeRun", hPipeGen, EPressurePipes, FPipe);
        Write("Electric", "ElectricPointsAlongPipe", hPointGen, EPoints, FPoint);
        Write("Electric", "ElectricFitting", hFittingGen, EFittings, FFitting);
        Write("Electric", "ElectricManhole", hManhole, EManholes, FManhole);
        Write("Electric", "ElectricServicePointMeter", hServicePoint, EServicePoints, FServicePoint);
        Write("Electric", "ElectricValve", hValveGen, EValves, FValve);
        Write("Electric", "ElectricLocateBox", hLocateGen, ELocateBoxes, FLocate);
        
        Write("Storm", "STGravityPipeRun", hGravityPipe, STGravityPipes, FWWGravity);
        Write("Storm", "STPressurePipeRun", hPipeGen, STPressurePipes, FPipe);
        Write("Storm", "STPointsAlongPipe", hPointGen, STPoints, FPoint);
        Write("Storm", "STFitting", hFittingGen, STFittings, FFitting);
        Write("Storm", "STManhole", hManhole, STManholes, FManhole);
        Write("Storm", "STServicePointMeter", hServicePoint, STServicePoints, FServicePoint);
        Write("Storm", "STValve", hValveGen, STValves, FValve);
        Write("Storm", "STLocateBox", hLocateGen, STLocateBoxes, FLocate);
    }"""

new_text = text[:start_index] + replacement + text[end_index:]

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_text)

print("ViewModel patched!")
