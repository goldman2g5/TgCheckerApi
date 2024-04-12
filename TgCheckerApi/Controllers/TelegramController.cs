using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Services;
using TgCheckerApi.Websockets;
using TgCheckerApi.Models.BaseModels;
using WTelegram;
using TL;
using System.Linq;
using TgCheckerApi.Utility;
using System.Text.Json;
using System.Security.Cryptography.Xml;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly ILogger<TelegramController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly TelegramClientService _tgclientService;


        public TelegramController(TgDbContext context, ILogger<TelegramController> logger, IWebHostEnvironment env, TelegramClientService telegramClientService)
        {
            _context = context;
            _logger = logger;
            _env = env;
            _tgclientService = telegramClientService;
        }

        [HttpGet("SendMessage")]
        public async Task<IActionResult> SendMessageAsync()
        {
            var _client = await _tgclientService.GetClient();
            var chats = await _client.Messages_GetAllChats();
            Console.WriteLine("This user has joined the following:");
            foreach (var (id, chat) in chats.chats)
                if (chat.IsActive)
                    Console.WriteLine($"{id,10}: {chat}");
            return Ok();
        }

        [HttpGet("GetMessagesByPeriod")]
        public async Task<IActionResult> GetMessagesByPeriodAsync(long channelId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var _client = await _tgclientService.GetClientByTelegramId(channelId);
                if (_client == null)
                {
                    return NotFound("No active client found for that channel");
                }

                var channelInfo = await _tgclientService.GetChannelAccessHash(channelId, _client);
                if (!channelInfo.HasValue)
                {
                    return NotFound("Channel not found or access hash unavailable.");
                }

                var (resolvedChannelId, accessHash) = channelInfo.Value;
                var inputPeer = new InputPeerChannel(resolvedChannelId, accessHash);

                var allMessages = new List<TL.Message>();
                int limit = 100;
                DateTime offsetDate = DateTime.UtcNow;
                int lastMessageId = 0;

                // Fetch messages from Telegram
                while (offsetDate > startDate)
                {
                    var messagesBatch = await _client.Messages_GetHistory(inputPeer, offset_id: lastMessageId, limit: limit, offset_date: offsetDate);
                    var messages = messagesBatch.Messages.OfType<TL.Message>()
                        .Where(m => m.Date >= startDate && m.Date <= endDate)
                        .ToList();

                    if (messages.Count == 0) break;
                    allMessages.AddRange(messages);
                    var earliestMessageInBatch = messages.OrderBy(m => m.Date).FirstOrDefault();

                    if (earliestMessageInBatch == null || earliestMessageInBatch.Date <= startDate) break;

                    offsetDate = earliestMessageInBatch.Date;
                    lastMessageId = earliestMessageInBatch.id;
                }

                // Database update logic
                var messageIds = allMessages.Select(m => m.id).ToList();
                var existingMessages = await _context.Messages
                                        .Where(m => messageIds.Contains(m.Id))
                                        .ToListAsync();

                foreach (var tlMessage in allMessages)
                {
                    var existingMessage = existingMessages.FirstOrDefault(m => m.Id == tlMessage.id);
                    if (existingMessage == null)
                    {
                        _context.Messages.Add(new Models.BaseModels.Message
                        {
                            Id = tlMessage.id,
                            ChannelTelegramId = channelId, // Adjust if necessary
                            Views = tlMessage.views,
                            Text = tlMessage.message,
                            // Map other fields as necessary
                        });
                    }
                    else
                    {
                        // Update fields as necessary
                        existingMessage.Views = tlMessage.views;
                        existingMessage.Text = tlMessage.message;
                        // Continue updating other fields as necessary
                    }
                }

                await _context.SaveChangesAsync();

                // Return fetched messages
                return Ok(allMessages.Select(m => new { m.id, m.Date, m.views, m.reactions }).OrderByDescending(m => m.Date));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to retrieve messages by period: {ex.Message}");
                return StatusCode(500, "An error occurred while attempting to fetch messages by period.");
            }
        }

        [HttpGet("GetMessagesByIds")]
        public async Task<IActionResult> GetMessagesByIdsAsync(long channelId, [FromQuery] List<int> messageIds)
        {
            if (messageIds == null || !messageIds.Any())
            {
                return BadRequest("Message IDs are required.");
            }

            try
            {
               
                var _client = await _tgclientService.GetClientByTelegramId(channelId);
                channelId = TelegramIdConverter.ToWTelegramClient(channelId);
                if (_client == null)
                {
                    return NotFound("No active client found for that channel");
                }

                // WTelegramClient's method to get messages by IDs
                var channelInfo = await _tgclientService.GetChannelAccessHash(channelId, _client);
                var (resolvedChannelId, accessHash) = channelInfo.Value;
                var result = await _client.Channels_GetMessages(new InputChannel(resolvedChannelId, accessHash), 
                    messageIds.Select(x => new InputMessageID() {id = x}).ToArray());
                if (result is not TL.Messages_MessagesBase messagesBase)
                {
                    return NotFound("Messages not found.");
                }

                var messages = messagesBase.Messages;
                //var views = await _client.Messages_GetMessagesViews(new InputChannel(resolvedChannelId, accessHash), messageIds.ToArray(), true);
                //var response = views.views;

                //Depending on your needs, you might return the messages directly,
                //or transform them into a DTO to hide certain properties or to better fit your response model.
                var response = messages.Select(m => new
                {
                    m.ID,
                    Date = m.Date,
                    FromId = m.From,
                    // Add other properties as needed
                });


                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        class Porter
        {



            private const string VOWEL = "аеиоуыэюя";

            private const string PERFECTIVEGROUND = "((ив|ивши|ившись|ыв|ывши|ывшись)|((?<=[ая])(в|вши|вшись)))$";

            private const string REFLEXIVE = "(с[яь])$";

            private const string ADJECTIVE = "(ее|ие|ые|ое|ими|ыми|ей|ий|ый|ой|ем|им|ым|ом|его|ого|еых|ую|юю|ая|яя|ою|ею)$";

            private const string PARTICIPLE = "((ивш|ывш|ующ)|((?<=[ая])(ем|нн|вш|ющ|щ)))$";

            private const string VERB = "((ила|ыла|ена|ейте|уйте|ите|или|ыли|ей|уй|ил|ыл|им|ым|ены|ить|ыть|ишь|ую|ю)|((?<=[ая])(ла|на|ете|йте|ли|й|л|ем|н|ло|но|ет|ют|ны|ть|ешь|нно)))$";

            private const string NOUN = "(а|ев|ов|ие|ье|е|иями|ями|ами|еи|ии|и|ией|ей|ой|ий|й|и|ы|ь|ию|ью|ю|ия|ья|я)$";

            private const string RVRE = "^(.*?[аеиоуыэюя])(.*)$";

            private const string DERIVATIONAL = "[^аеиоуыэюя][аеиоуыэюя]+[^аеиоуыэюя]+[аеиоуыэюя].*(?<=о)сть?$";

            private const string SUPERLATIVE = "(ейше|ейш)?";


            public string Stemm(string word)
            {
                word = word.ToLower();
                word = word.Replace("ё", "е");
                if (IsMatch(word, RVRE))
                {

                    if (!Replace(ref word, PERFECTIVEGROUND, ""))
                    {
                        Replace(ref word, REFLEXIVE, "");
                        if (Replace(ref word, ADJECTIVE, ""))
                        {
                            Replace(ref word, PARTICIPLE, "");
                        }
                        else
                        {
                            if (!Replace(ref word, VERB, ""))
                            {
                                Replace(ref word, NOUN, "");
                            }

                        }

                    }


                    Replace(ref word, "и$", "");

                    if (IsMatch(word, DERIVATIONAL))
                    {
                        Replace(ref word, "ость?$", "");
                    }


                    if (!Replace(ref word, "ь$", ""))
                    {
                        Replace(ref word, SUPERLATIVE, "");
                        Replace(ref word, "нн$", "н");
                    }

                }

                return word;
            }

            private bool IsMatch(string word, string matchingPattern)
            {
                return new Regex(matchingPattern).IsMatch(word);
            }

            private bool Replace(ref string replace, string cleaningPattern, string by)
            {
                string original = replace;
                replace = new Regex(cleaningPattern,
                            RegexOptions.ExplicitCapture |
                            RegexOptions.Singleline
                            ).Replace(replace, by);
                return original != replace;
            }

        }

        [HttpPost("GetRoot")]
        public ActionResult<string> GetRoot(string word)
        {
            if (word == null || string.IsNullOrWhiteSpace(word))
            {
                return BadRequest("Please provide a valid word.");
            }

            var stemmer = new Porter();
            var root = stemmer.Stemm(word);

            return Ok(root);
        }
    }
}
