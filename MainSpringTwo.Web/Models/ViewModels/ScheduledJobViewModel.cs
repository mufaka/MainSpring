using MainSpringTwo.Web.Models.Entities;
using MainSpringTwo.Web.Models.Plugins;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MainSpringTwo.Web.Models.ViewModels
{
    public class ScheduledJobViewModel
    {
        public ScheduledJob ScheduledJob { get; set; } = new()
        {
            Interval = 1,
            IsActive = true,
            ScheduleType = ScheduleType.Minute,
            StartTime = DateTime.UtcNow
        };

        [ValidateNever]
        public List<PluginParameter> PluginParameters { get; set; } = [];

        public List<ConfigurationValue> ConfigurationValues { get; set; } = [];

        [ValidateNever]
        public SelectList PluginOptions { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");

        [ValidateNever]
        public SelectList ScheduleTypeOptions { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    }
}
