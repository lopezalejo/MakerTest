param(
    [int]$SegundosAntesDeMatar = 3,
    [int]$TiempoMaxEspera = 60
)

$ErrorActionPreference = "Stop"
$apiKey = "makertest-api-key"
$txId = "TX-DEMO-$(Get-Date -Format 'yyyyMMddHHmmss')"
$hdr = @{ "X-Api-Key" = $apiKey; "Idempotency-Key" = $txId }

Write-Host "==> Demo failover: pago en nodo-a (8081)..."
$r = Invoke-RestMethod -Method Post -Uri "http://localhost:8081/pay" -Headers $hdr `
    -Body (@{ amount = 100.50 } | ConvertTo-Json) -ContentType "application/json"
Write-Host "Aceptado: $($r.transactionId) ($($r.status))"

Write-Host "==> Esperando $SegundosAntesDeMatar s y matando nodo-a..."
Start-Sleep $SegundosAntesDeMatar
docker stop payment-node-a | Out-Null

Write-Host "==> Consultando nodo-b..."
$limite = (Get-Date).AddSeconds($TiempoMaxEspera)
$estado = $null

while ((Get-Date) -lt $limite) {
    try {
        $estado = Invoke-RestMethod "http://localhost:8082/pay/$txId" -Headers @{ "X-Api-Key" = $apiKey }
        Write-Host "  $($estado.status) | dueño: $($estado.ownerNodeId) | completó: $($estado.completedByNodeId)"
        if ($estado.status -eq "Completed") {
            Write-Host "OK: $txId completado por $($estado.completedByNodeId)."
            exit 0
        }
    } catch { Write-Host "  Error: $($_.Exception.Message)" }
    Start-Sleep 2
}

throw "FALLO: $txId no se completó en $TiempoMaxEspera s."
