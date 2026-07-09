namespace SolidarityGrid.Application.Interfaces
{
    public interface IRegionalClock
    {
        DateTime UtcNow { get; }
        DateTime NowInRegionalZone { get; }
        string TimeZoneId { get; }
        string Culture { get; }
    }
}
