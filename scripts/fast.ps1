#!/usr/bin/env pwsh
#Requires -Version 7

param (
    [switch] $Update,
    [switch] $Download,
    [switch] $UpdateGame,
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
    if ($IsLinux) {
        $required_binaries = @(
            'curl'
            '7z'
            'msiextract'
        )
        foreach ($b in $required_binaries) {
            if ($null -eq (Get-Command $b -ErrorAction SilentlyContinue)) {
                Write-Output "Unable to find $b in PATH"
                Write-Output "Required binaries $($required_binaries -join ', ')"
                Exit
            }
        }

        if (!(Test-Path ./steam/bin -PathType Container)) {
            Write-Output 'Downloading SteamCMD binary'
            New-Item ./steam/bin -ItemType Directory -Force | Out-Null
            curl -sqL 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz' | tar -zxvf - -C ./steam/bin
        }

        if (!(Test-Path ./steam/Terraria/Terraria.exe -PathType Leaf) -or $UpdateGame) {
            New-Item ./steam/Terraria -ItemType Directory -Force | Out-Null
            ./steam/bin/steamcmd.sh +force_install_dir (Resolve-Path ./steam/Terraria) +login (Read-Host 'Steam username') +runscript (Resolve-Path ./steam/install-terraria.txt)
        }
        
        if (!(Test-Path ./Microsoft.NET -PathType Container)) {
            Write-Output 'Preparing .NET Framework 4.8 binaries'
            curl -qL 'https://download.visualstudio.microsoft.com/download/pr/7afca223-55d2-470a-8edc-6a1739ae3252/abd170b4b0ec15ad0222a809b761a036/ndp48-x86-x64-allos-enu.exe' -o /tmp/ndp48-x86-x64-allos-enu.exe
            7z x /tmp/ndp48-x86-x64-allos-enu.exe -o/tmp/ndp48-x86-x64-allos-enu netfx_Full.mzz netfx_Full_x64.msi
            msiextract /tmp/ndp48-x86-x64-allos-enu/netfx_Full_x64.msi -C /tmp/ndp48-x86-x64-allos-enu
            Move-Item /tmp/ndp48-x86-x64-allos-enu/Windows/Microsoft.NET ./
            foreach ($d in @(Get-ChildItem ./Microsoft.NET -Directory | Get-ChildItem -Filter *:v4)) {
                Rename-Item $d.FullName $($d.Name -replace ':v4', '')
            }

            Remove-Item /tmp/ndp48-x86-x64-allos-enu.exe
            Remove-Item /tmp/ndp48-x86-x64-allos-enu -Recurse

            Write-Output 'Preparing XNA binaries'
            $xna_version = 'v4.0_4.0.0.0__842cf8be1de50553'
            $xna_location_mapper = @{
                'Microsoft.Xna.Framework.Avatar.dll' = 'GAC_MSIL'
                'Microsoft.Xna.Framework.GamerServices.dll' = 'GAC_MSIL'
                'Microsoft.Xna.Framework.Input.Touch.dll' = 'GAC_MSIL'
                'Microsoft.Xna.Framework.Net.dll' = 'GAC_MSIL'
                'Microsoft.Xna.Framework.Storage.dll' = 'GAC_MSIL'
                'Microsoft.Xna.Framework.Video.dll' = 'GAC_MSIL'
                'Microsoft.Xna.Framework.dll' = 'GAC_32'
                'Microsoft.Xna.Framework.Game.dll' = 'GAC_32'
                'Microsoft.Xna.Framework.Graphics.dll' = 'GAC_32'
                'Microsoft.Xna.Framework.Xact.dll' = 'GAC_32'
            }
            msiextract ./steam/Terraria/xnafx40_redist.msi -C /tmp/xnafx40_redist
            foreach ($f in @(Get-ChildItem -File '/tmp/xnafx40_redist/Program Files/Microsoft XNA/XNA Game Studio/*.dll')) {
                $target_dir = Join-Path './Microsoft.NET/assembly' $xna_location_mapper[$f.Name] $f.BaseName $xna_version
                New-Item $target_dir -ItemType Directory -Force | Out-Null
                Move-Item $f $target_dir
            }
            Remove-Item /tmp/xnafx40_redist -Recurse
        }
    }
}

if (!(Test-Path ('./TerraAngelSetup/TerraAngelSetup/bin/Release/net8.0/TerraAngelSetup' | Join-ExecutableExtension) -PathType Leaf)) {
    Write-Output 'Building TerraAngelSetup'
    dotnet build ./TerraAngelSetup/TerraAngelSetup/TerraAngelSetup.csproj -c=Release
}

if ($Decompile -or $Start) {
    Write-Output 'Running TerraAngelSetup -decompile'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net8.0/TerraAngelSetup' | Join-ExecutableExtension) -decompile -patchinput TerraAngelPatches -decompilerinput ./steam/Terraria -noexitprompt"
}

if ($Patch -or $Update -or $Start) {
    Write-Output 'Running TerraAngelSetup -patch'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net8.0/TerraAngelSetup' | Join-ExecutableExtension) -patch -patchinput ./TerraAngelPatches -noexitprompt"
}

if ($Compile -or $Start) {
    Write-Output 'Building TerraAngel'
    dotnet build ./src/TerraAngel/Terraria/Terraria.csproj -p:Configuration=Release -p:RunAnalyzers=false
}

if ($Diff) {
    Write-Output 'Running TerraAngelSetup -diff'
    Invoke-Expression "$('./TerraAngelSetup/TerraAngelSetup/bin/Release/net8.0/TerraAngelSetup' | Join-ExecutableExtension) -diff -patchinput ./TerraAngelPatches -noexitprompt"
}