using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MaximEmmBots.Extensions;
using MaximEmmBots.Models.Json.Restaurants;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

// ReSharper disable LoopCanBeConvertedToQuery

namespace MaximEmmBots.Services.MailBot
{
    internal sealed class GmailClient : IMailClient
    {
        private readonly GmailService _gmailService;
        private readonly ITelegramBotClient _botClient;
        private readonly CultureService _cultureService;
        private readonly ILogger<GmailClient> _logger;
        
        public GmailClient(GmailService gmailService,
            ITelegramBotClient botClient,
            CultureService cultureService,
            ILogger<GmailClient> logger)
        {
            _gmailService = gmailService;
            _botClient = botClient;
            _cultureService = cultureService;
            _logger = logger;
        }

        public async IAsyncEnumerable<string> ExecuteForRestaurantAsync(Restaurant restaurant,
            string checklistName, string nofityMessage, CancellationToken cancellationToken)
        {
            const string userId = "me";

            var result = await _gmailService.Users.Threads.List(userId).ExecuteAsync(cancellationToken);

            foreach (var gmailThread in result.Threads.Where(t => t.Snippet.Contains(checklistName)).Reverse())
            {
                var threadInfo = await _gmailService.Users.Threads.Get(userId, gmailThread.Id)
                    .ExecuteAsync(cancellationToken);

                foreach (var gmailThreadMessage in threadInfo.Messages)
                {
                    if (!gmailThreadMessage.Payload.Headers.Any(h =>
                        h.Name == "Subject" && h.Value.Contains(checklistName, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    
                    var messageInfo = await _gmailService.Users.Messages.Get(userId, gmailThreadMessage.Id)
                        .ExecuteAsync(cancellationToken);
                                                                    

                    IEnumerable<MessagePart> GetAttachmentParts(MessagePart part)
                    {
                        if (part.Parts == null)
                            yield break;

                        foreach (var innerPart in part.Parts)
                        {
                            if (innerPart.Body.AttachmentId == null)
                                continue;

                            yield return innerPart;
                        }

                        foreach (var innerPart in part.Parts)
                        {
                            var found = GetAttachmentParts(innerPart);
                            if (found != null)
                                foreach (var foundItem in found)
                                    yield return foundItem;
                        }
                    }

                    var attachmentParts = GetAttachmentParts(messageInfo.Payload);
                    if (attachmentParts == null)
                        continue;

                    var photos = new List<(MemoryStream content, string filename)>();
                    var date = messageInfo.Payload.Headers.FirstOrDefault(h => h.Name == "X-Google-Original-Date");
                    var message = $"*{nofityMessage}\n{date?.Value}*";
                    
                    foreach (var attachmentPart in attachmentParts)
                    {
                        var attachment = await _gmailService.Users.Messages.Attachments.Get(userId, messageInfo.Id,
                            attachmentPart.Body.AttachmentId).ExecuteAsync(cancellationToken);
                        var attachmentBytes = Convert.FromBase64String(attachment.Data.ToBase64Url());
                        var attachmentStream = new MemoryStream(attachmentBytes);
                        
                        if (!attachmentPart.MimeType.StartsWith("image"))
                        {
                            try
                            {
                                await _botClient.SendDocumentAsync(restaurant.ChatId,
                                    new InputOnlineFile(attachmentStream, attachmentPart.Filename.ToFileName()),
                                    message, ParseMode.Markdown, cancellationToken: cancellationToken);
                            }
                            finally
                            {
                                await attachmentStream.DisposeAsync();
                            }
                        }
                        else
                        {
                            photos.Add((attachmentStream, attachmentPart.Filename));
                        }

                        yield return attachmentPart.Body.AttachmentId;
                    }

                    if (photos.Count == 1)
                    {
                        var (content, filename) = photos[0];
                        try
                        {
                            await _botClient.SendPhotoAsync(restaurant.ChatId, new InputOnlineFile(content, filename),
                                message, ParseMode.Markdown, cancellationToken: cancellationToken);
                        }
                        finally
                        {
                            await content.DisposeAsync();
                        }
                    }
                    else if (photos.Count != 0)
                    {
                        try
                        {
                            await _botClient.SendMediaGroupAsync(photos
                                    .Select<(MemoryStream content, string filename), IAlbumInputMedia>(item =>
                                        new InputMediaPhoto(new InputMedia(item.content, item.filename))
                                            {Caption = item.filename}),
                                restaurant.ChatId, cancellationToken: cancellationToken);
                        }
                        finally
                        {
                            await Task.WhenAll(photos.Select(photo => photo.content.DisposeAsync().AsTask()));
                        }
                    }
                }
            }
        }
    }
}