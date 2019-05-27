using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Json;
using Microsoft.Extensions.Hosting;
using TimeZoneConverter;

namespace MaximEmmBots
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
            var data = await SettingsExtensions.LoadDataAsync(settingsFilePath);

            var languageModels = SettingsExtensions.LoadLanguagesAsync(Directory.GetCurrentDirectory(),
                data.Restaurants.Select(r => r.Culture.Name).Distinct());
            var languageDictionary = new Dictionary<string, LocalizationModel>();
            var cultureDictionary = new Dictionary<string, CultureInfo>();
            await foreach (var (name, model) in languageModels)
            {
                languageDictionary[name] = model;
                cultureDictionary[name] = new CultureInfo(name);
            }

            var timeZoneDictionary = new Dictionary<string, TimeZoneInfo>();
            foreach (var timeZone in data.Restaurants.Select(r => r.Culture.TimeZone).Distinct())
                timeZoneDictionary[timeZone] = TZConvert.GetTimeZoneInfo(timeZone);

            var googleCredential = await GoogleSheetsExtensions.AuthorizeAsync(data.GoogleCredentials);
            var googleInitializer = new BaseClientService.Initializer
            {
                ApplicationName = "Telegram Bot",
                HttpClientInitializer = googleCredential
            };
            
            await new HostBuilder()
                .UseEnvironment(Environments.Development)
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddGeneralServices(data);
                    serviceCollection.AddGoogleServices(googleInitializer);
                    serviceCollection.AddBotServices(data.Bot.Token);
                    serviceCollection.AddWorkerServices();
                    serviceCollection.AddLocalizationServices(languageDictionary, timeZoneDictionary,
                        cultureDictionary);
                })
                .ConfigureLogging(LoggingExtensions.Configure)
                .RunConsoleAsync();
        }
    }
}