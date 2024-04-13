using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TgCheckerApi.Models.BaseModels
{
    public class MyDbContextFactory : IDbContextFactory<TgDbContext>
    {
        private readonly DbContextOptions<TgDbContext> _options;
        private readonly IServiceProvider _serviceProvider;

        public MyDbContextFactory(DbContextOptions<TgDbContext> options, IServiceProvider serviceProvider)
        {
            _options = options;
            _serviceProvider = serviceProvider;
        }

        public TgDbContext CreateDbContext()
        {
            return new TgDbContext(_options, _serviceProvider);
        }
    }
}
