$ErrorActionPreference = "Stop"

$ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$BuildDir = Join-Path $ProjectDir "build"

if ($env:GODOT_BIN) {
    $Godot = $env:GODOT_BIN
} else {
    $GodotCommand = Get-Command godot-mono, godot -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $GodotCommand) {
        throw "Godot 4.7.2 .NET editor not found. Set GODOT_BIN to the editor executable."
    }
    $Godot = $GodotCommand.Source
}

$LinuxDir = Join-Path $BuildDir "linux"
$WindowsDir = Join-Path $BuildDir "windows"
$PackagesDir = Join-Path $BuildDir "packages"
Remove-Item $LinuxDir, $WindowsDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $LinuxDir, $WindowsDir, $PackagesDir | Out-Null

dotnet build (Join-Path $ProjectDir "Sigilwoven.csproj") --configuration Release
if ($LASTEXITCODE -ne 0) { throw "C# release build failed." }

& $Godot --headless --path $ProjectDir --export-release "Linux x86_64" (Join-Path $LinuxDir "Sigilwoven.x86_64")
$LinuxExecutable = Join-Path $LinuxDir "Sigilwoven.x86_64"
$LinuxAssembly = Join-Path $LinuxDir "data_Sigilwoven_linuxbsd_x86_64\Sigilwoven.dll"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $LinuxExecutable) -or -not (Test-Path $LinuxAssembly)) { throw "Linux export failed or has no .NET runtime. Check Godot .NET export templates." }

& $Godot --headless --path $ProjectDir --export-release "Windows x86_64" (Join-Path $WindowsDir "Sigilwoven.exe")
$WindowsExecutable = Join-Path $WindowsDir "Sigilwoven.exe"
$WindowsAssembly = Join-Path $WindowsDir "data_Sigilwoven_windows_x86_64\Sigilwoven.dll"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $WindowsExecutable) -or -not (Test-Path $WindowsAssembly)) { throw "Windows export failed or has no .NET runtime. Check Godot .NET export templates." }

$WindowsArchive = Join-Path $PackagesDir "Sigilwoven-windows-x86_64.zip"
$LinuxArchive = Join-Path $PackagesDir "Sigilwoven-linux-x86_64.zip"
Remove-Item $WindowsArchive, $LinuxArchive -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $WindowsDir "*") -DestinationPath $WindowsArchive
Compress-Archive -Path (Join-Path $LinuxDir "*") -DestinationPath $LinuxArchive

Write-Host "Release packages: $PackagesDir"
