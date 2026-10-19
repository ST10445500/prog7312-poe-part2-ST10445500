# Stops whatever start.ps1 started. Safe to run when nothing is running.
# From the repo root: powershell -ExecutionPolicy Bypass -File .\stop.ps1

$ports = 5011, 7002, 5113, 7267

# By port, so only the api and the web client are hit and nothing else called dotnet.
$held = Get-NetTCPConnection -LocalPort $ports -State Listen -ErrorAction SilentlyContinue

foreach ($connection in $held)
{
    Stop-Process -Id $connection.OwningProcess -Force -ErrorAction SilentlyContinue
}

# The simulator listens on nothing, so it is found by its name and by the project it was started from.
Get-Process SmartX.Simulator -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
    Where-Object { $_.CommandLine -like '*SmartX.Simulator*' } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

# A build server left running between two builds is what once gave missing assembly errors.
dotnet build-server shutdown 2>&1 | Out-Null

Start-Sleep -Seconds 2

$left = Get-NetTCPConnection -LocalPort $ports -State Listen -ErrorAction SilentlyContinue

if ($left)
{
    Write-Host "Still listening on port $((($left | ForEach-Object { $_.LocalPort }) -join ', ')), stop that by hand."
    exit 1
}

Write-Host 'Stopped. Ports 5011, 7002, 5113 and 7267 are free.'
