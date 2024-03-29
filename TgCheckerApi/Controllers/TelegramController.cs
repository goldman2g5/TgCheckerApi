using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Services;
using TgCheckerApi.Websockets;
using TgCheckerApi.Models.BaseModels;
using WTelegram;
using TL;
using System.Linq;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly ILogger<TelegramController> _logger;
        private readonly Client _client;
        private readonly IWebHostEnvironment _env;


        public TelegramController(TgDbContext context, ILogger<TelegramController> logger, IWebHostEnvironment env, TelegramClientService telegramClientService)
        {
            _context = context;
            _logger = logger;
            _env = env;
            _client = telegramClientService.GetClient();
        }

        [HttpGet("SendMessage")]
        public async Task<IActionResult> SendMessageAsync()
        {
            var chats = await _client.Messages_GetAllChats();
            Console.WriteLine("This user has joined the following:");
            foreach (var (id, chat) in chats.chats)
                if (chat.IsActive)
                    Console.WriteLine($"{id,10}: {chat}");
            return Ok();
        }

        private async Task<(long channelId, long accessHash)?> GetChannelAccessHash(long channelId)
        {
            try
            {
                // Attempt to get all chats
                channelId = FromPyrogramToWTelegramClient(channelId);
                Console.WriteLine("Attempting to get all chats...");
                Messages_Chats result = await _client.Messages_GetAllChats();
                Dictionary<long, ChatBase> chats = result.chats;
                foreach (var (id, chat) in chats)
                    if (chat.IsActive)
                        Console.WriteLine($"{id,10}: {chat}");

                // Try to find the channel in the list of chats
                Console.WriteLine($"Looking for channel with ID: {channelId}");
                if (chats.TryGetValue(channelId, out ChatBase channel))
                {
                    // If the channel is found, resolve its username
                    Console.WriteLine($"Channel found. Resolving username: {channel.MainUsername}");
                    Contacts_ResolvedPeer peer = await _client.Contacts_ResolveUsername(channel.MainUsername);

                    if (peer.Channel != null)
                    {
                        Console.WriteLine("Channel resolved successfully.");
                        return (peer.Channel.ID, peer.Channel.access_hash);
                    }
                    else
                    {
                        Console.WriteLine("Resolved peer does not contain a channel.");
                    }
                }
                else
                {
                    Console.WriteLine("Channel not found in chats.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return null;
        }

        public static long FromWTelegramClientToPyrogram(long wTelegramChannelId)
        {
            // Pyrogram format requires the ID to be negative with the "-100" prefix
            if (wTelegramChannelId > 0)
            {
                // Construct the Pyrogram ID by adding the "-100" prefix numerically
                string prefixedIdStr = "-100" + wTelegramChannelId.ToString();
                if (long.TryParse(prefixedIdStr, out long pyrogramId))
                {
                    return pyrogramId;
                }
                else
                {
                    throw new ArgumentException("Failed to convert to Pyrogram format.", nameof(wTelegramChannelId));
                }
            }
            else
            {
                // It's already in Pyrogram format or doesn't require conversion
                return wTelegramChannelId;
            }
        }

        public static long FromPyrogramToWTelegramClient(long pyrogramChannelId)
        {
            string channelIdStr = pyrogramChannelId.ToString();
            // Check if the ID is in Pyrogram format (negative and starts with "-100")
            if (channelIdStr.StartsWith("-100"))
            {
                // Remove the "-100" prefix and convert back to long
                if (long.TryParse(channelIdStr.Substring(4), out long numericId))
                {
                    return numericId;
                }
                else
                {
                    throw new ArgumentException("Invalid Pyrogram channel ID format.", nameof(pyrogramChannelId));
                }
            }
            else
            {
                // It's already in WTelegramClient format or doesn't require conversion
                return pyrogramChannelId;
            }
        }

        [HttpGet("GetMessagesByYear")]
        public async Task<IActionResult> GetMessageCountByYearAsync(long channelId)
        {
            try
            {
                // Resolve the channel username to get its ID and access hash
                var channelInfo = await GetChannelAccessHash(channelId);
                if (!channelInfo.HasValue)
                {
                    return NotFound("Channel not found or access hash unavailable.");
                }

                // Prepare to fetch messages
                var (resolvedChannelId, accessHash) = channelInfo.Value;
                var inputPeer = new InputPeerChannel(resolvedChannelId, accessHash);

                // Define the year for which you want to fetch messages
                var now = DateTime.UtcNow;
                var startOfYear = new DateTime(now.Year, 1, 1);
                var endOfYear = new DateTime(now.Year + 1, 1, 1).AddSeconds(-1); // Last moment of the current year

                var allMessages = new List<Message>();
                int limit = 100; // Adjust based on API limitations and your needs
                DateTime? offsetDate = null; // Start with no offset date

                // Fetch messages in batches until we've covered the entire year
                while (true)
                {
                    var messagesBatch = await _client.Messages_GetHistory(inputPeer, limit: limit, offset_date: (offsetDate == null ? default : (DateTime)offsetDate));
                    var messages = messagesBatch.Messages.OfType<Message>().ToList();

                    if (messages.Count() == 0) break; // Exit loop if no more messages are returned

                    allMessages.AddRange(messages);

                    // Check if the last message in the batch is outside the target year
                    var lastMessageDate = messages.Last().Date;
                    if (lastMessageDate < startOfYear)
                    {
                        break; // All messages from the target year have been fetched
                    }

                    offsetDate = lastMessageDate; // Prepare the offset date for the next batch
                }

                // Now that we have all messages, count them by month as before
                var messageCountByMonth = allMessages
                    .GroupBy(m => new
                    {
                        Year = m.Date.Year,
                        Month = m.Date.Month
                    })
                    .Select(group => new
                    {
                        group.Key.Year,
                        group.Key.Month,
                        Count = group.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToList();

                return Ok(messageCountByMonth);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to retrieve message counts by year: {ex.Message}");
                return StatusCode(500, "An error occurred while attempting to fetch message counts by year.");
            }
        }
    }
}
