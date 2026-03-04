$ErrorActionPreference = "Stop"
Write-Host "Compiling Native C++ DLL for RCS COGO Security..."

# Find the latest Visual Studio installation with C++ workload
$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath

if ([string]::IsNullOrWhiteSpace($vsPath)) {
    Write-Host "Error: Could not find Visual Studio with C++ workload installed."
    Exit 1
}

# Source the VC variables script to set up CL and link paths
$vcvars = "$vsPath\VC\Auxiliary\Build\vcvars64.bat"
if (-Not (Test-Path $vcvars)) {
    Write-Host "Error: vcvars64.bat not found at $vcvars"
    Exit 1
}

# Create a temporary batch file to capture the environment variables from vcvars64.bat
$tempBat = Join-Path $env:TEMP "vcvars_setup.bat"
"@call `"$vcvars`" > NUL`n@set" | Out-File -FilePath $tempBat -Encoding ASCII

# Run the batch file and parse its output to set PowerShell environment variables
$vars = cmd /c $tempBat
foreach ($var in $vars) {
    if ($var -match "^([^=]+)=(.*)$") {
        Set-Item -Path "Env:\$($matches[1])" -Value $matches[2]
    }
}
Remove-Item $tempBat

# We need to compile the dll
$srcDir = "C:\Users\Daryl Banks\source\repos\RCS.Cogo.Enterprise.Modern\src\RCS.Cogo.Security"
Set-Location $srcDir

# Run MSVC compiler directly to produce a DLL (SecurityCore.dll)
# /LD creates a DLL, /EHsc enables standard C++ exceptions
Write-Host "Running cl.exe..."
cl.exe /LD /EHsc SecurityCore.cpp 

if ($LASTEXITCODE -ne 0) {
    Write-Host "Compiler error."
    Exit $LASTEXITCODE
}

# Copy the output dll to WPF project
$wpfBinDir = "..\RCS.Cogo.Wpf\bin\Debug\net8.0-windows"
if (-Not (Test-Path $wpfBinDir)) {
    New-Item -ItemType Directory -Force -Path $wpfBinDir | Out-Null
}

Copy-Item SecurityCore.dll "$wpfBinDir\SecurityCore.dll" -Force
Write-Host "Successfully compiled SecurityCore.dll and copied to Wpf output directory!"
