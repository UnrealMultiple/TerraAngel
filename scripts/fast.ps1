#!/usr/bin/env pwsh
#Requires -Version 7

param (
    [switch] $Start,
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

if (!(Test-Path ('TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -PathType Leaf)) {
    Write-Output 'Building TerraAngelSetup'
    # git submodule update --remote --recursive
    dotnet build TerraAngelSetup\TerraAngelSetup\TerraAngelSetup.csproj -c=Release
}

if ($Start) {
    Write-Output 'Running TerraAngelSetup'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -auto -nocopy -patchinput TerraAngelPatches"
}

if ($Diff) {
    Write-Output 'Running TerraAngelSetup -diff'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net7.0/TerraAngelSetup' | Join-ExecutableExtension) -diff -patchinput TerraAngelPatches"
}