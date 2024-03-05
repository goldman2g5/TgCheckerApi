using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Utility
{
    public class TagsService : ITagsService
    {
        private readonly TgDbContext _context;

        public TagsService(TgDbContext context)
        {
            _context = context;
        }

        public async Task RemoveExistingTagsFromChannel(int channelId)
        {
            var existingTags = await _context.ChannelHasTags
                .Where(cht => cht.Channel == channelId)
                .ToListAsync();

            _context.ChannelHasTags.RemoveRange(existingTags);
        }

        public async Task AddNewTagsToChannel(int channelId, List<string> tags)
        {
            var tagEntities = await GetOrCreateTags(tags);
            var channelTags = tagEntities.Select(t => new ChannelHasTag { Channel = channelId, Tag = t.Id }).ToList();

            _context.ChannelHasTags.AddRange(channelTags);
        }

        public async Task<List<Tag>> GetOrCreateTags(List<string> tags)
        {
            var tagEntities = new List<Tag>();

            foreach (var tagText in tags)
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Text == tagText);
                if (tag == null)
                {
                    tag = new Tag { Text = tagText };
                    _context.Tags.Add(tag);
                }

                tagEntities.Add(tag);
            }

            return tagEntities;
        }

    }
}
