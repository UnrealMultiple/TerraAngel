#!/usr/bin/env pwsh
#Requires -Version 5

param (
    [switch] $Update,
    [switch] $Download,
    [switch] $UpdateGame,
    [switch] $Start,
    [switch] $Decompile,
    [switch] $Patch,
    [switch] $Compile,
    [switch] $Diff,
    [switch] $I18n,
    [switch] $AutoInstallDeps
)

Set-Location "$PSScriptRoot"

filter Join-ExecutableExtension
{
    if ($IsWindows) {
        return $_ + '.exe'
    }
    return $_    
}

if ($Update) {
    git pull
    git submodule update --init --recursive
}

if ($Download) {
    if ($IsLinux) {
        if ($AutoInstallDeps) {
            Write-Output "--- Starting system environment check and dependency installation ---"

            if (Test-Path /etc/debian_version) {
                Write-Output "[Debian/Ubuntu Detected] Configuring 32-bit architecture and installing dependencies..."
                sudo dpkg --add-architecture i386
                sudo apt-get update
                sudo apt-get install -y lib32gcc-s1 curl 7zip msitools tar
            }
            elseif (Test-Path /etc/redhat-release) {
                Write-Output "[RHEL/CentOS Detected] Installing 32-bit dependencies..."
                sudo yum install -y glibc.i686 libstdc++.i686 curl p7zip msitools tar
            }
            elseif (Test-Path /etc/arch-release) {
                Write-Output "[Arch Linux Detected] Please ensure [multilib] repository is enabled!"
                sudo pacman -Sy --noconfirm lib32-gcc-libs curl 7zip msitools tar
            }
        }

        $detect_7z_bin = @('7z', '7zz', '7za') | Where-Object { $null -ne (Get-Command $_ -ErrorAction SilentlyContinue) } | Select-Object -First 1
        if ($null -eq $detect_7z_bin) {
            Write-Output "Error: Command 7z, 7zz, or 7za not found after installation attempt. Manual intervention required."
            Write-Output "You can add the flag -AutoInstallDeps to let the script attempt automatic installation of dependencies on Linux."
            Exit -1
        }

        $required_binaries = @('curl', 'msiextract')
        foreach ($b in $required_binaries) {
            if ($null -eq (Get-Command $b -ErrorAction SilentlyContinue)) {
                Write-Output "Error: Command $b still not found after installation attempt. Manual intervention required."
                Write-Output "You can add the flag -AutoInstallDeps to let the script attempt automatic installation of dependencies on Linux."
                Exit -1
            }
        }

        if (!(Test-Path ./steam/bin -PathType Container)) {
            Write-Output 'Downloading SteamCMD binary'
            Write-Host "Note: If this step fails, you can manually download and extract SteamCMD for Linux from https://developer.valvesoftware.com/wiki/SteamCMD#Linux and place the contents in ./steam/bin"
            Write-Host "If the steamcmd cannot run, you may missed the 'lib32gcc-s1' library. You can add the flag '-AutoInstallDeps' to let the script attempt automatic installation of dependencies on Linux."
            New-Item ./steam/bin -ItemType Directory -Force | Out-Null
            curl -sqL 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz' | tar -zxvf - -C ./steam/bin
        }

        if (!(Test-Path ./steam/Terraria/Terraria.exe -PathType Leaf) -or $UpdateGame) {
            Write-Output 'Downloading Terraria Windows version via SteamCMD...'
            New-Item ./steam/Terraria -ItemType Directory -Force | Out-Null
            ./steam/bin/steamcmd.sh +force_install_dir (Resolve-Path ./steam/Terraria) +login (Read-Host 'Steam username') +runscript (Resolve-Path ./steam/install-terraria.txt)
        }

        if (!(Test-Path ./Microsoft.NET -PathType Container)) {
            Write-Output 'Preparing .NET Framework 4.8 binaries'
            curl -qL 'https://download.visualstudio.microsoft.com/download/pr/7afca223-55d2-470a-8edc-6a1739ae3252/abd170b4b0ec15ad0222a809b761a036/ndp48-x86-x64-allos-enu.exe' -o /tmp/ndp48-x86-x64-allos-enu.exe
            & $detect_7z_bin x /tmp/ndp48-x86-x64-allos-enu.exe -o/tmp/ndp48-x86-x64-allos-enu netfx_Full.mzz netfx_Full_x64.msi
            msiextract /tmp/ndp48-x86-x64-allos-enu/netfx_Full_x64.msi -C /tmp/ndp48-x86-x64-allos-enu
            Move-Item /tmp/ndp48-x86-x64-allos-enu/Windows/Microsoft.NET ./
            foreach ($d in @(Get-ChildItem ./Microsoft.NET -Directory | Get-ChildItem -Filter *:v4)) {
                Rename-Item $d.FullName $($d.Name -replace ':v4', '')
            }

            Remove-Item /tmp/ndp48-x86-x64-allos-enu.exe
            Remove-Item /tmp/ndp48-x86-x64-allos-enu -Recurse
        }
        if (!(Test-Path ./Microsoft.NET/assembly/GAC_32/Microsoft.Xna.Framework/ -PathType Container)) {
            Write-Output 'Preparing XNA 4.0 reference libraries...'
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

if ($I18n) {
    $pot = './src/TerraAngel/Terraria/Assets/i18n/template.pot'
    Write-Output "[I18n] generating template.pot..."
    dotnet tool run GetText.Extractor -- -u -o -s './src/TerraAngel/Terraria/Terraria.csproj' -t $pot
    Remove-Item "$pot.bak" -ErrorAction Ignore
    foreach ($po in @(Get-ChildItem "./src/TerraAngel/Terraria/Assets/i18n/*.po")) {
        Write-Output "[I18n] [$($po.Name)] merging..."
        msgmerge --previous --update $po.FullName $pot
        Remove-Item "$po~" -ErrorAction Ignore
        $mo = [System.IO.Path]::ChangeExtension($po.FullName, '.mo')
        Write-Output "[I18n] [$([System.IO.Path]::GetFileName($mo))] generating..."
        msgfmt -o $mo $po
    }
}
