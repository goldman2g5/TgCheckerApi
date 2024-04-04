using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TgCheckerApi.Services;
using TgCheckerApi.Websockets;
using TgCheckerApi.Models.BaseModels;
using WTelegram;
using TL;
using System.Linq;
using TgCheckerApi.Utility;

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

                var allMessages = new List<Message>();
                int limit = 100; // Adjust based on your needs
                DateTime offsetDate = DateTime.UtcNow; // Start fetching from the current moment
                int lastMessageId = 0; // To keep track of pagination

                while (offsetDate > startDate)
                {
                    var messagesBatch = await _client.Messages_GetHistory(inputPeer, offset_id: lastMessageId, limit: limit, offset_date: offsetDate);
                    var messages = messagesBatch.Messages.OfType<Message>()
                        .Where(m => m.Date >= startDate && m.Date <= endDate)
                        .ToList();

                    if (messages.Count == 0) break;

                    allMessages.AddRange(messages);
                    var earliestMessageInBatch = messages.OrderBy(m => m.Date).FirstOrDefault();

                    if (earliestMessageInBatch == null || earliestMessageInBatch.Date <= startDate) break; // If the earliest message is before the start date, stop fetching

                    // Prepare for the next iteration
                    offsetDate = earliestMessageInBatch.Date; // Move offset date to the date of the earliest message in the current batch
                    Console.WriteLine(lastMessageId);
                    lastMessageId = earliestMessageInBatch.id; // Adjust the last message ID for pagination
                }

                // Depending on your requirements, you might want to return the messages or a count
                return Ok(allMessages.Select(m => new { m.id, m.Date, m.message }).OrderByDescending(m => m.Date)); // Messages are ordered from newest to oldest
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to retrieve messages by period: {ex.Message}");
                return StatusCode(500, "An error occurred while attempting to fetch messages by period.");
            }
        }

        [HttpGet("GetMessagesByYear")]
        public async Task<IActionResult> GetMessageCountByYearAsync(long channelId)
        {
            try
            {
                var _client = await _tgclientService.GetClientByTelegramId(channelId);
                if (_client == null)
                {
                    return NotFound("No active client found for that channel");
                }
                // Resolve the channel username to get its ID and access hash
                var channelInfo = await _tgclientService.GetChannelAccessHash(channelId, _client);
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
