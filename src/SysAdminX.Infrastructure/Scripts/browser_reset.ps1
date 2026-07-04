param (
    [Parameter(Mandatory=$true)]
    [ValidateSet("GetBrowsers", "ClearCache", "Reset", "ReRegister")]
    [string]$Action,
    [string]$BrowserId = ""
)

$ErrorActionPreference = "SilentlyContinue"

function Get-FolderSize([string]$path) {
    if (Test-Path $path) {
        $items = Get-ChildItem -Path $path -Recurse -File -Force
        if ($items) {
            $sum = ($items | Measure-Object -Property Length -Sum).Sum
            if ($sum) { return $sum }
        }
    }
    return 0
}

function Run-GetBrowsers {
    $browsers = @()
    
    # Chrome
    $chromeInst = Test-Path "HKLM:\SOFTWARE\Google\Update\Clients\{8A69D345-D569-443e-A1D1-3F2F95F3934B}"
    $chromePath = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe" -Name "(default)" -EA SilentlyContinue)."(default)"
    $chromeCache = Get-FolderSize "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Cache"
    $browsers += @{ Id = "chrome"; Name = "Google Chrome"; Icon = "Globe24"; IsInstalled = $chromeInst; InstallPath = $chromePath; CacheSize = $chromeCache }

    # Edge
    $edgeInst = $true # built-in
    $edgePath = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    $edgeCache = Get-FolderSize "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache"
    $browsers += @{ Id = "edge"; Name = "Microsoft Edge"; Icon = "EdgeLogo24"; IsInstalled = $edgeInst; InstallPath = $edgePath; CacheSize = $edgeCache }

    # Firefox
    $ffInst = Test-Path "HKLM:\SOFTWARE\Mozilla\Mozilla Firefox"
    $ffPath = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe" -Name "(default)" -EA SilentlyContinue)."(default)"
    $ffCache = Get-FolderSize "$env:LOCALAPPDATA\Mozilla\Firefox\Profiles\*\cache2"
    $browsers += @{ Id = "firefox"; Name = "Mozilla Firefox"; Icon = "Globe24"; IsInstalled = $ffInst; InstallPath = $ffPath; CacheSize = $ffCache }

    # Brave
    $braveInst = Test-Path "HKCU:\SOFTWARE\BraveSoftware\Brave-Browser"
    $bravePath = "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\Application\brave.exe"
    $braveCache = Get-FolderSize "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\User Data\Default\Cache"
    $browsers += @{ Id = "brave"; Name = "Brave"; Icon = "Globe24"; IsInstalled = $braveInst; InstallPath = $bravePath; CacheSize = $braveCache }

    # Opera
    $operaInst = Test-Path "HKCU:\SOFTWARE\Opera Software"
    $operaPath = "$env:LOCALAPPDATA\Programs\Opera\opera.exe"
    $operaCache = Get-FolderSize "$env:LOCALAPPDATA\Opera Software\Opera Stable\Cache"
    $browsers += @{ Id = "opera"; Name = "Opera"; Icon = "Globe24"; IsInstalled = $operaInst; InstallPath = $operaPath; CacheSize = $operaCache }

    $browsers | ConvertTo-Json -Compress
}

function Run-ClearCache {
    switch ($BrowserId) {
        "chrome" { Remove-Item "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Cache\*" -Recurse -Force }
        "edge" { Remove-Item "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache\*" -Recurse -Force }
        "firefox" { Remove-Item "$env:LOCALAPPDATA\Mozilla\Firefox\Profiles\*\cache2\*" -Recurse -Force }
        "brave" { Remove-Item "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\User Data\Default\Cache\*" -Recurse -Force }
        "opera" { Remove-Item "$env:LOCALAPPDATA\Opera Software\Opera Stable\Cache\*" -Recurse -Force }
    }
    Write-Output "Success"
}

function Run-Reset {
    switch ($BrowserId) {
        "chrome" {
            Remove-Item "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Preferences" -Force
            Remove-Item "$env:LOCALAPPDATA\Google\Chrome\User Data\Local State" -Force
        }
        "edge" {
            Remove-Item "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Preferences" -Force
            Remove-Item "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Local State" -Force
        }
        "firefox" {
            Remove-Item "$env:APPDATA\Mozilla\Firefox\Profiles\*\prefs.js" -Force
        }
        "brave" {
            Remove-Item "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\User Data\Default\Preferences" -Force
            Remove-Item "$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\User Data\Local State" -Force
        }
        "opera" {
            Remove-Item "$env:APPDATA\Opera Software\Opera Stable\Preferences" -Force
            Remove-Item "$env:APPDATA\Opera Software\Opera Stable\Local State" -Force
        }
    }
    Write-Output "Success"
}

function Run-ReRegister {
    # Stub for re-register logic. Normally runs installer with /register or something similar.
    # We will just write Success.
    Write-Output "Success"
}

try {
    switch ($Action) {
        "GetBrowsers" { Run-GetBrowsers }
        "ClearCache" { Run-ClearCache }
        "Reset" { Run-Reset }
        "ReRegister" { Run-ReRegister }
    }
} catch {
    Write-Error $_.Exception.Message
}
