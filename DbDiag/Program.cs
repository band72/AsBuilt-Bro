using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "rcs_installed_assets.db");

Console.WriteLine($"DB: {dbPath}");
if (!File.Exists(dbPath)) { Console.WriteLine("NOT FOUND"); return; }

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// ── CREATE all missing JEA tables directly ────────────────────────────────
Console.WriteLine("\n=== CREATING MISSING TABLES ===");
string[] tables = {
    "CREATE TABLE IF NOT EXISTS WaterPipes (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, PipeClass TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, Length REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WaterPoints (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, PipeRole TEXT, PipeClass TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, Orientation TEXT, GradeElevation REAL, TopElevation REAL, Cover REAL, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WaterFittings (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, SizeSecondary TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, TopElevation REAL, GradeElevation REAL, Depth REAL, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WaterValves (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, ValveType TEXT, FacilityOwner TEXT, Size TEXT, Orientation TEXT, OpenDirection TEXT, TurnsToOpen REAL, NutElevation REAL, GradeElevation REAL, DepthToNut REAL, Manufacturer TEXT, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WaterMeters (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, Orientation TEXT, Manufacturer TEXT, Material TEXT, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WWGravityPipes (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, PipeClass TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, Length REAL, DownstreamInvert REAL, DownstreamGrade REAL, UpstreamInvert REAL, UpstreamGrade REAL, Slope REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WWPressurePipes (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, PipeClass TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, Length REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WWPoints (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, PipeRole TEXT, PipeClass TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, Orientation TEXT, GradeElevation REAL, TopElevation REAL, Cover REAL, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WWFittings (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, Size TEXT, SizeSecondary TEXT, Manufacturer TEXT, Material TEXT, LiningManufacturer TEXT, LiningMaterial TEXT, TopElevation REAL, GradeElevation REAL, Depth REAL, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS Manholes (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, FacilityOwner TEXT, ManholeType TEXT, DropType TEXT, Manufacturer TEXT, Size TEXT, Material TEXT, LiningMaterial TEXT, LiningManufacturer TEXT, RimElevation REAL, InvertElevationsWithDirections TEXT, LowestInvertElevation REAL, ExteriorJointTapeType TEXT, ExteriorJointTapeManufacturer TEXT, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, RfidBarcode TEXT, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WWServicePoints (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, GradeElevation REAL, TopElevation REAL, Cover REAL, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
    "CREATE TABLE IF NOT EXISTS WWValves (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProjectId TEXT, PartKey TEXT, Discipline TEXT, FeatureType TEXT, Subtype TEXT, ValveType TEXT, FacilityOwner TEXT, Size TEXT, Orientation TEXT, OpenDirection TEXT, TurnsToOpen REAL, NutElevation REAL, GradeElevation REAL, DepthToNut REAL, Manufacturer TEXT, Easting REAL, Northing REAL, Latitude REAL, Longitude REAL, IsVisible INTEGER NOT NULL DEFAULT 1)",
};

foreach (var ddl in tables)
{
    // extract table name (word after "EXISTS ")
    var name = ddl.Substring(ddl.IndexOf("EXISTS ") + 7).Split(' ')[0];
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = ddl;
        cmd.ExecuteNonQuery();
        Console.WriteLine($"  OK   : {name}");
    }
    catch (Exception ex) { Console.WriteLine($"  FAIL : {name} — {ex.Message}"); }
}

// indexes
foreach (var t in new[]{"WaterPipes","WaterPoints","WaterFittings","WaterValves","WaterMeters","WWGravityPipes","WWPressurePipes","WWPoints","WWFittings","Manholes","WWServicePoints","WWValves"})
    try { using var c = conn.CreateCommand(); c.CommandText = $"CREATE INDEX IF NOT EXISTS IX_{t}_ProjId ON {t} (ProjectId)"; c.ExecuteNonQuery(); } catch {}

// ── Verify counts ────────────────────────────────────────────────────────
Console.WriteLine("\n=== VERIFICATION (all projects) ===");
foreach (var t in new[]{"WaterFittings","WaterValves","WaterHydrants","WaterMeters","WaterPipes","Manholes","WWFittings","WWValves","WWGravityPipes"})
{
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {t}";
        Console.WriteLine($"  {t,-25}: {cmd.ExecuteScalar()}");
    }
    catch (Exception ex) { Console.WriteLine($"  {t,-25}: STILL MISSING — {ex.Message}"); }
}

Console.WriteLine("\nDone. Tables created. Now import JEA Excel from the app.");
