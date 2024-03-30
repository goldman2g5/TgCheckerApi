using WTelegram;
using TgCheckerApi.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using TgCheckerApi.Models;
using TgCheckerApi.Utility;
using TL;
using System.Text.RegularExpressions;

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

                // Use a HashSet to track unique channel IDs to avoid duplicates
                HashSet<long> uniqueChannelIds = new HashSet<long>();

                foreach (var (id, chat) in chats.chats)
                {
                    if (!chat.IsActive) continue;
                    var pyrogramChannelId = TelegramIdConverter.FromWTelegramClientToPyrogram(id);
                    uniqueChannelIds.Add(pyrogramChannelId);

                    var channel = await dbContext.Channels
                                    .Where(c => c.TelegramId == pyrogramChannelId)
                                    .FirstOrDefaultAsync();

                    if (channel != null)
                    {
                        channel.TgclientId = wrapper.DatabaseId; // Associate channel with the client in the database
                        dbContext.Channels.Update(channel);
                    }
                }

                // After associating channels, update the ChannelCount for this client in the database
                var tgClient = await dbContext.TgClients.FindAsync(wrapper.DatabaseId);
                if (tgClient != null)
                {
                    tgClient.ChannelCount = uniqueChannelIds.Count; // Set the channel count
                    dbContext.TgClients.Update(tgClient); // Mark the entity as modified
                }

                // Save changes to the database
                await dbContext.SaveChangesAsync();
            }
        }

        async public Task<Client?> GetClient()
        {
            // Find the first TelegramClientWrapper instance with a matching DatabaseId
            var clientWrapper = Clients.FirstOrDefault();
            return clientWrapper?.Client;
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

            var pyrogramChannelId = TelegramIdConverter.FromWTelegramClientToPyrogram(telegramChannelId);
            var oguzok = await GetClient();
            await TryJoinChannel(oguzok, pyrogramChannelId, dbContext);
            return null;

            //var dbchannel = await dbContext.Channels
            //    .Where(c => c.TelegramId == pyrogramChannelId)
            //    .FirstOrDefaultAsync();

            //if (dbchannel == null) return null;

            //var clientWrapper = Clients.FirstOrDefault(c => c.DatabaseId == dbchannel.TgclientId);
            //if (clientWrapper == null)
            //{
            //    try
            //    {
            //        var clientsDbIds = Clients.Select(x => x.DatabaseId).ToList();
            //        if (!clientsDbIds.Any())
            //        {
            //            Console.WriteLine("No clients available to join the channel.");
            //            return null; // Or handle this case as needed
            //        }

            //        var clientsFromDb = await dbContext.TgClients
            //            .Where(x => clientsDbIds.Contains(x.Id))
            //            .OrderBy(c => c.ChannelCount)
            //            .ToListAsync();

            //        if (!clientsFromDb.Any())
            //        {
            //            Console.WriteLine("No matching clients found in database.");
            //            return null; // Or handle this case as needed
            //        }

            //        var lowestChannelCountClient = clientsFromDb.First();
            //        var matchingClientWrapper = Clients.FirstOrDefault(x => x.DatabaseId == lowestChannelCountClient.Id);

            //        if (matchingClientWrapper == null)
            //        {
            //            Console.WriteLine($"No client wrapper found for the lowest channel count client with ID: {lowestChannelCountClient.Id}");
            //            return null; // Or handle accordingly
            //        }

            //        var client = matchingClientWrapper.Client;
            //        Assuming Messages_GetAllChats does not throw exceptions on failure but you might want to wrap it in try-catch if it does
            //       var chatsResult = await client.Messages_GetAllChats();
            //        var channels = chatsResult.chats.Values.Where(x => x.IsChannel);
            //        Console.WriteLine(channels.Count());

            //        Console.WriteLine("This user has joined the following:");
            //        foreach (var channel in channels)
            //        {
            //            Console.WriteLine($"{channel.ID}: {channel.Title}");
            //        }

            //        var wClientChannelId = TelegramIdConverter.FromPyrogramToWTelegramClient(telegramChannelId);
            //        Console.WriteLine(wClientChannelId);
            //        Attempt to find and join the channel
            //        foreach (var chatBase in chatsResult.chats.Values)
            //        {
            //            Check if the chat is actually a channel
            //            if (chatBase is TL.Channel channel && channel.ID == wClientChannelId)
            //            {
            //                Console.WriteLine($"Found channel to join: {channel.Title} (ID: {channel.ID})");

            //                await TryJoinChannel(client, pyrogramChannelId);

            //                lowestChannelCountClient.ChannelCount += 1;
            //                dbContext.Update(lowestChannelCountClient);
            //                After joining, update the ChannelCount for the client in both the database and local list

            //               await dbContext.SaveChangesAsync();

            //                return client; // Return the client after successfully joining the channel
            //            }
            //        }

            //        Console.WriteLine("Channel to join not found or not a TL.Channel type.");
            //        return null; // Handle case where no suitable channel was found

            //        matchingClientWrapper.ChannelsCount = lowestChannelCountClient.ChannelCount; // Keep local state in sync
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine($"An error occurred while trying to join a channel: {ex.Message}");
            //            Consider logging the stack trace or additional details in a dedicated logging system
            //            return null;
            //        }
            //    }
            //return clientWrapper?.Client;
            }

        // Auxiliary method assuming existence, implement with appropriate exception handling and logging

        public static string RemoveTMeUrl(string input)
        {
            // Define the pattern to match the URL
            string pattern = @"https?:\/\/t\.me\/";

            // Replace the matched pattern with an empty string
            string result = Regex.Replace(input, pattern, string.Empty);

            return result;
        }

        private async Task TryJoinChannel(Client client, long id, TgDbContext context)
        {
            Console.WriteLine("JAAAAA KURVA JA HUESOS");
            try
            {
                
                
                var channel = await context.Channels.FirstOrDefaultAsync(x => x.TelegramId == id);
                id = TelegramIdConverter.FromPyrogramToWTelegramClient(id);
                var nosobackanazvanie = RemoveTMeUrl(channel.Url);
                //InputPeer apple = new InputPeer() {id=id };
                var popa = await client.Contacts_ResolveUsername(nosobackanazvanie);
                Console.WriteLine(popa.Channel.Title);
                var iosifstalin = new InputChannel(id, popa.Channel.access_hash);
                await client.Channels_JoinChannel(iosifstalin);
                Console.WriteLine($"Successfully joined channel: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to join channel {id}: {ex.Message}");
                // Handle or log the exception as needed
            }
        }

        private async Task<(long channelId, long accessHash)?> GetChannelAccessHash(long channelId)
        {
            try
            {
                // Attempt to get all chats
                channelId = TelegramIdConverter.FromPyrogramToWTelegramClient(channelId);
                Console.WriteLine("Attempting to get all chats...");
                var _client = await GetClient();
                Messages_Chats result = await _client.Messages_GetAllChats();
                Dictionary<long, ChatBase> chats = result.chats;
                foreach (var (id, chat) in chats)
                    if (chat.IsActive)
                        Console.WriteLine($"{id,10}: {chat}");

                // Try to find the channel in the list of chats
                Console.WriteLine($"Looking for channel with ID: {channelId}");
                if (chats.TryGetValue(channelId, out ChatBase channel))
                {
                    // If the channel is found, resolve its username
                    Console.WriteLine($"Channel found. Resolving username: {channel.MainUsername}");
                    Contacts_ResolvedPeer peer = await _client.Contacts_ResolveUsername(channel.MainUsername);

                    if (peer.Channel != null)
                    {
                        Console.WriteLine("Channel resolved successfully.");
                        return (peer.Channel.ID, peer.Channel.access_hash);
                    }
                    else
                    {
                        Console.WriteLine("Resolved peer does not contain a channel.");
                    }
                }
                else
                {
                    Console.WriteLine("Channel not found in chats.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return null;
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
