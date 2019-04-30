using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MaximEmmBots
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await new HostBuilder()
                .UseEnvironment(EnvironmentName.Development)
                .ConfigureAppConfiguration(builder =>
                {
                    
                })
                .ConfigureServices(services =>
                {
                    
                })
                .RunConsoleAsync();
        }
    }
}