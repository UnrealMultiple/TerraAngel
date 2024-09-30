#!/usr/bin/env pwsh
#Requires -Version 7

param (
    [switch] $Start,
    [switch] $Diff,
    [switch] $Compile,
    [switch] $Decompile,
    [switch] $Patch,
    [switch] $Update,
    [switch] $Download
)

Set-Location (Join-Path $PSScriptRoot '..')

filter Join-ExecutableExtension
{
    if ($IsWindows) {
        return $_ + '.exe'
    }
    return $_    
}

if ($Download) {
    if (!(Test-Path steam/bin -PathType Container)) {
        New-Item steam/bin -ItemType Directory -Force | Out-Null
        curl -sqL "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz" | tar -zxvf - -C steam/bin
    }
    New-Item steam/Terraria -ItemType Directory -Force | Out-Null
    ./steam/bin/steamcmd.sh +force_install_dir (Resolve-Path ./steam/Terraria) +login (Read-Host 'Steam username') +runscript (Resolve-Path ./steam/install-terraria.txt)
}

if ($Update) {
    git pull
}

if (!(Test-Path ('TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -PathType Leaf)) {
    Write-Output 'Building TerraAngelSetup'
    # git submodule update --remote --recursive
    dotnet build TerraAngelSetup/TerraAngelSetup/TerraAngelSetup.csproj -c=Release
}

if ($Start) {
    Write-Output 'Running TerraAngelSetup'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -auto -nocopy -patchinput TerraAngelPatches -decompilerinput steam/Terraria"
}

if ($Diff) {
    Write-Output 'Running TerraAngelSetup -diff'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -diff -patchinput TerraAngelPatches"
}

if ($Compile -or $Start) {
    Write-Output 'Building TerraAngel'
    dotnet build ./src/TerraAngel/Terraria/Terraria.csproj -p:Configuration=Release -p:RunAnalyzers=false
}

if ($Decompile) {
    Write-Output 'Running TerraAngelSetup -decompile'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -decompile -patchinput TerraAngelPatches -decompilerinput steam/Terraria"
}

if ($Patch -or $Update) {
    Write-Output 'Running TerraAngelSetup -patch'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -patch -patchinput TerraAngelPatches"
}
