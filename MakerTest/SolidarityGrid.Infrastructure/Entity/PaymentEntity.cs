using static SolidarityGrid.Domain.Entity.Payments.PaymentStatus;

namespace SolidarityGrid.Infrastructure.Entity
{
    public sealed class PaymentEntity
    {
        public required string TransactionId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public PaymentStatusEnum Status { get; set; }
        public string? OwnerNodeId { get; init; }
        public DateTime? LeaseUntil { get; init; }
        public long FencingToken { get; init; }
        public string? CompletedByNodeId { get; init; }
        public DateTime? CompletedAt { get; init; }
        public string? ResultMessage { get; init; }
        public byte[] RowVersion { get; set; } = [];
        public DateTime? CreatedAt { get; set; }
    }
}
