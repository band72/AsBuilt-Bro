using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RCS.Geo.Core;

namespace RCS.Geo.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// KmlExporterTests — validates structure, coordinate order, escaping, elevation
// KmlImporterTests — validates round-trip parse + projection + edge cases
// ─────────────────────────────────────────────────────────────────────────────

// ── KmlExporter ──────────────────────────────────────────────────────────────

public class KmlExporterTests
{
    private static readonly string TempDir = Path.GetTempPath();

    private static List<(string Id, string Description, (double Lat, double Lon) LatLon, double ElevFt)>
        OnePoint(double lat = 30.44, double lon = -84.28, double elevFt = 100.0, string id = "P1", string desc = "Test")
        => new() { (id, desc, (lat, lon), elevFt) };

    // ── Structure ──────────────────────────────────────────────────────────

    [Fact]
    public void Export_ProducesValidXmlDocument()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint());
        var xml = System.Xml.Linq.XDocument.Load(path);  // throws if invalid XML
        Assert.NotNull(xml.Root);
        File.Delete(path);
    }

    [Fact]
    public void Export_RootElementIsKml()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint());
        var xml = System.Xml.Linq.XDocument.Load(path);
        Assert.Equal("kml", xml.Root!.Name.LocalName);
        File.Delete(path);
    }

    [Fact]
    public void Export_ContainsOnePlacemark_ForOnePoint()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint());
        var text = File.ReadAllText(path);
        Assert.Contains("<Placemark>", text);
        File.Delete(path);
    }

    [Fact]
    public void Export_ContainsThreePlacemarks_ForThreePoints()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        var pts = new List<(string, string, (double, double), double)>
        {
            ("P1", "A", (30.0, -81.0), 10.0),
            ("P2", "B", (30.1, -81.1), 20.0),
            ("P3", "C", (30.2, -81.2), 30.0)
        };
        KmlExporter.Export(path, pts);
        var text = File.ReadAllText(path);
        Assert.Equal(3, CountOccurrences(text, "<Placemark>"));
        File.Delete(path);
    }

    [Fact]
    public void Export_PointIdAppears_InNameElement()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint(id: "SURVEY_42"));
        var text = File.ReadAllText(path);
        Assert.Contains("<name>SURVEY_42</name>", text);
        File.Delete(path);
    }

    // ── Coordinate order (KML spec: lon,lat,alt) ─────────────────────────

    [Fact]
    public void Export_CoordinatesAreInLonLatAltOrder()
    {
        // lat=30.44, lon=-84.28 →  KML spec: lon,lat,alt
        // coordinates element value must be: -84.28xxxxx,30.44xxxxx,...
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint(lat: 30.44, lon: -84.28));
        var text = File.ReadAllText(path);

        // Extract just the <coordinates>...</coordinates> content
        int startTag = text.IndexOf("<coordinates>", StringComparison.Ordinal) + "<coordinates>".Length;
        int endTag   = text.IndexOf("</coordinates>", startTag, StringComparison.Ordinal);
        Assert.True(startTag > 0 && endTag > startTag, "coordinates element not found");

        string coordValue = text.Substring(startTag, endTag - startTag).Trim();
        // Must start with the longitude (negative)
        Assert.StartsWith("-84", coordValue);
        // Latitude portion comes after the first comma
        int firstComma = coordValue.IndexOf(',');
        Assert.True(firstComma > 0, "No comma in coordinates");
        Assert.StartsWith("30", coordValue.Substring(firstComma + 1).TrimStart());

        File.Delete(path);
    }

    // ── Elevation conversion (ftUS → metres) ─────────────────────────────

    [Fact]
    public void Export_ElevationConverted_FtToMetres()
    {
        // 100 ftUS = 100 * 1200/3937 ≈ 30.480 m
        double expectedM = 100.0 * 1200.0 / 3937.0;
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint(elevFt: 100.0));
        var text = File.ReadAllText(path);
        // Check that the rounded value appears in the coordinates
        Assert.Contains($"{expectedM:F3}", text);
        File.Delete(path);
    }

    // ── XML escaping ──────────────────────────────────────────────────────

    [Fact]
    public void Export_XmlSpecialChars_AreEscaped()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint(id: "A&B", desc: "<test>"));
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("<name>A&B</name>",   text);   // raw & not allowed
        Assert.Contains("&amp;",                    text);
        Assert.DoesNotContain("<description><test>", text);  // raw < not allowed
        File.Delete(path);
    }

    // ── Empty list ────────────────────────────────────────────────────────

    [Fact]
    public void Export_EmptyList_ProducesValidKmlWithNoPlacemarks()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, new List<(string, string, (double, double), double)>());
        var xml = System.Xml.Linq.XDocument.Load(path);
        Assert.NotNull(xml.Root);
        Assert.DoesNotContain("<Placemark>", File.ReadAllText(path));
        File.Delete(path);
    }

    // ── KML namespace present ─────────────────────────────────────────────

    [Fact]
    public void Export_ContainsKml22Namespace()
    {
        var path = Path.Combine(TempDir, $"kml_test_{Guid.NewGuid():N}.kml");
        KmlExporter.Export(path, OnePoint());
        var text = File.ReadAllText(path);
        Assert.Contains("http://www.opengis.net/kml/2.2", text);
        File.Delete(path);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        { count++; idx += needle.Length; }
        return count;
    }
}

