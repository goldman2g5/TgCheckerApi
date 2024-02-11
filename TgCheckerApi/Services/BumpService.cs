using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Utility
{
    public class BumpService
    {
        private const int BumpIntervalMinutes = 3;

        public DateTime CalculateNextBumpTime(DateTime? lastBump)
        {
            return lastBump?.AddMinutes(BumpIntervalMinutes) ?? DateTime.MinValue;
        }

        public bool IsBumpAvailable(DateTime nextBumpTime)
        {
            return DateTime.Now >= nextBumpTime;
        }

        public int GetRemainingTimeInSeconds(DateTime nextBumpTime)
        {
            return (int)(nextBumpTime - DateTime.Now).TotalSeconds;
        }

        public void UpdateChannelBumpDetails(Channel channel, decimal multiplier)
        {
            // Apply the multiplier to the bump increment
            var bumpIncrement = 1 * multiplier;

            // Update channel details
            channel.Bumps = (channel.Bumps ?? 0) + bumpIncrement;
            channel.LastBump = DateTime.Now;
            channel.NotificationSent = false;
        }

        private static int GetBumpInterval() => BumpIntervalMinutes;
    }
}
