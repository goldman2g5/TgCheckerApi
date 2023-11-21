using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Models.PostModels;
using TgCheckerApi.Models.PutModels;
using TgCheckerApi.Services;
using TgCheckerApi.Utility;
using TgCheckerApi.Websockets;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHubContext<BotHub> _hubContext;
        private readonly ChannelService _channelService;
        private readonly TagsService _tagsService;
        private readonly BumpService _bumpService;
        private readonly SubscriptionService _subscriptionService;
        private readonly UserService _userService;
        private readonly NotificationService _notificationService;



        public ChannelController(TgDbContext context, IMapper mapper, IHubContext<BotHub> hubContext)
        {
            _context = context;
            _mapper = mapper;
            _hubContext = hubContext;
            _channelService = new ChannelService(context);
            _tagsService = new TagsService(context);
            _bumpService = new BumpService();
            _subscriptionService = new SubscriptionService(context);
            _userService = new UserService(context);
            _notificationService = new NotificationService(context);
        }

        // GET: api/Channel
        [BypassApiKey]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChannelGetModel>>> GetChannels()
        {
            var channels = await _context.Channels
                                         .OrderByDescending(channel => channel.Bumps)
                                         .ToListAsync();

            if (channels == null || channels.Count == 0)
            {
                return NotFound();
            }

            var channelGetModels = channels.Select(channel => _channelService.MapToChannelGetModel(channel)).ToList();

            return channelGetModels;
        }

        // GET: api/Channel/Page/{page}
        [BypassApiKey]
        [HttpGet("Page/{page}")]
        public async Task<ActionResult<IEnumerable<ChannelGetModel>>> GetChannels(int page = 1, [FromQuery] string? tags = null, [FromQuery] string? sortOption = null, [FromQuery] string? ascending = null, [FromQuery] string? search = null)
        {
            IQueryable<Channel> channelsQuery = _context.Channels;
            int PageSize = ChannelService.GetPageSize();

            channelsQuery = _channelService.ApplyTagFilter(channelsQuery, tags);
            channelsQuery = _channelService.ApplySort(channelsQuery, sortOption, Convert.ToBoolean(ascending));
            channelsQuery = _channelService.ApplySearch(channelsQuery, search);

            var totalChannelCount = await channelsQuery.CountAsync();

            channelsQuery = channelsQuery.Skip((page - 1) * PageSize).Take(PageSize);
            var channels = await channelsQuery.ToListAsync();

            if (!channels.Any())
            {
                return NotFound();
            }

            var channelGetModels = channels.Select(channel => _channelService.MapToChannelGetModel(channel)).ToList();
            int totalPages = (int)Math.Ceiling((double)totalChannelCount / PageSize);

            Response.Headers.Add("X-Total-Pages", totalPages.ToString());

            return channelGetModels;
        }

        [BypassApiKey]
        [RequiresJwtValidation]
        [HttpPut("{id}/Details")]
        public async Task<IActionResult> SetChannelDetails(int id, ChannelDetailsPutModel payload)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            var channel = await _context.Channels.FindAsync(id);
            if (channel == null)
            {
                return NotFound();
            }

            if (!_userService.UserHasAccessToChannel(user, channel))
            {
                return Unauthorized();
            }

            channel.Description = payload.desc;
            await _tagsService.RemoveExistingTagsFromChannel(id);
            await _tagsService.AddNewTagsToChannel(id, payload.tags.ToList());

            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: api/Channel/5
        [BypassApiKey]
        [HttpGet("{id}")]
        public async Task<ActionResult<ChannelGetModel>> GetChannel(int id)
        {
            if (_context.Channels == null)
            {
                return NotFound();
            }
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound();
            }

            var channelGetModel = _channelService.MapToChannelGetModel(channel);


            return channelGetModel;
        }

        [HttpGet("{id}/Tags")]
        public async Task<ActionResult<IEnumerable<string>>> GetChannelTags(int id)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound();
            }

            var channelHasTags = await _context.ChannelHasTags
                .Include(cht => cht.TagNavigation)
                .Where(cht => cht.Channel == id)
                .ToListAsync();

            var tags = channelHasTags.Select(cht => cht.TagNavigation?.Text).ToList();

            return tags;
        }

        [HttpPut("{id}/Tags")]
        public async Task<IActionResult> SetChannelTags(int id, List<string> tags)
        {
            var channel = await _context.Channels.FindAsync(id);
            if (channel == null)
            {
                return NotFound();
            }

            await _tagsService.RemoveExistingTagsFromChannel(id);
            await _tagsService.AddNewTagsToChannel(id, tags);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Channel/ToggleNotifications/5
        [HttpPut("ToggleNotifications/{id}")]
        public async Task<IActionResult> ToggleNotifications(int id)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound();
            }

            if (channel.Notifications == null)
            {
                channel.Notifications = true;
            }
            else
            {
                channel.Notifications = !channel.Notifications;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChannelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPut("TogglePromoPost/{id}")]
        public async Task<IActionResult> TogglePromoPost(int id)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound();
            }

            if (channel.PromoPost == null)
            {
                channel.PromoPost = true;
            }
            else
            {
                channel.PromoPost = !channel.PromoPost;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChannelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        [HttpPut("UpdatePromoPostDetails/{id}")]
        public async Task<IActionResult> UpdatePromoPostDetails(int id, [FromBody] UpdatePromoDTO dto)
        {
            try
            {
                var channel = await _context.Channels.FindAsync(id);

                if (channel == null)
                {
                    return NotFound(); // Channel with the provided ID not found
                }

                // Convert the PromoPostTime string to TimeOnly
                if (TimeOnly.TryParse(dto.PromoPostTime, out var timeOnly))
                {
                    channel.PromoPostTime = timeOnly;
                }
                else
                {
                    return BadRequest("Invalid PromoPostTime format. Please use the 'HH:mm:ss' format.");
                }

                // Update the PromoPostInterval
                if (dto.PromoPostInterval.HasValue && dto.PromoPostInterval > 0)
                {
                    channel.PromoPostInterval = dto.PromoPostInterval;
                }
                else
                {
                    return BadRequest("Invalid PromoPostInterval value. Please provide a positive number.");
                }

                await _context.SaveChangesAsync();

                return Ok(); // Successfully updated the PromoPostDetails
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPut("UpdatePromoPostInterval/{id}")]
        public async Task<IActionResult> UpdatePromoPostInterval(int id, [FromBody] int promoPostInterval)
        {
            try
            {
                var channel = await _context.Channels.FindAsync(id);

                if (channel == null)
                {
                    return NotFound(); // Channel with the provided ID not found
                }

                // Validate that promoPostInterval is a positive value
                if (promoPostInterval <= 0)
                {
                    return BadRequest("Invalid PromoPostInterval. Please use a positive integer value.");
                }

                channel.PromoPostInterval = promoPostInterval;

                await _context.SaveChangesAsync();

                return Ok(); // Successfully updated the PromoPostInterval
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        // POST: api/Channel/Bump/5
        [HttpPost("Bump/{id}")]
        public async Task<IActionResult> BumpChannel(int id)
        {
            var channel = await FindChannelById(id);

            if (channel == null)
            {
                return NotFound();
            }

            var nextBumpTime = _bumpService.CalculateNextBumpTime(channel.LastBump);

            if (_bumpService.IsBumpAvailable(nextBumpTime))
            {
                var remainingTime = _bumpService.GetRemainingTimeInSeconds(nextBumpTime);
                Response.Headers.Add("X-TimeLeft", remainingTime.ToString());
                return BadRequest($"Next bump available in {remainingTime} minutes.");
            }

            _bumpService.UpdateChannelBumpDetails(channel);

            if (!await TrySaveChanges())
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("Subscribe/{id}")]
        public async Task<IActionResult> SubscribeChannel(int id, int subtypeId)
        {
            var channel = await FindChannelById(id);
            if (channel == null) return NotFound();

            var currentServerTime = _subscriptionService.GetCurrentServerTime();
            var existingSubscription = await _subscriptionService.GetExistingSubscription(id, subtypeId, currentServerTime);

            if (existingSubscription != null)
            {
                await _subscriptionService.ExtendExistingSubscription(existingSubscription);
                return Ok($"Subscription for channel {id} has been extended by 1 month with subscription type {existingSubscription.Type.Name}.");
            }

            var subscriptionType = await _subscriptionService.GetSubscriptionType(subtypeId);
            if (subscriptionType == null) return BadRequest("Invalid subscription type.");

            await _subscriptionService.AddNewSubscription(id, subtypeId, currentServerTime);
            return Ok($"Channel {id} has been subscribed for 1 month with subscription type {subscriptionType.Name}.");
        }

        [HttpGet("Comments/{channelId}")]
        public async Task<ActionResult<List<CommentGetModel>>> GetComments(int channelId)
        {
            var comments = await _context.Comments
                .Where(c => c.ChannelId == channelId)
                .Include(c => c.User)
                .ToListAsync();

            var topLevelComments = comments
                .Where(c => c.ParentId == null)
                .ToList();

            var resultList = new List<CommentGetModel>();

            foreach (var comment in topLevelComments)
            {
                var commentGetModel = new CommentGetModel
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    UserId = comment.UserId,
                    ChannelId = comment.ChannelId,
                    ParentId = comment.ParentId,
                    CreatedAt = comment.CreatedAt,
                    Username = comment.User?.Username ?? string.Empty,
                    Rating = comment.Rating,
                    Replies = comments
                                .Where(c => c.ParentId == comment.Id)
                                .Select(c => new ReplyGetModel
                                {
                                    Id = c.Id,
                                    Content = c.Content,
                                    UserId = c.UserId,
                                    ChannelId = c.ChannelId,
                                    ParentId = c.ParentId,
                                    CreatedAt = c.CreatedAt,
                                    Username = c.User?.Username ?? string.Empty
                                })
                                .ToList()
                };

                resultList.Add(commentGetModel);
            }

            return Ok(resultList);
        }

        [HttpPut("{id}/flag")]
        public async Task<IActionResult> UpdateFlag(int id, [FromBody] string flag)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound("Channel not found");
            }

            channel.Flag = flag;

            _context.Channels.Update(channel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Endpoint to update language
        [HttpPut("{id}/language")]
        public async Task<IActionResult> UpdateLanguage(int id, [FromBody] string language)
        {
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound("Channel not found");
            }

            channel.Language = language;

            _context.Channels.Update(channel);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // GET: api/Channel/ByUser/5
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannelsByUser(int userId)
        {
            var channels = await _context.Channels.Where(c => c.User == userId).ToListAsync();

            if (channels == null || channels.Count == 0)
            {
                return NotFound();
            }

            channels = channels.OrderBy(x => x.Id).ToList();

            return channels;
        }

        [HttpGet("ByTelegramId/{telegramId}")]
        public async Task<ActionResult<Channel>> GetChannelByTelegramId(long telegramId)
        {
            var channel = await _context.Channels.FirstOrDefaultAsync(c => c.TelegramId == telegramId);

            if (channel == null)
            {
                return NotFound();
            }

            return channel;
        }

        // PUT: api/Channel/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChannel(int id, Channel channel)
        {
            if (id != channel.Id)
            {
                return BadRequest();
            }

            _context.Entry(channel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChannelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Channel
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Channel>> PostChannel(ChannelPostModel channel)
        {
          if (_context.Channels == null)
          { 
              return Problem("Entity set 'TgCheckerDbContext.Channels'  is null.");
          }
            _context.Channels.Add(channel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChannel", new { id = channel.Id }, channel);
        }

        // DELETE: api/Channel/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChannel(int id)
        {
            if (_context.Channels == null)
            {
                return NotFound();
            }
            var channel = await _context.Channels.FindAsync(id);
            if (channel == null)
            {
                return NotFound();
            }

            _context.Channels.Remove(channel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [RequiresJwtValidation]
        [HttpPost("Report/{id}")]
        public async Task<ActionResult<Channel>> ReportChannel(int id, ReportPostModel report)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;
            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (_context.Reports == null)
            {
                return Problem("Entity set 'TgCheckerDbContext.Reports' is null.");
            }

            report.UserId = user.Id;
            report.ReportTime = DateTime.Now;
            report.ChannelId = id;

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            var reportGetModel = _mapper.Map<ReportGetModel>(report);

            reportGetModel.ReporteeName = user.Username;

            await _hubContext.Clients.All.SendAsync("ReceiveReport", reportGetModel);



            return Ok();
        }

        private async Task<Channel> FindChannelById(int id)
        {
            return await _context.Channels.FindAsync(id);
        }

        private async Task<bool> TrySaveChanges()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Here, you can further improve by adding logging or other handling as per your application needs
            return false;
        }
        }

        private bool ChannelExists(int id)
        {
            return (_context.Channels?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
