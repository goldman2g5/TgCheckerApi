using WTelegram;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Services
{
    public class TelegramClientService
    {
        private readonly TgClientFactory _tgClientFactory;
        public static List<Client> Clients { get; private set; } = new List<Client>();

        public TelegramClientService(TgClientFactory tgClientFactory)
        {
            _tgClientFactory = tgClientFactory;
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
                    switch (await client.Login(loginInfo)) // Adjusted to use DTO
                    {
                        case "verification_code":
                            Console.Write("Code: "); loginInfo = Console.ReadLine(); break;
                        // Additional cases as needed
                        default: loginInfo = null; break;
                    }
                }
                Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
                Clients.Add(client); // Store the client for later use
            }
        }

        public Client GetClient()
        {
            // Return the first available client, if any
            return Clients.FirstOrDefault();
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
