using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using ClosedXML.Excel;

class Program
{
    static void Main()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectDir = Directory.GetParent(baseDir).Parent.Parent.Parent.Parent.FullName;
        string csvPath = Path.Combine(projectDir, "MasterUtilityCodes.csv");
        string imagesDir = Path.Combine(projectDir, "SymbolsLibrary");
        
        if (!Directory.Exists(imagesDir))
            Directory.CreateDirectory(imagesDir);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Symbols Mapping");

        // Headers
        ws.Cell("A1").Value = "Local Code";
        ws.Cell("B1").Value = "System Code";
        ws.Cell("C1").Value = "Description";
        ws.Cell("D1").Value = "Symbol Preview";
        var headerRange = ws.Range("A1:D1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkGray;
        headerRange.Style.Font.FontColor = XLColor.White;
        
        ws.Column(1).Width = 15;
        ws.Column(2).Width = 15;
        ws.Column(3).Width = 40;
        ws.Column(4).Width = 15;

        int row = 2;
        var lines = File.ReadAllLines(csvPath);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 3 || line.StartsWith("LocalCode")) continue;
            
            string localCode = parts[0];
            string sysCode = parts[1];
            string description = parts[2];

            ws.Cell(row, 1).Value = localCode;
            ws.Cell(row, 2).Value = sysCode;
            ws.Cell(row, 3).Value = description;

            Color c = GetColor(sysCode, description);
            string type = GetSymbolType(description);
            string imgFileName = $"{localCode}_{sysCode}.png".Replace("/", "_").Replace("\\", "_");
            string imgPath = Path.Combine(imagesDir, imgFileName);

            if (!File.Exists(imgPath))
                DrawSymbol(type, c, imgPath);

            ws.Row(row).Height = 35;
            
            if (File.Exists(imgPath))
            {
                var picture = ws.AddPicture(imgPath)
                                .MoveTo(ws.Cell(row, 4))
                                .WithSize(30, 30);
            }
            
