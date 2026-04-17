# reorganize_root.ps1
# Right-click this file > Run with PowerShell
# Organizes the repo root from 113 loose files into logical subdirectories.

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Move-ToFolder($files, $folder) {
    $dest = Join-Path $root $folder
    if (!(Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
    foreach ($f in $files) {
        $src = Join-Path $root $f
        if (Test-Path $src) {
            Move-Item -Path $src -Destination $dest -Force
            Write-Host "  Moved: $f  ->  $folder\" -ForegroundColor Cyan
        } else {
            Write-Host "  SKIP (not found): $f" -ForegroundColor DarkGray
        }
    }
}

Write-Host "`nRCS.Cogo.Enterprise.Modern — Root Reorganization" -ForegroundColor Yellow
Write-Host "Root: $root`n"

# ── 1. scripts\cogo  — Demo / test .cogo scripts ────────────────────────────
Write-Host "[1/12] scripts\cogo" -ForegroundColor Green
Move-ToFolder @(
    'ALL.cogo', 'EAST.cogo', 'SOUTH.cogo', 'north.cogo',
    'all_commands.cogo',
    'curves_and_rkrk_test.cogo',
    'demo_points.cogo',
    'error_test.cogo',
    'fig_curves_test.cogo',
    'reverse_curves_test.cogo',
    'robust_test.cogo',
    'showcase_network.cogo',
    'test_geom.cogo',
    'xc_test.cogo'
) 'scripts\cogo'

# ── 2. scripts\test_data  — Large TEST_ text scripts ────────────────────────
Write-Host "[2/12] scripts\test_data" -ForegroundColor Green
Move-ToFolder @(
    'TEST_COGO_SCRIPT-1.txt',
    'TEST_COGO_SCRIPT-1_ascii.txt',
    'TEST_COGO_V2.txt',
    'TEST_DATA_SCRIPT.txt',
    'TEST_ELECTRIC_V2.txt',
    'TEST_GAS_V2.txt',
    'TEST_PIPING_SCRIPT.txt',
    'TEST_POINTS_SCRIPT.txt',
    'TEST_SEWER_V2.txt',
    'TEST_STORM_V2.txt',
    'TEST_SYMBOLS_SCRIPT.txt',
    'TEST_WASTE_WATER_V2.txt',
    'TEST_WATER_V2.txt',
    'demo_piping.txt',
    'Water_System_Script.txt',
    'Subdivision_Integrated_Design.txt',
    'Subdivision_Water_Design.txt',
    'EPANET.txt'
) 'scripts\test_data'

# ── 3. scripts\python  — Python automation / tooling ────────────────────────
Write-Host "[3/12] scripts\python" -ForegroundColor Green
Move-ToFolder @(
    'check_xaml.py',
    'export_acad_tables.py',
    'extract_dxf.py',
    'extract_tables.py',
    'extract_tables_v2.py',
    'extract_tables_v3.py',
    'extract_tables_v4.py',
    'extract_tables_v5.py',
    'extract_tables_v6.py',
    'generate_excel.py',
    'parse_dxf_layers.py',
    'patch_appender.py',
    'patch_db_init.py',
    'patch_edit_form.py',
    'patch_edit_form2.py',
    'patch_exports.py',
    'patch_shell.py',
    'patch_ui_colors.py',
    'patch_universal.py',
    'patch_vm.py',
    'patch_vm2.py',
    'patch_vm3.py',
    'patch_vm_headers.py',
    'patch_xaml.py',
    'script.py',
    'script_ui.py',
    'utils_gen_cogo.py',
    'utils_gen_scripts.py'
) 'scripts\python'

# ── 4. scripts\powershell  — PowerShell & batch ──────────────────────────────
Write-Host "[4/12] scripts\powershell" -ForegroundColor Green
Move-ToFolder @(
    'DiagnosticQuery.ps1',
    'create_installer.ps1',
    'export_rules.ps1',
    'extract_excel.ps1',
    'reset_db.bat'
) 'scripts\powershell'

# ── 5. scripts\autocad  — AutoCAD LSP ───────────────────────────────────────
Write-Host "[5/12] scripts\autocad" -ForegroundColor Green
Move-ToFolder @(
    'export_tables.lsp'
) 'scripts\autocad'

# ── 6. scratch\cs  — Loose scratch C# / CSX files ───────────────────────────
Write-Host "[6/12] scratch\cs" -ForegroundColor Green
Move-ToFolder @(
    'Generator.cs',
    'gen_script.cs',
    'headless_test.cs',
    'test3.cs',
    'test_db.cs',
    'test_export.cs',
    'test_dxf.csx'
) 'scratch\cs'

# ── 7. data\csv  — CSV / Excel reference tables ──────────────────────────────
Write-Host "[7/12] data\csv" -ForegroundColor Green
Move-ToFolder @(
    'CLEANED_tables.xlsx',
    'GasElectricStormCodes.csv',
    'JEA_Validation_List.csv',
    'MasterUtilityCodes.csv',
    'ProjectPoints_Export.csv',
    'ProjectSchedule.csv',
    'RCS_TestPoints_Utilities.csv',
    'RCS_TestPoints_Utilities_ALLCODES.csv',
    'RCS_TestPoints_Utilities_ALLCODES_FULLDESC (1).txt',
    'UtilitySymbols_Mapping.xlsx',
    'utility_codes_master.csv',
    'utility_codes_master.xls.xlsx'
) 'data\csv'

# ── 8. data\dxf  — DXF geometry output files ────────────────────────────────
Write-Host "[8/12] data\dxf" -ForegroundColor Green
Move-ToFolder @(
    'Oakwood_Validation_Graph.dxf',
    'Project.dxf',
    'boundary_fix.dxf',
    'parcel_corrected.dxf',
    'dxf_10.txt',
    'dxf_points.txt',
    'temp_dxf.txt'
) 'data\dxf'

# ── 9. data\piping  — Piping network data ───────────────────────────────────
Write-Host "[9/12] data\piping" -ForegroundColor Green
Move-ToFolder @(
    'PipingNetwork.json',
    'PipingScript.txt',
    'PipingScript2.txt',
    'water_sewer.pipe'
) 'data\piping'

# ── 10. data\config  — Validation rules / schema ────────────────────────────
Write-Host "[10/12] data\config" -ForegroundColor Green
Move-ToFolder @(
    'schema.sql',
    'validation_rules.json',
    'validation_rules_dump.json'
) 'data\config'

# ── 11. logs  — Crash / debug / output logs ──────────────────────────────────
Write-Host "[11/12] logs" -ForegroundColor Green
Move-ToFolder @(
    'OutputLog.txt',
    'crashlog.txt',
    'crashlog_ascii.txt',
    'debug_acad.txt',
    'export_test.txt',
    'extracted.txt',
    'zoomlog.txt'
) 'logs'

# ── 12. src\scratch  — Stray files inside src\ ──────────────────────────────
Write-Host "[12/12] src\scratch" -ForegroundColor Green
$srcScratch = Join-Path $root 'src\scratch'
if (!(Test-Path $srcScratch)) { New-Item -ItemType Directory -Path $srcScratch | Out-Null }
foreach ($f in @('FIG.cogo', 'figs.cogo', 'test_pipe.cs')) {
    $src = Join-Path $root "src\$f"
    if (Test-Path $src) {
        Move-Item -Path $src -Destination $srcScratch -Force
        Write-Host "  Moved: src\$f  ->  src\scratch\" -ForegroundColor Cyan
    } else {
        Write-Host "  SKIP (not found): src\$f" -ForegroundColor DarkGray
    }
}

Write-Host "`n✅ Reorganization complete!" -ForegroundColor Green
Write-Host "Remaining root files: $((Get-ChildItem $root -File).Count)" -ForegroundColor Yellow
Read-Host "`nPress Enter to close"
