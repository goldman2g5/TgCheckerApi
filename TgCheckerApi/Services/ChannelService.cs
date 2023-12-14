﻿using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using TgCheckerApi.Models.BaseModels;
using TgCheckerApi.Models.GetModels;

namespace TgCheckerApi.Utility
{
    public class ChannelService
    {
        private const string DefaultSortOption = "popularity";
        private const int PageSize = 50;
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
                query = query.Include(channel => channel.ChannelHasTags).Where(channel =>channel.Name.Contains(search) || channel.ChannelHasTags.Any(cht => cht.TagNavigation.Text.ToLower().Contains(search.ToLower())));
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
                Language = channel.Language,
                Url = channel.Url,
                NotificationSent = channel.NotificationSent,
                Tags = GetChannelTags(channel),
                
            };
            if (channel.ChannelHasSubscriptions.Any())
            {
                channelGetModel.subType = channel.ChannelHasSubscriptions.FirstOrDefault().TypeId;
            }
            else
            {
                channelGetModel.subType = 0;
            }
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

        public IQueryable<Channel> ApplyLanguageFilter(IQueryable<Channel> query, string? language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(c => c.Language == language);
            }

            return query;
        }

        public static int GetPageSize() => PageSize;
    }
}
