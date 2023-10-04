﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        public UserController(TgDbContext context)
        {
            _context = context;
        }

        // GET: api/User
        [HttpGet("/GetMe")]
        public async Task<ActionResult<UserProfileModel>> GetMotherfucker(string token)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("GoIdAdObEyTeViZhEvShIh"));
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                return BadRequest("Invalid token.");
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
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out var rawValidatedToken);
                var uniqueKeyClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "key")?.Value;

                if (uniqueKeyClaim == null)
                {
                    return BadRequest("Token does not contain a unique key claim.");
                }

                var user = await _context.Users
                                        .Include(u => u.ChannelAccesses)
                                        .ThenInclude(ca => ca.Channel)
                                        .ThenInclude(c => c.ChannelHasTags)
                                        .ThenInclude(cht => cht.TagNavigation)
                                        .SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);

                if (user != null)
                {

                    var userProfile = new UserProfileModel
                    {
                        Channels = await GetUserChannels(user)
                    };

                    return userProfile;
                }
                return BadRequest("User does not exist");
            }
            catch (SecurityTokenException)
            {
                return BadRequest("Invalid token.");
            }
        }

        private async Task<List<ChannelGetModel>> GetUserChannels(User user)
        {
            var channels = new List<ChannelGetModel>();

            foreach (var access in user.ChannelAccesses)
            {
                if (access.Channel != null)
                {
                    var channelGetModel = new ChannelGetModel
                    {
                        Id = access.Channel.Id,
                        Name = access.Channel.Name,
                        Description = access.Channel.Description,
                        Members = access.Channel.Members,
                        Avatar = access.Channel.Avatar,
                        User = access.Channel.User,
                        Notifications = access.Channel.Notifications,
                        Bumps = access.Channel.Bumps,
                        LastBump = access.Channel.LastBump,
                        TelegramId = access.Channel.TelegramId,
                        NotificationSent = access.Channel.NotificationSent,
                        PromoPost = access.Channel.PromoPost,
                        PromoPostTime = access.Channel.PromoPostTime,
                        PromoPostInterval = access.Channel.PromoPostInterval,
                        PromoPostSent = access.Channel.PromoPostSent,
                        PromoPostLast = access.Channel.PromoPostLast,
                        Language = access.Channel.Language,
                        Flag = access.Channel.Flag,
                        Tags = await GetTags(access.Channel)
                    };
                    channels.Add(channelGetModel);
                }
            }
            return channels;
        }

        private async Task<List<string>> GetTags(Channel channel)
        {
            return channel.ChannelHasTags?.Select(cht => cht.TagNavigation?.Text).Where(t => t != null).ToList() ?? new List<string>();
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
