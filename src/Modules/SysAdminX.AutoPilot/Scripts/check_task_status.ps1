$taskName = "SolasSystemCarePro_WeeklyCare"
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($task) {
    $info = Get-ScheduledTaskInfo -TaskName $taskName
    $result = @{
        IsEnabled = $task.State -eq 'Ready' -or $task.State -eq 'Running'
        LastRunTime = $info.LastRunTime
        LastTaskResult = $info.LastTaskResult
        NextRunTime = $info.NextRunTime
    }
    $result | ConvertTo-Json
} else {
    @{ IsEnabled = $false } | ConvertTo-Json
}
