using Quartz;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Job
{
    public class RecalculateTopPosJob : IJob
    {
        private readonly TgDbContext _dbContext;
        private readonly ILogger<RecalculateTopPosJob> _logger;

        public RecalculateTopPosJob(TgDbContext dbContext, ILogger<RecalculateTopPosJob> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("RecalculateTopPosJob started at {Time}", DateTimeOffset.Now);
            try
            {
                // Fetch all channels, ordered by Bumps
                var channels = await _dbContext.Channels
                    .OrderByDescending(x => x.Bumps)
                    .ToListAsync();

                // Update TopPos
                for (int i = 0; i < channels.Count; i++)
                {
                    channels[i].TopPos = i + 1; // Assign ranking
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing RecalculateTopPosJob.");
            }
            _logger.LogInformation("RecalculateTopPosJob completed at {Time}", DateTimeOffset.Now);
        }
    }
}
