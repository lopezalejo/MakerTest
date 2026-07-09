using SolidarityGrid.Domain.Entity.Payments;
using SolidarityGrid.Infrastructure.Entity;

namespace SolidarityGrid.Infrastructure.Mapper
{
    public class PymentMapper
    {
        public static Payment MapPayment(PaymentEntity entity) =>
        new()
        {
            TransactionId = entity.TransactionId,
            PaymentAmount = entity.PaymentAmount,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };
    }
}
