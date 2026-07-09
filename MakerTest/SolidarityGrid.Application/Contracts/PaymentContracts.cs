namespace SolidarityGrid.Application.Contracts
{
    public sealed record PayRequest(decimal Amount);

    public sealed record PayResponse(
        string TransactionId,
        string Status,
        string Message,
        string? CompletedBy = null);

    public sealed record PaymentStatusResponse(
        string TransactionId,
        decimal PaymentAmount,
        string Status,
        string? OwnerNodeId,
        DateTime? LeaseUntil,
        long FencingToken,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        string? CompletedByNodeId,
        string? ResultMessage);
}
