param (
    [Parameter(Mandatory=$true)]
    [string]$CategoryId
)

$ErrorActionPreference = "SilentlyContinue"
$size = 0

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

switch ($CategoryId) {
    "browserHistory" {
        $size += Get-FolderSize "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\History"
        $size += Get-FolderSize "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\History"
        $size += Get-FolderSize "$env:APPDATA\Mozilla\Firefox\Profiles\*\places.sqlite"
    }
    "cookies" {
        $size += Get-FolderSize "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Network\Cookies"
        $size += Get-FolderSize "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Network\Cookies"
    }
    "dns" {
        # DNS cache size is negligible for files, but we can return a dummy value
        $size = 1024 * 512 # 512 KB
    }
    "thumbnail" {
        $size += Get-FolderSize "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db"
    }
    "recentDocs" {
        $size += Get-FolderSize "$env:APPDATA\Microsoft\Windows\Recent"
    }
    "clipboard" {
        $size = 1024 * 1024 # 1 MB dummy
    }
    "telemetry" {
        $size = 1024 * 1024 * 5 # 5 MB dummy
    }
    "recycleBin" {
        $rb = New-Object -ComObject Shell.Application
        $folder = $rb.Namespace(0xA)
        foreach ($item in $folder.Items()) {
            $size += $item.Size
        }
    }
}

Write-Output $size
