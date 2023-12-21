using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Models.NotificationModels;
using TgCheckerApi.Models.PostModels;

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
        public async Task<ActionResult<IEnumerable<BumpNotification>>> CheckExpiredSubscriptions()
        {
            DateTime currentTime = DateTime.Now;

            var expiredSubscriptions = await _context.ChannelHasSubscriptions
                .Include(s => s.Channel)
                .Include(s => s.Type)
                .Where(s => s.Expires != null && s.Expires <= DateTime.Now)
                .ToListAsync();

            var channelAccessIds = expiredSubscriptions.Select(s => s.ChannelId).Distinct().ToList();

            var notifications = await _context.ChannelAccesses
                .Include(ca => ca.Channel)
                .Include(ca => ca.User)
                .Where(ca => channelAccessIds.Contains(ca.ChannelId))
                .Select(ca => new BumpNotification
                {
                    ChannelAccess = ca,
                    ChannelName = ca.Channel.Name,
                    ChannelId = ca.Channel.Id,
                    TelegramUserId = (int)ca.User.TelegramId,
                    TelegramChatId = (int)ca.User.ChatId,
                    TelegamChannelId = (long)ca.Channel.TelegramId
                })
                .ToListAsync();

            _context.ChannelHasSubscriptions.RemoveRange(expiredSubscriptions);
            await _context.SaveChangesAsync();

            return notifications;
        }

        [HttpGet("GetPayment/{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        [HttpPost("CreatePayment")]
        public IActionResult Create([FromBody] CreatePaymentRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var payment = new Payment
                    {
                        SubscriptionTypeId = request.SubscriptionTypeId,
                        Duration = request.Duration,
                        AutoRenewal = request.AutoRenewal,
                        Discount = request.Discount,
                        ChannelId = request.ChannelId,
                        ChannelName = request.ChannelName,
                        UserId = request.UserId,
                        Username = request.Username,
                        Expires = CalculateExpiryDate(request.Duration)
                    };
                    payment.Status = "pending";
                    _context.Payments.Add(payment);
                    _context.SaveChanges();
                    return Ok(payment.Id);
                }
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
     
            return BadRequest(ModelState);
        }

        private DateTime CalculateExpiryDate(int duration)
        {
            // Implement logic to calculate the expiry date based on duration
            return DateTime.Now.AddMonths(duration); // Example implementation
        }
    }
}
