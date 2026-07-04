param (
    [Parameter(Mandatory=$true)]
    [ValidateSet("Schedule", "CheckResult")]
    [string]$Action
)

if ($Action -eq "Schedule") {
    Start-Process "MdSched.exe"
    Write-Output "Launched"
}
elseif ($Action -eq "CheckResult") {
    $events = Get-WinEvent -LogName "System" -MaxEvents 200 -ErrorAction SilentlyContinue | Where-Object { $_.ProviderName -eq "Microsoft-Windows-MemoryDiagnostics-Results" }
    $event = $events | Select-Object -First 1
    
    if ($null -ne $event) {
        $msg = $event.Message
        $time = $event.TimeCreated.ToString("yyyy-MM-dd HH:mm:ss")
        if ($msg -match "no errors") {
            Write-Output "Pass - $time - $msg"
        }
        else {
            Write-Output "Fail - $time - $msg"
        }
    } else {
        Write-Output "No results found"
    }
}
