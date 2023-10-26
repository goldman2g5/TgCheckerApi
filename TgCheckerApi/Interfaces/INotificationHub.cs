namespace TgCheckerApi.Interfaces
{
    public interface INotificationHub
    {
        Task SendToUserWithUniqueKey(string uniqueKey, string message);

    }
}
