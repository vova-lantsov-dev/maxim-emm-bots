using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using TimeZoneConverter;

namespace MaximEmm.Shared
{
    public sealed class CultureService
    {
        private readonly ConcurrentDictionary<string, CultureInfo> _cultures =
            new ConcurrentDictionary<string, CultureInfo>();
        private readonly ConcurrentDictionary<string, TimeZoneInfo> _timeZones =
            new ConcurrentDictionary<string, TimeZoneInfo>();
        
        private readonly IReadOnlyDictionary<string, LocalizationModel> _models;

        public CultureService(IReadOnlyDictionary<string, LocalizationModel> models)
        {
            _models = models;
        }

        public CultureInfo CultureFor(Restaurant restaurant)
        {
            return _cultures.GetOrAdd(restaurant.Culture.Name, cultureName => new CultureInfo(cultureName));
        }

        private TimeZoneInfo TimeZoneFor(Restaurant restaurant)
        {
            return _timeZones.GetOrAdd(restaurant.Culture.TimeZone, TZConvert.GetTimeZoneInfo);
        }

        public DateTime NowFor(Restaurant restaurant)
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneFor(restaurant));
        }

        public LocalizationModel ModelFor(Restaurant restaurant)
        {
            return _models[restaurant.Culture.Name];
        }
    }
}