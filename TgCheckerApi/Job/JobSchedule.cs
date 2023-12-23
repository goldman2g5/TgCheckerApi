namespace TgCheckerApi.Job
{
    public class JobSchedule
    {
        public Type JobType { get; private set; }
        public string CronExpression { get; private set; }

        public JobSchedule(Type jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }
    }
}
