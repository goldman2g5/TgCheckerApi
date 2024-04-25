using System.Threading.Tasks;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Interfaces
{
    public interface IElasticsearchIndexingService
    {
        Task IndexChannelsAsync(List<Channel> channels);

        Task RecreateIndexAsync();

        Task InitializeIndicesAsync();

    }
}
