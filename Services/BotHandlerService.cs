using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace MaximEmmBots.Services
{
    internal sealed class BotHandlerService : BackgroundService
    {
        private readonly IUpdateHandler _updateHandler;
        private readonly ITelegramBotClient _client;
        
        public BotHandlerService(IUpdateHandler updateHandler, ITelegramBotClient client)
        {
            _updateHandler = updateHandler;
            _client = client;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _client.ReceiveAsync(_updateHandler, stoppingToken);
        }
    }
}