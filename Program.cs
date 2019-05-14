using System.Threading.Tasks;
using MaximEmmBots.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await new HostBuilder()
                .UseEnvironment(EnvironmentName.Development)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<Context>();
                })
                .RunConsoleAsync();
        }
    }
}