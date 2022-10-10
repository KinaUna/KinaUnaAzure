using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class ProgenyAgeDataViewModel
    {
        public Progeny Progeny { get; set; }
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
        public string PictureDateTime { get; set; }

        public ProgenyAgeDataViewModel()
        {
            Progeny = new Progeny();
            Progeny.NickName = "";
            Progeny.TimeZone = Constants.DefaultTimezone;
            Years = "0";
            Months = "0";
            Weeks = new string[2];
            Days = "0";
            Hours = "0";
            Minutes = "0";
            NextBirthday = "0";
            MinutesMileStone = new string[2];
            HoursMileStone = new string[2];
            DaysMileStone = new string[2];
            WeeksMileStone = new string[2];
            PictureDateTime = "";
        }
    }
}
