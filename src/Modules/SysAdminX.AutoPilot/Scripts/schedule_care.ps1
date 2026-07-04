param (
    [string]$Day,
    [string]$Time
)
$taskName = "SolasSystemCarePro_WeeklyCare"
$executable = (Get-Process -Id $PID).Path
$action = New-ScheduledTaskAction -Execute $executable -Argument "--run-care"
$trigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek $Day -At $Time
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Force
