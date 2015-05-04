using Ninject;
using Quartz;
using Quartz.Spi;

namespace PlexScrobble.Jobs
{
    public class NinjectJobFactory : IJobFactory
    {
        private readonly IKernel _kernel;

        public NinjectJobFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _kernel.Get(bundle.JobDetail.JobType) as IJob;
        }

        public void ReturnJob(IJob job)
        {

        }
    }
}

