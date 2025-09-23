using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace KinaUna.Data.Contexts
{
    public class ProgenyDbContext(DbContextOptions<ProgenyDbContext> options) : DbContext(options)
    {
        public DbSet<Progeny> ProgenyDb { get; init; }
        public DbSet<UserAccess> UserAccessDb { get; init; }
        public DbSet<TimeLineItem> TimeLineDb { get; init; }
        public DbSet<UserInfo> UserInfoDb { get; init; }
        public DbSet<Location> LocationsDb { get; init; }
        public DbSet<CalendarItem> CalendarDb { get; init; }
        public DbSet<VocabularyItem> VocabularyDb { get; init; }
        public DbSet<Skill> SkillsDb { get; init; }
        public DbSet<Friend> FriendsDb { get; init; }
        public DbSet<Measurement> MeasurementsDb { get; init; }
        public DbSet<Sleep> SleepDb { get; init; }
        public DbSet<Note> NotesDb { get; init; }
        public DbSet<Contact> ContactsDb { get; init; }
        public DbSet<Address> AddressDb { get; init; }
        public DbSet<Vaccination> VaccinationsDb { get; init; }
        public DbSet<MobileNotification> MobileNotificationsDb { get; init; }
        public DbSet<UserInfo> DeletedUsers { get; init; }
        public DbSet<TextTranslation> TextTranslations { get; init; }
        public DbSet<KinaUnaLanguage> Languages { get; init; }
        public DbSet<KinaUnaText> KinaUnaTexts { get; init; }
        public DbSet<KinaUnaTextNumber> KinaUnaTextNumbers { get; init; }
        public DbSet<WebNotification> WebNotificationsDb { get; init; }
        public DbSet<PushDevices> PushDevices { get; init; }
        public DbSet<KinaUnaBackgroundTask> BackgroundTasksDb { get; init; }
        public DbSet<CalendarReminder> CalendarRemindersDb { get; init; }
        public DbSet<ProgenyInfo> ProgenyInfoDb { get; init; }
        public DbSet<RecurrenceRule> RecurrenceRulesDb { get; init; }
        public DbSet<TodoItem> TodoItemsDb { get; init; }
        public DbSet<KanbanItem> KanbanItemsDb { get; init; }
        public DbSet<KanbanBoard> KanbanBoardsDb { get; init; }
        public DbSet<UserGroup> UserGroupsDb { get; init; }
        public DbSet<UserGroupMember> UserGroupMembersDb { get; init; }
        public DbSet<ResourcePermission> ResourcePermissionsDb { get; init; }
    }
}
