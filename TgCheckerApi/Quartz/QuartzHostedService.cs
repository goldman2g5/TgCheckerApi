using Quartz.Spi;
using Quartz;
using TgCheckerApi.Job;
using TgCheckerApi.Quartz;

namespace TgCheckerApi.Services
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IEnumerable<JobSchedule> _jobSchedules;
        private IScheduler _scheduler;

        public QuartzHostedService(
            ISchedulerFactory schedulerFactory,
            IEnumerable<JobSchedule> jobSchedules,
            IJobFactory jobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _jobSchedules = jobSchedules;
            _jobFactory = jobFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            _scheduler.JobFactory = _jobFactory;

            // Define the LoadDelayedNotificationsJob job
            var loadDelayedNotificationsJob = JobBuilder.Create<LoadDelayedNotificationsJob>()
                .WithIdentity("LoadDelayedNotificationsJob", "SystemJobs")
                .Build();

            // Create a trigger that fires immediately
            var loadDelayedNotificationsJobtrigger = TriggerBuilder.Create()
                .WithIdentity("LoadDelayedNotificationsJobTrigger", "SystemJobs")
                .StartNow()
                .Build();

            // Schedule the job for immediate execution
            await _scheduler.ScheduleJob(loadDelayedNotificationsJob, loadDelayedNotificationsJobtrigger, cancellationToken);

            foreach (var jobSchedule in _jobSchedules)
            {
                var job = CreateJob(jobSchedule);
                var trigger = CreateTrigger(jobSchedule);

                await _scheduler.ScheduleJob(job, trigger, cancellationToken);

                // Check if it's the UpdateSubscribersJob and trigger it immediately on startup
                if (job.JobType == typeof(UpdateSubscribersJob) ||
                    job.JobType == typeof(RecalculateTopPosJob) )
                {
                    await _scheduler.TriggerJob(new JobKey(job.JobType.FullName), cancellationToken);
                }
            }

            await _scheduler.Start(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _scheduler?.Shutdown(cancellationToken);
        }

        // Method to create job
        private IJobDetail CreateJob(JobSchedule schedule)
        {
            var jobType = schedule.JobType;
            return JobBuilder
                .Create(jobType)
                .WithIdentity(jobType.FullName)
                .WithDescription(jobType.Name)
                .Build();
        }

        // Method to create trigger
        private ITrigger CreateTrigger(JobSchedule schedule)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{schedule.JobType.FullName}.trigger")
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.CronExpression)
                .Build();
        }
    }
}
