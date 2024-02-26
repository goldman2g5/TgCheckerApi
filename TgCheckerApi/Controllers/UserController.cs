using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TgCheckerApi.Models;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;
using TgCheckerApi.MiddleWare;
using TgCheckerApi.Utility;
using TgCheckerApi.Services;

namespace TgCheckerApi.MapperProfiles
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserService _userService;


        public UserController(TgDbContext context, IMapper mapper)
        {
            _context = context;
            _userService = new UserService(context);
        }

        [HttpGet("PaymentHistory")]
        [RequiresJwtValidation]
        public async Task<ActionResult<IEnumerable<PaymentHistoryModel>>> GetPaymentHistory()
        {
            var uniqueKeyClaim = User.FindFirst(c => c.Type == "key")?.Value;

            var user = await _userService.GetUserWithRelations(uniqueKeyClaim);

            if (user == null)
            {
                return Unauthorized(); // Now valid with the ActionResult return type
            }

            int userId = user.Id;

            var paymentHistory = await _context.Payments
                .Where(p => p.UserId == userId && p.Status == "Succeeded")
                .Select(p => new PaymentHistoryModel
                {
                    ChannelId = p.ChannelId,
                    PaymentId = p.Id,
                    ChannelSubscriptionType = p.SubtypeId, // Adjust according to actual subscription type
                    PurchaseDate = p.CreatedAt,
                    SubscriptionDuration = p.Duration,
                    AmountValue = p.AmountValue,
                    AmountCurrency = p.AmountCurrency
                })
                .ToListAsync();

            return paymentHistory;
        }

        [HttpGet("ActiveSubscriptions/{telegramId}")]
        public async Task<ActionResult<IEnumerable<SubscriptionModel>>> GetActiveSubscriptionsByTelegramId(long telegramId)
        {
            // Retrieve the user based on Telegram ID
            var user = await _context.Users
                .Include(u => u.Channels)
                .ThenInclude(c => c.ChannelHasSubscriptions)
                .ThenInclude(s => s.Type)
                .FirstOrDefaultAsync(u => u.TelegramId == telegramId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Filter and project the active subscriptions, prioritizing by highest SubTypeId
            var currentDate = DateTime.UtcNow;
            var activeSubscriptions = user.Channels
                .SelectMany(c => c.ChannelHasSubscriptions)
                .Where(s => s.Expires == null || s.Expires > currentDate)
                .GroupBy(s => s.ChannelId)
                .Select(g => g.OrderByDescending(s => s.TypeId).FirstOrDefault()) // Selecting the highest SubTypeId subscription
                .Where(s => s != null)
                .Select(s => new SubscriptionModel
                {
                    SubscriptionId = s.Id,
                    ChannelId = s.ChannelId ?? 0,
                    ChannelName = s.Channel?.Name,
                    SubscriptionTypeId = s.TypeId ?? 0,
                    SubscriptionTypeName = s.Type?.Name,
                    ExpirationDate = s.Expires
                    // Add other relevant fields as needed
                })
                .ToList();

            return activeSubscriptions;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            return await _context.Users.ToListAsync();
        }

        // GET: api/User/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpGet("ByTelegramId/{telegramId}")]
        public async Task<ActionResult<User>> GetUserByTelegramId(long telegramId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/User/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // POST: api/User
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(UserPostModel user)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'TgCheckerDbContext.Users' is null.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == user.TelegramId);
            if (existingUser != null)
            {
                return BadRequest("User with the same Telegram ID already exists.");
            }

            user.UniqueKey = Guid.NewGuid().ToString();

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // DELETE: api/User/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
