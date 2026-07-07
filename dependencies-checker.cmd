@echo off
setlocal EnableExtensions
set "SCRIPT_DIR=%~dp0"
set "APP_REPAIR_SCRIPT_ROOT=%SCRIPT_DIR%"
set "APP_REPAIR_SELF=%~f0"
cd /d "%TEMP%"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -Command "$self=$env:APP_REPAIR_SELF; $marker='### POWERSHELL_PAYLOAD ###'; $lines=[System.IO.File]::ReadAllLines($self); $index=[Array]::IndexOf($lines,$marker); if($index -lt 0){Write-Host 'Embedded PowerShell payload was not found.' -ForegroundColor Red; exit 1}; if($index -ge ($lines.Length - 1)){Write-Host 'Embedded PowerShell payload is empty.' -ForegroundColor Red; exit 1}; $script=($lines[($index + 1)..($lines.Length - 1)] -join [Environment]::NewLine); & ([scriptblock]::Create($script)) @args" %*
exit /b %ERRORLEVEL%
### POWERSHELL_PAYLOAD ###
# App Repair Tool v2
# Installs, reinstalls, or uninstalls selected apps only after the player chooses an action.
# Reinstall checks installer availability before uninstalling anything.
# Targets: Spotify, Discord, CapCut, Zen Browser, Roblox/bootstrapper choices, Roblox Studio, VS Code, EA app, Razer Synapse, Xbox PC, Voicemod, Clownfish, Steam, Medal, Overwolf

param(
    [switch]$AllowAdmin
)

$ErrorActionPreference = "Continue"

$VSCodeDeveloperDependencies = @(
    [pscustomobject]@{
        Name = "Git"
        Id = "Git.Git"
        DisplayNamePatterns = @("Git")
    },
    [pscustomobject]@{
        Name = "Python 3.14"
        Id = "Python.Python.3.14"
        DisplayNamePatterns = @("Python 3.14")
    },
    [pscustomobject]@{
        Name = "Visual Studio Build Tools 2022"
        Id = "Microsoft.VisualStudio.2022.BuildTools"
        DisplayNamePatterns = @("Visual Studio Build Tools 2022")
        InstallArguments = @(
            "--override",
            "--wait --quiet --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended --includeOptional --norestart"
        )
        UpgradeArguments = @(
            "--override",
            "--wait --quiet --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended --includeOptional --norestart"
        )
    },
    [pscustomobject]@{
        Name = "CMake"
        Id = "Kitware.CMake"
        DisplayNamePatterns = @("CMake")
    },
    [pscustomobject]@{
        Name = "Ninja"
        Id = "Ninja-build.Ninja"
        DisplayNamePatterns = @("Ninja")
    },
    [pscustomobject]@{
        Name = "LLVM"
        Id = "LLVM.LLVM"
        DisplayNamePatterns = @("LLVM")
    },
    [pscustomobject]@{
        Name = ".NET SDK 10"
        Id = "Microsoft.DotNet.SDK.10"
        DisplayNamePatterns = @("Microsoft .NET SDK 10", ".NET SDK 10")
    },
    [pscustomobject]@{
        Name = "Lua"
        Id = "DEVCOM.Lua"
        DisplayNamePatterns = @("Lua")
    },
    [pscustomobject]@{
        Name = "Lua Language Server"
        Id = "LuaLS.lua-language-server"
        DisplayNamePatterns = @("Lua Language Server")
    },
    [pscustomobject]@{
        Name = "Eclipse Temurin JDK 25"
        Id = "EclipseAdoptium.Temurin.25.JDK"
        DisplayNamePatterns = @("Eclipse Temurin JDK with Hotspot 25")
    },
    [pscustomobject]@{
        Name = "Node.js LTS"
        Id = "OpenJS.NodeJS.LTS"
        DisplayNamePatterns = @("Node.js LTS", "Node.js (LTS)")
    },
    [pscustomobject]@{
        Name = "pnpm"
        Id = "pnpm.pnpm"
        DisplayNamePatterns = @("pnpm")
    },
    [pscustomobject]@{
        Name = "Yarn"
        Id = "Yarn.Yarn"
        DisplayNamePatterns = @("Yarn")
    },
    [pscustomobject]@{
        Name = "Rustup"
        Id = "Rustlang.Rustup"
        DisplayNamePatterns = @("Rustup", "Rust")
    }
)

$VSCodeExtensions = @(
    [pscustomobject]@{ Name = "Python"; Id = "ms-python.python" },
    [pscustomobject]@{ Name = "Pylance"; Id = "ms-python.vscode-pylance" },
    [pscustomobject]@{ Name = "Jupyter"; Id = "ms-toolsai.jupyter" },
    [pscustomobject]@{ Name = "C/C++"; Id = "ms-vscode.cpptools" },
    [pscustomobject]@{ Name = "CMake Tools"; Id = "ms-vscode.cmake-tools" },
    [pscustomobject]@{ Name = "C#"; Id = "ms-dotnettools.csharp" },
    [pscustomobject]@{ Name = "C# Dev Kit"; Id = "ms-dotnettools.csdevkit" },
    [pscustomobject]@{ Name = "Extension Pack for Java"; Id = "vscjava.vscode-java-pack" },
    [pscustomobject]@{ Name = "Lua"; Id = "sumneko.lua" },
    [pscustomobject]@{ Name = "rust-analyzer"; Id = "rust-lang.rust-analyzer" },
    [pscustomobject]@{ Name = "ESLint"; Id = "dbaeumer.vscode-eslint" },
    [pscustomobject]@{ Name = "Prettier"; Id = "esbenp.prettier-vscode" },
    [pscustomobject]@{ Name = "HTML CSS Support"; Id = "ecmel.vscode-html-css" },
    [pscustomobject]@{ Name = "Live Server"; Id = "ritwickdey.LiveServer" }
)

$Apps = @(
    [pscustomobject]@{
        Name = "Spotify"
        Id = "Spotify.Spotify"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Spotify")
        DisplayNamePatterns = @("Spotify")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "spotify.com"
        IconText = "SP"
        IconColor = "#1DB954"
    },
    [pscustomobject]@{
        Name = "Discord"
        Id = "Discord.Discord"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Discord")
        DisplayNamePatterns = @("Discord")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "discord.com"
        IconText = "DI"
        IconColor = "#5865F2"
    },
    [pscustomobject]@{
        Name = "CapCut"
        Id = "ByteDance.CapCut"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("CapCut")
        DisplayNamePatterns = @("CapCut")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "capcut.com"
        IconText = "CC"
        IconColor = "#111827"
    },
    [pscustomobject]@{
        Name = "Zen Browser"
        Id = "Zen-Team.Zen-Browser"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("zen", "zen-browser")
        DisplayNamePatterns = @("Zen Browser", "Zen Browser (x64 en-US)", "Zen Browser (AArch64 en-US)")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "zen-browser.app"
        IconText = "ZN"
        IconColor = "#7868E6"
    },
    [pscustomobject]@{
        Name = "Roblox"
        Id = "Roblox.Roblox"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("RobloxPlayerBeta", "RobloxPlayerLauncher", "RobloxStudioBeta", "RobloxCrashHandler")
        DisplayNamePatterns = @("Roblox Player", "Roblox")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $true
        IconDomain = "roblox.com"
        IconText = "RB"
        IconColor = "#191B1F"
    },
    [pscustomobject]@{
        Name = "Roblox Studio"
        Id = "Roblox.RobloxStudio"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("RobloxStudioBeta", "RobloxStudioLauncherBeta", "RobloxCrashHandler")
        DisplayNamePatterns = @("Roblox Studio")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "roblox.com"
        IconText = "RS"
        IconColor = "#191B1F"
    },
    [pscustomobject]@{
        Name = "Visual Studio Code"
        Id = "Microsoft.VisualStudioCode"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Code")
        DisplayNamePatterns = @("Microsoft Visual Studio Code", "Visual Studio Code")
        AppxPackageNames = @()
        Dependencies = $VSCodeDeveloperDependencies
        VSCodeExtensions = $VSCodeExtensions
        IsRoblox = $false
        IconDomain = "code.visualstudio.com"
        IconText = "VS"
        IconColor = "#007ACC"
    },
    [pscustomobject]@{
        Name = "EA app"
        Id = "ElectronicArts.EADesktop"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("EADesktop", "EALauncher", "EABackgroundService", "EACefSubProcess", "EALocalHostSvc")
        DisplayNamePatterns = @("EA app", "EA Desktop")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "ea.com"
        IconText = "EA"
        IconColor = "#FF4747"
    },
    [pscustomobject]@{
        Name = "Epic Games Launcher"
        Id = "EpicGames.EpicGamesLauncher"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("EpicGamesLauncher", "EpicWebHelper", "EpicOnlineServices")
        DisplayNamePatterns = @("Epic Games Launcher", "Epic Online Services")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "epicgames.com"
        IconText = "EG"
        IconColor = "#111111"
    },
    [pscustomobject]@{
        Name = "Razer Synapse 4"
        Id = "RazerInc.RazerInstaller.Synapse4"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Razer Synapse", "Razer Central", "RazerAppEngine", "RazerAppEngineService", "Razer Synapse Service", "RzSynapse")
        DisplayNamePatterns = @("Razer Synapse 4", "Razer Synapse", "Razer Central")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "razer.com"
        IconText = "RZ"
        IconColor = "#44D62C"
    },
    [pscustomobject]@{
        Name = "Xbox PC"
        Id = "9MV0B5HZVK9Z"
        InstallMethod = "Winget"
        Source = "msstore"
        Processes = @("XboxPcApp", "Xbox", "GameBar", "XboxGameBarWidgets")
        DisplayNamePatterns = @("Xbox")
        AppxPackageNames = @("Microsoft.GamingApp")
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "xbox.com"
        IconText = "XB"
        IconColor = "#107C10"
    },
    [pscustomobject]@{
        Name = "Voicemod"
        Id = "Voicemod.Voicemod"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Voicemod", "VoicemodDesktop", "VoicemodV3")
        DisplayNamePatterns = @("Voicemod")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "voicemod.net"
        IconText = "VM"
        IconColor = "#00D4FF"
    },
    [pscustomobject]@{
        Name = "Clownfish Voice Changer"
        Id = "SharkLabs.ClownfishVoiceChanger"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("ClownfishVoiceChanger", "Clownfish")
        DisplayNamePatterns = @("Clownfish Voice Changer", "Clownfish")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "clownfish-translator.com"
        IconText = "CF"
        IconColor = "#F05A28"
    },
    [pscustomobject]@{
        Name = "Steam"
        Id = "Valve.Steam"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("steam", "steamwebhelper")
        DisplayNamePatterns = @("Steam")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "steampowered.com"
        IconText = "ST"
        IconColor = "#1B2838"
    },
    [pscustomobject]@{
        Name = "Medal"
        Id = "MedalB.V.Medal"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Medal", "MedalEncoder", "MedalOverlay")
        DisplayNamePatterns = @("Medal")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "medal.tv"
        IconText = "ME"
        IconColor = "#FFB800"
    },
    [pscustomobject]@{
        Name = "Overwolf (CurseForge package)"
        Id = "Overwolf.CurseForge"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Overwolf", "CurseForge")
        DisplayNamePatterns = @("Overwolf", "CurseForge")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $false
        IconDomain = "overwolf.com"
        IconText = "OW"
        IconColor = "#8F00FF"
    }
)

