$activePlan = powercfg /getactivescheme

if ($activePlan -match "e9a42b02-d5df-448d-aa00-03f14749eb61") {
    Write-Output "gaming"
} elseif ($activePlan -match "381b4222-f694-41f0-9685-ff5bb260df2e") {
    Write-Output "work"
} elseif ($activePlan -match "a1841308-3541-4fab-bc81-f71556f20b4a") {
    Write-Output "powersaver"
} else {
    Write-Output "unknown"
}
