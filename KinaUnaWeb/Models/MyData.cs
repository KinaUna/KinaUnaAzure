using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class MyData
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string TimeZone { get; set; }

        public string UserId { get; set; }
        public DateTime JoinDate { get; set; }
        public string ViewChild { get; set; }
        public string LoginProviders { get; set; }

        public List<Progeny> Children { get; set; }

        public List<Picture> PhotoList { get; set; }
        public List<Video> VideoList { get; set; }
        public List<CalendarItem> EventsList { get; set; }
        public List<VocabularyItem> VocabularyList { get; set; }
        public List<Skill> SkillsList { get; set; }
        public List<Friend> FriendsList { get; set; }
        public List<Measurement> MeasurementsList { get; set; }
        public List<Sleep> SleepList { get; set; }
        public List<Note> NotesList { get; set; }
        public List<Contact> ContactsList { get; set; }
        public List<Vaccination> VaccinationsList { get; set; }
        public List<Comment> CommentsList { get; set; }
        public List<Address> AddressList { get; set; }
        public List<CommentThread> CommentThreadsList { get; set; }

        public List<string> CommentLinks { get; set; }
        public List<string> CommentNames { get; set; }
        public List<string> FileNames { get; set; }
        public List<UserAccess> AccessList { get; set; }
    }
}
