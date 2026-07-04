$taskName = "SolasSystemCarePro_WeeklyCare"
Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
