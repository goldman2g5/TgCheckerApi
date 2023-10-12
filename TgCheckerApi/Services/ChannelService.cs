﻿using System.Data.Entity;
using System.Linq.Expressions;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;

namespace TgCheckerApi.Utility
{
    public class ChannelService
    {
        private const string DefaultSortOption = "popularity";
        private const int PageSize = 10;
        private readonly TgDbContext _context;

        public ChannelService(TgDbContext context)
        {
            _context = context;
        }

        public IQueryable<Channel> ApplyTagFilter(IQueryable<Channel> query, string? tags)
        {
            if (!string.IsNullOrEmpty(tags))
            {
                string[] tagList = tags.Split(',').Select(tag => tag.Trim()).ToArray();
                return query.Where(channel => channel.ChannelHasTags.Any(cht => tagList.Contains(cht.TagNavigation.Text)));
            }
            return query;
        }


        public IQueryable<Channel> ApplySort(IQueryable<Channel> query, string sortOption, bool ascending)
        {
            var sortOptions = new Dictionary<string, Expression<Func<Channel, object>>>
                {
        {"members", channel => channel.Members},
        {"activity", channel => channel.LastBump},
        {"popularity", channel => channel.Bumps}
    };



            string effectiveSortOption = sortOption ?? DefaultSortOption;
            var orderFunc = sortOptions.ContainsKey(effectiveSortOption)
                ? sortOptions[effectiveSortOption]
                : sortOptions[DefaultSortOption];

            return ascending ? query.OrderBy(orderFunc) : query.OrderByDescending(orderFunc);
        }

        public IQueryable<Channel> ApplySearch(IQueryable<Channel> query, string search)
        {
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(channel => channel.Name.Contains(search));
            }
            return query;
        }

        public ChannelGetModel MapToChannelGetModel(Channel channel)
        {
            var channelGetModel = new ChannelGetModel
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
                Tags = GetChannelTags(channel)
            };
            return channelGetModel;
        }

        public async Task<List<string>> GetChannelTagsAsync(int channelId)
        {
            return await _context.ChannelHasTags
                                 .Include(cht => cht.TagNavigation)
                                 .Where(cht => cht.Channel == channelId)
                                 .Select(cht => cht.TagNavigation.Text)
                                 .Where(tagText => !string.IsNullOrEmpty(tagText))
                                 .ToListAsync();
        }

        private List<string> GetChannelTags(Channel channel)
        {
            var tags = new List<string>();
            var channelHasTags = _context.ChannelHasTags
                .Include(cht => cht.TagNavigation)
                .Where(cht => cht.Channel == channel.Id)
                .ToList();

            foreach (var channelHasTag in channelHasTags)
            {
                var tagText = channelHasTag.TagNavigation?.Text;
                if (!string.IsNullOrEmpty(tagText))
                {
                    tags.Add(tagText);
                }
            }
            return tags;
        }

        public static int GetPageSize() => PageSize;
    }
}
