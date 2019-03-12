using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class CalendarItemViewModel
    {
        public int EventId { get; set; }
        public int ProgenyId { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; }
        public string Context { get; set; }
        public bool AllDay { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public List<CalendarItem> EventsList { get; set; }
        public ApplicationUser UserData { get; set; }
        public Progeny Progeny { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public string StartString { get; set; }
        public string EndString { get; set; }

        public CalendarItemViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList accList = new AccessLevelList();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;
        }
    }
}
