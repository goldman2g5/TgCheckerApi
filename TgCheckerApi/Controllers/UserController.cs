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

namespace TgCheckerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly TgDbContext _context;
        private readonly IMapper _mapper;


        public UserController(TgDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/User
        [HttpGet("/GetMe")]
        public async Task<ActionResult<UserProfileModel>> GetCurrentUserProfile(string token)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("GoIdAdObEyTeViZhEvShIh"));

            if (!IsValidToken(token, key, out ClaimsPrincipal claimsPrincipal))
            {
                return BadRequest("Invalid token.");
            }

            var uniqueKeyClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "key")?.Value;

            if (string.IsNullOrEmpty(uniqueKeyClaim))
            {
                return BadRequest("Token does not contain a unique key claim.");
            }

            var user = await GetUserWithRelations(uniqueKeyClaim);

            if (user == null)
            {
                return NotFound("User does not exist");
            }

            var userProfile = new UserProfileModel
            {
                Channels = _mapper.Map<IEnumerable<ChannelGetModel>>(user.ChannelAccesses.Select(ca => ca.Channel)).ToList()
            };

            return userProfile;

        }

        private bool IsValidToken(string token, SymmetricSecurityKey key, out ClaimsPrincipal claimsPrincipal)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                claimsPrincipal = null;
                return false;
            }

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false
            };

            try
            {
                claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out var rawValidatedToken);
                return true;
            }
            catch (SecurityTokenException)
            {
                claimsPrincipal = null;
                return false;
            }
        }

        private async Task<User> GetUserWithRelations(string uniqueKeyClaim)
        {
            return await _context.Users
                                   .Include(u => u.ChannelAccesses)
                                   .ThenInclude(ca => ca.Channel)
                                   .ThenInclude(c => c.ChannelHasTags)
                                   .ThenInclude(cht => cht.TagNavigation)
                                   .SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);
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
