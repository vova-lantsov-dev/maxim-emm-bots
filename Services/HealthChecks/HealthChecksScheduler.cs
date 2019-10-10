using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.HealthChecks;
using MaximEmmBots.Models.Json.Restaurants;
using MaximEmmBots.Services.Scheduling;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MaximEmmBots.Services.HealthChecks
{
    internal sealed class HealthChecksScheduler : IScheduler
    {
        private readonly ITelegramBotClient _client;
        private readonly Context _context;

        public HealthChecksScheduler(ITelegramBotClient client, Context context) 
        {
            _client = client;
            _context = context;
        }

        public string SchedulerName => "Health checks";

        public SchedulingMode SchedulingMode => SchedulingMode.Static;

        public Func<Restaurant, TimeSpan> SchedulingTime => _ => TimeSpan.FromHours(1d);

        public async Task OnElapseAsync(Restaurant restaurant, CancellationToken cancellationToken)
        {
            var healthChecks = await _context.HealthChecks
                .Find(FilterDefinition<HealthCheckEntry>.Empty)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var healthCheck in healthChecks)
            {
                var sb = new StringBuilder();
                var format = new CultureInfo("ru-RU");

                sb.AppendFormat(format, "<b>Ресурс: {0}</b>\n", healthCheck.Name);
                sb.AppendFormat(format, "Последняя проверка: {0}\n", healthCheck.LastCheck.ToString("f", format));
                sb.AppendFormat(format, "Все тесты пройдены: {0}\n\n", healthCheck.TestsPassed ? "да" : "нет");

                sb.AppendJoin("\n\n", healthCheck.Uris.Select(uri =>
                {
                    var (tagName, uriItem) = uri;
                    return $"<b>{tagName}\nИмя ресторана: {uriItem.RestaurantName}</b>\nТесты пройдены: {(uriItem.TestsPassed ? "да" : "нет")}\n" +
                    $"Всего получено данных: {uriItem.SuccessItemsScraped}\n" +
                    string.Join('\n', uriItem.Tests.Select((test, ind) => $"<i>{test.Name}</i>: {test.Status}"));
                }));

                await _client.SendTextMessageAsync(-1001463899405L, sb.ToString(), ParseMode.Html, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
