using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserService _userService;
        private readonly SubscriptionService _subscriptionService;


        public UserController(TgDbContext context, IMapper mapper)
        {
            _context = context;
            _userService = new UserService(context);
            _subscriptionService = new SubscriptionService(context);
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

        [HttpPost("Subscribe/{id}")]
        public async Task<IActionResult> SubscribeUser(int id, int subtypeId)
        {
            var user = await FindChannelById(id);
            if (user == null) return NotFound();

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

        private async Task<User> FindChannelById(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        private bool UserExists(int id)
        {
            return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
