using Quartz;

namespace Nuclea.Quartz.Business.Interfaces;

public interface IQuartzJob : IJob
{
    string JobName { get; }
    string JobGroup { get; }
    string Description { get; }
}
