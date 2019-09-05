using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IDataService
    {
        Task<List<Progeny>> GetProgenyUserIsAdmin(string email);
        Task<List<Progeny>> SetProgenyUserIsAdmin(string email);
        Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId);
        Task<List<UserAccess>> SetProgenyUserAccessList(int progenyId);
        Task<List<UserAccess>> GetUsersUserAccessList(string email);
        Task<List<UserAccess>> SetUsersUserAccessList(string email);
        Task<Progeny> GetProgeny(int id);
        Task<Progeny> SetProgeny(int id);
        Task RemoveProgeny(int id);
        Task<UserAccess> GetUserAccess(int id);
        Task<UserAccess> SetUserAccess(int id);
        Task RemoveUserAccess(int id, int progenyId, string userId);
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Task<UserInfo> GetUserInfoByEmail(string userEmail);
        Task<UserInfo> SetUserInfoByEmail(string userEmail);
        Task RemoveUserInfoByEmail(string userEmail, string userId, int userinfoId);
        Task<UserInfo> GetUserInfoById(int id);
        Task<UserInfo> GetUserInfoByUserId(string id);
        Task<Address> GetAddressItem(int id);
        Task<Address> SetAddressItem(int id);
        Task RemoveAddressItem(int id);
        Task<CalendarItem> GetCalendarItem(int id);
        Task<CalendarItem> SetCalendarItem(int id);
        Task RemoveCalendarItem(int id, int progenyId);
        Task<List<CalendarItem>> GetCalendarList(int progenyId);
        Task<Contact> GetContact(int id);
        Task<Contact> SetContact(int id);
        Task RemoveContact(int id, int progenyId);
        Task<List<Contact>> GetContactsList(int progenyId);
        Task<Friend> GetFriend(int id);
        Task<Friend> SetFriend(int id);
        Task RemoveFriend(int id, int progenyId);
        Task<List<Friend>> GetFriendsList(int progenyId);
        Task<Location> GetLocation(int id);
        Task<Location> SetLocation(int id);
        Task RemoveLocation(int id, int progenyId);
        Task<List<Location>> GetLocationsList(int progenyId);
        Task<TimeLineItem> GetTimeLineItem(int id);
        Task<TimeLineItem> SetTimeLineItem(int id);
        Task RemoveTimeLineItem(int timeLineItemId, int timeLineType, int progenyId);
        Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType);
        Task<List<TimeLineItem>> GetTimeLineList(int progenyId);
        Task<Measurement> GetMeasurement(int id);
        Task<Measurement> SetMeasurement(int id);
        Task RemoveMeasurement(int id, int progenyId);
        Task<List<Measurement>> GetMeasurementsList(int progenyId);
        Task<Note> GetNote(int id);
        Task<Note> SetNote(int id);
        Task RemoveNote(int id, int progenyId);
        Task<List<Note>> GetNotesList(int progenyId);
        Task<Skill> GetSkill(int id);
        Task<Skill> SetSkill(int id);
        Task RemoveSkill(int id, int progenyId);
        Task<List<Skill>> GetSkillsList(int progenyId);
        Task<Sleep> GetSleep(int id);
        Task<Sleep> SetSleep(int id);
        Task RemoveSleep(int id, int progenyId);
        Task<List<Sleep>> GetSleepList(int progenyId);
        Task<Vaccination> GetVaccination(int id);
        Task<Vaccination> SetVaccination(int id);
        Task RemoveVaccination(int id, int progenyId);
        Task<List<Vaccination>> GetVaccinationsList(int progenyId);
        Task<VocabularyItem> GetVocabularyItem(int id);
        Task<VocabularyItem> SetVocabularyItem(int id);
        Task RemoveVocabularyItem(int id, int progenyId);
        Task<List<VocabularyItem>> GetVocabularyList(int progenyId);
        Task UpdateProgenyAdmins(Progeny progeny);
    }
}
