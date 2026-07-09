namespace SolidarityGrid.Infrastructure.Options
{
    public sealed class RegionalOptions
    {
        public const string SectionName = "Regional";

        public string TimeZoneId { get; set; } = "America/Bogota";
        public string Culture { get; set; } = "es-CO";
        public bool UseUtcForPersistence { get; set; } = true;
    }
}
