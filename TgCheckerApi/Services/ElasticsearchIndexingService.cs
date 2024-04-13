using Microsoft.EntityFrameworkCore;
using Nest;
using TgCheckerApi.Interfaces;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.DTO;

namespace TgCheckerApi.Services
{
    public class ElasticsearchIndexingService : IElasticsearchIndexingService
    {
        private readonly IElasticClient _elasticClient;
        private readonly TgDbContext _dbContext;

        public ElasticsearchIndexingService(IElasticClient elasticClient, TgDbContext dbContext)
        {
            _elasticClient = elasticClient;
            _dbContext = dbContext;
        }

        public List<ChannelElasticDto> ConvertToDto(IEnumerable<Channel> channels)
        {
            return channels.Select(channel => new ChannelElasticDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Description = channel.Description,
                Members = channel.Members,
                Avatar = channel.Avatar,
                User = channel.User,
                Notifications = channel.Notifications,
                Bumps = channel.Bumps,
                LastBump = channel.LastBump,
                TelegramId = channel.TelegramId,
                NotificationSent = channel.NotificationSent,
                PromoPost = channel.PromoPost,
                PromoPostTime = channel.PromoPostTime,
                PromoPostInterval = channel.PromoPostInterval,
                PromoPostSent = channel.PromoPostSent,
                PromoPostLast = channel.PromoPostLast,
                Language = channel.Language,
                Flag = channel.Flag,
                Url = channel.Url,
                Hidden = channel.Hidden,
                TopPos = channel.TopPos,
                IsPartner = channel.IsPartner,
                TgclientId = channel.TgclientId
            }).ToList();
        }

        public async Task InitializeIndicesAsync()
        {
            var channels = await _dbContext.Channels.ToListAsync();
            if (!channels.Any())
                return;

            // Convert channels to DTOs
            var channelDtos = ConvertToDto(channels);

            // Index DTOs instead of entities
            var response = await _elasticClient.IndexManyAsync(channelDtos);
            if (!response.IsValid)
            {
                // Log error or handle it accordingly
                throw new Exception("Failed to index channels: " + response.DebugInformation);
            }
        }

        public async Task IndexChannelsAsync(List<Channel> channels)
        {
            var channelDtos = ConvertToDto(channels);
            await _elasticClient.BulkAsync(b => b.IndexMany(channelDtos));
            // Handle responses and errors appropriately
        }
    }
}
