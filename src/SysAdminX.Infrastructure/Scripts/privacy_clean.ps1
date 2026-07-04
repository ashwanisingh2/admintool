param (
    [Parameter(Mandatory=$true)]
    [string]$CategoryId
)

$ErrorActionPreference = "SilentlyContinue"

switch ($CategoryId) {
    "browserHistory" {
        Remove-Item "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\History" -Force
        Remove-Item "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\History" -Force
        Remove-Item "$env:APPDATA\Mozilla\Firefox\Profiles\*\places.sqlite" -Force
    }
    "cookies" {
        Remove-Item "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Network\Cookies" -Force
        Remove-Item "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Network\Cookies" -Force
    }
    "dns" {
        Clear-DnsClientCache
        ipconfig /flushdns | Out-Null
    }
    "thumbnail" {
        Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\thumbcache_*.db" -Force
    }
    "recentDocs" {
        Remove-Item "$env:APPDATA\Microsoft\Windows\Recent\*" -Force
    }
    "clipboard" {
        Clear-Clipboard
    }
    "telemetry" {
        # E.g. Clear-DiagnosticData (needs admin)
        Clear-DiagnosticData
    }
    "recycleBin" {
        Clear-RecycleBin -Force
    }
}

Write-Output "Success"
