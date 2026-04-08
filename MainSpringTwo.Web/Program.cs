using MainSpringTwo.Web.Data;
using MainSpringTwo.Web.Jobs;
using MainSpringTwo.Web.Plugins;
using MainSpringTwo.Web.Services;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSystemd();

            var pluginRegistry = new PluginRegistry();
            pluginRegistry.Register(new SamplePlugin());

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfire(config => config.UseInMemoryStorage());
            builder.Services.AddHangfireServer();
            builder.Services.AddSingleton(pluginRegistry);
            builder.Services.AddScoped<PluginExecutorJob>();
            builder.Services.AddScoped<LogPruningJob>();

            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<AppDbContext>();

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AuthorizeFilter());
            });
            builder.Services.AddRazorPages();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
                SeedData.SeedAsync(services).GetAwaiter().GetResult();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireAuthorizationFilter()]
            });

            RecurringJob.AddOrUpdate<PluginExecutorJob>(
                "plugin-executor",
                job => job.ExecuteAsync(),
                Cron.Minutely);

            RecurringJob.AddOrUpdate<LogPruningJob>(
                "log-pruning",
                job => job.ExecuteAsync(),
                Cron.Daily);

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages()
                .WithStaticAssets();

            app.Run();
        }
    }
}
