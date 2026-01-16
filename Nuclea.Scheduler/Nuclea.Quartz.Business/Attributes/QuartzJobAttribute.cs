namespace Nuclea.Quartz.Business.Attributes;


[AttributeUsage(AttributeTargets.Class)]
public class QuartzJobAttribute(string jobName, string jobGroup, string description = "") : Attribute
{
    public string JobName { get; } = jobName;
    public string JobGroup { get; } = jobGroup;
    public string Description { get; } = description;
}
