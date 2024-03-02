using Quartz.Spi;
using Quartz;

namespace TgCheckerApi.Quartz
{
    public class QuartzJobFactory : IJobFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public QuartzJobFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var scope = _serviceScopeFactory.CreateScope(); // Create a new scope
            try
            {
                var job = (IJob)scope.ServiceProvider.GetService(bundle.JobDetail.JobType); // Resolve the job within the scope
                return new ScopedJob(job, scope); // Wrap the job in a ScopedJob so the scope persists for the duration of the job
            }
            catch
            {
                scope.Dispose(); // Make sure to dispose of the scope in case of failure
                throw;
            }
        }

        public void ReturnJob(IJob job)
        {
            (job as ScopedJob)?.Dispose(); // Dispose of the ScopedJob, which will dispose of the scope
        }

        private class ScopedJob : IJob, IDisposable
        {
            private readonly IServiceScope _scope;
            private readonly IJob _job;

            public ScopedJob(IJob job, IServiceScope scope)
            {
                _job = job ?? throw new ArgumentNullException(nameof(job));
                _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            }

            public async Task Execute(IJobExecutionContext context)
            {
                try
                {
                    await _job.Execute(context); // Execute the actual job
                }
                finally
                {
                    Dispose(); // Dispose of the scope when done
                }
            }

            public void Dispose()
            {
                _scope.Dispose(); // Dispose of the scope
            }
        }
    }
}