// ── KmlImporter ──────────────────────────────────────────────────────────────

public class KmlImporterTests
{
    private static readonly string TempDir = Path.GetTempPath();

    // Helper: write a minimal KML and return its path
    private static string WriteKml(string coordinatesBody, string name = "P1", string description = "", string docName = "Test")
    {
        var kml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <name>{docName}</name>
    <Placemark>
      <name>{name}</name>
      <description>{description}</description>
      <Point>
        <coordinates>{coordinatesBody}</coordinates>
      </Point>
    </Placemark>
  </Document>
</kml>";
        var path = Path.Combine(TempDir, $"kml_import_{Guid.NewGuid():N}.kml");
        File.WriteAllText(path, kml, Encoding.UTF8);
        return path;
    }

    // ── Basic parse ───────────────────────────────────────────────────────

    [Fact]
    public void Import_SinglePlacemark_ReturnsSinglePoint()
    {
        var path = WriteKml("-84.28,30.44,0");
        var pts  = KmlImporter.Import(path, "EPSG:2238");
        Assert.Single(pts);
        File.Delete(path);
    }

    [Fact]
    public void Import_PointId_MatchesPlacemarkName()
    {
        var path = WriteKml("-84.28,30.44,0", name: "MY_PT");
        var pts  = KmlImporter.Import(path, "EPSG:2238");
        Assert.Equal("MY_PT", pts[0].Id);
        File.Delete(path);
    }

    [Fact]
    public void Import_Description_IsPreserved()
    {
        var path = WriteKml("-84.28,30.44,0", description: "Iron Rod");
        var pts  = KmlImporter.Import(path, "EPSG:2238");
        Assert.Equal("Iron Rod", pts[0].Description);
        File.Delete(path);
    }

    // ── Projection accuracy ───────────────────────────────────────────────

    [Fact]
    public void Import_RoundTrip_EastingWithinOneFoot()
    {
        // Forward: lat/lon → SP, then write KML, import KML → should recover same SP
        double lat =  30.44, lon = -84.28;
        var (origE, origN) = StatePlaneProjection.ToStatePlane(lat, lon, "EPSG:2238");

        // Build KML with exact lon,lat so importer should back-project to same SP
        var path = WriteKml($"{lon:F7},{lat:F7},0");
        var pts  = KmlImporter.Import(path, "EPSG:2238");

        Assert.InRange(pts[0].Easting,  origE - 1.0, origE + 1.0);
        File.Delete(path);
    }

    [Fact]
    public void Import_RoundTrip_NorthingWithinOneFoot()
    {
        double lat = 30.44, lon = -84.28;
        var (origE, origN) = StatePlaneProjection.ToStatePlane(lat, lon, "EPSG:2238");

        var path = WriteKml($"{lon:F7},{lat:F7},0");
        var pts  = KmlImporter.Import(path, "EPSG:2238");

        Assert.InRange(pts[0].Northing, origN - 1.0, origN + 1.0);
        File.Delete(path);
    }

