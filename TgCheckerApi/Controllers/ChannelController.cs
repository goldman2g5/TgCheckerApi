﻿using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Utility;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly ChannelUtility _channelUtility;

        public ChannelController(TgDbContext context)
        {
            _context = context;
            _channelUtility = new ChannelUtility(context);
        }

        // GET: api/Channel
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChannelGetModel>>> GetChannels()
        {
            var channels = await _context.Channels
                .OrderByDescending(channel => channel.Bumps)
                .ToListAsync();

            if (channels == null)
            {
                return NotFound();
            }

            var channelGetModels = new List<ChannelGetModel>();

            foreach (var channel in channels)
            {
                var channelGetModel = new ChannelGetModel
                {
                    Id = channel.Id,
                    Name = channel.Name,
                    Description = channel.Description,
                    Members = channel.Members,
                    Avatar = channel.Avatar,
                    User = channel.User,
                    Notifications = channel.Notifications,
                    Bumps = channel.Bumps,
                    LastBump = channel.LastBump,
                    TelegramId = channel.TelegramId,
                    NotificationSent = channel.NotificationSent,
                    Tags = new List<string>()
                };

                var channelHasTags = await _context.ChannelHasTags
                    .Include(cht => cht.TagNavigation)
                    .Where(cht => cht.Channel == channel.Id)
                    .ToListAsync();

                foreach (var channelHasTag in channelHasTags)
                {
                    var tagText = channelHasTag.TagNavigation?.Text;
                    if (!string.IsNullOrEmpty(tagText))
                    {
                        channelGetModel.Tags.Add(tagText);
                    }
                }

                channelGetModels.Add(channelGetModel);
            }

            return channelGetModels;
        }

        // GET: api/Channel/Page/{page}
        [HttpGet("Page/{page}")]
        public async Task<ActionResult<IEnumerable<ChannelGetModel>>> GetChannels(int page = 1, [FromQuery] string? tags = null, [FromQuery] string? sortOption = null)
        {
            IQueryable<Channel> channelsQuery = _context.Channels;
            int PageSize = ChannelUtility.GetPageSize();

            channelsQuery = _channelUtility.ApplyTagFilter(channelsQuery, tags);
            channelsQuery = _channelUtility.ApplySort(channelsQuery, sortOption);

            var totalChannelCount = await channelsQuery.CountAsync();

            channelsQuery = channelsQuery.Skip((page - 1) * PageSize).Take(PageSize);
            var channels = await channelsQuery.ToListAsync();

            if (!channels.Any())
            {
                return NotFound();
            }

            var channelGetModels = channels.Select(channel => _channelUtility.MapToChannelGetModel(channel)).ToList();
            int totalPages = (int)Math.Ceiling((double)totalChannelCount / PageSize);

            Response.Headers.Add("X-Total-Pages", totalPages.ToString());

            return channelGetModels;
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

            var existingTags = await _context.ChannelHasTags
                .Where(cht => cht.Channel == id)
                .ToListAsync();

            _context.ChannelHasTags.RemoveRange(existingTags);

            foreach (var tagText in tags)
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Text == tagText);
                if (tag == null)
                {
                    tag = new Tag { Text = tagText };
                    _context.Tags.Add(tag);
                }

                var channelHasTag = new ChannelHasTag { Channel = id, Tag = tag.Id };
                _context.ChannelHasTags.Add(channelHasTag);
            }

            await _context.SaveChangesAsync();

            return NoContent();
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

        // GET: api/Channel/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Channel>> GetChannel(int id)
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

            return channel;
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
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound();
            }

            // Define the interval between bumps (in minutes)
            int bumpIntervalMinutes = 1;

            // Calculate the minimum time for the next bump to be available
            DateTime nextBumpTime = channel.LastBump?.AddMinutes(bumpIntervalMinutes) ?? DateTime.MinValue;

            // Check if the current time is before the next bump time
            if (DateTime.Now < nextBumpTime)
            {
                // Calculate the remaining time until the next bump is available
                var remainingTime = (int)(nextBumpTime - DateTime.Now).TotalSeconds;
                Response.Headers.Add("X-TimeLeft", remainingTime.ToString());

                return BadRequest($"Next bump available in {bumpIntervalMinutes} minutes.");
            }

            // Increment the bumps value by 1
            channel.Bumps = (channel.Bumps ?? 0) + 1;

            // Update the last bump time to the current time
            channel.LastBump = DateTime.Now;

            // Set the NotificationSent property to false
            channel.NotificationSent = false;

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

        [HttpPost("Subscribe/{id}")]
        public async Task<IActionResult> SubscribeChannel(int id, int subtypeId)
        {
            // Find the channel based on the provided ID
            var channel = await _context.Channels.FindAsync(id);

            if (channel == null)
            {
                return NotFound();
            }

            // Get the time zone for Russia/Moscow
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

            // Convert the current time to the server's time zone
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // Check if the channel already has an active subscription of the same type
            var existingSubscription = await _context.ChannelHasSubscriptions
                .FirstOrDefaultAsync(s => s.ChannelId == id && s.Expires > now && s.TypeId == subtypeId);

            if (existingSubscription != null)
            {
                // Extend the expiration date by 1 month
                existingSubscription.Expires = existingSubscription.Expires.Value.AddMinutes(1);
                await _context.SaveChangesAsync();

                return Ok($"Subscription for channel {id} has been extended by 1 month with subscription type {existingSubscription.Type.Name}.");
            }

            // Get the subscription type based on subtypeId
            var subscriptionType = await _context.SubTypes.FirstOrDefaultAsync(s => s.Id == subtypeId);

            if (subscriptionType == null)
            {
                return BadRequest("Invalid subscription type.");
            }

            // Calculate the expiration date for the subscription (1 month from now)
            var expirationDate = now.AddMinutes(1);

            // Create a new subscription record
            var subscription = new ChannelHasSubscription
            {
                TypeId = subscriptionType.Id,
                Expires = expirationDate,
                ChannelId = id
            };

            _context.ChannelHasSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return Ok($"Channel {id} has been subscribed for 1 month with subscription type {subscriptionType.Name}.");
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

        private bool ChannelExists(int id)
        {
            return (_context.Channels?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
