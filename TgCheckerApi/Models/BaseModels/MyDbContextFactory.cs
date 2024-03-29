using Microsoft.EntityFrameworkCore;

namespace TgCheckerApi.Models.BaseModels
{
    public class MyDbContextFactory : IDbContextFactory<TgDbContext>
    {
        private readonly DbContextOptions<TgDbContext> _options;

        public MyDbContextFactory(DbContextOptions<TgDbContext> options)
        {
            _options = options;
        }

        public TgDbContext CreateDbContext()
        {
            return new TgDbContext(_options);
        }
    }
}
