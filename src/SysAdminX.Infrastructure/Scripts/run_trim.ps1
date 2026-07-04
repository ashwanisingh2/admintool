param(
    [Parameter(Mandatory=$true)]
    [string]$DriveLetter
)

try {
    $trimStatus = fsutil behavior query DisableDeleteNotify
    if ($trimStatus -match "1") {
        Write-Output "TRIM is disabled at OS level. Enabling it now..."
        fsutil behavior set DisableDeleteNotify 0
        Write-Output "TRIM enabled."
    }

    $osVersion = [System.Environment]::OSVersion.Version
    if ($osVersion.Major -ge 10) {
        # Optimize-Volume expects just the letter, e.g., C
        $letter = $DriveLetter.Substring(0, 1)
        Write-Output "Running Optimize-Volume on Windows 10+..."
        Optimize-Volume -DriveLetter $letter -ReTrim -Verbose
    } else {
        Write-Output "Running defrag on older Windows version..."
        defrag $DriveLetter /L /V
    }

    Write-Output "TRIM optimization completed successfully."
} catch {
    Write-Error "Failed to run TRIM: $_"
    exit 1
}
