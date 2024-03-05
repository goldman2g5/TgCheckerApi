using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Utility
{
    public interface ITagsService
    {
        Task AddNewTagsToChannel(int channelId, List<string> tags);
        Task<List<Tag>> GetOrCreateTags(List<string> tags);
        Task RemoveExistingTagsFromChannel(int channelId);
    }
}