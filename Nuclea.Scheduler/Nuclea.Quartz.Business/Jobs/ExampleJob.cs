using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nuclea.Data;
using Quartz;

namespace Nuclea.Quartz.Business.Jobs;

public class ExampleJob(ILogger logger, IHttpContextAccessor contextAccessor, NucleaDataContext context)
    : BaseQuartzJob(logger, contextAccessor, context)
{
    protected override Task ExecuteJobAsync(IJobExecutionContext context)
    {
        throw new NotImplementedException();
    }
}