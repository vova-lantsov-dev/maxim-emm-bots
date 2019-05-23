using System.IO;
using System.Threading.Tasks;
using Google.Apis.Services;
using MaximEmmBots.Extensions;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
            var data = await SettingsExtensions.LoadDataAsync(settingsFilePath);

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
                    serviceCollection.AddGeneralServices(data, googleInitializer);
                    serviceCollection.AddWorkerServices();
                })
                .ConfigureLogging(LoggingExtensions.Configure)
                .RunConsoleAsync();
        }
    }
}