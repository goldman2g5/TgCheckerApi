using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Utility
{
    public class SubscriptionService
    {
        private const string RussianStandardTimeZone = "Russian Standard Time";
        private const int SubscriptionDurationMinutes = 1;

        private readonly TgDbContext _context;  // Replace with your actual context type

        public SubscriptionService(TgDbContext context)
        {
            _context = context;
        }

        public DateTime GetCurrentServerTime()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(RussianStandardTimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }

        public async Task<ChannelHasSubscription> GetExistingSubscription(int channelId, int subtypeId, DateTime currentTime)
        {
            return await _context.ChannelHasSubscriptions
                .FirstOrDefaultAsync(s => s.ChannelId == channelId && s.Expires > currentTime && s.TypeId == subtypeId);
        }

        public async Task ExtendExistingSubscription(ChannelHasSubscription subscription)
        {
            subscription.Expires = subscription.Expires.Value.AddMinutes(SubscriptionDurationMinutes);
            await _context.SaveChangesAsync();
        }

        public async Task<SubType> GetSubscriptionType(int subtypeId)
        {
            return await _context.SubTypes.FirstOrDefaultAsync(s => s.Id == subtypeId);
        }

        public async Task AddNewSubscription(int channelId, int subtypeId, DateTime currentServerTime)
        {
            var subscription = new ChannelHasSubscription
            {
                TypeId = subtypeId,
                Expires = currentServerTime.AddMinutes(SubscriptionDurationMinutes),
                ChannelId = channelId
            };

            _context.ChannelHasSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
