$ErrorActionPreference = "Stop"
$excelPath = "C:\Users\Daryl Banks\Downloads\JEA As Built Template 2024.xlsx"
$jsonPath = "C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\validation_rules.json"

try {
    $excel = New-Object -ComObject Excel.Application -ErrorAction Stop
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $wb = $excel.Workbooks.Open($excelPath)
    $ws = $wb.Sheets.Item("Validation Rules")
    
    $data = @()
    $row = 3 # Start reading from row 3 (skip headers if any, or maybe headers are at 2, let's start at 2)
    
    # Read headers
    $heads = @()
    for ($c = 1; $c -le 8; $c++) {
        $hc = $ws.Cells.Item(2, $c).Value()
        if ($hc) { $heads += $hc.ToString() } else { $heads += "Col$c" }
    }
    
    $emptyCount = 0
    while ($row -le 1000) {
        $c1 = $ws.Cells.Item($row, 1).Value()
        $c2 = $ws.Cells.Item($row, 2).Value()
        
        if ([string]::IsNullOrWhiteSpace($c1) -and [string]::IsNullOrWhiteSpace($c2)) {
            $emptyCount++
            if ($emptyCount -ge 5) { break }
        } else {
            $emptyCount = 0
        }
        
        if ([string]::IsNullOrWhiteSpace($c1)) {
            $row++
            continue
        }
        
        $obj = @{}
        for ($c = 1; $c -le $heads.Count; $c++) {
            $val = $ws.Cells.Item($row, $c).Value()
            if ($val -ne $null) {
                $obj[$heads[$c-1]] = $val.ToString()
            } else {
                $obj[$heads[$c-1]] = ""
            }
        }
        $data += $obj
        $row++
    }
    
    $wb.Close($false)
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
    
    $data | ConvertTo-Json -Depth 5 | Out-File -FilePath $jsonPath -Encoding utf8
    Write-Host "Success"
} catch {
    Write-Error $_.Exception.Message
    if ($excel) {
        $excel.Quit()
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
    }
}
