using Quartz;

namespace TgCheckerApi.Job
{
    public class RatingResetJob : IJob
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RatingResetJob(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // Create and configure HttpClient
            var client = _httpClientFactory.CreateClient("MyClient");
            // Set up the request to the specific controller action
            var response = await client.GetAsync("/api/Channel/resetBumps"); // Adjust with your actual endpoint

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Successfully called reset method on controller.");
            }
            else
            {
                Console.WriteLine("Failed to call reset method on controller.");
                // Log or handle error
            }
        }
    }
}
