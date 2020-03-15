using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using MaximEmmBots.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace MaximEmmBots.Services
{
    internal sealed class RestartNotificationService : IHostedService
    {
        private readonly ITelegramBotClient _client;
        private readonly Data _data;

        public RestartNotificationService(ITelegramBotClient client, IOptions<DataOptions> options)
        {
            _client = client;
            _data = options.Value.Data;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var idOfAdminWithRestartAccess in _data.WithRestartAccess)
            {
                try
                {
                    await _client.SendTextMessageAsync(idOfAdminWithRestartAccess, "Bot is running!",
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // silent mode
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var idOfAdminWithRestartAccess in _data.WithRestartAccess)
            {
                try
                {
                    await _client.SendTextMessageAsync(idOfAdminWithRestartAccess, "Bot is stopping!",
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // silent mode
                }
            }
        }
    }
}