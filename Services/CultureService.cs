using System;
using System.Collections.Concurrent;
using System.Globalization;
using MaximEmmBots.Models.Json;
using TimeZoneConverter;

namespace MaximEmmBots.Services
{
    internal sealed class CultureService
    {
        private readonly ConcurrentDictionary<string, CultureInfo> _cultures = new ConcurrentDictionary<string, CultureInfo>();
        private readonly ConcurrentDictionary<string, TimeZoneInfo> _timeZones = new ConcurrentDictionary<string, TimeZoneInfo>();

        internal CultureInfo CultureFor(Restaurant restaurant)
        {
            return _cultures.GetOrAdd(restaurant.Culture.Name, name => new CultureInfo(name));
        }

        internal TimeZoneInfo TimeZoneFor(Restaurant restaurant)
        {
            return _timeZones.GetOrAdd(restaurant.Culture.TimeZone, TZConvert.GetTimeZoneInfo);
        }

        internal DateTime NowFor(Restaurant restaurant)
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneFor(restaurant));
        }
    }
}