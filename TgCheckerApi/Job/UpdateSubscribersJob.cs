using Quartz;
using static TgCheckerApi.Controllers.BotController;
using TgCheckerApi.Controllers;

namespace TgCheckerApi.Job
{
    public class UpdateSubscribersJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public UpdateSubscribersJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // Create a new scope to resolve scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var controller = scope.ServiceProvider.GetService<BotController>();
                // Add any required parameters for the method
                var dailySubRequest = new DailySubRequest();
                // ...initialize dailySubRequest as needed...
                Console.WriteLine("BEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\nBEBRA\n");
                // Call the controller method
                //await controller.CallSubscribersByChannels(dailySubRequest);
            }
        }
    }
}
