param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("disable", "enable", "rollback", "restore")]
    [string]$Action,

    [Parameter(Mandatory=$false)]
    [string]$HardwareId,

    [Parameter(Mandatory=$false)]
    [bool]$SafeMode = $false,

    [Parameter(Mandatory=$false)]
    [string]$BackupFilePath
)

$ErrorActionPreference = "Stop"

if ($Action -eq "disable") {
    if (-not $HardwareId) { throw "HardwareId required for disable" }
    
    # Backup registry
    $safeHwid = $HardwareId -replace '[\\/]', '_'
    $tempBackupPath = "$env:TEMP\solas_driver_backup_$safeHwid.reg"
    
    try {
        reg export "HKLM\SYSTEM\CurrentControlSet\Enum\$HardwareId" $tempBackupPath /y | Out-Null
        $backupSuccess = $true
    }
    catch {
        $backupSuccess = $false
    }
    
    if (-not $backupSuccess) {
        if ($SafeMode) {
            Write-Host "[SAFE_MODE_ABORT]"
            exit 1
        }
    } else {
        Write-Host "BACKUP_PATH:$tempBackupPath"
    }
    
    Disable-PnpDevice -InstanceId $HardwareId -Confirm:$false
}
elseif ($Action -eq "enable") {
    if (-not $HardwareId) { throw "HardwareId required for enable" }
    Enable-PnpDevice -InstanceId $HardwareId -Confirm:$false
}
elseif ($Action -eq "rollback") {
    if (-not $HardwareId) { throw "HardwareId required for rollback" }
    
    $infPath = (Get-PnpDeviceProperty -InstanceId $HardwareId -KeyName "DEVPKEY_Device_DriverInfPath").Data
    if ($infPath) {
        pnputil /delete-driver $infPath /uninstall
    } else {
        throw "Could not find INF path for $HardwareId"
    }
}
elseif ($Action -eq "restore") {
    if (-not $BackupFilePath -or -not (Test-Path $BackupFilePath)) {
        throw "Invalid BackupFilePath"
    }
    reg import $BackupFilePath
}
