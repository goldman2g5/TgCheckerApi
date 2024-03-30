using WTelegram;
using TgCheckerApi.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using TgCheckerApi.Utility;

namespace TgCheckerApi.Services
{
    public class TelegramClientService
    {
        private readonly TgClientFactory _tgClientFactory;
        private readonly IDbContextFactory<TgDbContext> _dbContextFactory;
        public static List<TelegramClientWrapper> Clients { get; private set; } = new List<TelegramClientWrapper>();

        public TelegramClientService(TgClientFactory tgClientFactory, IDbContextFactory<TgDbContext> dbContextFactory)
        {
            _tgClientFactory = tgClientFactory;
            _dbContextFactory = dbContextFactory;
        }

        public async Task InitializeAsync()
        {
            var clientConfigs = await _tgClientFactory.FetchClientConfigsAsync();

            foreach (var clientConfig in clientConfigs)
            {
                var client = new Client(clientConfig.Config);
                var loginInfo = clientConfig.PhoneNumber;

                while (client.User == null)
                {
                    switch (await client.Login(loginInfo))
                    {
                        case "verification_code":
                            Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                        default: loginInfo = null; break;
                    }
                }
                Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");

                // Store the client with its database ID
                Clients.Add(new TelegramClientWrapper(client, clientConfig.DatabaseId));
            }

            await SyncClientsToChannelsAsync();
        }

        public async Task SyncClientsToChannelsAsync()
        {
            foreach (var wrapper in Clients)
            {
                var chats = await wrapper.Client.Messages_GetAllChats();
                using var dbContext = _dbContextFactory.CreateDbContext();

                List<Channel> channelsToUpdate = new List<Channel>();

                foreach (var (id, chat) in chats.chats)
                {
                    if (!chat.IsActive) continue;
                    var pyrogramChannelId = TelegramIdConverter.FromWTelegramClientToPyrogram(id);

                    var channel = await dbContext.Channels
                                    .Where(c => c.TelegramId == pyrogramChannelId)
                                    .FirstOrDefaultAsync();

                    if (channel != null)
                    {
                        channel.TgclientId = wrapper.DatabaseId;
                        channelsToUpdate.Add(channel); 
                    }
                }

                if (channelsToUpdate.Any())
                {
                    dbContext.Channels.UpdateRange(channelsToUpdate);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        async public Task<Client?> GetClientByDatabaseId(int databaseId)
        {
            // Find the first TelegramClientWrapper instance with a matching DatabaseId
            var clientWrapper = Clients.FirstOrDefault(c => c.DatabaseId == databaseId);
            return clientWrapper?.Client;
        }

        public async Task<Client?> GetClientByTelegramId(long telegramChannelId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            // Convert the WTelegram channel ID to the Pyrogram format since the database stores IDs in Pyrogram format.
            var pyrogramChannelId = TelegramIdConverter.FromWTelegramClientToPyrogram(telegramChannelId);

            // Find the Channel entity by its TelegramId
            var channel = await dbContext.Channels
                .Where(c => c.TelegramId == pyrogramChannelId)
                .FirstOrDefaultAsync();

            if (channel == null) return null; // Channel not found

            // Find the TelegramClientWrapper instance with a matching DatabaseId
            var clientWrapper = Clients.FirstOrDefault(c => c.DatabaseId == channel.TgclientId);
            return clientWrapper?.Client;
        }
        public static void DisposeClients()
        {
            foreach (var client in Clients)
            {
                if (client is IDisposable disposableClient)
                {
                    disposableClient.Dispose();
                }
            }
        }
    }
}
