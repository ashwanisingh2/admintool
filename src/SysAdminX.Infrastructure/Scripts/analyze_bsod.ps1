$ErrorActionPreference = "Stop"

$results = @()
$dumpDir = "C:\Windows\Minidump"

# Try getting minidumps
$dumps = @()
if (Test-Path $dumpDir) {
    $dumps = Get-ChildItem -Path $dumpDir -Filter "*.dmp" -ErrorAction SilentlyContinue
}

# If no dumps found or we just want to rely on EventLog as fallback
# Query WER events
try {
    $werEvents = Get-WinEvent -LogName "System" -FilterHashtable @{ProviderName="Microsoft-Windows-WER-SystemErrorReporting"} -MaxEvents 50 -ErrorAction SilentlyContinue
    foreach ($event in $werEvents) {
        $msg = $event.Message
        $code = "0x00000000"
        if ($msg -match "bugcheck was: (0x[0-9a-fA-F]+)") {
            $code = $matches[1]
        }
        
        $dumpFile = ""
        if ($msg -match "Report was saved to: (.*\.dmp)") {
            $dumpFile = $matches[1]
        } elseif ($msg -match "Dump file: (.*\.dmp)") {
            $dumpFile = $matches[1]
        }
        
        $results += [PSCustomObject]@{
            DumpFile = $dumpFile
            BugCheckCode = $code
            BugCheckName = ""
            LikelyCause = "Unknown (extracted from EventLog)"
            Timestamp = $event.TimeCreated.ToString("o")
            StackTrace = "Stack trace not available in Event Log fallback."
        }
    }
} catch {
    # Ignore errors reading event log
}

# Add any dumps that were not in the event log
foreach ($dump in $dumps) {
    $found = $false
    foreach ($res in $results) {
        if ($res.DumpFile -and $res.DumpFile.EndsWith($dump.Name)) {
            $found = $true
            break
        }
    }
    
    if (-not $found) {
        $results += [PSCustomObject]@{
            DumpFile = $dump.FullName
            BugCheckCode = "0x00000000"
            BugCheckName = ""
            LikelyCause = "Unknown"
            Timestamp = $dump.CreationTime.ToString("o")
            StackTrace = "Unparsed minidump file."
        }
    }
}

$results | ConvertTo-Json -Depth 2 -Compress
