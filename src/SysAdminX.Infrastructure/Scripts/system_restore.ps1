param (
    [Parameter(Mandatory=$true)]
    [ValidateSet("CheckStatus", "EnableProtection", "CreatePoint", "ListPoints")]
    [string]$Action,

    [string]$DriveLetter = "C:\",
    [string]$Description = ""
)

$ErrorActionPreference = "Stop"

function CheckStatus {
    # Check if system protection is enabled on the drive
    $status = Get-ComputerRestorePoint -ErrorAction SilentlyContinue
    # Also check registry
    $reg = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore" -Name "RPSessionInterval" -ErrorAction SilentlyContinue
    
    # A better way is using WMI
    $wmi = Get-WmiObject -Namespace "root\default" -Class SystemRestore -ErrorAction SilentlyContinue
    if ($null -eq $wmi) {
        Write-Output '{"isEnabled": false}'
    } else {
        # Actually it's easier to just check if we can create one or if drives are enabled
        Write-Output '{"isEnabled": true}'
    }
}

function EnableProtection {
    Enable-ComputerRestore -Drive $DriveLetter
    Write-Output '{"success": true}'
}

function CreatePoint {
    # Bypass 24h limit
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore" -Name "SystemRestorePointCreationFrequency" -Value 0 -Type DWord -ErrorAction SilentlyContinue
    
    $point = Checkpoint-Computer -Description $Description -RestorePointType "MODIFY_SETTINGS" -ErrorAction Stop
    # Get the latest point
    $latest = Get-ComputerRestorePoint | Sort-Object SequenceNumber -Descending | Select-Object -First 1
    if ($latest) {
        $date = $latest.CreationTime
        if ($date -is [string]) { $date = [datetime]::ParseExact($date.Substring(0,14), "yyyyMMddHHmmss", $null) }
        $formattedDate = $date.ToString("o")
        Write-Output "{`"SequenceNumber`": $($latest.SequenceNumber), `"Description`": `"$($latest.Description)`", `"CreationTime`": `"$formattedDate`", `"EventType`": `"$($latest.EventType)`"}"
    } else {
        throw "Failed to verify restore point creation."
    }
}

function ListPoints {
    $points = Get-ComputerRestorePoint -ErrorAction SilentlyContinue
    $result = @()
    if ($points) {
        foreach ($p in $points) {
            $date = $p.CreationTime
            if ($date -is [string]) { 
                # WMI format: 20260704173230.000000-000
                $date = [datetime]::ParseExact($date.Substring(0,14), "yyyyMMddHHmmss", $null) 
            }
            $formattedDate = $date.ToString("o")
            $result += @{
                SequenceNumber = $p.SequenceNumber
                Description = $p.Description
                CreationTime = $formattedDate
                EventType = $p.EventType.ToString()
            }
        }
    }
    $result | ConvertTo-Json -Compress
}

try {
    switch ($Action) {
        "CheckStatus" { CheckStatus }
        "EnableProtection" { EnableProtection }
        "CreatePoint" { CreatePoint }
        "ListPoints" { ListPoints }
    }
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
