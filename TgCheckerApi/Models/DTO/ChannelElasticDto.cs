namespace TgCheckerApi.Models.DTO
{
    public class ChannelElasticDto
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public int? Members { get; set; }

        public int? User { get; set; }

        public bool? Notifications { get; set; }

        public decimal? Bumps { get; set; }

        public DateTime? LastBump { get; set; }

        public long? TelegramId { get; set; }

        public bool? NotificationSent { get; set; }

        public bool? PromoPost { get; set; }

        public TimeOnly? PromoPostTime { get; set; }

        public int? PromoPostInterval { get; set; }

        public bool? PromoPostSent { get; set; }

        public DateTime? PromoPostLast { get; set; }

        public string? Language { get; set; }

        public string? Flag { get; set; }

        public string? Url { get; set; }

        public bool? Hidden { get; set; }

        public int? TopPos { get; set; }

        public bool? IsPartner { get; set; }

        public int? TgclientId { get; set; }
    }
}
