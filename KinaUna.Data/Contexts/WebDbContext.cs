using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.Data.Contexts
{
    public class WebDbContext : DbContext
    {
        public WebDbContext(DbContextOptions<WebDbContext> options) : base(options)
        {

        }

        public DbSet<TimeLineItem> TimeLineDb { get; set; }
        public DbSet<CalendarItem> CalendarDb { get; set; }
        public DbSet<Location> LocationsDb { get; set; }
        public DbSet<VocabularyItem> VocabularyDb { get; set; }
        public DbSet<Skill> SkillsDb { get; set; }
        public DbSet<Friend> FriendsDb { get; set; }
        public DbSet<Measurement> MeasurementsDb { get; set; }
        public DbSet<Sleep> SleepDb { get; set; }
        public DbSet<Note> NotesDb { get; set; }
        public DbSet<Contact> ContactsDb { get; set; }
        public DbSet<Address> AddressDb { get; set; }
        public DbSet<Vaccination> VaccinationsDb { get; set; }
        public DbSet<WebNotification> WebNotificationsDb { get; set; }
        public DbSet<PushDevices> PushDevices { get; set; }
    }
}
