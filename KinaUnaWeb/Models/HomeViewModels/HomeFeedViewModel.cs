using System;

namespace KinaUnaWeb.Models.HomeViewModels
{
    public class HomeFeedViewModel: BaseItemsViewModel
    {
        public int ImageId { get; set; }
        public string ImageLink { get; init; }
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

        public string Location { get; set; }
        
        public PictureTime PictureTime { get; set; }
        public Picture DisplayPicture { get; set; }

        public HomeFeedViewModel()
        {
            
        }

        public HomeFeedViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetBirthTimeData()
        {
            BirthTime progBirthTime;
            if (!string.IsNullOrEmpty(CurrentProgeny.NickName) && CurrentProgeny.BirthDay.HasValue && CurrentAccessLevel < (int)AccessLevel.Public)
            {
                progBirthTime = new BirthTime(CurrentProgeny.BirthDay.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
            }
            else
            {
                progBirthTime = new BirthTime(new DateTime(2018, 02, 18, 18, 02, 00),
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
            }

            CurrentTime = progBirthTime.CurrentTime;
            Years = progBirthTime.CalcYears();
            Months = progBirthTime.CalcMonths();
            Weeks = progBirthTime.CalcWeeks();
            Days = progBirthTime.CalcDays();
            Hours = progBirthTime.CalcHours();
            Minutes = progBirthTime.CalcMinutes();
            NextBirthday = progBirthTime.CalcNextBirthday();
            MinutesMileStone = progBirthTime.CalcMileStoneMinutes();
            HoursMileStone = progBirthTime.CalcMileStoneHours();
            DaysMileStone = progBirthTime.CalcMileStoneDays();
            WeeksMileStone = progBirthTime.CalcMileStoneWeeks();
        }

        public Picture CreateTempPicture(string hostUrl)
        {
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = CurrentProgeny,
                AccessLevel = (int)AccessLevel.Public,
                PictureLink600 = hostUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = CurrentProgeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return tempPicture;
        }

        public void SetPictureTimeData()
        {
            PicTime = PictureTime.PictureDateTime;
            PicYears = PictureTime.CalcYears();
            PicMonths = PictureTime.CalcMonths();
            PicWeeks = PictureTime.CalcWeeks();
            PicDays = PictureTime.CalcDays();
            PicHours = PictureTime.CalcHours();
            PicMinutes = PictureTime.CalcMinutes();
        }

        public void SetDisplayPictureData()
        {
            if (PicTimeValid)
            {
                PictureTime = new PictureTime(CurrentProgeny.BirthDay!.Value, DisplayPicture.PictureTime,
                    TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
            }
            else
            {
                PictureTime = new PictureTime(new DateTime(2018, 02, 18, 20, 18, 00),
                    DisplayPicture.PictureTime, TimeZoneInfo.FindSystemTimeZoneById(CurrentProgeny.TimeZone));
            }

            ImageId = DisplayPicture.PictureId;
            Tags = DisplayPicture.Tags;
            Location = DisplayPicture.Location;
            
            ImageLink600 = DisplayPicture.PictureLink600;
        }
    }
}
