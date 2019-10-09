using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace MaximEmmBots.Services
{
    internal sealed class BotHandlerService : BackgroundService
    {
        private readonly IUpdateHandler _updateHandler;
        private readonly ITelegramBotClient _client;
        private readonly ILogger<BotHandlerService> _logger;
        
        public BotHandlerService(IUpdateHandler updateHandler,
            ITelegramBotClient client,
            ILogger<BotHandlerService> logger)
        {
            _updateHandler = updateHandler;
            _client = client;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BotHandlerService started");
            
            await _client.ReceiveAsync(_updateHandler, stoppingToken).ConfigureAwait(false);
        }
    }
}