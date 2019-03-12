using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class TimeLineViewModel
    {
        public List<TimeLineItem> TimeLineItems { get; set; }
        public List<Picture> PictureItems { get; set; }
        public List<Video> VideoItems { get; set; }
        public List<CalendarItem> CalendarItems { get; set; }
        public List<VocabularyItem> VocabularyItems { get; set; }
        public List<Skill> SkillItems { get; set; }
        public List<Friend> FriendItems { get; set; }
        public List<Measurement> MeasurementItems { get; set; }
        public List<Sleep> SleepItems { get; set; }
        public List<Note> NoteItems { get; set; }
        public List<Contact> ContactItems { get; set; }
        public List<Vaccination> VaccinationItems { get; set; }
    }
}