$RobloxChoices = @(
    [pscustomobject]@{
        Name = "Roblox Player"
        Id = "Roblox.Roblox"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("RobloxPlayerBeta", "RobloxPlayerLauncher", "RobloxStudioBeta", "RobloxCrashHandler")
        DisplayNamePatterns = @("Roblox Player", "Roblox")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $true
        IconDomain = "roblox.com"
        IconText = "RB"
        IconColor = "#191B1F"
    },
    [pscustomobject]@{
        Name = "Bloxstrap"
        Id = "pizzaboxer.Bloxstrap"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Bloxstrap", "RobloxPlayerBeta", "RobloxPlayerLauncher")
        DisplayNamePatterns = @("Bloxstrap")
        AppxPackageNames = @()
        Dependencies = @("Microsoft.DotNet.DesktopRuntime.6")
        IsRoblox = $true
        IconDomain = "github.com"
        IconText = "BX"
        IconColor = "#335FFF"
    },
    [pscustomobject]@{
        Name = "Fishstrap"
        Id = "Fishstrap.Fishstrap"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Fishstrap", "RobloxPlayerBeta", "RobloxPlayerLauncher")
        DisplayNamePatterns = @("Fishstrap")
        AppxPackageNames = @()
        Dependencies = @("Microsoft.DotNet.DesktopRuntime.6")
        IsRoblox = $true
        IconDomain = "github.com"
        IconText = "FS"
        IconColor = "#1D91D2"
    },
    [pscustomobject]@{
        Name = "Voidstrap"
        Id = $null
        InstallMethod = "GitHubRelease"
        Source = $null
        Repository = "voidstrap/Voidstrap"
        AssetPattern = "Voidstrap*.exe"
        Processes = @("Voidstrap", "RobloxPlayerBeta", "RobloxPlayerLauncher")
        DisplayNamePatterns = @("Voidstrap")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $true
        IconDomain = "github.com"
        IconText = "VD"
        IconColor = "#6F42C1"
    },
    [pscustomobject]@{
        Name = "Froststrap"
        Id = "Froststrap.Froststrap"
        InstallMethod = "Winget"
        Source = "winget"
        Processes = @("Froststrap", "RobloxPlayerBeta", "RobloxPlayerLauncher")
        DisplayNamePatterns = @("Froststrap")
        AppxPackageNames = @()
        Dependencies = @("Microsoft.DotNet.DesktopRuntime.10")
        IsRoblox = $true
        IconDomain = "github.com"
        IconText = "FR"
        IconColor = "#00AEEF"
    },
    [pscustomobject]@{
        Name = "Bubblestrap"
        Id = $null
        InstallMethod = "GitHubRelease"
        Source = $null
        Repository = "ItzBloxxy/Bubblestrap"
        AssetPattern = "Bubblestrap*.exe"
        Processes = @("Bubblestrap", "RobloxPlayerBeta", "RobloxPlayerLauncher")
        DisplayNamePatterns = @("Bubblestrap")
        AppxPackageNames = @()
        Dependencies = @()
        IsRoblox = $true
        IconDomain = "github.com"
        IconText = "BB"
        IconColor = "#42C6FF"
    }
)

$script:PackageAvailabilityCache = @{}
$script:GitHubAssetCache = @{}

$Root = if (-not [string]::IsNullOrWhiteSpace($env:APP_REPAIR_SCRIPT_ROOT)) {
    $env:APP_REPAIR_SCRIPT_ROOT.TrimEnd("\")
} elseif (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot
} else {
    (Get-Location).Path
}
function Pause-ForUser {
    Write-Host ""
    Read-Host "Press Enter to exit"
}

function Stop-Logging {
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
}

function Test-Winget {
    return $null -ne (Get-Command winget -ErrorAction SilentlyContinue)
}

function Invoke-Winget {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$Quiet
    )

    $shownArgs = ($Arguments | ForEach-Object {
        if ($_ -match "\s") { '"' + ($_ -replace '"', '\"') + '"' } else { $_ }
    }) -join " "

    if (-not $Quiet) {
        Write-Host "winget $shownArgs" -ForegroundColor DarkGray
    }

    $output = & winget @Arguments 2>&1
    $code = $LASTEXITCODE
    $text = ($output | Out-String)

    if (-not $Quiet -and $text.Trim().Length -gt 0) {
        $output | ForEach-Object { Write-Host $_ }
    }

    return [pscustomobject]@{
        ExitCode = $code
        Output   = $text
    }
}

function Test-AppInstalledById {
    param([Parameter(Mandatory = $true)][string]$Id)

    $result = Invoke-Winget -Quiet -Arguments @(
        "list", "--id", $Id, "-e",
        "--accept-source-agreements",
        "--disable-interactivity"
    )

    return ($result.Output -match [regex]::Escape($Id))
}

function Test-AppInstalledByDisplayName {
    param([Parameter(Mandatory = $true)][string[]]$Patterns)

    $registryPaths = @(
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )

    $installedApps = Get-ItemProperty -Path $registryPaths -ErrorAction SilentlyContinue
    foreach ($installedApp in $installedApps) {
        foreach ($pattern in $Patterns) {
            if ($installedApp.DisplayName -and $installedApp.DisplayName -like "*$pattern*") {
                return $true
            }
        }
    }

    return $false
}

function Test-AppInstalledByAppxPackage {
    param([Parameter(Mandatory = $true)][string[]]$PackageNames)

    foreach ($packageName in $PackageNames) {
        if ([string]::IsNullOrWhiteSpace($packageName)) {
            continue
        }

        if (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue) {
            return $true
        }
    }

    return $false
}

function Test-AppInstalled {
    param([Parameter(Mandatory = $true)]$App)

    if ($App.AppxPackageNames -and (Test-AppInstalledByAppxPackage -PackageNames $App.AppxPackageNames)) {
        return $true
    }

    if ($App.Id -and (Test-AppInstalledById -Id $App.Id)) {
        return $true
    }

    if ($App.DisplayNamePatterns -and (Test-AppInstalledByDisplayName -Patterns $App.DisplayNamePatterns)) {
        return $true
    }

    return $false
}

function Get-WingetSource {
    param([Parameter(Mandatory = $true)]$App)

    if ($App.PSObject.Properties.Name -contains "Source" -and -not [string]::IsNullOrWhiteSpace($App.Source)) {
        return $App.Source
    }

    return "winget"
}

function Test-WingetPackageAvailable {
    param([Parameter(Mandatory = $true)]$App)

    if (-not $App.Id) {
        return $false
    }

    $source = Get-WingetSource -App $App
    $cacheKey = "$source|$($App.Id)"

    if ($script:PackageAvailabilityCache.ContainsKey($cacheKey)) {
        return $script:PackageAvailabilityCache[$cacheKey]
    }

    $show = Invoke-Winget -Quiet -Arguments @(
        "show", "--id", $App.Id, "-e",
        "--source", $source,
        "--accept-source-agreements",
        "--disable-interactivity"
    )

    $available = ($show.ExitCode -eq 0 -and $show.Output -match [regex]::Escape($App.Id))

    if (-not $available) {
        $search = Invoke-Winget -Quiet -Arguments @(
            "search", "--id", $App.Id,
            "--source", $source,
            "--accept-source-agreements",
            "--disable-interactivity"
        )

        $available = ($search.ExitCode -eq 0 -and $search.Output -match [regex]::Escape($App.Id))
    }

    $script:PackageAvailabilityCache[$cacheKey] = $available
    return $available
}

