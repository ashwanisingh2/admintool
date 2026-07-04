$apps = @()

$hklm = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run"
if (Test-Path $hklm) {
    Get-ItemProperty $hklm | Get-Member -MemberType NoteProperty | ForEach-Object {
        $isEnabled = $true
        $approvedPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"
        if (Test-Path $approvedPath) {
            $val = Get-ItemPropertyValue -Path $approvedPath -Name $_.Name -ErrorAction SilentlyContinue
            if ($null -ne $val -and $val.Length -ge 1 -and ($val[0] -eq 0x02 -or $val[0] -eq 0x03)) {
                $isEnabled = $false
            }
        }
        $apps += [PSCustomObject]@{
            Name = $_.Name
            Command = (Get-ItemPropertyValue -Path $hklm -Name $_.Name)
            Source = "HKLM Registry"
            IsEnabled = $isEnabled
        }
    }
}

$hkcu = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
if (Test-Path $hkcu) {
    Get-ItemProperty $hkcu | Get-Member -MemberType NoteProperty | ForEach-Object {
        $isEnabled = $true
        $approvedPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"
        if (Test-Path $approvedPath) {
            $val = Get-ItemPropertyValue -Path $approvedPath -Name $_.Name -ErrorAction SilentlyContinue
            if ($null -ne $val -and $val.Length -ge 1 -and ($val[0] -eq 0x02 -or $val[0] -eq 0x03)) {
                $isEnabled = $false
            }
        }
        $apps += [PSCustomObject]@{
            Name = $_.Name
            Command = (Get-ItemPropertyValue -Path $hkcu -Name $_.Name)
            Source = "HKCU Registry"
            IsEnabled = $isEnabled
        }
    }
}

$startupAll = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup"
$startupUser = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"

foreach ($folder in @($startupAll, $startupUser)) {
    if (Test-Path $folder) {
        $sourceName = if ($folder -eq $startupAll) { "Startup Folder (All Users)" } else { "Startup Folder (Current User)" }
        Get-ChildItem -Path $folder -File | ForEach-Object {
            $isEnabled = $true
            $approvedPath = if ($folder -eq $startupAll) { "HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder" } else { "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder" }
            if (Test-Path $approvedPath) {
                $val = Get-ItemPropertyValue -Path $approvedPath -Name $_.Name -ErrorAction SilentlyContinue
                if ($null -ne $val -and $val.Length -ge 1 -and ($val[0] -eq 0x02 -or $val[0] -eq 0x03)) {
                    $isEnabled = $false
                }
            }
            $apps += [PSCustomObject]@{
                Name = $_.Name
                Command = $_.FullName
                Source = $sourceName
                IsEnabled = $isEnabled
            }
        }
    }
}

Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object {
    $_.Triggers | Where-Object { $_.CimClass.CimClassName -eq 'MSFT_TaskLogonTrigger' }
} | ForEach-Object {
    $isEnabled = $_.State -ne 'Disabled'
    $cmd = $_.Actions | Select-Object -ExpandProperty Execute -First 1 -ErrorAction SilentlyContinue
    if (-not $cmd) { $cmd = "Unknown" }
    $apps += [PSCustomObject]@{
        Name = $_.TaskName
        Command = $cmd
        Source = "Task Scheduler"
        IsEnabled = $isEnabled
    }
}

$apps | ConvertTo-Json -Depth 2
