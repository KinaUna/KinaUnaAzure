using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.Data.Contexts
{
    public class WebDbContext(DbContextOptions<WebDbContext> options) : DbContext(options)
    {
        public DbSet<TimeLineItem> TimeLineDb { get; init; }
        public DbSet<CalendarItem> CalendarDb { get; init; }
        public DbSet<Location> LocationsDb { get; init; }
        public DbSet<VocabularyItem> VocabularyDb { get; init; }
        public DbSet<Skill> SkillsDb { get; init; }
        public DbSet<Friend> FriendsDb { get; init; }
        public DbSet<Measurement> MeasurementsDb { get; init; }
        public DbSet<Sleep> SleepDb { get; init; }
        public DbSet<Note> NotesDb { get; init; }
        public DbSet<Contact> ContactsDb { get; init; }
        public DbSet<Address> AddressDb { get; init; }
        public DbSet<Vaccination> VaccinationsDb { get; init; }
        public DbSet<WebNotification> WebNotificationsDb { get; init; }
        public DbSet<PushDevices> PushDevices { get; init; }
    }
}
