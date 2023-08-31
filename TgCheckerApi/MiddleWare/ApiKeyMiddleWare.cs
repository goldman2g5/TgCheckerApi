using Microsoft.EntityFrameworkCore;
using System.Net;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.MiddleWare
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER_NAME = "X-API-KEY";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TgDbContext dbContext)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var bypassAuthentication = endpoint.Metadata.GetMetadata<BypassApiKeyAttribute>();

                if (bypassAuthentication != null)
                {
                    await _next(context);
                    return;
                }
            }

            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var potentialApiKey))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("API Key was missing from the request header.");
                return;
            }

            var apiKey = await dbContext.Apikeys.SingleOrDefaultAsync(ak => ak.Key == potentialApiKey);

            if (apiKey == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await _next(context);
        }
    }
}
