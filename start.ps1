# Starts the api, the web client and the simulator for a local run, in that order.
# From the repo root: powershell -ExecutionPolicy Bypass -File .\start.ps1
# -NoSimulator leaves the stream empty, -NoBrowser skips opening the page.
# Stop everything again with .\stop.ps1.

param(
    [switch]$NoSimulator,
    [switch]$NoBrowser
)

$root = $PSScriptRoot
Set-Location $root

# Api on 5011 and 7002, web client on 5113 and 7267.
$ports = 5011, 7002, 5113, 7267
$logs = Join-Path $env:TEMP 'smartx-logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null

# Waits for a url to answer 200, and stops everything again if it never does.
function Wait-ForUrl($url, $name, $log)
{
    for ($i = 0; $i -lt 60; $i++)
    {
        $code = curl.exe -sk -o NUL -w '%{http_code}' $url

        if ($code -eq '200')
        {
            return
        }

        Start-Sleep -Seconds 1
    }

    Write-Host "$name did not answer at $url within a minute. The end of its log:"
    Get-Content $log -Tail 15 -ErrorAction SilentlyContinue
    & (Join-Path $root 'stop.ps1')
    exit 1
}

# The pin in global.json wants a .NET 10 SDK, and dotnet says so itself when there is none.
$sdk = (dotnet --version 2>&1) -join ' '

if ($LASTEXITCODE -ne 0 -or -not $sdk.StartsWith('10.'))
{
    Write-Host "This needs a .NET 10 SDK and dotnet said: $sdk"
    exit 1
}

# Something left over from an earlier run would hold a port and the build would fail on locked files.
$held = Get-NetTCPConnection -LocalPort $ports -State Listen -ErrorAction SilentlyContinue |
    Sort-Object LocalPort, OwningProcess -Unique

if ($held)
{
    # A port shows twice when it listens on both ipv4 and ipv6, so each is only named once.
    foreach ($connection in $held)
    {
        $owner = Get-Process -Id $connection.OwningProcess -ErrorAction SilentlyContinue
        Write-Host "Port $($connection.LocalPort) is already in use by $($owner.ProcessName) (process $($connection.OwningProcess))."
    }

    Write-Host 'Run .\stop.ps1 first if that is a copy of this app.'
    exit 1
}

# The api and the web client talk over https, and the simulator does too.
dotnet dev-certs https --check --trust 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0)
{
    Write-Host 'The https development certificate is missing or not trusted.'
    Write-Host 'Windows is about to ask whether to trust it, say yes.'
    dotnet dev-certs https --trust

    dotnet dev-certs https --check --trust 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host 'The certificate is still not trusted, so the browser would refuse the api. Run dotnet dev-certs https --trust and try again.'
        exit 1
    }
}

Write-Host 'Building...'
dotnet build SmartX.slnx --nologo -v q -clp:ErrorsOnly

if ($LASTEXITCODE -ne 0)
{
    Write-Host 'The build failed, so nothing was started.'
    exit 1
}

# Hidden windows, so the output goes to a log instead.
$apiLog = Join-Path $logs 'api.log'
Start-Process dotnet -ArgumentList 'run', '--project', 'SmartX.Api', '--no-build', '--launch-profile', 'https' `
    -WorkingDirectory $root -WindowStyle Hidden `
    -RedirectStandardOutput $apiLog -RedirectStandardError (Join-Path $logs 'api.err') | Out-Null

Write-Host 'Waiting for the api...'
Wait-ForUrl 'https://localhost:7002/api/stream/snapshot' 'The api' $apiLog

$webLog = Join-Path $logs 'web.log'
Start-Process dotnet -ArgumentList 'run', '--project', 'SmartX.Web', '--no-build', '--launch-profile', 'http' `
    -WorkingDirectory $root -WindowStyle Hidden `
    -RedirectStandardOutput $webLog -RedirectStandardError (Join-Path $logs 'web.err') | Out-Null

Write-Host 'Waiting for the web client...'
Wait-ForUrl 'http://localhost:5113/' 'The web client' $webLog

# The simulator gets a window of its own, Ctrl-C in it stops it cleanly.
if (-not $NoSimulator)
{
    Start-Process dotnet -ArgumentList 'run', '--project', 'SmartX.Simulator', '--no-build' -WorkingDirectory $root
    Write-Host 'The simulator has its own window. Backfill takes a few seconds before the live readings start.'
}

Write-Host ''
Write-Host 'Web client: http://localhost:5113/commandstream'
Write-Host 'Api:        https://localhost:7002/'
Write-Host "Logs:       $logs"
Write-Host 'Stop it all with .\stop.ps1'

if (-not $NoBrowser)
{
    Start-Process 'http://localhost:5113/commandstream'
}
