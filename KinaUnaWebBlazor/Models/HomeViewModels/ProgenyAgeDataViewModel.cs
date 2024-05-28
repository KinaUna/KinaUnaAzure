using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.HomeViewModels
{
    public class ProgenyAgeDataViewModel
    {
        public Progeny Progeny { get; set; } = new()
        {
            NickName = "",
            TimeZone = Constants.DefaultTimezone
        };

        public string Years { get; set; } = "0";
        public string Months { get; set; } = "0";
        public string[] Weeks { get; set; } = new string[2];
        public string Days { get; set; } = "0";
        public string Hours { get; set; } = "0";
        public string Minutes { get; set; } = "0";
        public string NextBirthday { get; set; } = "0";
        public string[] MinutesMileStone { get; set; } = new string[2];
        public string[] HoursMileStone { get; set; } = new string[2];
        public string[] DaysMileStone { get; set; } = new string[2];
        public string[] WeeksMileStone { get; set; } = new string[2];
        public string PictureDateTime { get; set; } = "";
    }
}
