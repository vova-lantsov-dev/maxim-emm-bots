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
        private static async Task Main(string[] args)
        {
            var data = await SettingsExtensions.LoadDataAsync();
            
            data.Restaurants = new List<Restaurant>();
            await foreach (var restaurant in SettingsExtensions.YieldRestaurantsAsync())
                data.Restaurants.Add(restaurant);

            var languageModels = SettingsExtensions.YieldLanguagesAsync();
            var languageDictionary = new Dictionary<string, LocalizationModel>();
            await foreach (var (name, model) in languageModels)
                languageDictionary[name] = model;

            var googleCredential = await GoogleExtensions.AuthorizeAsync(data.GoogleCredentials);
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
                    serviceCollection.AddBotServices(data.Bot.Token);
                    
                    serviceCollection.AddDistributionBot();
                    serviceCollection.AddGuestsBot();
                    serviceCollection.AddReviewBot();
                    
                    serviceCollection.AddGoogleServices(googleInitializer);
                    serviceCollection.AddLocalizationServices(languageDictionary);
                    serviceCollection.AddStatsBot();
                })
                .ConfigureLogging(LoggingExtensions.Configure)
                .RunConsoleAsync();
        }
    }
}