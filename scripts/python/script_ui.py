import re

def h(title, prop):
    return f'<DataGridTextColumn Header="{title}" Binding="{{Binding {prop}}}" />'

cols_pipe_cross = [
    h("Crossing Number", "CrossingNumber"),
    h("Upper Pipe Type", "UpperPipeType"),
    h("Upper Pipe Size (Inches)", "UpperPipeSize"),
    h("Finished Grade Elevation (Feet)", "GradeElevation"),
    h("Upper Pipe Top Elevation (Feet)", "UpperPipeTopElevation"),
    h("Cover to Top of Upper Pipe (Feet)", "UpperCover"),
    h("Upper Pipe Bottom Elevation (Feet)", "UpperPipeBottomElevation"),
    h("Lower Pipe Type", "LowerPipeType"),
    h("Lower Pipe Size (Inches)", "LowerPipeSize"),
    h("Lower Pipe Top Elevation (Feet)", "LowerPipeTopElevation"),
    h("Cover to Top of Lower Pipe (Feet)", "LowerCover"),
    h("Separation Between Pipes (Feet)", "Separation"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

cols_pipe_gen = [
    h("Pipe Run Number", "PartKey"),
    h("Pipe Subtype", "Subtype"),
    h("Facility Owner", "FacilityOwner"),
    h("Pipe Size (Inches)", "Size"),
    h("Pipe Class", "PipeClass"),
    h("Pipe Manufacturer", "Manufacturer"),
    h("Pipe Material", "Material"),
    h("Pipe Lining Manufacturer", "LiningManufacturer"),
    h("Pipe Lining Material", "LiningMaterial"),
    h("Measured Length (Feet)", "Length")
]

cols_point_gen = [
    h("Pipe Location Number", "PartKey"),
    h("Pipe Subtype", "Subtype"),
    h("Facility Owner", "FacilityOwner"),
    h("Pipe Size (Inches)", "Size"),
    h("Pipe Orientation", "Orientation"),
    h("Pipe Class", "PipeClass"),
    h("Pipe Manufacturer", "Manufacturer"),
    h("Pipe Material", "Material"),
    h("Pipe Lining Manufacturer", "LiningManufacturer"),
    h("Pipe Lining Material", "LiningMaterial"),
    h("Finished Grade Elevation (Feet)", "GradeElevation"),
    h("Pipe Top Elevation (Feet)", "TopElevation"),
    h("Pipe Cover (Feet)", "Cover"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

cols_fitting_gen = [
    h("Fitting Number", "PartKey"),
    h("Fitting Subtype", "Subtype"),
    h("Facility Owner", "FacilityOwner"),
    h("Fitting Size Primary (Inches)", "Size"),
    h("Fitting Size Secondary (Inches)", "SizeSecondary"),
    h("Manufacturer", "Manufacturer"),
    h("Fitting Material", "Material"),
    h("Lining Manufacturer", "LiningManufacturer"),
    h("Lining Material", "LiningMaterial"),
    h("Fitting Top Elevation (Feet)", "TopElevation"),
    h("Finished Grade Elevation (Feet)", "GradeElevation"),
    h("Fitting Depth (Feet)", "Depth"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

cols_valve_gen = [
    h("Valve Number", "PartKey"),
    h("Valve Subtype", "Subtype"),
    h("Valve Type", "ValveType"),
    h("Facility Owner", "FacilityOwner"),
    h("Valve Size", "Size"),
    h("Valve Orientation", "Orientation"),
    h("Valve Open Direction", "OpenDirection"),
    h("Turns to Open", "TurnsToOpen"),
    h("Valve Nut Elevation (Feet)", "NutElevation"),
    h("Finished Grade Elevation (Feet)", "GradeElevation"),
    h("Depth to Nut (Feet)", "DepthToNut"),
    h("Valve Manufacturer", "Manufacturer"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

cols_hydrant_gen = [
    h("Hydrant Number", "PartKey"),
    h("Facility Owner", "FacilityOwner"),
    h("Hydrant Manufacture Date (Year)", "YearManufactured"),
    h("Hydrant Manufacturer", "Manufacturer"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude"),
    h("RFID/Barcode Number", "RfidBarcode")
]

cols_meter_gen = [
    h("Meter Box Number", "PartKey"),
    h("Proposed Meter Size", "Size"),
    h("Meter Box Subtype", "Subtype"),
    h("Facility Owner", "FacilityOwner"),
    h("Meter Box Orientation", "Orientation"),
    h("Meter Box Manufacturer/Supplier", "Manufacturer"),
    h("Meter Box Material", "Material"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

cols_locate_gen = [
    h("Locate Box Number", "PartKey"),
    h("Locate Box Subtype", "Subtype"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

cols_gravity_pipe = [
    h("Sewer Pipe Run Number (GM#)", "PartKey"),
    h("Sewer Pipe Subtype", "Subtype"),
    h("Facility Owner", "FacilityOwner"),
    h("Pipe Size (Inches)", "Size"),
    h("Pipe Class", "PipeClass"),
    h("Pipe Manufacturer", "Manufacturer"),
    h("Pipe Material", "Material"),
    h("Pipe Lining Manufacturer", "LiningManufacturer"),
    h("Pipe Lining Material", "LiningMaterial"),
    h("Pipe Run Length (feet)", "Length"),
    h("Downstream Pipe Invert Elevation (feet)", "DownstreamInvert"),
    h("Downstream Grade Elevation at Invert (feet)", "DownstreamGrade"),
    h("Upstream Pipe Invert Elevation (feet)", "UpstreamInvert"),
    h("Upstream Grade Elevation at Invert (feet)", "UpstreamGrade"),
    h("Slope (percent)", "Slope")
]

cols_manhole = [
    h("Manhole Number (MH#)", "PartKey"),
    h("Manhole Subtype", "Subtype"),
    h("Facility Owner", "FacilityOwner"),
    h("Manhole Type", "ManholeType"),
    h("Manhole Drop Type", "DropType"),
    h("Manufacturer or Supplier", "Manufacturer"),
    h("Manhole Size (Feet)", "Size"),
    h("Manhole Material", "Material"),
    h("Manhole Lining Material", "LiningMaterial"),
    h("Manhole Lining Manufacturer", "LiningManufacturer"),
    h("Rim Elevation (Feet)", "RimElevation"),
    h("Invert Elevations (Feet) with Directions", "InvertElevationsWithDirections"),
    h("Lowest Invert Elevation (feet)", "LowestInvertElevation"),
    h("Exterior Joint Tape Type", "ExteriorJointTapeType"),
    h("Exterior Joint Tape Manufacturer", "ExteriorJointTapeManufacturer"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude"),
    h("RFID/Barcode Number", "RfidBarcode")
]

cols_service_point = [
    h("Wastewater Service Point Number", "PartKey"),
    h("Service Point Subtype", "Subtype"),
    h("Finished Grade Elevation at Service Point", "GradeElevation"),
    h("Top of Pipe Elevation at Service Point (Feet)", "TopElevation"),
    h("Depth of Cover (Feet)", "Cover"),
    h("X Coord (State Plane Easting Feet)", "Easting"),
    h("Y Coord (State Plane Northing Feet)", "Northing"),
    h("Latitude (Decimal Degrees)", "Latitude"),
    h("Longitude (Decimal Degrees)", "Longitude")
]

maps = {
    "PipeCrossings": cols_pipe_cross,
    "WaterPipes": cols_pipe_gen,
    "WaterPoints": cols_point_gen,
    "WaterFittings": cols_fitting_gen,
    "WaterValves": cols_valve_gen,
    "WaterHydrants": cols_hydrant_gen,
    "WaterMeters": cols_meter_gen,
    "WaterLocateBoxes": cols_locate_gen,
    "WWGravityPipes": cols_gravity_pipe,
    "WWPressurePipes": cols_pipe_gen,
    "WWPoints": cols_point_gen,
    "WWFittings": cols_fitting_gen,
    "Manholes": cols_manhole,
    "WWServicePoints": cols_service_point,
    "WWValves": cols_valve_gen,
    "WWLocateBoxes": cols_locate_gen,
    "ReclaimedPipes": cols_pipe_gen,
    "ReclaimedPoints": cols_point_gen,
    "ReclaimedFittings": cols_fitting_gen,
    "ReclaimedValves": cols_valve_gen,
    "ReclaimedHydrants": cols_hydrant_gen,
    "ReclaimedMeters": cols_meter_gen,
    "ReclaimedLocateBoxes": cols_locate_gen,
    "GGravityPipes": cols_gravity_pipe,
    "GPressurePipes": cols_pipe_gen,
    "GPoints": cols_point_gen,
    "GFittings": cols_fitting_gen,
    "GManholes": cols_manhole,
    "GServicePoints": cols_service_point,
    "GValves": cols_valve_gen,
    "GLocateBoxes": cols_locate_gen,
    "EGravityPipes": cols_gravity_pipe,
    "EPressurePipes": cols_pipe_gen,
    "EPoints": cols_point_gen,
    "EFittings": cols_fitting_gen,
    "EManholes": cols_manhole,
    "EServicePoints": cols_service_point,
    "EValves": cols_valve_gen,
    "ELocateBoxes": cols_locate_gen,
    "STGravityPipes": cols_gravity_pipe,
    "STPressurePipes": cols_pipe_gen,
    "STPoints": cols_point_gen,
    "STFittings": cols_fitting_gen,
    "STManholes": cols_manhole,
    "STServicePoints": cols_service_point,
    "STValves": cols_valve_gen,
    "STLocateBoxes": cols_locate_gen,
    "ChilledPipes": cols_pipe_gen,
    "ChilledPoints": cols_point_gen,
    "ChilledFittings": cols_fitting_gen,
    "ChilledValves": cols_valve_gen,
    "ChilledMeters": cols_meter_gen,
    "ChilledLocateBoxes": cols_locate_gen
}

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\Views\InstalledAssetsView.xaml'
with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

for binding, new_cols in maps.items():
    pattern = r'(<DataGrid ItemsSource="\{Binding ' + binding + r'\}"(.*?)>)(.*?)(</DataGrid>)'
    
    def replacement(match):
        header_tag = match.group(1)
        trailer_tag = match.group(4)
        cols_content = "\n                             ".join(new_cols)
        
        return f'{header_tag}\n                         <DataGrid.Columns>\n                             {cols_content}\n                         </DataGrid.Columns>\n                    {trailer_tag}'
        
    text = re.sub(pattern, replacement, text, flags=re.DOTALL)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(text)
