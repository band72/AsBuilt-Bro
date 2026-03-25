# Quick static scan of SampleScripts for obvious command errors
$scriptDir = $PSScriptRoot
$scripts = Get-ChildItem $scriptDir -Filter "*.txt" | Sort-Object Name

foreach ($file in $scripts) {
    Write-Host "`n=== $($file.Name) ===" -ForegroundColor Cyan
    $lines = Get-Content $file.FullName
    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++
        $trimmed = $line.Trim()
        if ($trimmed -eq "" -or $trimmed.StartsWith("//")) { continue }
        
        $cmd = ($trimmed -split "\s+")[0].ToUpper()
        
        # Commands known to be valid
        $knownCmds = @("PT","NE","NEZ","STN","BS","BD","BEG","CONT","END","CLOSE","CLEAR","RESET",
                       "INV","AZ","DIST","ANG","TRAV","COPYPT","DELPT","MAPCHK","MAPCHECK",
                       "LIST","SHOW","DISP","ECHO","LOG","SAVE","LOAD","HELP","ABOUT","SET",
                       "CALC","SD","VD","GRADE","SLOPE","STADIA","AREA","EXPORT","REPORT",
                       "BEG","END","CONT","CLOSE","FIG","PT","A","B","L","C","D",
                       "PIPE-ENGINE-ON","PIPE-ENGINE-OFF","COGO-ENGINE-ON","COGO-ENGINE-OFF",
                       "PRUN","SS-B","SS-C","SS-E","W-B","W-C","W-E","E-B","E-C","E-E",
                       "G-B","G-C","G-E","ST-B","ST-C","ST-E","R-B","R-C","R-E","CH-B","CH-C","CH-E",
                       "PC","CRV","RT","C3","OFFSET","MOD","MCS","MCE","RKLN","RKAZ","RKBRG",
                       "RKKRK","BL","CL","HI","XS","AP","LAT","BL","ALIGNMENT","PROFILE",
                       "VPI","HALBL-ON","HALBL-OFF","RESET-ON","RESET-OFF","SYNC","PNT",
                       "XC","MAPCHK","MAPCHKALL","XPT","SYNCPTS","DELPT","OC","FS","AZAZ",
                       "BB","LNLN","ARCARC","RKRK","INV","INVERSE","START","POINT","TRAV",
                       "UNITS","ATMOS","TEMP","PRESS","SF","CR","ANGLES","VERT","HORIZ","EDM","PRISM","COLL",
                       "LN","TRN","ROT","HALBL-ON","HALBL-OFF","BD","COPYPT")
        
        if ($knownCmds -notcontains $cmd -and $cmd -ne "") {
            Write-Host "  [Line $lineNum] Unknown command: '$cmd' -> $trimmed" -ForegroundColor Yellow
        }
    }
    Write-Host "  Done." -ForegroundColor Green
}
Write-Host "`nScan Complete." -ForegroundColor White
