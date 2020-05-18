using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using _Host = Microsoft.Extensions.Hosting.Host;
using Microsoft.Extensions.Logging;

namespace Host
{
    internal static class Program
    {
        private static async Task Main()
        {
            var host = CreateHostBuilder().Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder() =>
            _Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var env = context.HostingEnvironment.EnvironmentName;
                    builder.AddJsonFile("jwt.json", true, true)
                        .AddJsonFile($"jwt.{env}.json", true, true);
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddSentry();
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}