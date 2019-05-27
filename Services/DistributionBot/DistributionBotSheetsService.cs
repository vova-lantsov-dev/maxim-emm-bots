using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MaximEmmBots.Models.Json;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MaximEmmBots.Services.DistributionBot
{
    internal sealed class DistributionBotSheetsService
    {
        private readonly ITelegramBotClient _client;
        private readonly Data _data;
        private readonly ILogger<GoogleSheetsService> _logger;
        private readonly IReadOnlyDictionary<string, LocalizationModel> _localizationModels;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _timeZones;
        private readonly IReadOnlyDictionary<string, CultureInfo> _cultures;
        private readonly GoogleSheetsService _googleSheetsService;

        public DistributionBotSheetsService(ITelegramBotClient client,
            Data data,
            ILogger<GoogleSheetsService> logger,
            IReadOnlyDictionary<string, LocalizationModel> localizationModels,
            IReadOnlyDictionary<string, TimeZoneInfo> timeZones,
            IReadOnlyDictionary<string, CultureInfo> cultures,
            GoogleSheetsService googleSheetsService)
        {
            _client = client;
            _data = data;
            _logger = logger;
            _localizationModels = localizationModels;
            _timeZones = timeZones;
            _cultures = cultures;
            _googleSheetsService = googleSheetsService;
        }

        internal async Task ExecuteAsync(string timeZoneName, string cultureName,
            CancellationToken stoppingToken, int userId = 0)
        {
            var tomorrow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZones[timeZoneName]).AddDays(1d);
            var culture = _cultures[cultureName];
            var monthName = culture.DateTimeFormat.GetMonthName(tomorrow.Month);
            var model = _localizationModels[cultureName];
            
            var range = $"{monthName} {tomorrow:MM/yyyy}!$A$1:$YY";
            var response =
                await _googleSheetsService.GetValueRangeAsync(_data.DistributionBot.SpreadsheetId, range,
                    stoppingToken);
            if (response?.Values == null || response.Values.Count == 0)
            {
                if (userId > 0)
                    await _client.SendTextMessageAsync(userId, "Расписание на этот месяц недоступно.",
                        cancellationToken: stoppingToken);
                return;
            }

            var day = tomorrow.Day;
            var dateText = tomorrow.ToString("dd MMMM yyyy", culture);
            var privates = new Dictionary<int, (string text, long chatId)>();
            var groups = new Dictionary<long, List<(int userId, string name, string time)>>();
            
            foreach (var row in response.Values)
            {
                if (row.Count < 3)
                    continue;
                var row2 = row[2].ToString();
                if (row.Count < day + 3 || !int.TryParse(row[1].ToString(), out var rowUserId) &&
                    (row2.Length == 0 || row2.IndexOfAny("@+78".ToCharArray()) != 0) || privates.ContainsKey(rowUserId))
                    continue;

                var dayText = row[day + 2].ToString();
                if (string.IsNullOrWhiteSpace(dayText))
                    continue;

                var place = char.ToLower(dayText[0], culture);
                var restaurant = _data.Restaurants.Find(r => r.PlaceId == place.ToString());
                if (restaurant == default)
                    continue;
                var name = row[0].ToString();

                var infoText = new StringBuilder();
                var i = 1;
                for (; i < dayText.Length; i++)
                {
                    if (infoText.Length == 2)
                        break;
                    
                    var character = dayText[i];
                    if (!char.IsDigit(character))
                        break;

                    infoText.Append(character);
                }
                
                infoText.Append(":00");
                infoText.Append(dayText.Length > i ? " " + dayText.Substring(i).TrimStart() : string.Empty);

                var groupItem = (rowUserId, name, infoText.ToString());
                if (!groups.ContainsKey(restaurant.ChatId))
                    groups[restaurant.ChatId] = new List<(int, string, string)> {groupItem};
                else groups[restaurant.ChatId].Add(groupItem);

                if (rowUserId == 0 || userId > 0 && rowUserId != userId)
                    continue;

                infoText.AppendFormat(" {0}", restaurant.PlaceInfo);
                
                privates.Add(rowUserId, ($"Дорогой(ая/ое) {name}, *{dateText}* ты работаешь с {infoText}!", restaurant.ChatId));
            }
            
            if (privates.Count == 0)
                return;
            
            foreach (var (privateUserId, (privateText, chatId)) in privates)
            {
                var privateTextBuilder = new StringBuilder();
                privateTextBuilder.Append(privateText);
                privateTextBuilder.Append("\n\n");
                privateTextBuilder.AppendFormat("Также {0} с тобой работают:\n", dateText);
                privateTextBuilder.AppendJoin('\n', groups[chatId].Where(u => u.userId != privateUserId).Select(
                    u => u.userId > 0
                        ? $"[{u.name}](tg://user?id={u.userId}) с {u.time}"
                        : $"*{u.name}* с {u.time}"));

                try
                {
                    await _client.SendTextMessageAsync(privateUserId, privateTextBuilder.ToString(),
                        ParseMode.Markdown, cancellationToken: stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to send a message to private chat with id equals to {0}", privateUserId);
                }
            }

            if (userId == 0)
                foreach (var (chatId, users) in groups)
                {
                    var restaurant = _data.Restaurants.Find(r => r.ChatId == chatId);
                    if (restaurant == default)
                        continue;
                    
                    var groupTextBuilder = new StringBuilder();
                    groupTextBuilder.AppendFormat("*{0}* у нас {1} работают:\n", dateText, restaurant.PlaceInfo);
                    groupTextBuilder.AppendJoin('\n', users.Select(u => u.userId > 0
                        ? $"[{u.name}](tg://user?id={u.userId}) с {u.time}"
                        : $"*{u.name}* с {u.time}"));

                    try
                    {
                        await _client.SendTextMessageAsync(chatId, groupTextBuilder.ToString(),
                            ParseMode.Markdown, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to send a message to group with id equals to {0}", chatId);
                    }
                }
        }
    }
}