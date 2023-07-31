﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : Controller
    {

        private readonly TgDbContext _context;

        public SubscriptionController(TgDbContext context)
        {
            _context = context;
        }

        // GET: api/Subscription/Types
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubType>>> GetSubscriptionTypes()
        {
            if (_context.SubTypes == null)
            {
                return NotFound();
            }
            return await _context.SubTypes.ToListAsync();
        }

        // POST: api/Subscription/CheckExpiredSubscriptions
        [HttpGet("CheckExpiredSubscriptions")]
        public async Task<ActionResult<IEnumerable<Notification>>> CheckExpiredSubscriptions()
        {
            DateTime currentTime = DateTime.Now;

            // Get expired subscriptions
            var expiredSubscriptions = await _context.ChannelHasSubscriptions
                .Include(s => s.Channel)
                .Include(s => s.Type)
                .Where(s => s.Expires != null && s.Expires <= DateTime.Now)
                .ToListAsync();

            Console.WriteLine(expiredSubscriptions.Count());

            // Get channel access IDs with expired subscriptions
            var channelAccessIds = expiredSubscriptions.Select(s => s.ChannelId).Distinct().ToList();

            // Generate notifications for expired subscriptions
            var notifications = await _context.ChannelAccesses
                .Include(ca => ca.Channel)
                .Include(ca => ca.User)
                .Where(ca => channelAccessIds.Contains(ca.ChannelId))
                .Select(ca => new Notification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    ChannelId = ca.Channel.Id,
                    TelegramUserId = (int)ca.User.TelegramId,
                    TelegramChatId = (int)ca.User.ChatId,
                    TelegamChannelId = (long)ca.Channel.TelegramId
                })
                .ToListAsync();

            // Delete expired subscriptions
            _context.ChannelHasSubscriptions.RemoveRange(expiredSubscriptions);
            await _context.SaveChangesAsync();

            return notifications;
        }
    }
}