    // ── Elevation ─────────────────────────────────────────────────────────

    [Fact]
    public void Import_Elevation_ConvertedFromMetresToFeet()
    {
        // KML: 30.48 m → 100 ftUS
        var path = WriteKml("-81.0,30.0,30.48");
        var pts  = KmlImporter.Import(path, "EPSG:2236");
        // 30.48 m * (3937/1200) ≈ 100.0 ft
        Assert.InRange(pts[0].ElevFt, 99.9, 100.1);
        File.Delete(path);
    }

    [Fact]
    public void Import_MissingElevation_DefaultsToZero()
    {
        // Only 2 components in coordinate string
        var path = WriteKml("-81.0,30.0");
        var pts  = KmlImporter.Import(path, "EPSG:2236");
        Assert.Equal(0.0, pts[0].ElevFt);
        File.Delete(path);
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Fact]
    public void Import_MissingName_GeneratesAutoId()
    {
        var kml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <Point><coordinates>-81.0,30.0,0</coordinates></Point>
    </Placemark>
  </Document>
</kml>";
        var path = Path.Combine(TempDir, $"kml_import_{Guid.NewGuid():N}.kml");
        File.WriteAllText(path, kml);
        var pts = KmlImporter.Import(path, "EPSG:2236", startingPointId: 5);
        Assert.StartsWith("KML", pts[0].Id);
        File.Delete(path);
    }

    [Fact]
    public void Import_NonPointPlacemark_IsSkipped()
    {
        var kml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark>
      <name>LineFeature</name>
      <LineString><coordinates>-81.0,30.0,0 -81.1,30.1,0</coordinates></LineString>
    </Placemark>
    <Placemark>
      <name>PointFeature</name>
      <Point><coordinates>-81.5,30.5,0</coordinates></Point>
    </Placemark>
  </Document>
</kml>";
        var path = Path.Combine(TempDir, $"kml_import_{Guid.NewGuid():N}.kml");
        File.WriteAllText(path, kml);
        var pts = KmlImporter.Import(path, "EPSG:2236");
        Assert.Single(pts);   // only the Point placemark
        Assert.Equal("PointFeature", pts[0].Id);
        File.Delete(path);
    }

    [Fact]
    public void Import_EmptyFile_ThrowsOrReturnsEmpty()
    {
        var path = Path.Combine(TempDir, $"kml_import_{Guid.NewGuid():N}.kml");
        File.WriteAllText(path, @"<?xml version=""1.0""?><kml xmlns=""http://www.opengis.net/kml/2.2""><Document></Document></kml>");
        var pts = KmlImporter.Import(path, "EPSG:2236");
        Assert.Empty(pts);
        File.Delete(path);
    }

    [Fact]
    public void Import_MultiplePoints_AllImported()
    {
        var kml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<kml xmlns=""http://www.opengis.net/kml/2.2"">
  <Document>
    <Placemark><name>A</name><Point><coordinates>-81.0,30.0,0</coordinates></Point></Placemark>
    <Placemark><name>B</name><Point><coordinates>-81.1,30.1,0</coordinates></Point></Placemark>
    <Placemark><name>C</name><Point><coordinates>-81.2,30.2,0</coordinates></Point></Placemark>
  </Document>
</kml>";
        var path = Path.Combine(TempDir, $"kml_import_{Guid.NewGuid():N}.kml");
        File.WriteAllText(path, kml);
        var pts = KmlImporter.Import(path, "EPSG:2236");
        Assert.Equal(3, pts.Count);
        File.Delete(path);
    }

    [Fact]
    public void Import_HtmlDescriptionStripped()
    {
        var path = WriteKml("-81.0,30.0,0", description: "&lt;b&gt;Iron Rod&lt;/b&gt;");
        // After XML round-trip the description text will be "<b>Iron Rod</b>" which KmlImporter should strip
        var pts = KmlImporter.Import(path, "EPSG:2236");
        Assert.DoesNotContain("<b>", pts[0].Description);
        File.Delete(path);
    }
}
