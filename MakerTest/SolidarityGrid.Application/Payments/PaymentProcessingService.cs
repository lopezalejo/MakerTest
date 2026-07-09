using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Application.Interfaces.Repository;
using SolidarityGrid.Application.Options;

namespace SolidarityGrid.Application.Payments
{
    public sealed class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly PaymentProcessingOptions _options;
        private readonly ILogger<PaymentProcessingService> _logger;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random _random = new();

        public PaymentProcessingService(
            IServiceScopeFactory scopeFactory, 
            IOptions<PaymentProcessingOptions> options,
            ILogger<PaymentProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        public void Enqueue(string transactionId, bool reclaimed) =>
        _ = ProcessAsync(transactionId, reclaimed, CancellationToken.None);

        public async Task ProcessAsync(string transactionId, bool reclaimed, CancellationToken cancellationToken)
        {
            if (!_inFlight.TryAdd(transactionId, 0))
            {
                // Ya existiría una transacción en curso por lo que se omitiría
                return;
            }

            try
            {
                ClaimResult? claim;
                using (var claimScope = _scopeFactory.CreateScope())
                {
                    var repository = claimScope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                    claim = await repository.MarkClaimAsync(
                        transactionId,
                        _options.NodeId,
                        _options.LeaseSeconds,
                        cancellationToken);
                }

                if (claim is null)
                    return;

                if (reclaimed)
                    NodeLog.Warn(_logger, _options.NodeId, $"Asignando transacción {transactionId} tras caída de un compañero.");

                using (var processingScope = _scopeFactory.CreateScope())
                {
                    var repository = processingScope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                    await repository.MarkProcessingAsync(
                        transactionId,
                        _options.NodeId,
                        claim.FencingToken,
                        _options.LeaseSeconds,
                        cancellationToken);
                }

                NodeLog.Info(_logger, _options.NodeId, $"Procesando transacción {transactionId}...");

                var processingSeconds = _random.Next(_options.MinProcessingSeconds, _options.MaxProcessingSeconds + 1);
                var elapsed = 0;

                while (elapsed < processingSeconds)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var step = Math.Min(2, processingSeconds - elapsed);
                    await Task.Delay(TimeSpan.FromSeconds(step), cancellationToken);
                    elapsed += step;

                    using var renewScope = _scopeFactory.CreateScope();
                    var renewRepository = renewScope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                    await renewRepository.RenewLeaseAsync(
                        transactionId,
                        _options.NodeId,
                        claim.FencingToken,
                        _options.LeaseSeconds,
                        cancellationToken);
                }

                var message = $"Pago procesado en {_options.NodeId} ({processingSeconds}s simulados).";
                using (var completeScope = _scopeFactory.CreateScope())
                {
                    var repository = completeScope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                    if (await repository.MarkCompleteAsync(transactionId, _options.NodeId, claim.FencingToken, message, cancellationToken))
                        NodeLog.Info(_logger, _options.NodeId, $"Transacción {transactionId} completada con éxito.");
                }
            }
            finally
            {
                _inFlight.TryRemove(transactionId, out _);
            }
        }
    }
}
