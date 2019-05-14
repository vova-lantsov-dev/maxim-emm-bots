using System.IO;
using System.Threading.Tasks;
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
            
            await new HostBuilder()
                .UseEnvironment(Environments.Development)
                .ConfigureServices(serviceCollection => serviceCollection.AddServices(data))
                .ConfigureLogging(LoggingExtensions.Configure)
                .RunConsoleAsync();
        }
    }
}