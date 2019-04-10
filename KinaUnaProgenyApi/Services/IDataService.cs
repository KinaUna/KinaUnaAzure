using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IDataService
    {
        List<Progeny> GetProgenyUserIsAdmin(string email);
        List<Progeny> SetProgenyUserIsAdmin(string email);
        List<UserAccess> GetProgenyUserAccessList(int progenyId);
        List<UserAccess> SetProgenyUserAccessList(int progenyId);
        List<UserAccess> GetUsersUserAccessList(string email);
        List<UserAccess> SetUsersUserAccessList(string email);
        Progeny GetProgeny(int id);
        Progeny SetProgeny(int id);
        void RemoveProgeny(int id);
        UserAccess GetUserAccess(int id);
        UserAccess SetUserAccess(int id);
        void RemoveUserAccess(int id, int progenyId, string userId);
        UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail);
        UserInfo GetUserInfoByEmail(string userEmail);
        UserInfo SetUserInfoByEmail(string userEmail);
        void RemoveUserInfoByEmail(string userEmail, string userId, int userinfoId);
        UserInfo GetUserInfoById(int id);
        UserInfo GetUserInfoByUserId(string id);
        Address GetAddressItem(int id);
        Address SetAddressItem(int id);
        void RemoveAddressItem(int id);
        CalendarItem GetCalendarItem(int id);
        CalendarItem SetCalendarItem(int id);
        void RemoveCalendarItem(int id, int progenyId);
        List<CalendarItem> GetCalendarList(int progenyId);
        Contact GetContact(int id);
        Contact SetContact(int id);
        void RemoveContact(int id, int progenyId);
        List<Contact> GetContactsList(int progenyId);
        Friend GetFriend(int id);
        Friend SetFriend(int id);
        void RemoveFriend(int id, int progenyId);
        List<Friend> GetFriendsList(int progenyId);
        Location GetLocation(int id);
        Location SetLocation(int id);
        void RemoveLocation(int id, int progenyId);
        List<Location> GetLocationsList(int progenyId);
        TimeLineItem GetTimeLineItem(int id);
        TimeLineItem SetTimeLineItem(int id);
        void RemoveTimeLineItem(int timeLineItemId, int timeLineType, int progenyId);
        TimeLineItem GetTimeLineItemByItemId(string itemId, int itemType);
        List<TimeLineItem> GetTimeLineList(int progenyId);
        Measurement GetMeasurement(int id);
        Measurement SetMeasurement(int id);
        void RemoveMeasurement(int id, int progenyId);
        List<Measurement> GetMeasurementsList(int progenyId);
        Note GetNote(int id);
        Note SetNote(int id);
        void RemoveNote(int id, int progenyId);
        List<Note> GetNotesList(int progenyId);
        Skill GetSkill(int id);
        Skill SetSkill(int id);
        void RemoveSkill(int id, int progenyId);
        List<Skill> GetSkillsList(int progenyId);
        Sleep GetSleep(int id);
        Sleep SetSleep(int id);
        void RemoveSleep(int id, int progenyId);
        List<Sleep> GetSleepList(int progenyId);
        Vaccination GetVaccination(int id);
        Vaccination SetVaccination(int id);
        void RemoveVaccination(int id, int progenyId);
        List<Vaccination> GetVaccinationsList(int progenyId);
        VocabularyItem GetVocabularyItem(int id);
        VocabularyItem SetVocabularyItem(int id);
        void RemoveVocabularyItem(int id, int progenyId);
        List<VocabularyItem> GetVocabularyList(int progenyId);
    }
}
