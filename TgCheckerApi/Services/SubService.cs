using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace TgCheckerApi.Utility
{
    public class SubscriptionService
    {
        private const string RussianStandardTimeZone = "Russian Standard Time";
        private const int SubscriptionDurationMinutes = 1;
        public static Dictionary<int, PriceDetail> LiteSubPricing = new Dictionary<int, PriceDetail>
        {
            {7, new PriceDetail { DefaultPrice = 252, DiscountedPrice = 227 }},
            {14, new PriceDetail { DefaultPrice = 446, DiscountedPrice = 402 }},
            {30, new PriceDetail { DefaultPrice = 790, DiscountedPrice = 711 }},
            {90, new PriceDetail { DefaultPrice = 2252, DiscountedPrice = 2026 }},
            {180, new PriceDetail { DefaultPrice = 4266, DiscountedPrice = 3839 }},
            {365, new PriceDetail { DefaultPrice = 8058, DiscountedPrice = 7252 }}
        };

        public static Dictionary<int, PriceDetail> ProSubPricing = new Dictionary<int, PriceDetail>
        {
            {7, new PriceDetail { DefaultPrice = 476, DiscountedPrice = 428 }},
            {14, new PriceDetail { DefaultPrice = 842, DiscountedPrice = 758 }},
            {30, new PriceDetail { DefaultPrice = 1490, DiscountedPrice = 1341 }},
            {90, new PriceDetail { DefaultPrice = 4247, DiscountedPrice = 3822 }},
            {180, new PriceDetail { DefaultPrice = 8046, DiscountedPrice = 7241 }},
            {365, new PriceDetail { DefaultPrice = 15198, DiscountedPrice = 13678 }}

        };

        public static Dictionary<int, PriceDetail> SuperSubPricing = new Dictionary<int, PriceDetail>
        {
            {7, new PriceDetail { DefaultPrice = 894, DiscountedPrice = 804 }},
            {14, new PriceDetail { DefaultPrice = 1581, DiscountedPrice = 1423 }},
            {30, new PriceDetail { DefaultPrice = 2799, DiscountedPrice = 2519 }},
            {90, new PriceDetail { DefaultPrice = 7977, DiscountedPrice = 7179 }},
            {180, new PriceDetail { DefaultPrice = 15115, DiscountedPrice = 13603 }},
            {365, new PriceDetail { DefaultPrice = 28550, DiscountedPrice = 25695 }}
        };



        private readonly TgDbContext _context;  // Replace with your actual context type

        public SubscriptionService(TgDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> HandleSubscription(int channelId, int subtypeId, int durationInDays, int userId)
        {
            var channel = await FindChannelById(channelId);
            if (channel == null) return new NotFoundResult();

            var currentServerTime = GetCurrentServerTime();
            var existingSubscription = await GetExistingSubscription(channelId, subtypeId, currentServerTime);

            if (existingSubscription != null)
            {
                await ExtendExistingSubscription(existingSubscription, durationInDays);
                return new OkObjectResult($"Subscription for channel {channelId} has been extended by {durationInDays} days with subscription type {existingSubscription.Type.Name}.");
            }

            var subscriptionType = await GetSubscriptionType(subtypeId);
            if (subscriptionType == null) return new BadRequestObjectResult("Invalid subscription type.");

            await AddNewSubscription(channelId, subtypeId, currentServerTime, durationInDays);
            return new OkObjectResult($"Channel {channelId} has been subscribed for {durationInDays} days with subscription type {subscriptionType.Name}.");
        }

        // Helper method to find a channel by its ID
        private async Task<Channel> FindChannelById(int channelId)
        {
            return await _context.Channels.FindAsync(channelId);
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

        public async Task<SubType> GetSubscriptionType(int subtypeId)
        {
            return await _context.SubTypes.FirstOrDefaultAsync(s => s.Id == subtypeId);
        }

        public async Task ExtendExistingSubscription(ChannelHasSubscription subscription, int durationInDays = SubscriptionDurationMinutes)
        {
            subscription.Expires = subscription.Expires.Value.AddDays(durationInDays);
            await _context.SaveChangesAsync();
        }

        public async Task AddNewSubscription(int channelId, int subtypeId, DateTime currentServerTime, int durationInDays = SubscriptionDurationMinutes)
        {
            var subscription = new ChannelHasSubscription
            {
                TypeId = subtypeId,
                Expires = currentServerTime.AddDays(durationInDays),
                ChannelId = channelId
            };

            _context.ChannelHasSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
        }

        public int GetSubscriptionPricing(int subtypeId, int duration, bool isDiscounted)
        {
            Dictionary<int, PriceDetail> pricing = null;

            switch (subtypeId)
            {
                case 1:
                    pricing = LiteSubPricing;
                    break;
                case 2:
                    pricing = ProSubPricing;
                    break;
                case 3:
                    pricing = SuperSubPricing;
                    break;
            }

            if (pricing != null && pricing.TryGetValue(duration, out PriceDetail detail))
            {
                return isDiscounted ? detail.DiscountedPrice : detail.DefaultPrice;
            }

            return 0;
        }
    }
}
