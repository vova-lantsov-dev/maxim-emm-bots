using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MaximEmmBots.Extensions;
using MongoDB.Driver;
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
        private readonly Context _context;
        
        public GmailClient(GmailService gmailService,
            ITelegramBotClient botClient,
            Context context)
        {
            _gmailService = gmailService;
            _botClient = botClient;
            _context = context;
        }

        public async IAsyncEnumerable<string> ExecuteForRestaurantAsync(long chatId,
            string checklistName, string nofityMessage, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const string userId = "me";

            var result = await _gmailService.Users.Threads.List(userId).ExecuteAsync(cancellationToken).ConfigureAwait(false);

            foreach (var gmailThread in result.Threads.Where(t => t.Snippet.Contains(checklistName, StringComparison.Ordinal)).Reverse())
            {
                var threadInfo = await _gmailService.Users.Threads.Get(userId, gmailThread.Id)
                    .ExecuteAsync(cancellationToken).ConfigureAwait(false);

                foreach (var gmailThreadMessage in threadInfo.Messages)
                {
                    if (!gmailThreadMessage.Payload.Headers.Any(h =>
                        h.Name == "Subject" && h.Value.Contains(checklistName, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    
                    if (await _context.SentChecklists.Find(sc => sc.MessageId == gmailThreadMessage.Id)
                        .AnyAsync(cancellationToken).ConfigureAwait(false))
                        continue;
                    
                    var messageInfo = await _gmailService.Users.Messages.Get(userId, gmailThreadMessage.Id)
                        .ExecuteAsync(cancellationToken).ConfigureAwait(false);


                    static IEnumerable<MessagePart> GetAttachmentParts(MessagePart part)
                    {
                        if (part.Parts == null)
                            yield break;

                        foreach (var innerPart in part.Parts)
                        {
                            if (innerPart.Body.AttachmentId == null || innerPart.MimeType == null)
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

                    var attachmentParts = GetAttachmentParts(messageInfo.Payload)?.ToArray();
                    if (attachmentParts == null)
                        continue;

                    if (attachmentParts.Length > 0)
                    {
                        var photos = new List<(MemoryStream content, string filename)>(attachmentParts.Length);
                        var date = messageInfo.Payload.Headers.FirstOrDefault(h => h.Name == "X-Google-Original-Date");
                        var message = $"*{nofityMessage}\n{date?.Value}*";

                        foreach (var attachmentPart in attachmentParts)
                        {
                            var attachment = await _gmailService.Users.Messages.Attachments.Get(userId, messageInfo.Id,
                                attachmentPart.Body.AttachmentId).ExecuteAsync(cancellationToken).ConfigureAwait(false);
                            var attachmentBytes = Convert.FromBase64String(attachment.Data.ToBase64Url());
                            var attachmentStream = new MemoryStream(attachmentBytes);

                            if (!attachmentPart.MimeType.StartsWith("image", StringComparison.Ordinal))
                            {
                                try
                                {
                                    await _botClient.SendDocumentAsync(chatId,
                                            new InputOnlineFile(attachmentStream, attachmentPart.Filename.ToFileName()),
                                            caption: message, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken)
                                        .ConfigureAwait(false);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
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
                        }

                        if (photos.Count == 1)
                        {
                            var (content, filename) = photos[0];
                            try
                            {
                                await _botClient.SendPhotoAsync(chatId,
                                        new InputOnlineFile(content, filename),
                                        message, ParseMode.Markdown, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            finally
                            {
                                await content.DisposeAsync();
                            }
                        }
                        else
                        {
                            try
                            {
                                await _botClient.SendMediaGroupAsync(chatId,
                                    photos.Select<(MemoryStream content, string filename), IAlbumInputMedia>(item =>
                                        new InputMediaPhoto(new InputMedia(item.content, item.filename))
                                            {Caption = item.filename}),
                                    cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            finally
                            {
                                await Task.WhenAll(photos.Select(photo => photo.content.DisposeAsync().AsTask()))
                                    .ConfigureAwait(false);
                            }
                        }
                    }

                    yield return messageInfo.Id;
                }
            }
        }
    }
}