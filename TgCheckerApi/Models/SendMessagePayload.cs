namespace TgCheckerApi.Models
{
    public class SendMessagePayload
    {
        public string Username { get; set; }
        public int UserId { get; set; }
        public string Unique_key { get; set; }
    }
}
