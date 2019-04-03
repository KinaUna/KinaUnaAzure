using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public interface IProgenyHttpClient
    {
        Task<HttpClient> GetClient();

        Task<UserInfo> GetUserInfo(string email);
        Task<UserInfo> GetUserInfoByUserId(string userId);
        Task<Progeny> GetProgeny(int progenyId);
        Task<Progeny> AddProgeny(Progeny progeny);
        Task<Progeny> UpdateProgeny(Progeny progeny);
        Task<bool> DeleteProgeny(int progenyId);
        Task<List<Progeny>> GetProgenyAdminList(string email);
        Task<List<UserAccess>> GetProgenyAccessList(int progenyId);
        Task<List<UserAccess>> GetUserAccessList(string userEmail);
        Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel);
        Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel);
        Task<UserAccess> GetUserAccess(int userAccessId);
        Task<UserAccess> AddUserAccess(UserAccess userAccess);
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);
        Task<bool> DeleteUserAccess(int userAccessId);
        Task<UserInfo> UpdateUserInfo(UserInfo userinfo);
        Task<Sleep> GetSleepItem(int sleepId);
        Task<Sleep> AddSleep(Sleep sleep);
        Task<Sleep> UpdateSleep(Sleep sleep);
        Task<bool> DeleteSleepItem(int sleepId);
        Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel);
        Task<CalendarItem> GetCalendarItem(int eventId);
        Task<CalendarItem> AddCalendarItem(CalendarItem eventItem);
        Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem);
        Task<bool> DeleteCalendarItem(int sleepId);
        Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel);
        Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, int accessLevel);
        Task<Contact> GetContact(int contactId);
        Task<Contact> AddContact(Contact contact);
        Task<Contact> UpdateContact(Contact contact);
        Task<bool> DeleteContact(int contactId);
        Task<List<Contact>> GetContactsList(int progenyId, int accessLevel);
        Task<Address> GetAddress(int addressId);
        Task<Address> AddAddress(Address address);
        Task<Address> UpdateAddress(Address address);
        Task<Friend> GetFriend(int friendId);
        Task<Friend> AddFriend(Friend friend);
        Task<Friend> UpdateFriend(Friend friend);
        Task<bool> DeleteFriend(int friendId);
        Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel);
        Task<Location> GetLocation(int locationId);
        Task<Location> AddLocation(Location location);
        Task<Location> UpdateLocation(Location location);
        Task<bool> DeleteLocation(int locationId);
        Task<List<Location>> GetLocationsList(int progenyId, int accessLevel);
        Task<Measurement> GetMeasurement(int measurementId);
        Task<Measurement> AddMeasurement(Measurement measurement);
        Task<Measurement> UpdateMeasurement(Measurement measurement);
        Task<bool> DeleteMeasurement(int measurementId);
        Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel);
        Task<Note> GetNote(int noteId);
        Task<Note> AddNote(Note note);
        Task<Note> UpdateNote(Note note);
        Task<bool> DeleteNote(int noteId);
        Task<List<Note>> GetNotesList(int progenyId, int accessLevel);
        Task<Skill> GetSkill(int skillId);
        Task<Skill> AddSkill(Skill skill);
        Task<Skill> UpdateSkill(Skill skill);
        Task<bool> DeleteSkill(int skillId);
        Task<List<Skill>> GetSkillsList(int progenyId, int accessLevel);
        Task<Vaccination> GetVaccination(int vaccinationId);
        Task<Vaccination> AddVaccination(Vaccination vaccination);
        Task<Vaccination> UpdateVaccination(Vaccination vaccination);
        Task<bool> DeleteVaccination(int vaccinationId);
        Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel);
        Task<VocabularyItem> GetWord(int wordId);
        Task<VocabularyItem> AddWord(VocabularyItem word);
        Task<VocabularyItem> UpdateWord(VocabularyItem word);
        Task<bool> DeleteWord(int wordId);
        Task<List<VocabularyItem>> GetWordsList(int progenyId, int accessLevel);
        Task<TimeLineItem> GetTimeLineItem(string itemId, int itemType);
        Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem);
        Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem);
        Task<bool> DeleteTimeLineItem(int timeLineItemId);
        Task<List<TimeLineItem>> GetTimeline(int progenyId, int accessLevel);
        Task SetViewChild(string userId, UserInfo userinfo);
    }
}
