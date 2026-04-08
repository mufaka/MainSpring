using MainSpringTwo.Web.Models.Entities;

namespace MainSpringTwo.Web.Services
{
    public static class ScheduleHelper
    {
        public static bool ShouldRun(ScheduledJob job, DateTime now)
        {
            var normalizedNow = Normalize(now);
            var nextRun = GetNextRunTime(job, normalizedNow);

            return nextRun.HasValue && nextRun.Value == normalizedNow;
        }

        public static DateTime? GetNextRunTime(ScheduledJob job, DateTime fromTime)
        {
            if (!job.IsActive)
            {
                return null;
            }

            var startTime = Normalize(job.StartTime);
            var currentTime = Normalize(fromTime);
            var interval = Math.Max(1, job.Interval);

            return job.ScheduleType switch
            {
                ScheduleType.Month => GetNextMonthlyRunTime(startTime, currentTime, interval),
                ScheduleType.Minute or ScheduleType.Hour or ScheduleType.Day or ScheduleType.Week
                    => GetNextIntervalRunTime(job.ScheduleType, startTime, currentTime, interval),
                _ => null
            };
        }

        private static DateTime? GetNextIntervalRunTime(
            ScheduleType scheduleType,
            DateTime startTime,
            DateTime currentTime,
            int interval)
        {
            var minutesInPeriod = scheduleType switch
            {
                ScheduleType.Minute => 1,
                ScheduleType.Hour => 60,
                ScheduleType.Day => 1440,
                ScheduleType.Week => 10080,
                _ => 0
            };

            if (minutesInPeriod == 0)
            {
                return null;
            }

            var intervalInMinutes = minutesInPeriod * interval;

            if (currentTime <= startTime)
            {
                return startTime;
            }

            var minutesSinceStart = (long)Math.Floor((currentTime - startTime).TotalMinutes);
            var intervalsElapsed = minutesSinceStart / intervalInMinutes;
            var candidate = startTime.AddMinutes(intervalsElapsed * intervalInMinutes);

            return candidate < currentTime
                ? candidate.AddMinutes(intervalInMinutes)
                : candidate;
        }

        private static DateTime? GetNextMonthlyRunTime(DateTime startTime, DateTime currentTime, int interval)
        {
            if (currentTime <= startTime)
            {
                return startTime;
            }

            var monthsSinceStart = Math.Max(0, ((currentTime.Year - startTime.Year) * 12) + currentTime.Month - startTime.Month);
            var remainder = monthsSinceStart % interval;
            var candidateMonthOffset = remainder == 0 ? monthsSinceStart : monthsSinceStart + (interval - remainder);
            var candidate = BuildMonthlyOccurrence(startTime, candidateMonthOffset);

            if (candidate < currentTime)
            {
                candidate = BuildMonthlyOccurrence(startTime, candidateMonthOffset + interval);
            }

            return candidate;
        }

        private static DateTime BuildMonthlyOccurrence(DateTime startTime, int monthOffset)
        {
            var target = startTime.AddMonths(monthOffset);
            var scheduledDay = Math.Min(startTime.Day, DateTime.DaysInMonth(target.Year, target.Month));

            return new DateTime(
                target.Year,
                target.Month,
                scheduledDay,
                startTime.Hour,
                startTime.Minute,
                0,
                DateTimeKind.Utc);
        }

        private static DateTime Normalize(DateTime value)
        {
            var utc = value.Kind == DateTimeKind.Utc
                ? value
                : value.ToUniversalTime();

            return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);
        }
    }
}
