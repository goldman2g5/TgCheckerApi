using WTelegram;

namespace TgCheckerApi.Services
{
    public class TelegramClientService
    {
        public Client Client { get; private set; }

        public TelegramClientService(IWebHostEnvironment env)
        {
            InitializeAsync(env).Wait();
        }

        private async Task InitializeAsync(IWebHostEnvironment env)
        {
            var client = new Client(config =>
            {
                switch (config)
                {
                    case "api_id": return "23558497"; // Your API ID
                    case "api_hash": return "2b461873dd2dea7e091f7af28fbe11e1"; // Your API Hash
                    case "session_pathname": return Path.Combine(env.ContentRootPath, "WTelegramSessions", "WTelegram.session"); // Path for the session file
                    default: return null; // Let WTelegramClient handle other configurations
                }
            });

            // Embedding the login logic directly into the factory
            var loginInfo = "+79103212166";
            while (client.User == null)
                switch (await client.Login(loginInfo)) // returns which config is needed to continue login
                {
                    case "verification_code": Console.Write("Code: "); loginInfo = Console.ReadLine(); break;

                    //case "name": loginInfo = "John Doe"; break;    // if sign-up is required (first/last_name)
                    //case "password": loginInfo = "secret!"; break; // if user has enabled 2FA
                    default: loginInfo = null; break;
                }
            Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");
            Client = client;
        }
    }
}
