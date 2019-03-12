using KinaUnaWeb.Models.ItemViewModels;
using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.HomeViewModels
{
    public class HomeFeedViewModel
    {
        public Progeny Progeny { get; set; }

        public int UserAccessLevel { get; set; }
        public List<CalendarItem> EventsList { get; set; }
        public int ImageId { get; set; }
        public string ImageLink { get; set; }
        public string ImageLink600 { get; set; }
        
        public string CurrentTime { get; set; }
        public string Years { get; set; }
        public string Months { get; set; }
        public string[] Weeks { get; set; }
        public string Days { get; set; }
        public string Hours { get; set; }
        public string Minutes { get; set; }
        public string NextBirthday { get; set; }
        public string[] MinutesMileStone { get; set; }
        public string[] HoursMileStone { get; set; }
        public string[] DaysMileStone { get; set; }
        public string[] WeeksMileStone { get; set; }
        public bool PicTimeValid { get; set; }
        
        public string PicTime { get; set; }
        public string PicYears { get; set; }
        public string PicMonths { get; set; }
        public string[] PicWeeks { get; set; }
        public string PicDays { get; set; }
        public string PicHours { get; set; }
        public string PicMinutes { get; set; }

        public string Tags { get; set; }
        public string Location { get; set; }
        public TimeLineViewModel LatestPosts { get; set; }
    }
}
