using MainSpringTwo.Web.Models.Entities;
using MainSpringTwo.Web.Plugins;
using MainSpringTwo.Web.Services;

namespace MainSpringTwo.Tests;

public class ScheduleHelperTests
{
    [Fact]
    public void ComputeNextRunTime_ReturnsStartTime_ForNewJob()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job);

        Assert.Equal(startTime, nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_AdvancesForMinutelyJob()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime);

        Assert.Equal(startTime.AddMinutes(5), nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_AdvancesForHourlyJob()
    {
        var startTime = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Hour, 2);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime);

        Assert.Equal(startTime.AddHours(2), nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_AdvancesForDailyJob()
    {
        var startTime = new DateTime(2026, 4, 8, 8, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Day, 1);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime);

        Assert.Equal(startTime.AddDays(1), nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_AdvancesForWeeklyJob()
    {
        var startTime = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Week, 1);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime);

        Assert.Equal(startTime.AddDays(7), nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_AdvancesForMonthlyJob()
    {
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Month, 1);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime);

        Assert.Equal(new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc), nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_HandlesMonthlyEndOfMonthFallback()
    {
        var startTime = new DateTime(2026, 1, 31, 9, 30, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Month, 1);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime);

        Assert.Equal(new DateTime(2026, 2, 28, 9, 30, 0, DateTimeKind.Utc), nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_ReturnsNull_ForInactiveJob()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5, isActive: false);

        var nextRun = ScheduleHelper.ComputeNextRunTime(job);

        Assert.Null(nextRun);
    }

    [Fact]
    public void ComputeNextRunTime_SkipsToCorrectInterval_WhenRunTimeMissed()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5);

        // Job ran at start, then the next several ticks were missed.
        // afterTime at 12:13 should advance to 12:15 (the next aligned interval).
        var nextRun = ScheduleHelper.ComputeNextRunTime(job, startTime.AddMinutes(13));

        Assert.Equal(startTime.AddMinutes(15), nextRun);
    }

    [Fact]
    public void PluginRegistry_ReturnsRegisteredSamplePluginByName()
    {
        var plugin = new SamplePlugin();
        var registry = new PluginRegistry([plugin]);

        var fromName = registry.GetByName("Sample");
        var all = registry.GetAll();

        Assert.Same(plugin, fromName);
        Assert.Contains(plugin, all);
    }

    private static ScheduledJob CreateJob(DateTime startTime, ScheduleType scheduleType, int interval, bool isActive = true)
    {
        return new ScheduledJob
        {
            ScheduledJobId = 1,
            Name = "Test Job",
            PluginId = 1,
            ScheduleType = scheduleType,
            Interval = interval,
            StartTime = startTime,
            IsActive = isActive,
            InsertDate = startTime,
            UpdateDate = startTime
        };
    }
}
