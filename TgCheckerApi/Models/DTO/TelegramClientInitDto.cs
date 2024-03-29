namespace TgCheckerApi.Models.DTO
{
    public class TelegramClientInitDto
    {
        public Func<string, string> Config { get; set; }
        public string PhoneNumber { get; set; }
    }
}
