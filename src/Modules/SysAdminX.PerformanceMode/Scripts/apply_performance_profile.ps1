param(
    [Parameter(Mandatory=$true)]
    [string]$ProfileId
)

$Balanced = "381b4222-f694-41f0-9685-ff5bb260df2e"
$PowerSaver = "a1841308-3541-4fab-bc81-f71556f20b4a"
$Ultimate = "e9a42b02-d5df-448d-aa00-03f14749eb61"

if ($ProfileId -eq 'gaming') {
    $planOutput = powercfg /aliases
    if ($planOutput -notmatch $Ultimate) {
        powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 | Out-Null
    }
}

switch ($ProfileId) {
    'gaming' {
        powercfg /setactive $Ultimate
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\GameBar" -Name "AllowAutoGameMode" -Value 1 -ErrorAction SilentlyContinue
        Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" -Name "HwSchMode" -Value 2 -ErrorAction SilentlyContinue
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" -Name "GlobalUserDisabled" -Value 1 -ErrorAction SilentlyContinue
    }
    'work' {
        powercfg /setactive $Balanced
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\GameBar" -Name "AllowAutoGameMode" -Value 0 -ErrorAction SilentlyContinue
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" -Name "GlobalUserDisabled" -Value 0 -ErrorAction SilentlyContinue
    }
    'powersaver' {
        powercfg /setactive $PowerSaver
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\GameBar" -Name "AllowAutoGameMode" -Value 0 -ErrorAction SilentlyContinue
        Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" -Name "GlobalUserDisabled" -Value 1 -ErrorAction SilentlyContinue
    }
}
