using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Json;
using Microsoft.Extensions.Hosting;

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
            await foreach (var (name, model) in languageModels)
                languageDictionary[name] = model;

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
                    serviceCollection.AddDistributionBot();
                    serviceCollection.AddGuestsBot();
                    
                    serviceCollection.AddGeneralServices(data);
                    serviceCollection.AddGoogleServices(googleInitializer);
                    serviceCollection.AddBotServices(data.Bot.Token);
                    serviceCollection.AddWorkerServices();
                    serviceCollection.AddLocalizationServices(languageDictionary);
                    serviceCollection.AddChartServices();
                })
                .ConfigureLogging(LoggingExtensions.Configure)
                .RunConsoleAsync();
        }
    }
}