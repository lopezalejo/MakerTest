using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolidarityGrid.Application.Contracts;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Application.Interfaces.Repository;
using SolidarityGrid.Application.Options;
using SolidarityGrid.Domain.Entity.Payments;

namespace SolidarityGrid.Application.Payments
{
    public sealed class AcceptPaymentHandler
    {
        private readonly IPaymentRepository _repository;
        private readonly IPaymentProcessingService _processor;
        private readonly PaymentProcessingOptions _options;
        private readonly ILogger<AcceptPaymentHandler> _logger;

        public AcceptPaymentHandler(
            IPaymentRepository repository,
            IPaymentProcessingService processor,
            IOptions<PaymentProcessingOptions> options,
            ILogger<AcceptPaymentHandler> logger)
        {
            _repository = repository;
            _processor = processor;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PayResponse> HandleAsync(string transactionId, decimal amount, CancellationToken cancellationToken)
        {
            var payment = await _repository.CreateOrGetExist(transactionId, amount, cancellationToken);

            if (payment?.Status == PaymentStatus.PaymentStatusEnum.Completado)
            {
                NodeLog.Info(_logger, _options.NodeId, $"Transacción {transactionId} ya completada. Idempotencia aplicada.");
                return new PayResponse(transactionId, payment.Status.ToString(), "Transacción ya completada.", payment.CompletedByNodeId);
            }

            if (payment?.Status is PaymentStatus.PaymentStatusEnum.Asigado or PaymentStatus.PaymentStatusEnum.Procesando
                && payment.OwnerNodeId != _options.NodeId)
            {
                NodeLog.Info(_logger, _options.NodeId, $"Transacción {transactionId} en curso por {payment.OwnerNodeId}.");
                return new PayResponse(transactionId, payment.Status.ToString(), "Transacción aceptada para procesamiento.");
            }

            _processor.Enqueue(transactionId, reclaimed: false);
            return new PayResponse(transactionId, PaymentStatus.PaymentStatusEnum.Asigado.ToString(), "Transacción aceptada para procesamiento.");
        }
    }
}
