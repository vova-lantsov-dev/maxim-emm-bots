using System;
using System.Collections.Concurrent;
using System.Globalization;
using MaximEmmBots.Models.Json.Restaurants;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;

namespace MaximEmmBots.Services
{
    internal sealed class CultureService
    {
        private readonly ConcurrentDictionary<string, Lazy<CultureInfo>> _cultures =
            new ConcurrentDictionary<string, Lazy<CultureInfo>>();
        private readonly ConcurrentDictionary<string, Lazy<TimeZoneInfo>> _timeZones =
            new ConcurrentDictionary<string, Lazy<TimeZoneInfo>>();
        
        private readonly ILogger<CultureService> _logger;

        public CultureService(ILogger<CultureService> logger)
        {
            _logger = logger;
        }

        internal CultureInfo CultureFor(Restaurant restaurant)
        {
            _logger.LogDebug("Get culture for {0}: {1}", restaurant.ChatId, restaurant.Culture.Name);
            return _cultures.GetOrAdd(restaurant.Culture.Name, CultureInfoInitializer).Value;
        }

        internal TimeZoneInfo TimeZoneFor(Restaurant restaurant)
        {
            _logger.LogDebug("Get time zone for {0}: {1}", restaurant.ChatId, restaurant.Culture.TimeZone);
            return _timeZones.GetOrAdd(restaurant.Culture.TimeZone, TimeZoneInitializer).Value;
        }

        internal DateTime NowFor(Restaurant restaurant)
        {
            _logger.LogDebug("Get now for {0}", restaurant.ChatId);
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