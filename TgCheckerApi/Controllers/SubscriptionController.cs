using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.Models.NotificationModels;
using TgCheckerApi.Models.PostModels;
using TgCheckerApi.Services;
using TgCheckerApi.Utility;

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : Controller
    {

        private readonly TgDbContext _context;
        private readonly UserService _userService;
        private readonly SubscriptionService _subService;

        public SubscriptionController(TgDbContext context)
        {
            _context = context;
            _userService = new UserService(context);
            _subService = new SubscriptionService(context);
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
            var payment = await _context.TelegramPayments.FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        [RequiresJwtValidation]
        [BypassApiKey]
        [HttpPost("CreatePayment")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;
            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (user is null)
            {
                return Unauthorized();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Calculate the price using the GetSubscriptionPricing method.
                    // Assumes 'Discount' is a boolean indicating whether the price is discounted.
                    int price = _subService.GetSubscriptionPricing(request.SubscriptionTypeId, request.Duration, (request.Discount == 0));

                    if (price <= 0)
                    {
                        return BadRequest("Invalid details");
                    }

                    var payment = new TelegramPayment
                    {
                        SubscriptionTypeId = request.SubscriptionTypeId,
                        Duration = request.Duration,
                        AutoRenewal = request.AutoRenewal,
                        Discount = request.Discount,
                        Price = price,
                        ChannelId = request.ChannelId,
                        ChannelName = request.ChannelName,
                        UserId = user.Id,
                        Username = user.Username,
                        Expires = CalculateExpiryDate(request.Duration),
                        Status = "pending"
                    };

                    await _context.TelegramPayments.AddAsync(payment);
                    await _context.SaveChangesAsync();
                    return Ok(payment.Id);
                }           
            }
            catch (Exception e)
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
