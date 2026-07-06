param(
    [string]$Action,
    [string]$BackupDir,
    [string]$BackupFile
)

if ($Action -eq 'Backup') {
    if (-not (Test-Path -Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    }
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $hklmFile = Join-Path -Path $BackupDir -ChildPath "HKLM_$timestamp.reg"
    $hkcuFile = Join-Path -Path $BackupDir -ChildPath "HKCU_$timestamp.reg"
    
    reg export HKLM $hklmFile /y *>&1 | Out-Null
    reg export HKCU $hkcuFile /y *>&1 | Out-Null
    
    if ((Test-Path -Path $hklmFile) -and (Test-Path -Path $hkcuFile)) {
        $hklmSize = (Get-Item $hklmFile).Length
        $hkcuSize = (Get-Item $hkcuFile).Length
        Write-Output "SUCCESS|$timestamp|$hklmFile|$hklmSize|$hkcuFile|$hkcuSize"
    } else {
        Write-Error "Backup failed."
    }
}
elseif ($Action -eq 'Restore') {
    if (Test-Path -Path $BackupFile) {
        reg import $BackupFile *>&1 | Out-Null
        if ($?) {
            Write-Output "SUCCESS"
        } else {
            Write-Error "Restore failed."
        }
    } else {
        Write-Error "Backup file not found: $BackupFile"
    }
}
