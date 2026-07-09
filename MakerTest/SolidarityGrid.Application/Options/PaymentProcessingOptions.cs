namespace SolidarityGrid.Application.Options
{
    public class PaymentProcessingOptions
    {
        public const string SectionName = "PaymentProcessing";

        public string NodeId { get; set; } = "node-a";
        public int LeaseSeconds { get; set; } = 12;
        public int MinProcessingSeconds { get; set; } = 5;
        public int MaxProcessingSeconds { get; set; } = 10;
    }
}
