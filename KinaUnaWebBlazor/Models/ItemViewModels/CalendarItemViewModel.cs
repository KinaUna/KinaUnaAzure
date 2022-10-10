using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class CalendarItemViewModel: BaseViewModel
    {
        public int EventId { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
        public string Title { get; set; } = "";
        public string Notes { get; set; } = "";
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; } = "";
        public string Context { get; set; } = "";
        public bool AllDay { get; set; } = false;
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public List<CalendarItem> EventsList { get; set; } = new List<CalendarItem>();
        public ApplicationUser UserData { get; set; } = new ApplicationUser();
        public Progeny Progeny { get; set; } = new Progeny();
        public List<SelectListItem> ProgenyList { get; set; } = new List<SelectListItem>();
        public bool IsAdmin { get; set; } = false;
        public string StartString { get; set; } = "";
        public string EndString { get; set; } = "";
        public CalendarItem CalendarItem { get; set; } = new CalendarItem();
    }
}
