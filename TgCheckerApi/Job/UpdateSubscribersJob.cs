using Quartz;
using static TgCheckerApi.Controllers.BotController;
using TgCheckerApi.Controllers;
using TgCheckerApi.Models.BaseModels;
using System.Data.Entity;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;

namespace TgCheckerApi.Job
{
    public class UpdateSubscribersJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public UpdateSubscribersJob(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                Thread.Sleep(10000);
                var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();
                var logger = scope.ServiceProvider.GetService<ILogger<UpdateSubscribersJob>>();

                if (httpClientFactory == null)
                {
                    logger.LogError("HttpClientFactory is not registered. Cannot proceed with the HTTP call.");
                    return;
                }

                var client = httpClientFactory.CreateClient("MyClient");

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "api/Bot/getSubscribersByChannels")
                    {
                        Content = new StringContent(
                            JsonConvert.SerializeObject(new DailySubRequest { ChannelId = new List<int>(), AllChannels = true }),
                            Encoding.UTF8,
                            "application/json")
                    };

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        logger.LogInformation("Successfully processed all channels via HTTP request at: {DateTime.UtcNow}");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        logger.LogError($"Failed to process channels via HTTP request. Status: {response.StatusCode}, Content: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred executing UpdateSubscribersJob via HTTP request.");
                }
            }
        }
    }
}
