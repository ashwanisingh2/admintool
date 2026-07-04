param(
    [string]$Drive = "C:\",
    [int]$MinSizeMB = 100
)

$minSizeBytes = [long]$MinSizeMB * 1024 * 1024

$files = Get-ChildItem -Path $Drive -Recurse -File -ErrorAction SilentlyContinue | 
    Where-Object { $_.Length -gt $minSizeBytes } | 
    Sort-Object Length -Descending | 
    Select-Object -First 100

$results = @()
foreach ($f in $files) {
    $results += @{
        FilePath = $f.FullName
        SizeInBytes = $f.Length
        LastModified = $f.LastWriteTime.ToString("o")
        Extension = $f.Extension
    }
}

$results | ConvertTo-Json -Depth 2 -Compress
