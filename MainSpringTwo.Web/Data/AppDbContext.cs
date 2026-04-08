using MainSpringTwo.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MainSpringTwo.Web.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Plugin> Plugins => Set<Plugin>();

        public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();

        public DbSet<ConfigurationValue> ConfigurationValues => Set<ConfigurationValue>();

        public DbSet<ApplicationConfiguration> ApplicationConfigurations => Set<ApplicationConfiguration>();

        public DbSet<JobHistory> JobHistories => Set<JobHistory>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<UtcDateTimeConverter>();

            configurationBuilder.Properties<DateTime?>()
                .HaveConversion<NullableUtcDateTimeConverter>();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Plugin>(entity =>
            {
                entity.HasIndex(x => x.Name).IsUnique();
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(2000);
            });

            builder.Entity<ScheduledJob>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ScheduleType).HasConversion<int>();
                entity.HasOne(x => x.Plugin)
                    .WithMany(x => x.ScheduledJobs)
                    .HasForeignKey(x => x.PluginId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ConfigurationValue>(entity =>
            {
                entity.Property(x => x.ParameterName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Value).HasMaxLength(4000);
                entity.HasOne(x => x.ScheduledJob)
                    .WithMany(x => x.ConfigurationValues)
                    .HasForeignKey(x => x.ScheduledJobId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationConfiguration>(entity =>
            {
                entity.HasIndex(x => new { x.CategoryName, x.ConfigurationName }).IsUnique();
                entity.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ConfigurationName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ConfigurationValue).HasMaxLength(4000);
            });

            builder.Entity<JobHistory>(entity =>
            {
                entity.HasIndex(x => x.RunTime);
                entity.Property(x => x.Message).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.Detail).HasMaxLength(8000);
                entity.HasOne(x => x.ScheduledJob)
                    .WithMany(x => x.JobHistories)
                    .HasForeignKey(x => x.ScheduledJobId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    internal class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    internal class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter()
            : base(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }
}
