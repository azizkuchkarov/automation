param(
    [switch]$Detached,
    [switch]$Down,
    [switch]$Logs
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if ($Down) {
    docker compose down
    exit $LASTEXITCODE
}

if ($Logs) {
    docker compose logs -f
    exit $LASTEXITCODE
}

$args = @("compose", "up", "--build")
if ($Detached) { $args += "-d" }

docker @args
exit $LASTEXITCODE
