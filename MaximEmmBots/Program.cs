using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Services;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Models.Json.Restaurants;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots
{
    internal static class Program
    {
        private static async Task Main()
        {
            var environment = Environment.GetEnvironmentVariable("BOTS_ENVIRONMENT") ?? "Development";
            var data = await SettingsExtensions.LoadDataAsync(environment == "Development").ConfigureAwait(false);
            
            data.Restaurants = new List<Restaurant>();
            await foreach (var restaurant in SettingsExtensions.YieldRestaurantsAsync(environment == "Development"))
                data.Restaurants.Add(restaurant);

            var languageModels = SettingsExtensions.YieldLanguagesAsync();
            var languageDictionary = new Dictionary<string, LocalizationModel>();
            await foreach (var (name, model) in languageModels)
                languageDictionary[name] = model;

            var googleCredential = await GoogleExtensions.AuthorizeAsync(data.GoogleCredentials).ConfigureAwait(false);
            var googleInitializer = new BaseClientService.Initializer
            {
                ApplicationName = "Telegram Bot",
                HttpClientInitializer = googleCredential
            };
            
            await new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddGeneralServices(data);
                    serviceCollection.AddBotServices(data.Bot.Token);
                    
                    serviceCollection.AddDistributionBot();
                    serviceCollection.AddGuestsBot();
                    serviceCollection.AddReviewBot();
                    serviceCollection.AddStatsBot();
                    serviceCollection.AddMailBot();
                    
                    serviceCollection.AddGoogleServices(googleInitializer);
                    serviceCollection.AddLocalizationServices(languageDictionary);

                    serviceCollection.AddHealthChecks();
                })
                .ConfigureLogging(LoggingExtensions.Configure)
                .RunConsoleAsync().ConfigureAwait(false);
        }
    }
}