#!/usr/bin/env pwsh
#Requires -Version 7

param (
    [switch] $Update,
    [switch] $Download,
    [switch] $Start,
    [switch] $Decompile,
    [switch] $Patch,
    [switch] $Compile,
    [switch] $Diff
)

Set-Location (Join-Path $PSScriptRoot '..')

filter Join-ExecutableExtension
{
    if ($IsWindows) {
        return $_ + '.exe'
    }
    return $_    
}

if ($Update) {
    git pull
}

if ($Download) {
    if (!(Test-Path steam/bin -PathType Container)) {
        New-Item steam/bin -ItemType Directory -Force | Out-Null
        curl -sqL "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz" | tar -zxvf - -C steam/bin
    }
    New-Item steam/Terraria -ItemType Directory -Force | Out-Null
    ./steam/bin/steamcmd.sh +force_install_dir (Resolve-Path ./steam/Terraria) +login (Read-Host 'Steam username') +runscript (Resolve-Path ./steam/install-terraria.txt)
}

if (!(Test-Path ('TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -PathType Leaf)) {
    Write-Output 'Building TerraAngelSetup'
    dotnet build TerraAngelSetup/TerraAngelSetup/TerraAngelSetup.csproj -c=Release
}

if ($Decompile -or $Start) {
    Write-Output 'Running TerraAngelSetup -decompile'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -decompile -patchinput TerraAngelPatches -decompilerinput steam/Terraria -noexitprompt"
}

if ($Patch -or $Update -or $Start) {
    Write-Output 'Running TerraAngelSetup -patch'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -patch -patchinput TerraAngelPatches -noexitprompt"
}

if ($Compile -or $Start) {
    Write-Output 'Building TerraAngel'
    dotnet build ./src/TerraAngel/Terraria/Terraria.csproj -p:Configuration=Release -p:RunAnalyzers=false
}

if ($Diff) {
    Write-Output 'Running TerraAngelSetup -diff'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -diff -patchinput TerraAngelPatches -noexitprompt"
}