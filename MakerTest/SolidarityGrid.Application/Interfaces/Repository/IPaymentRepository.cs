using SolidarityGrid.Domain.Entity.Payments;

namespace SolidarityGrid.Application.Interfaces.Repository
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetAsync(string transactionId, CancellationToken cancellationToken);
        Task<Payment?> CreateOrGetExist(string transactionId, decimal? paymentAmount, CancellationToken cancellationToken);
        Task<ClaimResult?> MarkClaimAsync(string transactionId, string nodeId, int leaseSeconds, CancellationToken cancellationToken);
        Task<ClaimResult?> ReclaimFromDeadPeerAsync(string transactionId, string deadPeerId, string newOwnerId, int leaseSeconds, CancellationToken cancellationToken);
        Task MarkProcessingAsync(string transactionId, string nodeId, long fencingToken, int leaseSeconds, CancellationToken cancellationToken);
        Task RenewLeaseAsync(string transactionId, string nodeId, long fencingToken, int leaseSeconds, CancellationToken cancellationToken);
        Task<bool> MarkCompleteAsync(string transactionId, string nodeId, long fencingToken, string resultMessage, CancellationToken cancellationToken);
        Task<IReadOnlyList<Payment>> GetInFlightByOwnerAsync(string ownerNodeId, CancellationToken cancellationToken);
    }
}

public sealed record ClaimResult(string TransactionId, long FencingToken);