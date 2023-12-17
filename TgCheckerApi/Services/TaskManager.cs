namespace TgCheckerApi.Services
{
    public class TaskManager
    {
        public Dictionary<string, TaskCompletionSource<string>> _pendingTasks = new Dictionary<string, TaskCompletionSource<string>>();
    }
}
