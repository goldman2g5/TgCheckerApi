using Quartz.Spi;
using Quartz;

namespace TgCheckerApi.Quartz
{
    public class ScopedJobFactory : IJobFactory
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ScopedJobFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var scope = _serviceScopeFactory.CreateScope();
            IJob job;

            try
            {
                job = scope.ServiceProvider.GetService(bundle.JobDetail.JobType) as IJob;
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return new ScopedJob(job, scope);
        }

        public void ReturnJob(IJob job)
        {
            (job as ScopedJob)?.Dispose();
        }

        private class ScopedJob : IJob, IDisposable
        {
            private readonly IServiceScope _scope;
            private readonly IJob _job;

            public ScopedJob(IJob job, IServiceScope scope)
            {
                _job = job;
                _scope = scope;
            }

            public async Task Execute(IJobExecutionContext context)
            {
                await _job.Execute(context);
            }

            public void Dispose()
            {
                _scope.Dispose();
            }
        }
    }
}
