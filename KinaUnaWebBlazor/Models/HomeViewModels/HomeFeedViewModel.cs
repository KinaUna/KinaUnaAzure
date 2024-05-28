using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class HomeFeedViewModel: BaseViewModel
    {
        public Progeny? Progeny { get; set; }

        public int UserAccessLevel { get; set; } = 5;
        public List<CalendarItem> EventsList { get; set; } = [];
        public int ImageId { get; set; } = 0;
        public string ImageLink { get; set; } = "";
        public string ImageLink600 { get; set; } = "";
        
        public string CurrentTime { get; set; } = ""; 
        public string Years { get; set; } = "";
        public string Months { get; set; } = "";
        public string[] Weeks { get; set; } = new string[2];
        public string Days { get; set; } = "";
        public string Hours { get; set; } = "";
        public string Minutes { get; set; } = "";
        public string NextBirthday { get; set; } = "";
        public string[] MinutesMileStone { get; set; } = new string[2];
        public string[] HoursMileStone { get; set; } = new string[2];
        public string[] DaysMileStone { get; set; } = new string[2];
        public string[] WeeksMileStone { get; set; } = new string[2];
        public bool PicTimeValid { get; set; } = false;
        
        public string PicTime { get; set; } = "";
        public string PicYears { get; set; } = "";
        public string PicMonths { get; set; } = "";
        public string[] PicWeeks { get; set; } = new string[2];
        public string PicDays { get; set; } = "";
        public string PicHours { get; set; } = "";
        public string PicMinutes { get; set; } = "";

        public string Tags { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
