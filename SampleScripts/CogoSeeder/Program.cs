using System;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using RCS.Data;
using RCS.Data.Entities;

namespace CogoSeeder
{
    class Program
    {
        static void Main(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: CogoSeeder <path-to-excel-file>");
                return;
            }

            var excelPath = args[0];
            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"Cannot find Excel file: {excelPath}");
                return;
            }

            Console.WriteLine($"Loading Excel Data from: {excelPath}");

            var projectId = Guid.NewGuid().ToString().ToUpper();
            var projectName = $"JEA Excel Rehydration - {DateTime.Now:yyyy-MM-dd HH:mm}";

            using var db = new AppDbContext();
            
            Console.WriteLine($"Establishing New Project: {projectName} ({projectId})");
            db.Projects.Add(new ProjectEntity
            {
                ProjectId = projectId,
                ProjectName = projectName,
                ProjectNumber = "JEA-" + DateTime.Now.ToString("yyyyMMdd"),
                DataSource = excelPath,
                County = "Duval"
            });

            using var package = new ExcelPackage(new FileInfo(excelPath));

            // Load Sewer Manholes
            var manholeSheet = package.Workbook.Worksheets["Sewer Manhole"];
            if (manholeSheet != null)
            {
                int rowCount = manholeSheet.Dimension.Rows;
                int added = 0;
                // Header mapping: Manhole Number=1, Subtype=3, Facility Owner=4, Manhole Type=7, Drop Type=9,
                // Manhole Size=10, Material=12, Lining Material=14, Depth=18, Rim=20, Y=33, X=32, Lat=34, Lon=35
                for (int row = 2; row <= rowCount; row++)
                {
                    if (string.IsNullOrWhiteSpace(manholeSheet.Cells[row, 1].Text)) continue;
                    var mh = new Manhole
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProjectId = projectId,
                        PartKey = manholeSheet.Cells[row, 1].Text,
                        Subtype = manholeSheet.Cells[row, 3].Text,
                        FacilityOwner = manholeSheet.Cells[row, 4].Text,
                        ManholeType = manholeSheet.Cells[row, 7].Text,
                        DropType = manholeSheet.Cells[row, 9].Text,
                        Size = manholeSheet.Cells[row, 10].Text,
                        Material = manholeSheet.Cells[row, 12].Text,
                        RimElevation = ParseDouble(manholeSheet.Cells[row, 20].Text),
                        Depth = ParseDouble(manholeSheet.Cells[row, 18].Text),
                        Easting = ParseDouble(manholeSheet.Cells[row, 32].Text),
                        Northing = ParseDouble(manholeSheet.Cells[row, 33].Text),
                        Latitude = ParseDouble(manholeSheet.Cells[row, 34].Text),
                        Longitude = ParseDouble(manholeSheet.Cells[row, 35].Text),
                    };
                    db.Manholes.Add(mh);
                    added++;
                }
                Console.WriteLine($"Parsed {added} Sewer Manholes.");
            }

            // Load Water Fittings
            var waterFittingSheet = package.Workbook.Worksheets["Water Fitting"];
            if (waterFittingSheet != null)
            {
                int rowCount = waterFittingSheet.Dimension.Rows;
                int added = 0;
                // Fitting Number=1, Subtype=3, Owner=4, Size Pri=7, Size Red=8, Material=10, Elev=12, X=16, Y=17, Lat=18, Lon=19
                for (int row = 2; row <= rowCount; row++)
                {
                    if (string.IsNullOrWhiteSpace(waterFittingSheet.Cells[row, 1].Text)) continue;
                    var ft = new WaterFitting
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProjectId = projectId,
                        PartKey = waterFittingSheet.Cells[row, 1].Text,
                        Subtype = waterFittingSheet.Cells[row, 3].Text,
                        FacilityOwner = waterFittingSheet.Cells[row, 4].Text,
                        Size = waterFittingSheet.Cells[row, 7].Text,
                        SizeSecondary = waterFittingSheet.Cells[row, 8].Text,
                        Material = waterFittingSheet.Cells[row, 10].Text,
                        TopElevation = ParseDouble(waterFittingSheet.Cells[row, 12].Text),
                        Easting = ParseDouble(waterFittingSheet.Cells[row, 16].Text),
                        Northing = ParseDouble(waterFittingSheet.Cells[row, 17].Text),
                        Latitude = ParseDouble(waterFittingSheet.Cells[row, 18].Text),
                        Longitude = ParseDouble(waterFittingSheet.Cells[row, 19].Text),
                    };
                    db.WaterFittings.Add(ft);
                    
                    var pointNum = ft.PartKey ?? added.ToString();
                    Console.WriteLine($"ST {pointNum} {ft.Northing:F4} {ft.Easting:F4} {ft.TopElevation:F2} \"{ft.Subtype} {ft.Size} IN\"");
                    
                    // Generate a sequential PRUN link to the previous node
                    if (added > 0)
                    {
                        var prevPoint = waterFittingSheet.Cells[row - 1, 1].Text;
                        if (!string.IsNullOrEmpty(prevPoint))
                        {
                            var sizeVal = ft.Size;
                            if (string.IsNullOrEmpty(sizeVal)) sizeVal = "8"; // default 8 inch
                            Console.WriteLine($"PRUN START {prevPoint} {pointNum} {sizeVal} {ft.TopElevation:F2} {ft.TopElevation:F2}");
                        }
                    }
                    
                    added++;
                }
                Console.WriteLine($"\nParsed {added} Water Fittings.");
            }

            db.SaveChanges();
            Console.WriteLine("[SUCCESS] New Project Seeded!");
        }

        private static double ParseDouble(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            return double.TryParse(val, out double d) ? d : 0;
        }
    }
}