            row++;
        }

        string outPath = Path.Combine(projectDir, "UtilitySymbols_Mapping.xlsx");
        try 
        {
            workbook.SaveAs(outPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not save xlsx: {ex.Message}");
        }
        Console.WriteLine("Done! Created UtilitySymbols_Mapping.xlsx and SymbolsLibrary folder.");
    }

    static Color GetColor(string sys, string d)
    {
        sys = sys.ToUpper();
        d = d.ToUpper();
        if (d.Contains("CHIL") || sys.Contains("CH")) return Color.LightSkyBlue;
        if (d.Contains("WASTE") || d.Contains("SEW") || sys.Contains("WW")) return Color.Green;
        if (d.Contains("WATER") || sys == "W" || sys.Contains("WAT")) return Color.Blue;
        if (d.Contains("STORM") || d.Contains("DRAIN") || sys.Contains("ST") || sys == "D") return Color.Cyan;
        if (d.Contains("RECLAIM") || sys.Contains("REC") || sys.Contains("R")) return Color.Purple;
        if (d.Contains("GAS") || sys == "G") return Color.Orange;
        if (d.Contains("ELEC") || sys == "E" || sys.Contains("EL")) return Color.Red;
        return Color.Gray;
    }

    static string GetSymbolType(string desc)
    {
        string d = desc.ToUpper();
        if (d.Contains("POLE")) return "pole";
        if (d.Contains("BOX")) return "box";
        if (d.Contains("AIR RELEASE") || d.Contains("ARV")) return "air_release";
        if (d.Contains("HEADWALL") || d.Contains("HW")) return "headwall";
        if (d.Contains("CATCH BASIN") || d.Contains("DROP INLET") || d.Contains("INLET") || d.Contains("CB") || d.Contains("DI")) return "grate";
        if (d.Contains("MANHOLE") || d.Contains("VAULT") || d.Contains("JUNCTION")) return "manhole";
        if (d.Contains("VALVE")) return "valve";
        if (d.Contains("FITTING")) return "fitting";
        if (d.Contains("HYDRANT")) return "hydrant";
        if (d.Contains("METER")) return "meter";
        if (d.Contains("BACK FLOW") || d.Contains("BFP") || d.Contains("PREVENTER")) return "circle";
        if (d.Contains("BLOW") || d.Contains("BO")) return "blowoff";
        if (d.Contains("POINT")) return "point";
        if (d.Contains("RUN") || d.Contains("PIPE")) return "line";
        return "default";
    }

    static void DrawSymbol(string type, Color color, string file)
    {
        using var bmp = new Bitmap(60, 60);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        using var pen = new Pen(Color.White, 3);
        
        switch (type)
        {
            case "manhole":
                g.FillEllipse(brush, 10, 10, 40, 40);
                g.DrawEllipse(pen, 10, 10, 40, 40);
                g.DrawLine(pen, 15, 15, 45, 45);
                g.DrawLine(pen, 45, 15, 15, 45);
                break;
            case "valve":
                g.FillPolygon(brush, new Point[] { new Point(10,20), new Point(30,30), new Point(10,40) });
                g.FillPolygon(brush, new Point[] { new Point(50,20), new Point(30,30), new Point(50,40) });
                g.DrawPolygon(pen, new Point[] { new Point(10,20), new Point(30,30), new Point(10,40) });
                g.DrawPolygon(pen, new Point[] { new Point(50,20), new Point(30,30), new Point(50,40) });
                break;
            case "fitting":
                g.FillEllipse(brush, 20, 20, 20, 20);
                g.DrawEllipse(pen, 20, 20, 20, 20);
                break;
            case "hydrant":
                g.FillEllipse(brush, 20, 20, 20, 20);
                g.DrawEllipse(pen, 20, 20, 20, 20);
                g.DrawLine(pen, 30, 10, 30, 50);
                g.DrawLine(pen, 10, 30, 50, 30);
                break;
            case "meter":
                g.FillRectangle(brush, 10, 10, 40, 40);
                g.DrawRectangle(pen, 10, 10, 40, 40);
                break;
            case "circle":
                g.FillEllipse(brush, 10, 10, 40, 40);
                g.DrawEllipse(pen, 10, 10, 40, 40);
                break;
            case "blowoff":
                g.FillEllipse(brush, 10, 10, 40, 40);
                g.DrawEllipse(pen, 10, 10, 40, 40);
                using (var font = new Font("Arial", 20, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("B", font, textBrush, new RectangleF(10, 10, 40, 40), format);
                }
                break;
            case "air_release":
                g.FillEllipse(brush, 10, 10, 40, 40);
                g.DrawEllipse(pen, 10, 10, 40, 40);
                using (var font = new Font("Arial", 14, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("AR", font, textBrush, new RectangleF(10, 10, 40, 40), format);
                }
                break;
            case "grate":
                g.FillRectangle(brush, 10, 10, 40, 40);
                g.DrawRectangle(pen, 10, 10, 40, 40);
                g.DrawLine(pen, 10, 20, 50, 20);
                g.DrawLine(pen, 10, 30, 50, 30);
                g.DrawLine(pen, 10, 40, 50, 40);
                g.DrawLine(pen, 20, 10, 20, 50);
                g.DrawLine(pen, 30, 10, 30, 50);
                g.DrawLine(pen, 40, 10, 40, 50);
                break;
            case "headwall":
                g.FillRectangle(brush, 5, 20, 50, 20);
                g.DrawRectangle(pen, 5, 20, 50, 20);
                using (var font = new Font("Arial", 10, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("WALL", font, textBrush, new RectangleF(5, 20, 50, 20), format);
                }
                break;
            case "line":
                using (var bigPen = new Pen(color, 6)) g.DrawLine(bigPen, 5, 30, 55, 30);
                break;
            case "pole":
                g.FillEllipse(brush, 10, 10, 40, 40);
                g.DrawEllipse(pen, 10, 10, 40, 40);
                using (var penE = new Pen(Color.White, 3))
                {
                    g.DrawLine(penE, 22, 18, 22, 42); // Vertical
                    g.DrawLine(penE, 22, 18, 38, 18); // Top horizontal
                    g.DrawLine(penE, 22, 30, 34, 30); // Mid horizontal
                    g.DrawLine(penE, 22, 42, 38, 42); // Bottom horizontal
                }
                break;
            case "box":
                g.FillRectangle(brush, 10, 10, 40, 40);
                g.DrawRectangle(pen, 10, 10, 40, 40);
                using (var penLight = new Pen(Color.White, 3))
                {
                    g.DrawLines(penLight, new Point[] {
                        new Point(34, 15),
                        new Point(20, 30),
                        new Point(32, 30),
                        new Point(24, 45)
                    });
                }
                break;
            default: // point
                g.FillPolygon(brush, new Point[] { new Point(30, 10), new Point(50, 40), new Point(10, 40) });
                g.DrawPolygon(pen, new Point[] { new Point(30, 10), new Point(50, 40), new Point(10, 40) });
                break;
        }

        bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);
    }
}
