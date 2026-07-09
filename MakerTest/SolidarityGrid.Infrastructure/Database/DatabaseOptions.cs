namespace SolidarityGrid.Infrastructure.Database
{
    public class DatabaseOptions
    {
        public const string SectionName = "ConnectionStrings";

        public string SolidarityGrid { get; set; } = string.Empty;
    }
}
