import sys

file_path = r'c:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Wpf\ViewModels\InstalledAssetsViewModel.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    text = f.read()

start_str = "    public void ExportToFolder(string baseName)"
end_str = "}\n" # Just ending the class

clean_methods = """
    public void ExportToFolder(string folderPath, string format)
    {
        string C(string? s) => "\\\"" + (s ?? "").Replace("\\\"", "\\\"\\\"") + "\\\"";
        bool isTab = format.Equals("txt", StringComparison.OrdinalIgnoreCase);
        bool isExcel = format.Equals("xls", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase);

        void Write<T>(string title, string[] headers, System.Collections.ObjectModel.ObservableCollection<T> items, Func<T, object?[]> formatter)
        {
            if (isExcel)
            {
                using var wb = new ClosedXML.Excel.XLWorkbook();
                var ws = wb.Worksheets.Add(title);
                ws.SheetView.FreezeRows(1);
                
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                }
                
                int row = 2;
                foreach(var item in items)
                {
                    var vals = formatter(item);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        var v = vals[i];
                        if (v is double d) ws.Cell(row, i + 1).Value = d;
                        else if (v is int integer) ws.Cell(row, i + 1).Value = integer;
                        else ws.Cell(row, i + 1).Value = v?.ToString() ?? "";
                    }
                    row++;
                }
                wb.SaveAs(System.IO.Path.Combine(folderPath, $"{title}.xlsx"));
            }
            else
            {
                string delim = isTab ? "\\t" : ",";
                string ext = isTab ? "txt" : "csv";
                using var sw = new System.IO.StreamWriter(System.IO.Path.Combine(folderPath, $"{title}.{ext}"));
                sw.WriteLine(string.Join(delim, headers));
                
                foreach(var item in items)
                {
                    var vals = formatter(item);
                    var strVals = System.Linq.Enumerable.Select(vals, v => {
                        if (v == null) return "";
                        if (v is double || v is int) return v.ToString();
                        return isTab ? (v.ToString() ?? "").Replace("\\t", " ") : C(v.ToString());
                    });
                    sw.WriteLine(string.Join(delim, strVals));
                }
            }
        }

        string[] hFigure = new[] { "AssetId", "Name", "Layer", "Description", "ScriptContent" };
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

        Write("PipeCrossings", hPipeCross, PipeCrossings, FCross);

        Write("WaterPipeRun", hPipeGen, WaterPipes, FPipe);
        Write("WaterPointsAlongPipe", hPointGen, WaterPoints, FPoint);
        Write("WaterFitting", hFittingGen, WaterFittings, FFitting);
        Write("WaterValve", hValveGen, WaterValves, FValve);
        Write("WaterHydrant", hHydrantGen, WaterHydrants, FHydrant);
        Write("WaterMeter", hMeterGen, WaterMeters, FMeter);
        Write("WaterLocateBox", hLocateGen, WaterLocateBoxes, FLocate);

        Write("WWGravityPipeRun", hGravityPipe, WWGravityPipes, FWWGravity);
        Write("WWPressurePipeRun", hPipeGen, WWPressurePipes, FPipe);
        Write("WWPointsAlongPipe", hPointGen, WWPoints, FPoint);
        Write("WWFitting", hFittingGen, WWFittings, FFitting);
        Write("Manhole", hManhole, Manholes, FManhole);
        Write("WWServicePointMeter", hServicePoint, WWServicePoints, FServicePoint);
        Write("WWValve", hValveGen, WWValves, FValve);
        Write("WWLocateBox", hLocateGen, WWLocateBoxes, FLocate);

        Write("ReclaimedPipeRun", hPipeGen, ReclaimedPipes, FPipe);
        Write("ReclaimedPointsAlongPipe", hPointGen, ReclaimedPoints, FPoint);
        Write("ReclaimedFitting", hFittingGen, ReclaimedFittings, FFitting);
        Write("ReclaimedValve", hValveGen, ReclaimedValves, FValve);
        Write("ReclaimedHydrant", hHydrantGen, ReclaimedHydrants, FHydrant);
        Write("ReclaimedMeter", hMeterGen, ReclaimedMeters, FMeter);
        Write("ReclaimedLocateBox", hLocateGen, ReclaimedLocateBoxes, FLocate);

        Write("ChilledPipeRun", hPipeGen, ChilledPipes, FPipe);
        Write("ChilledPointsAlongPipe", hPointGen, ChilledPoints, FPoint);
        Write("ChilledFitting", hFittingGen, ChilledFittings, FFitting);
        Write("ChilledValve", hValveGen, ChilledValves, FValve);
        Write("ChilledMeter", hMeterGen, ChilledMeters, FMeter);
        Write("ChilledLocateBox", hLocateGen, ChilledLocateBoxes, FLocate);

        Write("GasGravityPipeRun", hGravityPipe, GGravityPipes, FWWGravity);
        Write("GasPressurePipeRun", hPipeGen, GPressurePipes, FPipe);
        Write("GasPointsAlongPipe", hPointGen, GPoints, FPoint);
        Write("GasFitting", hFittingGen, GFittings, FFitting);
        Write("GasManhole", hManhole, GManholes, FManhole);
        Write("GasServicePointMeter", hServicePoint, GServicePoints, FServicePoint);
        Write("GasValve", hValveGen, GValves, FValve);
        Write("GasLocateBox", hLocateGen, GLocateBoxes, FLocate);

        Write("ElectricGravityPipeRun", hGravityPipe, EGravityPipes, FWWGravity);
        Write("ElectricPressurePipeRun", hPipeGen, EPressurePipes, FPipe);
        Write("ElectricPointsAlongPipe", hPointGen, EPoints, FPoint);
        Write("ElectricFitting", hFittingGen, EFittings, FFitting);
        Write("ElectricManhole", hManhole, EManholes, FManhole);
        Write("ElectricServicePointMeter", hServicePoint, EServicePoints, FServicePoint);
        Write("ElectricValve", hValveGen, EValves, FValve);
        Write("ElectricLocateBox", hLocateGen, ELocateBoxes, FLocate);
        
        Write("STGravityPipeRun", hGravityPipe, STGravityPipes, FWWGravity);
        Write("STPressurePipeRun", hPipeGen, STPressurePipes, FPipe);
        Write("STPointsAlongPipe", hPointGen, STPoints, FPoint);
        Write("STFitting", hFittingGen, STFittings, FFitting);
        Write("STManhole", hManhole, STManholes, FManhole);
        Write("STServicePointMeter", hServicePoint, STServicePoints, FServicePoint);
        Write("STValve", hValveGen, STValves, FValve);
        Write("STLocateBox", hLocateGen, STLocateBoxes, FLocate);
    }
    
    public void ExportAllToSingleFile(string path, string format)
    {
        string C(string? s) => "\\\"" + (s ?? "").Replace("\\\"", "\\\"\\\"") + "\\\"";
        bool isTab = format.Equals("txt", StringComparison.OrdinalIgnoreCase);
        bool isExcel = format.Equals("xls", StringComparison.OrdinalIgnoreCase) || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase);

        System.IO.StreamWriter? sw = null;
        ClosedXML.Excel.XLWorkbook? wb = null;
        ClosedXML.Excel.IXLWorksheet? ws = null;
        int currentRow = 1;
        string currentSheet = "";

        if (isExcel) 
            wb = new ClosedXML.Excel.XLWorkbook();
        else
            sw = new System.IO.StreamWriter(path);
            
        void Write<T>(string discipline, string title, string[] headers, System.Collections.ObjectModel.ObservableCollection<T> items, Func<T, object?[]> formatter)
        {
            if (isExcel)
            {
                if (currentSheet != discipline)
                {
                    currentSheet = discipline;
                    ws = wb!.Worksheets.Add(discipline);
                    currentRow = 1;
                    ws.SheetView.FreezeRows(1);
                }
                
                if (currentRow > 1) currentRow++;
                ws!.Cell(currentRow, 1).Value = $"--- {title} ---";
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow++;
                
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(currentRow, i + 1).Value = headers[i];
                    ws.Cell(currentRow, i + 1).Style.Font.Bold = true;
                }
                currentRow++;
                
                foreach(var item in items)
                {
                    var vals = formatter(item);
                    for (int i = 0; i < vals.Length; i++)
                    {
                        var v = vals[i];
                        if (v is double d) ws.Cell(currentRow, i + 1).Value = d;
                        else if (v is int integer) ws.Cell(currentRow, i + 1).Value = integer;
                        else ws.Cell(currentRow, i + 1).Value = v?.ToString() ?? "";
                    }
                    currentRow++;
                }
            }
            else
            {
                string delim = isTab ? "\\t" : ",";
                sw!.WriteLine($"--- {title} ---");
                sw.WriteLine(string.Join(delim, headers));
                
                foreach(var item in items)
                {
                    var vals = formatter(item);
                    var strVals = System.Linq.Enumerable.Select(vals, v => {
                        if (v == null) return "";
                        if (v is double || v is int) return v.ToString();
                        return isTab ? (v.ToString() ?? "").Replace("\\t", " ") : C(v.ToString());
                    });
                    sw.WriteLine(string.Join(delim, strVals));
                }
                sw.WriteLine();
            }
        }

        string[] hFigure = new[] { "AssetId", "Name", "Layer", "Description", "ScriptContent" };
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

        // Metadata
        if (isExcel)
        {
            var CurrentProject = _dbContext.Projects.FirstOrDefault(p => p.ProjectId == _currentProjectId);
            var wsMeta = wb!.Worksheets.Add("As Built Info");
            wsMeta.Cell(1, 1).Value = "Field"; wsMeta.Cell(1, 2).Value = "Information"; wsMeta.Cell(1, 1).Style.Font.Bold = true; wsMeta.Cell(1, 2).Style.Font.Bold = true;
            wsMeta.Cell(2, 1).Value = "Project Name"; wsMeta.Cell(2, 2).Value = CurrentProject?.ProjectName ?? "";
            wsMeta.Cell(3, 1).Value = "County"; wsMeta.Cell(3, 2).Value = CurrentProject?.County ?? "";
            wsMeta.Cell(4, 1).Value = "Hyperlink"; wsMeta.Cell(4, 2).Value = CurrentProject?.Hyperlink ?? "";
            wsMeta.Cell(5, 1).Value = "As Built Date"; wsMeta.Cell(5, 2).Value = CurrentProject?.AsBuiltDate ?? "";
            wsMeta.Cell(6, 1).Value = "Data Source"; wsMeta.Cell(6, 2).Value = CurrentProject?.DataSource ?? "";
            wsMeta.Cell(7, 1).Value = "Availability Number"; wsMeta.Cell(7, 2).Value = CurrentProject?.AvailabilityNumber ?? "";
            wsMeta.Cell(8, 1).Value = "Capital Project Number"; wsMeta.Cell(8, 2).Value = CurrentProject?.CapitalProjectNumber ?? "";
            wsMeta.Columns().AdjustToContents();
            
            wb!.SaveAs(path);
        }
        else
        {
            sw!.Dispose();
        }
    }
}
"""

start_idx = text.find(start_str)

new_text = text[:start_idx] + clean_methods

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_text)

print("Properly replaced both Export methods to match strict compliance schemas and fixed duplicate method error!")
