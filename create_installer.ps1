$PublishDir = "src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\win-x64\publish"
$SetupScript = "src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\SETUP.iss"
$InnoSetupCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "Publishing the application..." -ForegroundColor Cyan
dotnet publish "src\RCS.Cogo.Wpf\RCS.Cogo.Wpf.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed. Aborting." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Creating the installer using Inno Setup..." -ForegroundColor Cyan
if (Test-Path $InnoSetupCompiler) {
    & $InnoSetupCompiler $SetupScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer created successfully!" -ForegroundColor Green
        Write-Host "You can find it at: src\RCS.Cogo.Wpf\bin\Release\net8.0-windows\Output\RCS.ASBUILT.PRO.exe" -ForegroundColor Yellow
    }
    else {
        Write-Host "Inno Setup compilation failed." -ForegroundColor Red
    }
}
else {
    Write-Host "Inno Setup compiler not found at $InnoSetupCompiler." -ForegroundColor Red
}
