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
    }
}
