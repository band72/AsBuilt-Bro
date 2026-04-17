# Quick SQLite diagnostic via dotnet-script or inline C#
$dbPath = "$env:LOCALAPPDATA\rcs_installed_assets.db"
Write-Host "=== DB Path: $dbPath ===" -ForegroundColor Cyan
Write-Host "File size: $((Get-Item $dbPath).Length) bytes" -ForegroundColor Gray

# Use Microsoft.Data.Sqlite via a temp csx script
$script = @"
#r "nuget: Microsoft.Data.Sqlite, 8.0.0"
using Microsoft.Data.Sqlite;
using System;

var db = @"$dbPath";
using var conn = new SqliteConnection("Data Source=" + db);
conn.Open();

string[] tables = { "WaterFittings", "WaterValves", "WaterHydrants", "WaterMeters", "WaterLocateBoxes", "WWFittings", "Manholes", "WaterPipes" };

Console.WriteLine("=== ROW COUNTS (ALL projects) ===");
foreach (var t in tables) {
    try {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM " + t;
        var count = cmd.ExecuteScalar();
        Console.WriteLine($"  {t,-25}: {count}");
    } catch (Exception ex) { Console.WriteLine($"  {t}: ERROR - {ex.Message}"); }
}

Console.WriteLine();
Console.WriteLine("=== PROJECT IDs IN WaterFittings ===");
try {
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT DISTINCT ProjectId, COUNT(*) as cnt FROM WaterFittings GROUP BY ProjectId";
    using var r = cmd.ExecuteReader();
    while (r.Read()) Console.WriteLine($"  ProjectId='{r[0]}'  rows={r[1]}");
} catch { Console.WriteLine("  (empty or table missing)"); }

Console.WriteLine();
Console.WriteLine("=== PROJECT IDs IN Manholes ===");
try {
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT DISTINCT ProjectId, COUNT(*) as cnt FROM Manholes GROUP BY ProjectId";
    using var r = cmd.ExecuteReader();
    while (r.Read()) Console.WriteLine($"  ProjectId='{r[0]}'  rows={r[1]}");
} catch { Console.WriteLine("  (empty or table missing)"); }

Console.WriteLine();
Console.WriteLine("=== SAMPLE WaterFittings rows (first 3) ===");
try {
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Id, ProjectId, PartKey, Subtype, Northing, Easting FROM WaterFittings LIMIT 3";
    using var r = cmd.ExecuteReader();
    while (r.Read()) Console.WriteLine($"  Id={r[0]}  Proj={r[1]}  Key={r[2]}  Sub={r[3]}  N={r[4]}  E={r[5]}");
} catch { Console.WriteLine("  (empty)"); }
"@

$scriptFile = "$env:TEMP\db_diag.csx"
$script | Out-File $scriptFile -Encoding UTF8

# Try dotnet-script
$result = dotnet script $scriptFile 2>&1
if ($LASTEXITCODE -eq 0) {
    $result
} else {
    Write-Host "dotnet-script not available. Install with: dotnet tool install -g dotnet-script" -ForegroundColor Yellow
    Write-Host "Trying alternative approach..." -ForegroundColor Yellow
    
    # Inline via DbTest project approach
    Write-Host "`nManual path: check $dbPath with DB Browser for SQLite or similar tool" -ForegroundColor Cyan
}
