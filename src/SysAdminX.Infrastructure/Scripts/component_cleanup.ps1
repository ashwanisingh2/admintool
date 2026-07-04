param(
    [ValidateSet('Analyze', 'Cleanup')]
    [string]$Action = 'Analyze'
)

if ($Action -eq 'Analyze') {
    DISM /Online /Cleanup-Image /AnalyzeComponentStore
} elseif ($Action -eq 'Cleanup') {
    DISM /Online /Cleanup-Image /StartComponentCleanup
}
