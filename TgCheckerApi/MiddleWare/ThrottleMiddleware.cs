using System.Net;

namespace TgCheckerApi.MiddleWare
{
    public class ThrottleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TimeSpan _timeSpan;
        private readonly int _maxRequest;
        private readonly Dictionary<string, (DateTime, int)> _cache;

        public ThrottleMiddleware(RequestDelegate next, int maxRequestPerTimeSpan, int timeSpanInSeconds)
        {
            _next = next;
            _maxRequest = maxRequestPerTimeSpan;
            _timeSpan = TimeSpan.FromSeconds(timeSpanInSeconds);
            _cache = new Dictionary<string, (DateTime, int)>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientKey = context.Connection.RemoteIpAddress.ToString();

            if (_cache.ContainsKey(clientKey))
            {
                var (lastRequestTime, requestCount) = _cache[clientKey];

                if ((DateTime.Now - lastRequestTime) < _timeSpan && requestCount >= _maxRequest)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                    return;
                }
                if ((DateTime.Now - lastRequestTime) < _timeSpan)
                {
                    _cache[clientKey] = (lastRequestTime, requestCount + 1);
                }
                else
                {
                    _cache[clientKey] = (DateTime.Now, 1);
                }
            }
            else
            {
                _cache.Add(clientKey, (DateTime.Now, 1));
            }

            await _next(context);
        }
    }
}
