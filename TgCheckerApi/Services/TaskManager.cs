namespace TgCheckerApi.Services
{
    public class TaskManager
    {
        public Dictionary<string, TaskCompletionSource<string>> _pendingTasks = new Dictionary<string, TaskCompletionSource<string>>();
        public Dictionary<string, TaskCompletionSource<string>> _requestCache = new Dictionary<string, TaskCompletionSource<string>>();
    }
}
