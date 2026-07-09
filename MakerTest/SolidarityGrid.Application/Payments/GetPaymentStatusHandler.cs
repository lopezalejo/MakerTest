using SolidarityGrid.Application.Contracts;
using SolidarityGrid.Application.Interfaces.Repository;

namespace SolidarityGrid.Application.Payments
{
    public sealed class GetPaymentStatusHandler
    {
        private readonly IPaymentRepository _repository;

        public GetPaymentStatusHandler(IPaymentRepository repository) => _repository = repository;

        public async Task<PaymentStatusResponse?> HandleAsync(string transactionId, CancellationToken cancellationToken)
        {
            var payment = await _repository.GetAsync(transactionId, cancellationToken);
            if (payment is null)
                return null;

            return new PaymentStatusResponse(
                payment.TransactionId,
                payment.PaymentAmount ?? 0,
                payment.Status.ToString(),
                payment.OwnerNodeId,
                payment.LeaseUntil,
                payment.FencingToken,
                payment.CreatedAt ?? DateTime.MinValue,
                payment.CompletedAt ?? DateTime.MinValue,
                payment.CompletedByNodeId,
                payment.ResultMessage);
        }
    }
}
