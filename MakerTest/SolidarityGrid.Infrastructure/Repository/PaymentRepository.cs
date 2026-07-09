using Microsoft.EntityFrameworkCore;
using SolidarityGrid.Application.Interfaces.Repository;
using SolidarityGrid.Domain.Entity.Payments;
using SolidarityGrid.Infrastructure.Database;
using SolidarityGrid.Infrastructure.Entity;
using SolidarityGrid.Infrastructure.Mapper;

namespace SolidarityGrid.Infrastructure.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly SolidaryGridDBContext _dbContext;
        public PaymentRepository(SolidaryGridDBContext dbContext) => _dbContext = dbContext;

        public async Task<Payment?> GetAsync(string transactionId, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
            return entity is null ? null : PymentMapper.MapPayment(entity);
        }

        public async Task<Payment?> CreateOrGetExist(string transactionId, decimal? paymentAmount, CancellationToken cancellationToken)
        {
            var existingPayment = await _dbContext.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);

            if (existingPayment != null)
            {
                return new Payment
                {
                    TransactionId = existingPayment.TransactionId,
                    PaymentAmount = existingPayment.PaymentAmount,
                    Status = existingPayment.Status,
                    CreatedAt = existingPayment.CreatedAt
                };
            }
            var newPaymentEntity = new PaymentEntity
            {
                TransactionId = transactionId,
                PaymentAmount = paymentAmount,
                CreatedAt = DateTime.Now
            };

            _dbContext.Payments.Add(newPaymentEntity);
            try
            {
                await _dbContext.SaveChangesAsync();
                return await GetAsync(transactionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _dbContext.ChangeTracker.Clear();
            }

            return await GetAsync(transactionId, cancellationToken);
        }

        public async Task<ClaimResult?> MarkClaimAsync(
        string transactionId,
        string nodeId,
        int leaseSeconds,
        CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var leaseUntil = now.AddSeconds(leaseSeconds);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var rows = await _dbContext.Payments
                .Where(p => p.TransactionId == transactionId
                    && p.Status != PaymentStatus.PaymentStatusEnum.Completado
                    && (p.Status == PaymentStatus.PaymentStatusEnum.Pendiente
                        || p.OwnerNodeId == nodeId
                        || p.LeaseUntil == null
                        || p.LeaseUntil < now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.PaymentStatusEnum.Asigado)
                    .SetProperty(p => p.OwnerNodeId, nodeId)
                    .SetProperty(p => p.LeaseUntil, leaseUntil)
                    .SetProperty(p => p.FencingToken, p => p.FencingToken + 1),
                    cancellationToken);

            if (rows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var fencingToken = await _dbContext.Payments
                .AsNoTracking()
                .Where(p => p.TransactionId == transactionId)
                .Select(p => p.FencingToken)
                .FirstAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ClaimResult(transactionId, fencingToken);
        }

        public async Task<ClaimResult?> ReclaimFromDeadPeerAsync(
            string transactionId,
            string deadPeerId,
            string newOwnerId,
            int leaseSeconds,
            CancellationToken cancellationToken)
        {
            var leaseUntil = DateTime.UtcNow.AddSeconds(leaseSeconds);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var rows = await _dbContext.Payments
                .Where(p => p.TransactionId == transactionId
                    && p.OwnerNodeId == deadPeerId
                    && (p.Status == PaymentStatus.PaymentStatusEnum.Asigado || p.Status == PaymentStatus.PaymentStatusEnum.Procesando)
                    && p.Status != PaymentStatus.PaymentStatusEnum.Completado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.PaymentStatusEnum.Asigado)
                    .SetProperty(p => p.OwnerNodeId, newOwnerId)
                    .SetProperty(p => p.LeaseUntil, leaseUntil)
                    .SetProperty(p => p.FencingToken, p => p.FencingToken + 1),
                    cancellationToken);

            if (rows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var fencingToken = await _dbContext.Payments
                .AsNoTracking()
                .Where(p => p.TransactionId == transactionId)
                .Select(p => p.FencingToken)
                .FirstAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ClaimResult(transactionId, fencingToken);
        }

        public Task MarkProcessingAsync(
            string transactionId,
            string nodeId,
            long fencingToken,
            int leaseSeconds,
            CancellationToken cancellationToken)
        {
            var leaseUntil = DateTime.UtcNow.AddSeconds(leaseSeconds);

            return _dbContext.Payments
                .Where(p => p.TransactionId == transactionId
                    && p.OwnerNodeId == nodeId
                    && p.FencingToken == fencingToken
                    && p.Status != PaymentStatus.PaymentStatusEnum.Completado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.PaymentStatusEnum.Procesando)
                    .SetProperty(p => p.LeaseUntil, leaseUntil),
                    cancellationToken);
        }

        public Task RenewLeaseAsync(
            string transactionId,
            string nodeId,
            long fencingToken,
            int leaseSeconds,
            CancellationToken cancellationToken)
        {
            var leaseUntil = DateTime.UtcNow.AddSeconds(leaseSeconds);

            return _dbContext.Payments
                .Where(p => p.TransactionId == transactionId
                    && p.OwnerNodeId == nodeId
                    && p.FencingToken == fencingToken
                    && p.Status == PaymentStatus.PaymentStatusEnum.Procesando)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(p => p.LeaseUntil, leaseUntil),
                    cancellationToken);
        }

        public async Task<bool> MarkCompleteAsync(
            string transactionId,
            string nodeId,
            long fencingToken,
            string resultMessage,
            CancellationToken cancellationToken)
        {
            var rows = await _dbContext.Payments
                .Where(p => p.TransactionId == transactionId
                    && p.OwnerNodeId == nodeId
                    && p.FencingToken == fencingToken
                    && p.Status != PaymentStatus.PaymentStatusEnum.Completado)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, PaymentStatus.PaymentStatusEnum.Completado)
                    .SetProperty(p => p.CompletedAt, DateTime.UtcNow)
                    .SetProperty(p => p.CompletedByNodeId, nodeId)
                    .SetProperty(p => p.ResultMessage, resultMessage)
                    .SetProperty(p => p.LeaseUntil, (DateTime?)null),
                    cancellationToken);

            return rows == 1;
        }

        public async Task<IReadOnlyList<Payment>> GetInFlightByOwnerAsync(string ownerNodeId, CancellationToken cancellationToken)
        {
            var entities = await _dbContext.Payments
                .AsNoTracking()
                .Where(p => p.OwnerNodeId == ownerNodeId
                    && (p.Status == PaymentStatus.PaymentStatusEnum.Asigado || p.Status == PaymentStatus.PaymentStatusEnum.Procesando))
                .ToListAsync(cancellationToken);

            return entities.Select(PymentMapper.MapPayment).ToList();
        }

    }
}
