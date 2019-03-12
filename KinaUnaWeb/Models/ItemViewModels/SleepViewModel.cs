using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SleepViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<Sleep> SleepList { get; set; }
        public List<Sleep> ChartList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public int SleepId { get; set; }
        public int ProgenyId { get; set; }
        public DateTime? SleepStart { get; set; }
        public DateTime? SleepEnd { get; set; }
        public DateTime CreatedDate { get; set; }
        public int SleepRating { get; set; }
        public string SleepNotes { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }
        public TimeSpan SleepDuration { get; set; }
        public string StartString { get; set; } // For calendar view.
        public string EndString { get; set; } // For calendar view.
        public TimeSpan SleepTotal { get; set; }
        public TimeSpan TotalAverage { get; set; }
        public TimeSpan SleepLastMonth { get; set; }
        public TimeSpan LastMonthAverage { get; set; }
        public TimeSpan SleepLastYear { get; set; }
        public TimeSpan LastYearAverage { get; set; }
        public SleepViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
