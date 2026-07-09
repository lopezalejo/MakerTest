param([switch]$SinDemo)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path -Parent $PSScriptRoot)

try {
    Write-Host "==> Levantando cluster..."
    docker compose up --build -d

    Write-Host "==> Esperando nodos saludables..."
    $limite = (Get-Date).AddMinutes(5)
    while ((Get-Date) -lt $limite) {
        $ok = (docker ps --filter "name=payment-node" --filter "health=healthy" -q | Measure-Object).Count
        if ($ok -ge 2) { break }
        Start-Sleep 5
    }

    if ($SinDemo) { Write-Host "Listo: nodos 8081-8083, SQL 1433." }
    else { & "$PSScriptRoot\demo-failover.ps1" }
}
finally { Pop-Location }
