namespace TgCheckerApi.Models
{
    public class SendMessagePayload
    {
        public string Username { get; set; }
        public long UserId { get; set; }
        public string Unique_key { get; set; }
    }
}
