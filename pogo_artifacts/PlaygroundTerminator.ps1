#The script launches PCM counters and stores probe results in CSV file.
#ver 1.0.1

Write-Host -NoNewLine 'Waiting for AUT '
$passedSec = 0;
$proc = $null
$processName = 'Playground.Net46'

do {
    Start-Sleep -Seconds 90
    $proc = Get-Process $processName -ErrorAction SilentlyContinue
    $passedSec ++;
    
    if($passedSec -eq 10) {
        Write-Host ' terminating!'
        Write-Host 'Was not able to find MediaStreamer.UI'
        exit
    }

}while (-Not $proc)

Write-Host ' found!'
Start-Sleep -Seconds 10

Stop-Process -processname $processName

