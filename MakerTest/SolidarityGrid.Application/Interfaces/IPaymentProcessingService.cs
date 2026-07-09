namespace SolidarityGrid.Application.Interfaces
{
    public interface IPaymentProcessingService
    {
        void Enqueue(string transactionId, bool reclaimed);
    }
}
