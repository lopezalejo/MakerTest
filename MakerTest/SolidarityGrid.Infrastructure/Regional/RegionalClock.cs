using Microsoft.Extensions.Options;
using SolidarityGrid.Application.Interfaces;
using SolidarityGrid.Infrastructure.Options;

namespace SolidarityGrid.Infrastructure.Regional
{
    public sealed class RegionalClock : IRegionalClock
    {
        private readonly TimeZoneInfo _timeZone;

        public RegionalClock(IOptions<RegionalOptions> options)
        {
            var regional = options.Value;
            Culture = regional.Culture;

            var configuredZone = regional.TimeZoneId;
            try
            {
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuredZone);
                TimeZoneId = configuredZone;
            }
            catch (TimeZoneNotFoundException)
            {
                _timeZone = TimeZoneInfo.Utc;
                TimeZoneId = TimeZoneInfo.Utc.Id;
            }
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime NowInRegionalZone => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _timeZone);

        public string TimeZoneId { get; }

        public string Culture { get; }
    }

}
