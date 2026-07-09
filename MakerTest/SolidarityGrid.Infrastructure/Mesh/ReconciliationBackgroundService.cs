using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolidarityGrid.Application;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Application.Interfaces.Repository;
using SolidarityGrid.Application.Options;
using SolidarityGrid.Infrastructure.Options;

namespace SolidarityGrid.Infrastructure.Mesh
{
    public sealed class ReconciliationBackgroundService : BackgroundService
    {
        private readonly MeshOptions _meshOptions;
        private readonly PaymentProcessingOptions _processingOptions;
        private readonly IPeerRegistry _peerRegistry;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReconciliationBackgroundService> _logger;

        public ReconciliationBackgroundService(
            IOptions<MeshOptions> meshOptions,
            IOptions<PaymentProcessingOptions> processingOptions,
            IPeerRegistry peerRegistry,
            IServiceScopeFactory scopeFactory,
            ILogger<ReconciliationBackgroundService> logger)
        {
            _meshOptions = meshOptions.Value;
            _processingOptions = processingOptions.Value;
            _peerRegistry = peerRegistry;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var peerUrl in _meshOptions.Peers)
                {
                    var peerId = ExtractPeerId(peerUrl);
                    if (!_peerRegistry.IsConsideredDead(peerId, _meshOptions.PeerTimeoutMs))
                        continue;

                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                    var processor = scope.ServiceProvider.GetRequiredService<IPaymentProcessingService>();

                    var orphaned = await repository.GetInFlightByOwnerAsync(peerId, stoppingToken);
                    foreach (var payment in orphaned)
                    {
                        var reclaim = await repository.ReclaimFromDeadPeerAsync(
                            payment.TransactionId,
                            peerId,
                            _meshOptions.NodeId,
                            _processingOptions.LeaseSeconds,
                            stoppingToken);

                        if (reclaim is null)
                            continue;

                        NodeLog.Warn(
                            _logger,
                            _meshOptions.NodeId,
                            $"Detecté que {peerId} dejó de responder. Asumiendo transacción {payment.TransactionId}.");

                        processor.Enqueue(payment.TransactionId, reclaimed: true);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private static string ExtractPeerId(string peerUrl) =>
            Uri.TryCreate(peerUrl, UriKind.Absolute, out var uri) ? uri.Host : peerUrl;
    }
}
