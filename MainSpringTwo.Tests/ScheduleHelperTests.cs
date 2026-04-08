using MainSpringTwo.Web.Models.Entities;
using MainSpringTwo.Web.Plugins;
using MainSpringTwo.Web.Services;

namespace MainSpringTwo.Tests;

public class ScheduleHelperTests
{
    [Fact]
    public void ShouldRun_ReturnsTrue_ForMinutelyJobAtExactInterval()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddMinutes(5));

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsFalse_ForMinutelyJobOffInterval()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddMinutes(3));

        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsTrue_ForHourlyJobAtInterval()
    {
        var startTime = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Hour, 2);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddHours(2));

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsFalse_ForHourlyJobOffInterval()
    {
        var startTime = new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Hour, 2);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddHours(1));

        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsTrue_ForDailyJobAtInterval()
    {
        var startTime = new DateTime(2026, 4, 8, 8, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Day, 1);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddDays(1));

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsTrue_ForWeeklyJobAtInterval()
    {
        var startTime = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Week, 1);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddDays(7));

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsTrue_ForMonthlyJobOnMatchingDay()
    {
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Month, 1);

        var shouldRun = ScheduleHelper.ShouldRun(job, new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsTrue_ForMonthlyJobEndOfMonthFallback()
    {
        var startTime = new DateTime(2026, 1, 31, 9, 30, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Month, 1);

        var shouldRun = ScheduleHelper.ShouldRun(job, new DateTime(2026, 2, 28, 9, 30, 0, DateTimeKind.Utc));

        Assert.True(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsFalse_ForMonthlyJobWrongDay()
    {
        var startTime = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Month, 1);

        var shouldRun = ScheduleHelper.ShouldRun(job, new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc));

        Assert.False(shouldRun);
    }

    [Fact]
    public void ShouldRun_ReturnsFalse_ForInactiveJob()
    {
        var startTime = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var job = CreateJob(startTime, ScheduleType.Minute, 5, isActive: false);

        var shouldRun = ScheduleHelper.ShouldRun(job, startTime.AddMinutes(5));

        Assert.False(shouldRun);
    }

    [Fact]
    public void PluginRegistry_ReturnsRegisteredSamplePluginByName()
    {
        var registry = new PluginRegistry();
        var plugin = new SamplePlugin();
        registry.Register(plugin);

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
