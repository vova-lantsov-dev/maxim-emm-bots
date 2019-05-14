using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
                    
                })
                .RunConsoleAsync();
        }
    }
}