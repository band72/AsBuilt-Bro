Install-Module -Name ImportExcel -Scope CurrentUser -Force
Import-Module ImportExcel
$data = Import-Excel 'utility_codes_master.xls.xlsx' -WorksheetName 'Validation rules'
$data | ConvertTo-Json -Depth 5 | Out-File -FilePath 'validation_rules.json' -Encoding utf8
