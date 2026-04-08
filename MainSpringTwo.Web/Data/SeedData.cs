using Microsoft.AspNetCore.Identity;
using MainSpringTwo.Web.Models.Entities;
using MainSpringTwo.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace MainSpringTwo.Web.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var pluginRegistry = services.GetRequiredService<PluginRegistry>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var now = DateTime.UtcNow;

            if (!userManager.Users.Any())
            {
                var admin = new IdentityUser
                {
                    UserName = "admin",
                    Email = "admin@localhost"
                };

                await userManager.CreateAsync(admin, "ChangeMe123!");
            }

            foreach (var plugin in pluginRegistry.GetAll())
            {
                var existingPlugin = await dbContext.Plugins
                    .FirstOrDefaultAsync(x => x.Name == plugin.Name);

                if (existingPlugin is null)
                {
                    dbContext.Plugins.Add(new Plugin
                    {
                        Name = plugin.Name,
                        Description = plugin.Description,
                        IsActive = true,
                        InsertDate = now,
                        UpdateDate = now
                    });

                    continue;
                }

                existingPlugin.Description = plugin.Description;
                existingPlugin.IsActive = true;
                existingPlugin.UpdateDate = now;
            }

            var hasRetentionSetting = await dbContext.ApplicationConfigurations.AnyAsync(config =>
                config.CategoryName == "Logging" &&
                config.ConfigurationName == "RetentionDays");

            if (!hasRetentionSetting)
            {
                dbContext.ApplicationConfigurations.Add(new ApplicationConfiguration
                {
                    CategoryName = "Logging",
                    ConfigurationName = "RetentionDays",
                    ConfigurationValue = "7",
                    InsertDate = now,
                    UpdateDate = now
                });
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
