using WTelegram;

namespace TgCheckerApi.Models
{
    public class TelegramClientWrapper
{
    public Client Client { get; set; }
    public int DatabaseId { get; set; } // Database record ID of the TgClient

    public TelegramClientWrapper(Client client, int databaseId)
    {
        Client = client;
        DatabaseId = databaseId;
    }
}
}
