param(
    [Parameter(Mandatory=$true)]
    [string]$Name,

    [Parameter(Mandatory=$true)]
    [string]$Source,

    [Parameter(Mandatory=$true)]
    [bool]$Enable
)

$approvedPath = $null

if ($Source -eq "HKLM Registry") {
    $approvedPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"
} elseif ($Source -eq "HKCU Registry") {
    $approvedPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"
} elseif ($Source -eq "Startup Folder (All Users)") {
    $approvedPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"
} elseif ($Source -eq "Startup Folder (Current User)") {
    $approvedPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"
} elseif ($Source -eq "Task Scheduler") {
    if ($Enable) {
        Enable-ScheduledTask -TaskName $Name -ErrorAction SilentlyContinue | Out-Null
    } else {
        Disable-ScheduledTask -TaskName $Name -ErrorAction SilentlyContinue | Out-Null
    }
    return
}

if ($approvedPath) {
    if (-not (Test-Path $approvedPath)) {
        New-Item -Path $approvedPath -Force | Out-Null
    }
    
    # 02 00 00 00 00 00 00 00 00 00 00 00
    if ($Enable) {
        Set-ItemProperty -Path $approvedPath -Name $Name -Value ([byte[]](0x06,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force | Out-Null
    } else {
        Set-ItemProperty -Path $approvedPath -Name $Name -Value ([byte[]](0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -Type Binary -Force | Out-Null
    }
}
