using MainSpringTwo.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MainSpringTwo.Web.Models.ViewModels
{
    public class JobHistoryIndexViewModel
    {
        public List<JobHistory> Entries { get; set; } = [];

        public int Page { get; set; }

        public int TotalPages { get; set; }

        public int? FilterScheduledJobId { get; set; }

        [ValidateNever]
        public SelectList ScheduledJobOptions { get; set; } = new(Enumerable.Empty<SelectListItem>(), "Value", "Text");
    }
}
