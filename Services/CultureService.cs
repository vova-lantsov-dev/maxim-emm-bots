using System;
using System.Collections.Concurrent;
using System.Globalization;
using MaximEmmBots.Models.Json;
using TimeZoneConverter;

namespace MaximEmmBots.Services
{
    internal sealed class CultureService
    {
        private readonly ConcurrentDictionary<string, Lazy<CultureInfo>> _cultures = new ConcurrentDictionary<string, Lazy<CultureInfo>>();
        private readonly ConcurrentDictionary<string, Lazy<TimeZoneInfo>> _timeZones = new ConcurrentDictionary<string, Lazy<TimeZoneInfo>>();

        internal CultureInfo CultureFor(Restaurant restaurant)
        {
            return _cultures.GetOrAdd(restaurant.Culture.Name, CultureInfoInitializer).Value;
        }

        internal TimeZoneInfo TimeZoneFor(Restaurant restaurant)
        {
            return _timeZones.GetOrAdd(restaurant.Culture.TimeZone, TimeZoneInitializer).Value;
        }

        internal DateTime NowFor(Restaurant restaurant)
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneFor(restaurant));
        }

        #region Extensions

        private static Lazy<CultureInfo> CultureInfoInitializer(string cultureName)
        {
            return new Lazy<CultureInfo>(() => new CultureInfo(cultureName));
        }

        private static Lazy<TimeZoneInfo> TimeZoneInitializer(string timeZoneName)
        {
            return new Lazy<TimeZoneInfo>(() => TZConvert.GetTimeZoneInfo(timeZoneName));
        }
        
        #endregion
    }
}