function Get-GitHubReleaseAsset {
    param([Parameter(Mandatory = $true)]$App)

    $cacheKey = "$($App.Repository)|$($App.AssetPattern)"
    if ($script:GitHubAssetCache.ContainsKey($cacheKey)) {
        return $script:GitHubAssetCache[$cacheKey]
    }

    try {
        $release = Invoke-RestMethod `
            -Uri "https://api.github.com/repos/$($App.Repository)/releases/latest" `
            -Headers @{ "User-Agent" = "AppRepairTool" } `
            -UseBasicParsing

        $asset = $release.assets |
            Where-Object { $_.name -like $App.AssetPattern -and $_.name -like "*.exe" } |
            Select-Object -First 1

        if ($asset) {
            $result = [pscustomobject]@{
                Name = $asset.name
                Url = $asset.browser_download_url
                Release = $release.tag_name
            }
            $script:GitHubAssetCache[$cacheKey] = $result
            return $result
        }
    } catch {
        Write-Host "Could not check GitHub release for $($App.Name): $($_.Exception.Message)" -ForegroundColor Yellow
    }

    $script:GitHubAssetCache[$cacheKey] = $null
    return $null
}

function Test-AppInstallerAvailable {
    param([Parameter(Mandatory = $true)]$App)

    switch ($App.InstallMethod) {
        "Winget" {
            return (Test-WingetPackageAvailable -App $App)
        }
        "GitHubRelease" {
            return ($null -ne (Get-GitHubReleaseAsset -App $App))
        }
        default {
            return $false
        }
    }
}

function ConvertTo-DependencyApp {
    param([Parameter(Mandatory = $true)]$Dependency)

    if ($Dependency -is [string]) {
        return [pscustomobject]@{
            Name = $Dependency
            Id = $Dependency
            InstallMethod = "Winget"
            Source = "winget"
            Processes = @()
            DisplayNamePatterns = @($Dependency)
            AppxPackageNames = @()
            Dependencies = @()
            InstallArguments = @()
            UpgradeArguments = @()
            Required = $true
        }
    }

    $propertyNames = @($Dependency.PSObject.Properties.Name)
    $id = if ($propertyNames -contains "Id") { [string]$Dependency.Id } else { "" }
    $name = if ($propertyNames -contains "Name" -and -not [string]::IsNullOrWhiteSpace($Dependency.Name)) { [string]$Dependency.Name } else { $id }
    $source = if ($propertyNames -contains "Source" -and -not [string]::IsNullOrWhiteSpace($Dependency.Source)) { [string]$Dependency.Source } else { "winget" }
    $displayNamePatterns = if ($propertyNames -contains "DisplayNamePatterns") { @($Dependency.DisplayNamePatterns) } else { @($name, $id) }
    $installArguments = if ($propertyNames -contains "InstallArguments") { @($Dependency.InstallArguments) } else { @() }
    $upgradeArguments = if ($propertyNames -contains "UpgradeArguments") { @($Dependency.UpgradeArguments) } else { @() }
    $required = if ($propertyNames -contains "Required") { [bool]$Dependency.Required } else { $true }

    return [pscustomobject]@{
        Name = $name
        Id = $id
        InstallMethod = "Winget"
        Source = $source
        Processes = @()
        DisplayNamePatterns = $displayNamePatterns
        AppxPackageNames = @()
        Dependencies = @()
        InstallArguments = $installArguments
        UpgradeArguments = $upgradeArguments
        Required = $required
    }
}

function Ensure-AppDependencies {
    param(
        [Parameter(Mandatory = $true)]$App,
        [switch]$Force
    )

    foreach ($dependency in @($App.Dependencies)) {
        $dependencyApp = ConvertTo-DependencyApp -Dependency $dependency
        if (-not $dependencyApp -or [string]::IsNullOrWhiteSpace($dependencyApp.Id)) {
            continue
        }

        $dependencyInstalled = Test-AppInstalledById -Id $dependencyApp.Id
        if ($dependencyInstalled -and -not $Force) {
            Write-Host "Dependency already installed: $($dependencyApp.Name) ($($dependencyApp.Id))" -ForegroundColor DarkGray
            continue
        }

        if (-not (Test-WingetPackageAvailable -App $dependencyApp)) {
            Write-Host "Could not confirm dependency availability in winget: $($dependencyApp.Name) ($($dependencyApp.Id)). Trying install anyway." -ForegroundColor Yellow
        }

        if ($dependencyInstalled) {
            Write-Host "Reinstalling required dependency: $($dependencyApp.Name) ($($dependencyApp.Id))"
        } else {
            Write-Host "Installing required dependency: $($dependencyApp.Name) ($($dependencyApp.Id))"
        }

        $dependencyInstall = Install-WingetApp -App $dependencyApp

        if (-not $dependencyInstall -and -not (Test-AppInstalledById -Id $dependencyApp.Id)) {
            Write-Host "Could not install required dependency: $($dependencyApp.Name) ($($dependencyApp.Id))" -ForegroundColor Red
            if ($dependencyApp.Required) {
                return $false
            }
        } elseif (-not $dependencyInstall) {
            Write-Host "Winget reported a problem, but the dependency is still detected: $($dependencyApp.Name) ($($dependencyApp.Id))" -ForegroundColor Yellow
        }
    }

    return $true
}

function Test-AppReadyToInstall {
    param(
        [Parameter(Mandatory = $true)]$App,
        [switch]$ForceDependencies
    )

    if (-not (Test-AppInstallerAvailable -App $App)) {
        Write-Host "Installer source is not available for $($App.Name). Skipping so nothing gets removed." -ForegroundColor Red
        return $false
    }

    if (-not (Ensure-AppDependencies -App $App -Force:$ForceDependencies)) {
        Write-Host "Dependencies are not ready for $($App.Name). Skipping so nothing gets removed." -ForegroundColor Red
        return $false
    }

    return $true
}

function Stop-AppProcesses {
    param([Parameter(Mandatory = $true)]$App)

    foreach ($processName in @($App.Processes)) {
        $matches = Get-Process -Name $processName -ErrorAction SilentlyContinue
        if ($matches) {
            Write-Host "Closing process: $processName"
            $matches | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }
}

function Wait-ForRemoved {
    param([Parameter(Mandatory = $true)]$App)

    for ($i = 1; $i -le 10; $i++) {
        Start-Sleep -Seconds 2
        if (-not (Test-AppInstalled -App $App)) {
            return $true
        }
    }

    return $false
}

function Install-WingetApp {
    param([Parameter(Mandatory = $true)]$App)

    Write-Host "Installing $($App.Name)..."
    $source = Get-WingetSource -App $App

    $installArguments = @(
        "install", "--id", $App.Id, "-e",
        "--source", $source,
        "--silent",
        "--accept-source-agreements",
        "--accept-package-agreements",
        "--disable-interactivity",
        "--force"
    )

    if ($App.PSObject.Properties.Name -contains "InstallArguments") {
        foreach ($argument in @($App.InstallArguments)) {
            if (-not [string]::IsNullOrWhiteSpace($argument)) {
                $installArguments += $argument
            }
        }
    }

    $install = Invoke-Winget -Arguments $installArguments

    if ($install.ExitCode -eq 0) {
        return $true
    }

    if ($install.Output -match "include-unknown|version number cannot be determined|already installed|Trying to upgrade") {
        Write-Host "Winget still sees $($App.Name) as installed with an unknown version. Trying upgrade fallback..." -ForegroundColor Yellow

        $upgradeArguments = @(
            "upgrade", "--id", $App.Id, "-e",
            "--source", $source,
            "--silent",
            "--accept-source-agreements",
            "--accept-package-agreements",
            "--disable-interactivity",
            "--include-unknown",
            "--force"
        )

        if ($App.PSObject.Properties.Name -contains "UpgradeArguments") {
            foreach ($argument in @($App.UpgradeArguments)) {
                if (-not [string]::IsNullOrWhiteSpace($argument)) {
                    $upgradeArguments += $argument
                }
            }
        }

        $upgrade = Invoke-Winget -Arguments $upgradeArguments

        if ($upgrade.ExitCode -eq 0) {
            return $true
        }
    }

    return $false
}

function Install-GitHubReleaseApp {
    param([Parameter(Mandatory = $true)]$App)

    $asset = Get-GitHubReleaseAsset -App $App
    if (-not $asset) {
        Write-Host "No downloadable installer asset was found for $($App.Name)." -ForegroundColor Red
        return $false
    }

    $downloadDir = Join-Path $env:TEMP "AppRepairTool"
    New-Item -ItemType Directory -Path $downloadDir -Force | Out-Null

    $installerPath = Join-Path $downloadDir $asset.Name
    Write-Host "Downloading $($App.Name) $($asset.Release)..."
    Write-Host $asset.Url -ForegroundColor DarkGray

    try {
        Invoke-WebRequest -Uri $asset.Url -OutFile $installerPath -UseBasicParsing
    } catch {
        Write-Host "Download failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }

    Write-Host "Running installer: $installerPath"
    try {
        $process = Start-Process -FilePath $installerPath -Wait -PassThru
        return ($null -eq $process.ExitCode -or $process.ExitCode -eq 0)
    } catch {
        Write-Host "Installer could not be started: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Install-App {
    param([Parameter(Mandatory = $true)]$App)

    switch ($App.InstallMethod) {
        "Winget" {
            return (Install-WingetApp -App $App)
        }
        "GitHubRelease" {
            return (Install-GitHubReleaseApp -App $App)
        }
        default {
            Write-Host "Unknown install method for $($App.Name): $($App.InstallMethod)" -ForegroundColor Red
            return $false
        }
    }
}

function Get-VSCodeCliPath {
    $paths = @()

    $codeCommand = Get-Command "code.cmd" -ErrorAction SilentlyContinue
    if ($codeCommand) {
        $paths += $codeCommand.Source
    }

    $codeCommand = Get-Command "code" -ErrorAction SilentlyContinue
    if ($codeCommand) {
        $paths += $codeCommand.Source
    }

    if ($env:LocalAppData) {
        $paths += Join-Path $env:LocalAppData "Programs\Microsoft VS Code\bin\code.cmd"
    }

    if ($env:ProgramFiles) {
        $paths += Join-Path $env:ProgramFiles "Microsoft VS Code\bin\code.cmd"
    }

    if (${env:ProgramFiles(x86)}) {
        $paths += Join-Path ${env:ProgramFiles(x86)} "Microsoft VS Code\bin\code.cmd"
    }

    foreach ($path in @($paths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    return $null
}

function Install-VSCodeExtensions {
    param(
        [Parameter(Mandatory = $true)]$App,
        [switch]$Force
    )

    if (-not ($App.PSObject.Properties.Name -contains "VSCodeExtensions") -or @($App.VSCodeExtensions).Count -eq 0) {
        return $true
    }

    $codePath = Get-VSCodeCliPath
    if (-not $codePath) {
        Write-Host "VS Code command line was not found, so extensions could not be installed." -ForegroundColor Red
        return $false
    }

    $allSucceeded = $true
    foreach ($extension in @($App.VSCodeExtensions)) {
        if ($extension -is [string]) {
            $extensionId = $extension
            $extensionName = $extension
        } else {
            $extensionId = [string]$extension.Id
            $extensionName = if ($extension.Name) { [string]$extension.Name } else { $extensionId }
        }

        if ([string]::IsNullOrWhiteSpace($extensionId)) {
            continue
        }

        if ($Force) {
            Write-Host "Reinstalling VS Code extension: $extensionName ($extensionId)"
        } else {
            Write-Host "Installing VS Code extension: $extensionName ($extensionId)"
        }

        $extensionOutput = & $codePath "--install-extension" $extensionId "--force" 2>&1
        $extensionExitCode = $LASTEXITCODE

        if ($extensionOutput) {
            $extensionOutput | ForEach-Object { Write-Host $_ -ForegroundColor DarkGray }
        }

        if ($extensionExitCode -ne 0) {
            Write-Host "Could not install VS Code extension: $extensionName ($extensionId)" -ForegroundColor Red
            $allSucceeded = $false
        }
    }

    return $allSucceeded
}

function Invoke-AppPostInstallTasks {
    param(
        [Parameter(Mandatory = $true)]$App,
        [switch]$Force
    )

    $tasksSucceeded = $true

    if (-not (Install-VSCodeExtensions -App $App -Force:$Force)) {
        $tasksSucceeded = $false
    }

    return $tasksSucceeded
}

function Install-AppIfMissing {
    param(
        [Parameter(Mandatory = $true)]$App,
        [switch]$ForceDependencies
    )

    Write-Host ""
    Write-Host "===== $($App.Name) =====" -ForegroundColor Cyan

    if (Test-AppInstalled -App $App) {
        Write-Host "$($App.Name) is already installed. Main app install skipped." -ForegroundColor Yellow
        $dependenciesReady = Ensure-AppDependencies -App $App -Force:$ForceDependencies
        $postInstallTasksReady = Invoke-AppPostInstallTasks -App $App -Force:$ForceDependencies

        if (-not $dependenciesReady -or -not $postInstallTasksReady) {
            Write-Host "$($App.Name) supporting setup may have failed." -ForegroundColor Red
        }
        return
    }

    if (-not (Test-AppReadyToInstall -App $App -ForceDependencies:$ForceDependencies)) {
        return
    }

    $installed = Install-App -App $App

    if ($installed -or (Test-AppInstalled -App $App)) {
        if (Invoke-AppPostInstallTasks -App $App -Force:$ForceDependencies) {
            Write-Host "$($App.Name) install finished." -ForegroundColor Green
        } else {
            Write-Host "$($App.Name) install finished, but supporting setup may have failed." -ForegroundColor Red
        }
    } else {
        Write-Host "$($App.Name) install may have failed." -ForegroundColor Red
    }
}

function Get-AppKey {
    param([Parameter(Mandatory = $true)]$App)

    $idPart = if ($App.Id) { $App.Id } elseif ($App.Repository) { $App.Repository } else { $App.Name }
    return "$($App.InstallMethod)|$idPart|$($App.Name)"
}

function Get-InstallableAppCatalog {
    return @($Apps)
}

function ConvertTo-SafeFileName {
    param([Parameter(Mandatory = $true)][string]$Name)

    $safeName = ($Name -replace '[^\w\.-]+', '-').Trim("-")
    if ([string]::IsNullOrWhiteSpace($safeName)) {
        return "app"
    }

    return $safeName
}

function New-AppFallbackIcon {
    param(
        [Parameter(Mandatory = $true)]$App,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $size = 128
    $bitmap = New-Object System.Drawing.Bitmap $size, $size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $background = [System.Drawing.ColorTranslator]::FromHtml("#2F343D")
        if ($App.IconColor) {
            try { $background = [System.Drawing.ColorTranslator]::FromHtml($App.IconColor) } catch {}
        }

        $brush = New-Object System.Drawing.SolidBrush $background
        $shapePath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $radius = 28
        $rect = New-Object System.Drawing.Rectangle 8, 8, 112, 112
        $diameter = $radius * 2
        $shapePath.AddArc($rect.Left, $rect.Top, $diameter, $diameter, 180, 90)
        $shapePath.AddArc(($rect.Right - $diameter), $rect.Top, $diameter, $diameter, 270, 90)
        $shapePath.AddArc(($rect.Right - $diameter), ($rect.Bottom - $diameter), $diameter, $diameter, 0, 90)
        $shapePath.AddArc($rect.Left, ($rect.Bottom - $diameter), $diameter, $diameter, 90, 90)
        $shapePath.CloseFigure()
        $graphics.FillPath($brush, $shapePath)
        $shapePath.Dispose()
        $brush.Dispose()

        $iconText = if ($App.IconText) { [string]$App.IconText } else { ([string]$App.Name).Substring(0, [Math]::Min(2, ([string]$App.Name).Length)).ToUpperInvariant() }
        $font = New-Object System.Drawing.Font "Segoe UI Semibold", 34, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
        $textBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $graphics.DrawString($iconText, $font, $textBrush, (New-Object System.Drawing.RectangleF 0, 0, $size, $size), $format)

        $format.Dispose()
        $textBrush.Dispose()
        $font.Dispose()
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Test-AppIconNeedsRefresh {
    param([Parameter(Mandatory = $true)][string]$Path)

    try {
        $image = [System.Drawing.Image]::FromFile($Path)
        try {
            return $false
        } finally {
            $image.Dispose()
        }
    } catch {
        return $true
    }
}

function Get-AppIconPath {
    param([Parameter(Mandatory = $true)]$App)

    $iconDir = Join-Path $Root "assets\app-icons"
    New-Item -ItemType Directory -Path $iconDir -Force | Out-Null

    $iconPath = Join-Path $iconDir ((ConvertTo-SafeFileName -Name $App.Name) + ".png")
    if (Test-Path -LiteralPath $iconPath) {
        if (-not (Test-AppIconNeedsRefresh -Path $iconPath)) {
            return $iconPath
        }
        Remove-Item -LiteralPath $iconPath -Force -ErrorAction SilentlyContinue
    }

    $downloaded = $false
    if ($App.IconDomain) {
        $domain = [uri]::EscapeDataString("https://$($App.IconDomain)")
        $iconUrl = "https://www.google.com/s2/favicons?domain_url=$domain&sz=256"
        try {
            $client = New-Object System.Net.WebClient
            try {
                $client.DownloadFile($iconUrl, $iconPath)
                $downloaded = $true
            } finally {
                $client.Dispose()
            }
        } catch {
            $downloaded = $false
        }
    }

    if ($downloaded) {
        try {
            $image = [System.Drawing.Image]::FromFile($iconPath)
            try {
                if ($image.Width -gt 0 -and $image.Height -gt 0) {
                    return $iconPath
                }
            } finally {
                $image.Dispose()
            }
            Remove-Item -LiteralPath $iconPath -Force -ErrorAction SilentlyContinue
        } catch {
            Remove-Item -LiteralPath $iconPath -Force -ErrorAction SilentlyContinue
        }
    }

    New-AppFallbackIcon -App $App -Path $iconPath
    return $iconPath
}

function Get-AppIconImage {
    param([Parameter(Mandatory = $true)]$App)

    $iconPath = Get-AppIconPath -App $App
    if (-not (Test-Path -LiteralPath $iconPath)) {
        New-AppFallbackIcon -App $App -Path $iconPath
    }

    $bytes = [System.IO.File]::ReadAllBytes($iconPath)
    $stream = New-Object System.IO.MemoryStream(,$bytes)
    $sourceImage = [System.Drawing.Image]::FromStream($stream)
    $bitmap = New-Object System.Drawing.Bitmap 56, 56
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($sourceImage, 0, 0, 56, 56)
    } finally {
        $graphics.Dispose()
        $sourceImage.Dispose()
        $stream.Dispose()
    }

    return $bitmap
}

function Set-AppTileStyle {
    param([Parameter(Mandatory = $true)]$Tile)

    if ($Tile.Checked) {
        $Tile.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#DCEBFF")
        $Tile.FlatAppearance.BorderColor = [System.Drawing.ColorTranslator]::FromHtml("#2979FF")
        $Tile.FlatAppearance.BorderSize = 2
    } else {
        $Tile.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#F5F7FA")
        $Tile.FlatAppearance.BorderColor = [System.Drawing.ColorTranslator]::FromHtml("#C7CDD8")
        $Tile.FlatAppearance.BorderSize = 1
    }
}

function Update-SelectionCounter {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)]$InstallCounter,
        [Parameter(Mandatory = $true)]$ReinstallCounter
    )

    $InstallCounter.Text = "$($State.InstallSelections.Count) selected"
    $ReinstallCounter.Text = "$($State.ReinstallSelections.Count) selected"
}

function New-AppTile {
    param(
        [Parameter(Mandatory = $true)]$App,
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)]$InstallCounter,
        [Parameter(Mandatory = $true)]$ReinstallCounter
    )

    $tile = New-Object System.Windows.Forms.CheckBox
    $tile.Appearance = [System.Windows.Forms.Appearance]::Button
    $tile.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $tile.Width = 152
    $tile.Height = 118
    $tile.Margin = New-Object System.Windows.Forms.Padding 6
    $tile.Padding = New-Object System.Windows.Forms.Padding 6
    $tile.Text = $App.Name
    $tile.Font = New-Object System.Drawing.Font "Segoe UI", 9
    $tile.TextAlign = [System.Drawing.ContentAlignment]::BottomCenter
    $tile.ImageAlign = [System.Drawing.ContentAlignment]::TopCenter
    $tile.TextImageRelation = [System.Windows.Forms.TextImageRelation]::ImageAboveText
    $tile.Image = Get-AppIconImage -App $App
    $tile.Tag = [pscustomobject]@{
        App = $App
        Action = $Action
    }
    Set-AppTileStyle -Tile $tile

    $tile.Add_CheckedChanged({
        $tileState = $this.FindForm().Tag
        $tag = $this.Tag
        $key = Get-AppKey -App $tag.App

        if ($tag.Action -eq "Install") {
            if ($this.Checked) {
                $tileState.InstallSelections[$key] = $tag.App
                if ($tileState.ReinstallTiles.ContainsKey($key)) {
                    $tileState.ReinstallTiles[$key].Checked = $false
                }
            } else {
                $tileState.InstallSelections.Remove($key)
            }
        } else {
            if ($this.Checked) {
                $tileState.ReinstallSelections[$key] = $tag.App
                if ($tileState.InstallTiles.ContainsKey($key)) {
                    $tileState.InstallTiles[$key].Checked = $false
                }
            } else {
                $tileState.ReinstallSelections.Remove($key)
            }
        }

        Set-AppTileStyle -Tile $this
        Update-SelectionCounter -State $tileState -InstallCounter $tileState.InstallCounter -ReinstallCounter $tileState.ReinstallCounter
    })

    $key = Get-AppKey -App $App
    if ($Action -eq "Install") {
        $State.InstallTiles[$key] = $tile
    } else {
        $State.ReinstallTiles[$key] = $tile
    }

    return $tile
}

function Add-GuiSection {
    param(
        [Parameter(Mandatory = $true)]$Container,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)]$Counter,
        [Parameter(Mandatory = $true)]$Apps,
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $true)]$State
    )

    $section = New-Object System.Windows.Forms.FlowLayoutPanel
    $section.AutoSize = $true
    $section.AutoSizeMode = [System.Windows.Forms.AutoSizeMode]::GrowAndShrink
    $section.FlowDirection = [System.Windows.Forms.FlowDirection]::TopDown
    $section.WrapContents = $false
    $section.Margin = New-Object System.Windows.Forms.Padding 0, 0, 0, 16
    $section.Padding = New-Object System.Windows.Forms.Padding 0
    $section.Width = [Math]::Max(620, $Container.ClientSize.Width - 8)

    $header = New-Object System.Windows.Forms.Panel
    $header.Height = 34
    $header.Dock = [System.Windows.Forms.DockStyle]::Top
    $header.Margin = New-Object System.Windows.Forms.Padding 0, 0, 0, 4

    $titleLabel = New-Object System.Windows.Forms.Label
    $titleLabel.Text = $Title
    $titleLabel.Dock = [System.Windows.Forms.DockStyle]::Left
    $titleLabel.Width = 260
    $titleLabel.Font = New-Object System.Drawing.Font "Segoe UI Semibold", 13
    $titleLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleLeft

    $Counter.Dock = [System.Windows.Forms.DockStyle]::Right
    $Counter.Width = 120
    $Counter.Font = New-Object System.Drawing.Font "Segoe UI", 9
    $Counter.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#4A5568")
    $Counter.TextAlign = [System.Drawing.ContentAlignment]::MiddleRight

    $header.Controls.Add($Counter)
    $header.Controls.Add($titleLabel)
    $Container.Controls.Add($header)

    $flow = New-Object System.Windows.Forms.FlowLayoutPanel
    $flow.AutoSize = $true
    $flow.AutoSizeMode = [System.Windows.Forms.AutoSizeMode]::GrowAndShrink
    $flow.Dock = [System.Windows.Forms.DockStyle]::Top
    $flow.FlowDirection = [System.Windows.Forms.FlowDirection]::LeftToRight
    $flow.WrapContents = $true
    $flow.Margin = New-Object System.Windows.Forms.Padding 0, 0, 0, 10

    if ($Apps.Count -eq 0) {
        $empty = New-Object System.Windows.Forms.Label
        $empty.AutoSize = $true
        $empty.Margin = New-Object System.Windows.Forms.Padding 8
        $empty.Font = New-Object System.Drawing.Font "Segoe UI", 10
        $empty.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#718096")
        $empty.Text = "No apps are available for this section."
        $flow.Controls.Add($empty)
    } else {
        foreach ($app in $Apps) {
            $flow.Controls.Add((New-AppTile -App $app -Action $Action -State $State -InstallCounter $State.InstallCounter -ReinstallCounter $State.ReinstallCounter))
        }
    }

    $Container.Controls.Add($flow)
    return $flow
}

function Show-AppSelectionGui {
    param(
        [Parameter(Mandatory = $true)]$InstallApps,
        [Parameter(Mandatory = $true)]$ReinstallApps,
        [switch]$MissingOnly
    )

    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
    } catch {
        Write-Host "Could not load the GUI components: $($_.Exception.Message)" -ForegroundColor Yellow
        return [pscustomobject]@{
            Cancelled = $true
            Install = @()
            Reinstall = @()
        }
    }

    [System.Windows.Forms.Application]::EnableVisualStyles()

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "App Repair Tool"
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    $form.Size = New-Object System.Drawing.Size 920, 700
    $form.MinimumSize = New-Object System.Drawing.Size 760, 560
    $form.Font = New-Object System.Drawing.Font "Segoe UI", 10
    $form.BackColor = [System.Drawing.Color]::White

    $state = [pscustomobject]@{
        InstallSelections = @{}
        ReinstallSelections = @{}
        InstallTiles = @{}
        ReinstallTiles = @{}
        InstallCounter = New-Object System.Windows.Forms.Label
        ReinstallCounter = New-Object System.Windows.Forms.Label
        Result = $null
    }
    $form.Tag = $state

    $header = New-Object System.Windows.Forms.Panel
    $header.Dock = [System.Windows.Forms.DockStyle]::Top
    $header.Height = 72
    $header.Padding = New-Object System.Windows.Forms.Padding 18, 12, 18, 8
    $header.BackColor = [System.Drawing.Color]::White

    $title = New-Object System.Windows.Forms.Label
    $title.Text = "Choose apps to install or reinstall"
    $title.Dock = [System.Windows.Forms.DockStyle]::Top
    $title.Height = 28
    $title.Font = New-Object System.Drawing.Font "Segoe UI Semibold", 15

    $subtitle = New-Object System.Windows.Forms.Label
    $subtitle.Text = if ($MissingOnly) { "Missing-app recovery only shows apps that are not currently detected." } else { "Pick any tiles below, then click Done to run the selected actions." }
    $subtitle.Dock = [System.Windows.Forms.DockStyle]::Top
    $subtitle.Height = 24
    $subtitle.Font = New-Object System.Drawing.Font "Segoe UI", 9
    $subtitle.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#4A5568")

    $header.Controls.Add($subtitle)
    $header.Controls.Add($title)

    $footer = New-Object System.Windows.Forms.Panel
    $footer.Dock = [System.Windows.Forms.DockStyle]::Bottom
    $footer.Height = 64
    $footer.Padding = New-Object System.Windows.Forms.Padding 18, 10, 18, 10
    $footer.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#F7FAFC")

    $done = New-Object System.Windows.Forms.Button
    $done.Text = "Done"
    $done.Width = 110
    $done.Height = 36
    $done.Anchor = [System.Windows.Forms.AnchorStyles]::Right
    $done.Left = 770
    $done.Top = 14
    $done.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#2563EB")
    $done.ForeColor = [System.Drawing.Color]::White
    $done.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $done.FlatAppearance.BorderSize = 0

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = "Cancel"
    $cancel.Width = 110
    $cancel.Height = 36
    $cancel.Anchor = [System.Windows.Forms.AnchorStyles]::Right
    $cancel.Left = 650
    $cancel.Top = 14
    $cancel.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $cancel.BackColor = [System.Drawing.Color]::White

    $footer.Controls.Add($done)
    $footer.Controls.Add($cancel)

    $scroll = New-Object System.Windows.Forms.Panel
    $scroll.Dock = [System.Windows.Forms.DockStyle]::Fill
    $scroll.AutoScroll = $true
    $scroll.BackColor = [System.Drawing.Color]::White
    $scroll.Padding = New-Object System.Windows.Forms.Padding 18, 4, 18, 18

    $container = New-Object System.Windows.Forms.TableLayoutPanel
    $container.ColumnCount = 1
    $container.RowCount = 0
    $container.Dock = [System.Windows.Forms.DockStyle]::Top
    $container.AutoSize = $true
    $container.AutoSizeMode = [System.Windows.Forms.AutoSizeMode]::GrowAndShrink
    $container.BackColor = [System.Drawing.Color]::White

    $installFlow = Add-GuiSection -Container $container -Title "Install selected" -Counter $state.InstallCounter -Apps $InstallApps -Action "Install" -State $state
    $reinstallFlow = Add-GuiSection -Container $container -Title "Reinstall selected" -Counter $state.ReinstallCounter -Apps $ReinstallApps -Action "Reinstall" -State $state
    $scroll.Controls.Add($container)
    $form.Controls.Add($scroll)

    $resizeFlows = {
        $newWidth = [Math]::Max(600, $scroll.ClientSize.Width - 50)
        $container.Width = $newWidth
        $installFlow.Width = $newWidth
        $reinstallFlow.Width = $newWidth
        $cancel.Left = $footer.ClientSize.Width - 240
        $done.Left = $footer.ClientSize.Width - 120
    }
    $form.Add_Shown($resizeFlows)
    $form.Add_Resize($resizeFlows)

    Update-SelectionCounter -State $state -InstallCounter $state.InstallCounter -ReinstallCounter $state.ReinstallCounter

    $done.Add_Click({
        $tileState = $this.FindForm().Tag
        $tileState.Result = [pscustomobject]@{
            Cancelled = $false
            Install = @($InstallApps | Where-Object { $tileState.InstallSelections.ContainsKey((Get-AppKey -App $_)) })
            Reinstall = @($ReinstallApps | Where-Object { $tileState.ReinstallSelections.ContainsKey((Get-AppKey -App $_)) })
        }
        $this.FindForm().DialogResult = [System.Windows.Forms.DialogResult]::OK
        $this.FindForm().Close()
    })

    $cancel.Add_Click({
        $this.FindForm().DialogResult = [System.Windows.Forms.DialogResult]::Cancel
        $this.FindForm().Close()
    })

    [void]$form.ShowDialog()

    if ($state.Result) {
        return $state.Result
    }

    return [pscustomobject]@{
        Cancelled = $true
        Install = @()
        Reinstall = @()
    }
}

function Invoke-SelectedAppActions {
    param(
        $InstallApps,
        $ReinstallApps
    )

    $installList = @($InstallApps | Where-Object { $null -ne $_ })
    $reinstallList = @($ReinstallApps | Where-Object { $null -ne $_ })

    if ($installList.Count -eq 0 -and $reinstallList.Count -eq 0) {
        Write-Host "No apps were selected." -ForegroundColor Yellow
        return
    }

    Write-Host ""
    Write-Host "Selected install actions: $($installList.Count)" -ForegroundColor Cyan
    Write-Host "Selected reinstall actions: $($reinstallList.Count)" -ForegroundColor Cyan

    foreach ($app in $installList) {
        Install-AppIfMissing -App $app
    }

    foreach ($app in $reinstallList) {
        Reinstall-App -App $app
    }
}

function Reinstall-App {
    param([Parameter(Mandatory = $true)]$App)

    Write-Host ""
    Write-Host "===== $($App.Name) =====" -ForegroundColor Cyan

    if (-not (Test-AppInstalled -App $App)) {
        Write-Host "$($App.Name) is not installed. Running install instead and reinstalling supporting tools for this selection." -ForegroundColor Yellow
        Install-AppIfMissing -App $App -ForceDependencies
        return
    }

    if (-not (Test-AppReadyToInstall -App $App -ForceDependencies)) {
        return
    }

    Write-Host "$($App.Name) detected. Closing related processes..."
    Stop-AppProcesses -App $App

    if ($App.InstallMethod -ne "Winget") {
        Write-Host "$($App.Name) is installed from a direct release. Running the installer over the current install instead of uninstalling first." -ForegroundColor Yellow
        $installed = Install-App -App $App

        if ($installed -or (Test-AppInstalled -App $App)) {
            if (Invoke-AppPostInstallTasks -App $App -Force) {
                Write-Host "$($App.Name) reinstall finished." -ForegroundColor Green
            } else {
                Write-Host "$($App.Name) reinstall finished, but supporting setup may have failed." -ForegroundColor Red
            }
        } else {
            Write-Host "$($App.Name) reinstall may have failed." -ForegroundColor Red
        }
        return
    }

    Write-Host "Uninstalling $($App.Name)..."
    $source = Get-WingetSource -App $App
    $uninstall = Invoke-Winget -Arguments @(
        "uninstall", "--id", $App.Id, "-e",
        "--source", $source,
        "--silent",
        "--all-versions",
        "--accept-source-agreements",
        "--disable-interactivity",
        "--force"
    )

    if ($uninstall.ExitCode -ne 0) {
        Write-Host "$($App.Name) uninstall reported a problem. Trying reinstall/repair anyway..." -ForegroundColor Yellow
    } else {
        [void](Wait-ForRemoved -App $App)
    }

    $installed = Install-App -App $App

    if ($installed -or (Test-AppInstalled -App $App)) {
        if (Invoke-AppPostInstallTasks -App $App -Force) {
            Write-Host "$($App.Name) repair finished." -ForegroundColor Green
        } else {
            Write-Host "$($App.Name) repair finished, but supporting setup may have failed." -ForegroundColor Red
        }
    } else {
        Write-Host "$($App.Name) repair may have failed." -ForegroundColor Red
    }
}

function Invoke-AppPrompt {
    param([Parameter(Mandatory = $true)]$App)

    Write-Host ""
    Write-Host "===== $($App.Name) =====" -ForegroundColor Cyan

    if (Test-AppInstalled -App $App) {
        Write-Host "Status: installed" -ForegroundColor Green
    } else {
        Write-Host "Status: not installed" -ForegroundColor Yellow
    }

    while ($true) {
        $answer = Read-Host "Choose [i] install, [r] reinstall, or [s] skip"
        switch ($answer.ToLowerInvariant()) {
            "i" {
                Install-AppIfMissing -App $App
                return
            }
            "r" {
                Reinstall-App -App $App
                return
            }
            "s" {
                Write-Host "Skipped $($App.Name)." -ForegroundColor Yellow
                return
            }
            default {
                Write-Host "Please type i, r, or s." -ForegroundColor Yellow
            }
        }
    }
}

function Select-RobloxChoice {
    Write-Host ""
    Write-Host "===== Roblox =====" -ForegroundColor Cyan
    Write-Host "Choose the Roblox installer/bootstrapper to work with."

    for ($i = 0; $i -lt $RobloxChoices.Count; $i++) {
        Write-Host ("[{0}] {1}" -f ($i + 1), $RobloxChoices[$i].Name)
    }
    Write-Host "[s] skip Roblox"

    while ($true) {
        $choice = Read-Host "Select a Roblox option"
        if ($choice.ToLowerInvariant() -eq "s") {
            Write-Host "Skipped Roblox." -ForegroundColor Yellow
            return $null
        }

        $number = 0
        if ([int]::TryParse($choice, [ref]$number) -and $number -ge 1 -and $number -le $RobloxChoices.Count) {
            return $RobloxChoices[$number - 1]
        }

        Write-Host "Please select a listed number, or s to skip." -ForegroundColor Yellow
    }
}

function Invoke-AppMenu {
    param([switch]$MissingOnly)

    $catalog = Get-InstallableAppCatalog

    if ($MissingOnly) {
        Write-Host "Checking installed apps before showing recovery choices..."
        $missingApps = @($catalog | Where-Object { -not (Test-AppInstalled -App $_) })
        $selection = Show-AppSelectionGui -InstallApps $missingApps -ReinstallApps @() -MissingOnly
    } else {
        $selection = Show-AppSelectionGui -InstallApps $catalog -ReinstallApps $catalog
    }

    if ($selection.Cancelled) {
        Write-Host "App selection was cancelled." -ForegroundColor Yellow
        return
    }

    Invoke-SelectedAppActions -InstallApps $selection.Install -ReinstallApps $selection.Reinstall
}

function Update-SelectionCounter {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)]$RepairCounter,
        [Parameter(Mandatory = $true)]$UninstallCounter
    )

    $RepairCounter.Text = "$($State.RepairSelections.Count) selected"
    $UninstallCounter.Text = "$($State.UninstallSelections.Count) selected"
}

function New-AppTile {
    param(
        [Parameter(Mandatory = $true)]$App,
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)]$RepairCounter,
        [Parameter(Mandatory = $true)]$UninstallCounter
    )

    $tile = New-Object System.Windows.Forms.CheckBox
    $tile.Appearance = [System.Windows.Forms.Appearance]::Button
    $tile.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $tile.Width = 164
    $tile.Height = 126
    $tile.Margin = New-Object System.Windows.Forms.Padding 6
    $tile.Padding = New-Object System.Windows.Forms.Padding 8
    $tile.Text = $App.Name
    $tile.Font = New-Object System.Drawing.Font "Segoe UI", 9
    $tile.TextAlign = [System.Drawing.ContentAlignment]::BottomCenter
    $tile.ImageAlign = [System.Drawing.ContentAlignment]::TopCenter
    $tile.TextImageRelation = [System.Windows.Forms.TextImageRelation]::ImageAboveText
    $tile.Image = Get-AppIconImage -App $App
    $tile.Tag = [pscustomobject]@{
        App = $App
        Action = $Action
    }
    Set-AppTileStyle -Tile $tile

    $tile.Add_CheckedChanged({
        $tileState = $this.FindForm().Tag
        $tag = $this.Tag
        $key = Get-AppKey -App $tag.App

        if ($tag.Action -eq "Repair") {
            if ($this.Checked) {
                $tileState.RepairSelections[$key] = $tag.App
                if ($tileState.UninstallTiles.ContainsKey($key)) {
                    $tileState.UninstallTiles[$key].Checked = $false
                }
            } else {
                $tileState.RepairSelections.Remove($key)
            }
        } else {
            if ($this.Checked) {
                $tileState.UninstallSelections[$key] = $tag.App
                if ($tileState.RepairTiles.ContainsKey($key)) {
                    $tileState.RepairTiles[$key].Checked = $false
                }
            } else {
                $tileState.UninstallSelections.Remove($key)
            }
        }

        Set-AppTileStyle -Tile $this
        Update-SelectionCounter -State $tileState -RepairCounter $tileState.RepairCounter -UninstallCounter $tileState.UninstallCounter
    })

    $key = Get-AppKey -App $App
    if ($Action -eq "Repair") {
        $State.RepairTiles[$key] = $tile
    } else {
        $State.UninstallTiles[$key] = $tile
    }

    return $tile
}

function Add-RobloxVariantPicker {
    param(
        [Parameter(Mandatory = $true)]$Container,
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $true)]$State
    )

    if (@($RobloxChoices).Count -eq 0) {
        return $null
    }

    $panel = New-Object System.Windows.Forms.Panel
    $panel.Height = 46
    $panel.Width = [Math]::Max(420, $Container.Width)
    $panel.Dock = [System.Windows.Forms.DockStyle]::Top
    $panel.Margin = New-Object System.Windows.Forms.Padding 0, 0, 0, 8
    $panel.Padding = New-Object System.Windows.Forms.Padding 8, 6, 8, 6

    $label = New-Object System.Windows.Forms.Label
    $label.Text = "Roblox version"
    $label.Width = 120
    $label.Dock = [System.Windows.Forms.DockStyle]::Left
    $label.TextAlign = [System.Drawing.ContentAlignment]::MiddleLeft
    $label.Font = New-Object System.Drawing.Font "Segoe UI Semibold", 9

    $combo = New-Object System.Windows.Forms.ComboBox
    $combo.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
    $combo.DisplayMember = "Name"
    $combo.Width = 260
    $combo.Dock = [System.Windows.Forms.DockStyle]::Left
    $combo.Font = New-Object System.Drawing.Font "Segoe UI", 9
    [void]$combo.Items.AddRange([object[]]$RobloxChoices)
    if ($combo.Items.Count -gt 0) {
        $combo.SelectedIndex = 0
    }

    $panel.Controls.Add($combo)
    $panel.Controls.Add($label)
    $Container.Controls.Add($panel)

    if ($Action -eq "Repair") {
        $State.RobloxRepairCombo = $combo
    } else {
        $State.RobloxUninstallCombo = $combo
    }

    return $panel
}

function Add-GuiSection {
    param(
        [Parameter(Mandatory = $true)]$Container,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)]$Counter,
        [Parameter(Mandatory = $true)]$Apps,
        [Parameter(Mandatory = $true)][string]$Action,
        [Parameter(Mandatory = $true)]$State
    )

    $section = New-Object System.Windows.Forms.Panel
    $section.Margin = New-Object System.Windows.Forms.Padding 0, 0, 0, 16
    $section.Padding = New-Object System.Windows.Forms.Padding 0
    $section.Width = [Math]::Max(620, $Container.ClientSize.Width - 8)

    $header = New-Object System.Windows.Forms.Panel
    $header.Height = 34
    $header.Width = $section.Width
    $header.Left = 0
    $header.Top = 0
    $header.Anchor = [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
    $header.Margin = New-Object System.Windows.Forms.Padding 0, 16, 0, 4

    $titleLabel = New-Object System.Windows.Forms.Label
    $titleLabel.Text = $Title
    $titleLabel.Dock = [System.Windows.Forms.DockStyle]::Left
    $titleLabel.Width = 300
    $titleLabel.Font = New-Object System.Drawing.Font "Segoe UI Semibold", 13
    $titleLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleLeft

    $Counter.Dock = [System.Windows.Forms.DockStyle]::Right
    $Counter.Width = 120
    $Counter.Font = New-Object System.Drawing.Font "Segoe UI", 9
    $Counter.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#4A5568")
    $Counter.TextAlign = [System.Drawing.ContentAlignment]::MiddleRight

    $header.Controls.Add($Counter)
    $header.Controls.Add($titleLabel)
    $section.Controls.Add($header)

    $nextTop = $header.Bottom + 8
    $picker = $null
    if ($Action -eq "Repair" -and @($Apps | Where-Object { $_.IsRoblox }).Count -gt 0) {
        $picker = Add-RobloxVariantPicker -Container $section -Action $Action -State $State
        $picker.Dock = [System.Windows.Forms.DockStyle]::None
        $picker.Left = 0
        $picker.Top = $nextTop
        $picker.Width = $section.Width
        $nextTop = $picker.Bottom + 8
    }

    $flow = New-Object System.Windows.Forms.FlowLayoutPanel
    $flow.AutoSize = $false
    $flow.Width = $section.Width
    $flow.Left = 0
    $flow.Top = $nextTop
    $flow.Anchor = [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Right
    $flow.FlowDirection = [System.Windows.Forms.FlowDirection]::LeftToRight
    $flow.WrapContents = $true
    $flow.Margin = New-Object System.Windows.Forms.Padding 0, 0, 0, 10

    if (@($Apps).Count -eq 0) {
        $empty = New-Object System.Windows.Forms.Label
        $empty.AutoSize = $true
        $empty.Margin = New-Object System.Windows.Forms.Padding 8
        $empty.Font = New-Object System.Drawing.Font "Segoe UI", 10
        $empty.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#718096")
        $empty.Text = "No apps are available for this section."
        $flow.Controls.Add($empty)
    } else {
        foreach ($app in @($Apps)) {
            $flow.Controls.Add((New-AppTile -App $app -Action $Action -State $State -RepairCounter $State.RepairCounter -UninstallCounter $State.UninstallCounter))
        }
    }

    $tileOuterWidth = 176
    $tileOuterHeight = 138
    $tileCount = [Math]::Max(1, @($Apps).Count)
    $columns = [Math]::Max(1, [Math]::Floor(($flow.Width - 8) / $tileOuterWidth))
    $rows = [Math]::Max(1, [Math]::Ceiling($tileCount / $columns))
    $flow.Height = ($rows * $tileOuterHeight) + 10

    $section.Controls.Add($flow)
    $section.Height = $flow.Bottom + 18
    $Container.Controls.Add($section)
    return [pscustomobject]@{
        Section = $section
        Flow = $flow
        Header = $header
        Picker = $picker
    }
}

function Get-SelectedGuiApps {
    param(
        [Parameter(Mandatory = $true)]$Apps,
        [Parameter(Mandatory = $true)]$Selections,
        $RobloxCombo,
        [switch]$ExpandRobloxForUninstall
    )

    $selected = @()
    foreach ($app in @($Apps)) {
        if (-not $Selections.ContainsKey((Get-AppKey -App $app))) {
            continue
        }

        if ($app.IsRoblox -and $ExpandRobloxForUninstall) {
            $selected += @($RobloxChoices)
        } elseif ($app.IsRoblox) {
            $robloxChoice = if ($RobloxCombo -and $RobloxCombo.SelectedItem) { $RobloxCombo.SelectedItem } else { $RobloxChoices[0] }
            $selected += $robloxChoice
        } else {
            $selected += $app
        }
    }

    return @($selected)
}

function Show-AppSelectionGui {
    param(
        [Parameter(Mandatory = $true)]$RepairApps,
        [Parameter(Mandatory = $true)]$UninstallApps,
        [switch]$MissingOnly
    )

    try {
        Add-Type -AssemblyName System.Windows.Forms
        Add-Type -AssemblyName System.Drawing
    } catch {
        Write-Host "Could not load the GUI components: $($_.Exception.Message)" -ForegroundColor Yellow
        return [pscustomobject]@{
            Cancelled = $true
            Repair = [object[]]@()
            Uninstall = [object[]]@()
            RepairListed = $false
        }
    }

    [System.Windows.Forms.Application]::EnableVisualStyles()

    $form = New-Object System.Windows.Forms.Form
    $form.Text = "App Repair Tool"
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    $form.Size = New-Object System.Drawing.Size 960, 740
    $form.MinimumSize = New-Object System.Drawing.Size 780, 580
    $form.Font = New-Object System.Drawing.Font "Segoe UI", 10
    $form.BackColor = [System.Drawing.Color]::White

    $state = [pscustomobject]@{
        RepairSelections = @{}
        UninstallSelections = @{}
        RepairTiles = @{}
        UninstallTiles = @{}
        RepairCounter = New-Object System.Windows.Forms.Label
        UninstallCounter = New-Object System.Windows.Forms.Label
        RobloxRepairCombo = $null
        RobloxUninstallCombo = $null
        Result = $null
    }
    $form.Tag = $state

    $header = New-Object System.Windows.Forms.Panel
    $header.Dock = [System.Windows.Forms.DockStyle]::Top
    $header.Height = 76
    $header.Padding = New-Object System.Windows.Forms.Padding 18, 12, 18, 8
    $header.BackColor = [System.Drawing.Color]::White

    $title = New-Object System.Windows.Forms.Label
    $title.Text = "Choose apps to repair or uninstall"
    $title.Dock = [System.Windows.Forms.DockStyle]::Top
    $title.Height = 28
    $title.Font = New-Object System.Drawing.Font "Segoe UI Semibold", 15

    $subtitle = New-Object System.Windows.Forms.Label
    $subtitle.Text = if ($MissingOnly) { "Missing-app recovery shows apps that are not currently detected." } else { "Repair installs missing apps and reinstalls detected apps. Uninstall removes detected traces where possible." }
    $subtitle.Dock = [System.Windows.Forms.DockStyle]::Top
    $subtitle.Height = 28
    $subtitle.Font = New-Object System.Drawing.Font "Segoe UI", 9
    $subtitle.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#4A5568")

    $header.Controls.Add($subtitle)
    $header.Controls.Add($title)

    $footer = New-Object System.Windows.Forms.Panel
    $footer.Dock = [System.Windows.Forms.DockStyle]::Bottom
    $footer.Height = 64
    $footer.Padding = New-Object System.Windows.Forms.Padding 18, 10, 18, 10
    $footer.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#F7FAFC")

    $repairListed = New-Object System.Windows.Forms.Button
    $repairListed.Text = "R"
    $repairListed.Width = 44
    $repairListed.Height = 44
    $repairListed.Anchor = [System.Windows.Forms.AnchorStyles]::Left
    $repairListed.Left = 18
    $repairListed.Top = 10
    $repairListed.Font = New-Object System.Drawing.Font "Segoe UI Semibold", 13
    $repairListed.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $repairListed.FlatAppearance.BorderColor = [System.Drawing.ColorTranslator]::FromHtml("#CBD5E1")
    $repairListed.BackColor = [System.Drawing.Color]::White
    $repairListed.ForeColor = [System.Drawing.ColorTranslator]::FromHtml("#1F2937")

    $repairListedTip = New-Object System.Windows.Forms.ToolTip
    $repairListedTip.SetToolTip($repairListed, "Repair every app currently listed and queue game launcher verification where supported.")

    $done = New-Object System.Windows.Forms.Button
    $done.Text = "Run selected"
    $done.Width = 140
    $done.Height = 36
    $done.Anchor = [System.Windows.Forms.AnchorStyles]::Right
    $done.Left = 800
    $done.Top = 14
    $done.BackColor = [System.Drawing.ColorTranslator]::FromHtml("#2563EB")
    $done.ForeColor = [System.Drawing.Color]::White
    $done.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $done.FlatAppearance.BorderSize = 0

    $cancel = New-Object System.Windows.Forms.Button
    $cancel.Text = "Cancel"
    $cancel.Width = 110
    $cancel.Height = 36
    $cancel.Anchor = [System.Windows.Forms.AnchorStyles]::Right
    $cancel.Left = 680
    $cancel.Top = 14
    $cancel.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
    $cancel.BackColor = [System.Drawing.Color]::White

    $footer.Controls.Add($repairListed)
    $footer.Controls.Add($done)
    $footer.Controls.Add($cancel)

    $scroll = New-Object System.Windows.Forms.Panel
    $scroll.Dock = [System.Windows.Forms.DockStyle]::Fill
    $scroll.AutoScroll = $true
    $scroll.BackColor = [System.Drawing.Color]::White
    $scroll.Padding = New-Object System.Windows.Forms.Padding 18, 4, 18, 18

    $container = New-Object System.Windows.Forms.FlowLayoutPanel
    $container.Dock = [System.Windows.Forms.DockStyle]::Top
    $container.AutoSize = $true
    $container.AutoSizeMode = [System.Windows.Forms.AutoSizeMode]::GrowAndShrink
    $container.FlowDirection = [System.Windows.Forms.FlowDirection]::TopDown
    $container.WrapContents = $false
    $container.BackColor = [System.Drawing.Color]::White

    $repairSection = Add-GuiSection -Container $container -Title "Install or reinstall" -Counter $state.RepairCounter -Apps $RepairApps -Action "Repair" -State $state
    $uninstallSection = Add-GuiSection -Container $container -Title "Uninstall" -Counter $state.UninstallCounter -Apps $UninstallApps -Action "Uninstall" -State $state
    $scroll.Controls.Add($container)
    $form.Controls.Add($scroll)
    $form.Controls.Add($header)
    $form.Controls.Add($footer)

    $resizeFlows = {
        $newWidth = [Math]::Max(620, $scroll.ClientSize.Width - 50)
        $container.Width = $newWidth
        foreach ($sectionInfo in @($repairSection, $uninstallSection)) {
            if ($sectionInfo -and $sectionInfo.Section) {
                $sectionInfo.Section.Width = $newWidth
                foreach ($child in $sectionInfo.Section.Controls) {
                    if ($child -is [System.Windows.Forms.Control]) {
                        $child.Width = $newWidth
                    }
                }
            }
            if ($sectionInfo -and $sectionInfo.Header) {
                $sectionInfo.Header.Width = $newWidth
            }
            if ($sectionInfo -and $sectionInfo.Picker) {
                $sectionInfo.Picker.Width = $newWidth
            }
            if ($sectionInfo -and $sectionInfo.Flow) {
                $sectionInfo.Flow.Width = $newWidth
                $tileOuterWidth = 176
                $tileOuterHeight = 138
                $tileCount = [Math]::Max(1, $sectionInfo.Flow.Controls.Count)
                $columns = [Math]::Max(1, [Math]::Floor(($sectionInfo.Flow.Width - 8) / $tileOuterWidth))
                $rows = [Math]::Max(1, [Math]::Ceiling($tileCount / $columns))
                $sectionInfo.Flow.Height = ($rows * $tileOuterHeight) + 10
                if ($sectionInfo.Section) {
                    $sectionInfo.Section.Height = $sectionInfo.Flow.Bottom + 18
                }
            }
        }
        $cancel.Left = $footer.ClientSize.Width - 260
        $done.Left = $footer.ClientSize.Width - 150
    }
    $form.Add_Shown($resizeFlows)
    $form.Add_Resize($resizeFlows)

    Update-SelectionCounter -State $state -RepairCounter $state.RepairCounter -UninstallCounter $state.UninstallCounter

    $repairListed.Add_Click({
        $confirm = [System.Windows.Forms.MessageBox]::Show(
            "Repair every app currently listed and queue launcher game verification for Steam, Epic, Xbox, EA, Roblox, and detected Roblox bootstraps where possible?",
            "Repair listed apps",
            [System.Windows.Forms.MessageBoxButtons]::YesNo,
            [System.Windows.Forms.MessageBoxIcon]::Question
        )

        if ($confirm -ne [System.Windows.Forms.DialogResult]::Yes) {
            return
        }

        $tileState = $this.FindForm().Tag
        $tileState.Result = [pscustomobject]@{
            Cancelled = $false
            Repair = [object[]]@()
            Uninstall = [object[]]@()
            RepairListed = $true
        }
        $this.FindForm().DialogResult = [System.Windows.Forms.DialogResult]::OK
        $this.FindForm().Close()
    })

    $done.Add_Click({
        $tileState = $this.FindForm().Tag
        $repairSelection = @(Get-SelectedGuiApps -Apps $RepairApps -Selections $tileState.RepairSelections -RobloxCombo $tileState.RobloxRepairCombo)
        $uninstallSelection = @(Get-SelectedGuiApps -Apps $UninstallApps -Selections $tileState.UninstallSelections -RobloxCombo $tileState.RobloxUninstallCombo -ExpandRobloxForUninstall)
        $tileState.Result = [pscustomobject]@{
            Cancelled = $false
            Repair = [object[]]$repairSelection
            Uninstall = [object[]]$uninstallSelection
            RepairListed = $false
        }
        $this.FindForm().DialogResult = [System.Windows.Forms.DialogResult]::OK
        $this.FindForm().Close()
    })

    $cancel.Add_Click({
        $this.FindForm().DialogResult = [System.Windows.Forms.DialogResult]::Cancel
        $this.FindForm().Close()
    })

    [void]$form.ShowDialog()

    if ($state.Result) {
        return $state.Result
    }

    return [pscustomobject]@{
        Cancelled = $true
        Repair = [object[]]@()
        Uninstall = [object[]]@()
        RepairListed = $false
    }
}

function Get-AppUninstallRegistryEntries {
    param([Parameter(Mandatory = $true)]$App)

    $registryPaths = @(
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )

    $patterns = @()
    if ($App.DisplayNamePatterns) {
        $patterns += @($App.DisplayNamePatterns)
    }
    if ($App.Name) {
        $patterns += [string]$App.Name
    }
    if ($App.Id) {
        $patterns += [string]$App.Id
    }

    $entries = @()
    foreach ($entry in (Get-ItemProperty -Path $registryPaths -ErrorAction SilentlyContinue)) {
        foreach ($pattern in $patterns) {
            if ($entry.DisplayName -and $entry.DisplayName -like "*$pattern*") {
                $entries += $entry
                break
            }
        }
    }

    return @($entries | Sort-Object DisplayName -Unique)
}

function Invoke-RegistryUninstall {
    param([Parameter(Mandatory = $true)]$Entry)

    $uninstallString = if (-not [string]::IsNullOrWhiteSpace($Entry.QuietUninstallString)) {
        [string]$Entry.QuietUninstallString
    } else {
        [string]$Entry.UninstallString
    }

    if ([string]::IsNullOrWhiteSpace($uninstallString)) {
        return $false
    }

    Write-Host "Running uninstaller: $($Entry.DisplayName)"
    try {
        $process = Start-Process -FilePath "cmd.exe" -ArgumentList @("/c", $uninstallString) -Wait -PassThru
        return ($null -eq $process.ExitCode -or $process.ExitCode -eq 0)
    } catch {
        Write-Host "Registry uninstaller could not be started: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

function Uninstall-App {
    param(
        [Parameter(Mandatory = $true)]$App,
        [switch]$SkipHeader
    )

    if ($App.IsRoblox -and $App.Name -eq "Roblox") {
        if (-not $SkipHeader) {
            Write-Host ""
            Write-Host "===== Roblox =====" -ForegroundColor Cyan
        }

        $allSucceeded = $true
        foreach ($robloxChoice in @($RobloxChoices)) {
            if (-not (Uninstall-App -App $robloxChoice -SkipHeader)) {
                $allSucceeded = $false
            }
        }
        return $allSucceeded
    }

    if (-not $SkipHeader) {
        Write-Host ""
        Write-Host "===== $($App.Name) =====" -ForegroundColor Cyan
    }

    if (-not (Test-AppInstalled -App $App)) {
        Write-Host "$($App.Name) is not currently detected. No uninstall needed." -ForegroundColor Yellow
        return $true
    }

    Write-Host "Closing related processes for $($App.Name)..."
    Stop-AppProcesses -App $App

    foreach ($packageName in @($App.AppxPackageNames)) {
        if ([string]::IsNullOrWhiteSpace($packageName)) {
            continue
        }

        $packages = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
        foreach ($package in $packages) {
            Write-Host "Removing app package: $($package.PackageFullName)"
            Remove-AppxPackage -Package $package.PackageFullName -ErrorAction SilentlyContinue
        }
    }

    if (-not (Test-AppInstalled -App $App)) {
        return $true
    }

    if ($App.Id) {
        Write-Host "Uninstalling $($App.Name) with winget..."
        $source = Get-WingetSource -App $App
        $arguments = @(
            "uninstall", "--id", $App.Id, "-e",
            "--source", $source,
            "--silent",
            "--all-versions",
            "--accept-source-agreements",
            "--disable-interactivity",
            "--force"
        )
        $uninstall = Invoke-Winget -Arguments $arguments
        if ($uninstall.ExitCode -eq 0 -and (Wait-ForRemoved -App $App)) {
            return $true
        }
    }

    foreach ($entry in (Get-AppUninstallRegistryEntries -App $App)) {
        [void](Invoke-RegistryUninstall -Entry $entry)
        if (Wait-ForRemoved -App $App) {
            return $true
        }
    }

    if (-not (Test-AppInstalled -App $App)) {
        return $true
    }

    Write-Host "$($App.Name) may still be installed." -ForegroundColor Red
    return $false
}

function Reinstall-App {
    param([Parameter(Mandatory = $true)]$App)

    Write-Host ""
    Write-Host "===== $($App.Name) =====" -ForegroundColor Cyan

    if (-not (Test-AppInstalled -App $App)) {
        Write-Host "$($App.Name) is not detected. Installing it." -ForegroundColor Yellow
        Install-AppIfMissing -App $App -ForceDependencies
        return
    }

    if (-not (Test-AppReadyToInstall -App $App -ForceDependencies)) {
        return
    }

    Write-Host "$($App.Name) detected. It will be uninstalled, then installed again."
    $uninstalled = Uninstall-App -App $App -SkipHeader
    if (-not $uninstalled) {
        Write-Host "$($App.Name) uninstall was not clean. Trying install/repair anyway..." -ForegroundColor Yellow
    }

    $installed = Install-App -App $App

    if ($installed -or (Test-AppInstalled -App $App)) {
        if (Invoke-AppPostInstallTasks -App $App -Force) {
            Write-Host "$($App.Name) install/reinstall finished." -ForegroundColor Green
        } else {
            Write-Host "$($App.Name) install/reinstall finished, but supporting setup may have failed." -ForegroundColor Red
        }
    } else {
        Write-Host "$($App.Name) install/reinstall may have failed." -ForegroundColor Red
    }
}

function Install-OrReinstallApp {
    param([Parameter(Mandatory = $true)]$App)

    Reinstall-App -App $App
}

function Invoke-WingetRepairApp {
    param([Parameter(Mandatory = $true)]$App)

    if ([string]::IsNullOrWhiteSpace($App.Id)) {
        return $false
    }

    $source = Get-WingetSource -App $App
    $repairArguments = @(
        "repair", "--id", $App.Id, "-e",
        "--source", $source,
        "--silent",
        "--accept-source-agreements",
        "--accept-package-agreements",
        "--disable-interactivity"
    )

    $repair = Invoke-Winget -Arguments $repairArguments
    return ($repair.ExitCode -eq 0)
}

function Repair-AppFootprint {
    param([Parameter(Mandatory = $true)]$App)

    Write-Host ""
    Write-Host "===== Repair $($App.Name) =====" -ForegroundColor Cyan
    Write-Host "Using installer/launcher repair paths only; deleted files are not manually restored." -ForegroundColor DarkGray

    if (-not (Test-AppInstalled -App $App)) {
        Write-Host "$($App.Name) is not detected. Installing it instead." -ForegroundColor Yellow
        Install-AppIfMissing -App $App -ForceDependencies
        return
    }

    if (-not (Ensure-AppDependencies -App $App -Force)) {
        Write-Host "$($App.Name) dependencies may still need attention." -ForegroundColor Yellow
    }

    Stop-AppProcesses -App $App

    $repaired = $false
    if ($App.InstallMethod -eq "Winget" -and $App.Id) {
        $repaired = Invoke-WingetRepairApp -App $App
        if (-not $repaired) {
            Write-Host "Winget repair was not available or did not finish cleanly for $($App.Name). Trying installer repair fallback..." -ForegroundColor Yellow
        }
    }

    if (-not $repaired) {
        $repaired = Install-App -App $App
    }

    $postInstallReady = Invoke-AppPostInstallTasks -App $App -Force
    if ($repaired -or (Test-AppInstalled -App $App)) {
        if ($postInstallReady) {
            Write-Host "$($App.Name) repair finished." -ForegroundColor Green
        } else {
            Write-Host "$($App.Name) repair finished, but supporting setup may have failed." -ForegroundColor Yellow
        }
    } else {
        Write-Host "$($App.Name) repair may have failed." -ForegroundColor Red
    }
}

function Get-UniqueApps {
    param($Apps)

    $result = @()
    $seen = @{}
    foreach ($app in @($Apps | Where-Object { $null -ne $_ })) {
        $key = Get-AppKey -App $app
        if ($seen.ContainsKey($key)) {
            continue
        }

        $seen[$key] = $true
        $result += $app
    }

    return @($result)
}

function Expand-ListedRepairApps {
    param($Apps)

    $listed = @($Apps | Where-Object { $null -ne $_ })
    $expanded = @($listed)

    if (@($listed | Where-Object { $_.IsRoblox -or $_.Name -like "Roblox*" }).Count -gt 0) {
        $installedRobloxChoices = @($RobloxChoices | Where-Object { Test-AppInstalled -App $_ })
        if ($installedRobloxChoices.Count -gt 0) {
            $expanded += $installedRobloxChoices
        } else {
            $expanded += $RobloxChoices[0]
        }
    }

    return Get-UniqueApps -Apps $expanded
}

function Test-RepairListHasApp {
    param(
        [Parameter(Mandatory = $true)]$Apps,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    foreach ($app in @($Apps)) {
        foreach ($name in $Names) {
            if ($app.Name -eq $name) {
                return $true
            }
        }
    }

    return $false
}

function Get-SteamInstallPath {
    $candidates = @()

    try {
        $steamKey = Get-ItemProperty -Path "HKCU:\Software\Valve\Steam" -ErrorAction SilentlyContinue
        if ($steamKey.SteamPath) { $candidates += [string]$steamKey.SteamPath }
    } catch {}

    try {
        $steamKey = Get-ItemProperty -Path "HKLM:\Software\WOW6432Node\Valve\Steam" -ErrorAction SilentlyContinue
        if ($steamKey.InstallPath) { $candidates += [string]$steamKey.InstallPath }
    } catch {}

    if ($env:ProgramFiles) { $candidates += (Join-Path $env:ProgramFiles "Steam") }
    if (${env:ProgramFiles(x86)}) { $candidates += (Join-Path ${env:ProgramFiles(x86)} "Steam") }

    foreach ($candidate in @($candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
        $normalized = $candidate -replace "/", "\"
        if (Test-Path -LiteralPath $normalized) {
            return $normalized
        }
    }

    return $null
}

function Get-SteamLibrarySteamAppsPaths {
    param([Parameter(Mandatory = $true)][string]$SteamPath)

    $paths = @()
    $paths += (Join-Path $SteamPath "steamapps")
    $libraryFile = Join-Path $SteamPath "steamapps\libraryfolders.vdf"

    if (Test-Path -LiteralPath $libraryFile) {
        foreach ($line in (Get-Content -LiteralPath $libraryFile -ErrorAction SilentlyContinue)) {
            if ($line -match '^\s*"\d+"\s+"([^"]+)"') {
                $paths += (Join-Path (($matches[1] -replace "\\\\", "\") -replace "/", "\") "steamapps")
            } elseif ($line -match '^\s*"path"\s+"([^"]+)"') {
                $paths += (Join-Path (($matches[1] -replace "\\\\", "\") -replace "/", "\") "steamapps")
            }
        }
    }

    return @($paths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -Unique)
}

function Get-SteamInstalledGames {
    $steamPath = Get-SteamInstallPath
    if (-not $steamPath) {
        return @()
    }

    $games = @()
    foreach ($steamApps in (Get-SteamLibrarySteamAppsPaths -SteamPath $steamPath)) {
        foreach ($manifest in (Get-ChildItem -LiteralPath $steamApps -Filter "appmanifest_*.acf" -File -ErrorAction SilentlyContinue)) {
            $text = Get-Content -LiteralPath $manifest.FullName -Raw -ErrorAction SilentlyContinue
            if ([string]::IsNullOrWhiteSpace($text)) {
                continue
            }

            $appId = $null
            $name = $manifest.BaseName
            if ($text -match '"appid"\s+"([^"]+)"') { $appId = $matches[1] }
            if ($text -match '"name"\s+"([^"]+)"') { $name = $matches[1] }

            if (-not [string]::IsNullOrWhiteSpace($appId)) {
                $games += [pscustomobject]@{
                    Name = $name
                    AppId = $appId
                    Manifest = $manifest.FullName
                }
            }
        }
    }

    return @($games | Sort-Object AppId -Unique)
}

function Repair-SteamInstalledGames {
    $games = @(Get-SteamInstalledGames)
    if ($games.Count -eq 0) {
        Write-Host "No Steam game manifests were found to verify." -ForegroundColor Yellow
        return
    }

    Write-Host ""
    Write-Host "===== Steam game verification =====" -ForegroundColor Cyan
    Write-Host "Queueing Steam validation for $($games.Count) installed game(s)."

    foreach ($game in $games) {
        Write-Host "Steam verify: $($game.Name) ($($game.AppId))"
        try {
            Start-Process -FilePath ("steam://validate/{0}" -f $game.AppId) | Out-Null
            Start-Sleep -Milliseconds 500
        } catch {
            Write-Host "Could not queue Steam validation for $($game.Name): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

function Get-EpicInstalledGames {
    $manifestRoot = Join-Path $env:ProgramData "Epic\EpicGamesLauncher\Data\Manifests"
    if (-not (Test-Path -LiteralPath $manifestRoot)) {
        return @()
    }

    $games = @()
    foreach ($manifest in (Get-ChildItem -LiteralPath $manifestRoot -Filter "*.item" -File -ErrorAction SilentlyContinue)) {
        try {
            $data = Get-Content -LiteralPath $manifest.FullName -Raw -ErrorAction Stop | ConvertFrom-Json
            $appName = if ($data.AppName) { [string]$data.AppName } elseif ($data.MainGameAppName) { [string]$data.MainGameAppName } else { "" }
            if ([string]::IsNullOrWhiteSpace($appName)) {
                continue
            }

            $displayName = if ($data.DisplayName) { [string]$data.DisplayName } else { $appName }
            $games += [pscustomobject]@{
                Name = $displayName
                AppName = $appName
                InstallLocation = [string]$data.InstallLocation
            }
        } catch {}
    }

    return @($games | Sort-Object AppName -Unique)
}

function Repair-EpicInstalledGames {
    $games = @(Get-EpicInstalledGames)
    if ($games.Count -eq 0) {
        Write-Host "No Epic game manifests were found to verify." -ForegroundColor Yellow
        return
    }

    Write-Host ""
    Write-Host "===== Epic game verification =====" -ForegroundColor Cyan
    Write-Host "Opening Epic verify actions for $($games.Count) installed game(s) where the launcher protocol supports it."

    foreach ($game in $games) {
        Write-Host "Epic verify: $($game.Name)"
        try {
            $encodedApp = [uri]::EscapeDataString($game.AppName)
            Start-Process -FilePath ("com.epicgames.launcher://apps/{0}?action=verify" -f $encodedApp) | Out-Null
            Start-Sleep -Milliseconds 500
        } catch {
            Write-Host "Could not queue Epic verification for $($game.Name): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

function Repair-AppxPackagesByName {
    param([Parameter(Mandatory = $true)][string[]]$PackageNames)

    foreach ($packageName in $PackageNames) {
        if ([string]::IsNullOrWhiteSpace($packageName)) {
            continue
        }

        foreach ($package in (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue)) {
            $manifest = Join-Path $package.InstallLocation "AppxManifest.xml"
            if (-not (Test-Path -LiteralPath $manifest)) {
                continue
            }

            Write-Host "Re-registering app package: $($package.Name)"
            try {
                Add-AppxPackage -DisableDevelopmentMode -Register $manifest -ErrorAction Stop
            } catch {
                Write-Host "Package repair failed for $($package.Name): $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }
}

function Repair-XboxGamingInstall {
    Write-Host ""
    Write-Host "===== Xbox PC / Gaming Services repair =====" -ForegroundColor Cyan

    Repair-AppxPackagesByName -PackageNames @(
        "Microsoft.GamingApp",
        "Microsoft.GamingServices",
        "Microsoft.Xbox.TCUI",
        "Microsoft.XboxGamingOverlay",
        "Microsoft.XboxIdentityProvider"
    )

    $xboxGameRoots = @()
    foreach ($drive in (Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue)) {
        $candidate = Join-Path ($drive.Root) "XboxGames"
        if (Test-Path -LiteralPath $candidate) {
            $xboxGameRoots += $candidate
        }
    }

    if ($xboxGameRoots.Count -gt 0) {
        Write-Host "Detected Xbox game libraries:"
        $xboxGameRoots | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    }

    try {
        Start-Process -FilePath "msxbox://gamepass" | Out-Null
        Write-Host "Opened Xbox app so installed game repairs can continue from the launcher if needed."
    } catch {
        Write-Host "Could not open Xbox app: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Repair-EAInstalledGames {
    Write-Host ""
    Write-Host "===== EA app game repair =====" -ForegroundColor Cyan

    $eaRoots = @()
    if ($env:ProgramFiles) {
        $eaRoots += (Join-Path $env:ProgramFiles "EA Games")
        $eaRoots += (Join-Path $env:ProgramFiles "Electronic Arts")
    }
    if (${env:ProgramFiles(x86)}) {
        $eaRoots += (Join-Path ${env:ProgramFiles(x86)} "Origin Games")
    }

    $detectedRoots = @($eaRoots | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -Unique)
    if ($detectedRoots.Count -gt 0) {
        Write-Host "Detected EA game libraries:"
        $detectedRoots | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
    } else {
        Write-Host "No EA game library folders were found in the common locations." -ForegroundColor Yellow
    }

    $eaDesktopPaths = @()
    if ($env:ProgramFiles) {
        $eaDesktopPaths += (Join-Path $env:ProgramFiles "Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe")
    }
    if (${env:ProgramFiles(x86)}) {
        $eaDesktopPaths += (Join-Path ${env:ProgramFiles(x86)} "Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe")
    }

    $eaDesktop = @($eaDesktopPaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
    if ($eaDesktop.Count -gt 0) {
        try {
            Start-Process -FilePath $eaDesktop[0] | Out-Null
            Write-Host "Opened EA app so installed game repairs can continue from the launcher if needed."
        } catch {
            Write-Host "Could not open EA app: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

function Invoke-ListedRepairSweep {
    param([Parameter(Mandatory = $true)]$Apps)

    $repairApps = @(Expand-ListedRepairApps -Apps $Apps)
    if ($repairApps.Count -eq 0) {
        Write-Host "There are no apps in the current list to repair." -ForegroundColor Yellow
        return
    }

    Write-Host ""
    Write-Host "Repairing every app in the current list and checking launcher game libraries..." -ForegroundColor Cyan
    Write-Host "This uses official installers, winget repair/install fallback, AppX re-registration, and launcher verification protocols." -ForegroundColor DarkGray

    foreach ($app in $repairApps) {
        Repair-AppFootprint -App $app
    }

    if (Test-RepairListHasApp -Apps $repairApps -Names @("Steam")) {
        Repair-SteamInstalledGames
    }

    if (Test-RepairListHasApp -Apps $repairApps -Names @("Epic Games Launcher")) {
        Repair-EpicInstalledGames
    }

    if (Test-RepairListHasApp -Apps $repairApps -Names @("Xbox PC")) {
        Repair-XboxGamingInstall
    }

    if (Test-RepairListHasApp -Apps $repairApps -Names @("EA app")) {
        Repair-EAInstalledGames
    }

    Write-Host ""
    Write-Host "Listed app repair pass finished." -ForegroundColor Green
}

function Invoke-SelectedAppActions {
    param(
        $RepairApps,
        $UninstallApps
    )

    $repairList = @($RepairApps | Where-Object { $null -ne $_ })
    $uninstallList = @($UninstallApps | Where-Object { $null -ne $_ })
    $repairCount = $repairList.Count
    $uninstallCount = $uninstallList.Count

    if ($repairCount -eq 0 -and $uninstallCount -eq 0) {
        Write-Host "No apps were selected." -ForegroundColor Yellow
        return
    }

    Write-Host ""
    Write-Host "Selected install/reinstall actions: $repairCount" -ForegroundColor Cyan
    Write-Host "Selected uninstall actions: $uninstallCount" -ForegroundColor Cyan

    foreach ($app in $repairList) {
        Install-OrReinstallApp -App $app
    }

    foreach ($app in $uninstallList) {
        Uninstall-App -App $app
    }
}

function Invoke-AppPrompt {
    param([Parameter(Mandatory = $true)]$App)

    Write-Host ""
    Write-Host "===== $($App.Name) =====" -ForegroundColor Cyan

    if (Test-AppInstalled -App $App) {
        Write-Host "Status: installed" -ForegroundColor Green
    } else {
        Write-Host "Status: not installed" -ForegroundColor Yellow
    }

    while ($true) {
        $answer = Read-Host "Choose [r] install/reinstall, [u] uninstall, or [s] skip"
        switch ($answer.ToLowerInvariant()) {
            "r" {
                Install-OrReinstallApp -App $App
                return
            }
            "u" {
                Uninstall-App -App $App
                return
            }
            "s" {
                Write-Host "Skipped $($App.Name)." -ForegroundColor Yellow
                return
            }
            default {
                Write-Host "Please type r, u, or s." -ForegroundColor Yellow
            }
        }
    }
}

function Invoke-AppMenu {
    param([switch]$MissingOnly)

    $catalog = Get-InstallableAppCatalog

    if ($MissingOnly) {
        Write-Host "Checking installed apps before showing recovery choices..."
        $repairApps = @($catalog | Where-Object { $_.IsRoblox -or -not (Test-AppInstalled -App $_) })
        $uninstallApps = @()
    } else {
        $repairApps = @($catalog)
        $uninstallApps = @($catalog)
    }

    $selection = Show-AppSelectionGui -RepairApps $repairApps -UninstallApps $uninstallApps -MissingOnly:$MissingOnly

    if ($selection.Cancelled) {
        Write-Host "App selection was cancelled." -ForegroundColor Yellow
        return
    }

    if ($selection.PSObject.Properties.Name -contains "RepairListed" -and $selection.RepairListed) {
        $currentListedApps = @(Get-UniqueApps -Apps (@($repairApps) + @($uninstallApps)))
        Invoke-ListedRepairSweep -Apps $currentListedApps
        return
    }

    $selectedRepair = @($selection.Repair | Where-Object { $null -ne $_ })
    $selectedUninstall = @($selection.Uninstall | Where-Object { $null -ne $_ })
    Invoke-SelectedAppActions -RepairApps $selectedRepair -UninstallApps $selectedUninstall
}

Clear-Host
Write-Host "App Repair Tool v2" -ForegroundColor Cyan
Write-Host "Select apps in the GUI, then click Run selected to install, reinstall, or uninstall."
Write-Host "Install/reinstall checks installer sources and dependencies before uninstalling anything."
Write-Host "The script does not manually delete user data/cache folders."
Write-Host ""

if (Test-IsAdmin) {
    Write-Host "This is running as Administrator." -ForegroundColor Yellow
    Write-Host "Spotify's installer can fail from an administrator context, so this tool should usually be run normally."
    if (-not $AllowAdmin) {
        Write-Host "Close this window, then double-click Run-AppRepairTool.cmd without choosing Run as administrator." -ForegroundColor Yellow
        Stop-Logging
        Pause-ForUser
        exit 1
    }
}

if (-not (Test-Winget)) {
    Write-Host "winget is not installed or not available in PATH." -ForegroundColor Red
    Write-Host "Install/update App Installer from Microsoft Store, then run this again."
    Stop-Logging
    Pause-ForUser
    exit 1
}

Invoke-AppMenu

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Stop-Logging
Pause-ForUser

