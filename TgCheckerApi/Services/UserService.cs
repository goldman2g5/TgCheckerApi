using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Services
{
    public class UserService
    {
        private readonly TgDbContext _context;

        public UserService(TgDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserWithRelations(string uniqueKeyClaim)
        {
            return await _context.Users
                           .Include(u => u.ChannelAccesses)
                           .ThenInclude(ca => ca.Channel)
                           .ThenInclude(c => c.ChannelHasTags)
                           .ThenInclude(cht => cht.TagNavigation)
                           .Include(u => u.Comments)
                           .ThenInclude(c => c.Channel)
                           .SingleOrDefaultAsync(u => u.UniqueKey == uniqueKeyClaim);
        }

        public async Task<bool> IsUserAdminByTelegramId(long telegramId)
        {
            return await _context.Admins.AnyAsync(a => a.TelegramId == telegramId);
        }


        public bool UserHasAccessToChannel(User user, Channel channel)
        {
            return user.ChannelAccesses.Any(ca => ca.ChannelId == channel.Id);
        }
    }
}
