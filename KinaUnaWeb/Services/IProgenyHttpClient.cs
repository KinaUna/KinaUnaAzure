using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// The progeny http client interface.
    /// Contains the methods for adding, retrieving and updating progeny and user data.
    /// </summary>
    public interface IProgenyHttpClient
    {
        Task<HttpClient> GetClient();

        /// <summary>
        /// Gets a user's information from the email address.
        /// </summary>
        /// <param name="email">string: The user's email address</param>
        /// <returns>UserInfo</returns>
        Task<UserInfo> GetUserInfo(string email);

        /// <summary>
        /// Gets a user's information from the userId.
        /// </summary>
        /// <param name="userId">string: The user's Id (GUID from ApplicationUser's Id field).</param>
        /// <returns>UserInfo</returns>
        Task<UserInfo> GetUserInfoByUserId(string userId);

        /// <summary>
        /// Gets progeny from the progeny's id.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id.</param>
        /// <returns>Progeny</returns>
        Task<Progeny> GetProgeny(int progenyId);

        /// <summary>
        /// Adds a new progeny.
        /// </summary>
        /// <param name="progeny">Progeny: The Progeny object to be added.</param>
        /// <returns>Progeny: The Progeny object that was added.</returns>
        Task<Progeny> AddProgeny(Progeny progeny);

        /// <summary>
        /// Updates a Progeny.
        /// </summary>
        /// <param name="progeny">Progeny: The Progeny object with updated values.</param>
        /// <returns>Progeny: The updated Progeny object.</returns>
        Task<Progeny> UpdateProgeny(Progeny progeny);

        /// <summary>
        /// Removes a progeny.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny to be removed.</param>
        /// <returns>bool: True if successfully removed.</returns>
        Task<bool> DeleteProgeny(int progenyId);

        /// <summary>
        /// Gets a list of Progeny objects where the user is an admin.
        /// </summary>
        /// <param name="email">string: The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        Task<List<Progeny>> GetProgenyAdminList(string email);

        /// <summary>
        /// Gets the list of UserAccess for a progeny.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetProgenyAccessList(int progenyId);

        /// <summary>
        /// Gets the list of UserAccess for a user.
        /// </summary>
        /// <param name="userEmail">string: The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        Task<List<UserAccess>> GetUserAccessList(string userEmail);

        /// <summary>
        /// Gets the list of locations for a Progeny that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the latest 5 posts for a progeny, that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel);

        /// <summary>
        /// Gets all the posts from today's date last year, that the user has access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's id.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the UserAccess with a given UserAccess Id.
        /// </summary>
        /// <param name="userAccessId">int: The Id of the UserAccess.</param>
        /// <returns>UserAccess</returns>
        Task<UserAccess> GetUserAccess(int userAccessId);

        /// <summary>
        /// Adds a new UserAccess.
        /// </summary>
        /// <param name="userAccess">UserAccess: The UserAccess object to be added.</param>
        /// <returns>UserAccess: The UserAccess object that was added.</returns>
        Task<UserAccess> AddUserAccess(UserAccess userAccess);

        /// <summary>
        /// Updates a UserAccess object.
        /// </summary>
        /// <param name="userAccess">UserAccess: The UserAccess object to be updated.</param>
        /// <returns>UserAccess: The updated UserAccess object.</returns>
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);

        /// <summary>
        /// Removes a UserAccess object.
        /// </summary>
        /// <param name="userAccessId">int: The UserAccess object's Id.</param>
        /// <returns>bool: True if the UserAccess object was successfully removed.</returns>
        Task<bool> DeleteUserAccess(int userAccessId);

        /// <summary>
        /// Updates a UserInfo object.
        /// </summary>
        /// <param name="userinfo">UserInfo: The UserInfo object to update.</param>
        /// <returns>UserInfo: The updated UserInfo object.</returns>
        Task<UserInfo> UpdateUserInfo(UserInfo userinfo);

        /// <summary>
        /// Gets a single Sleep object.
        /// </summary>
        /// <param name="sleepId">int: The Id of the sleep object.</param>
        /// <returns>Sleep</returns>
        Task<Sleep> GetSleepItem(int sleepId);

        /// <summary>
        /// Adds a new Sleep object.
        /// </summary>
        /// <param name="sleep">Sleep: The Sleep object to be added.</param>
        /// <returns>Sleep: The added sleep object.</returns>
        Task<Sleep> AddSleep(Sleep sleep);

        /// <summary>
        /// Updates a Sleep object.
        /// </summary>
        /// <param name="sleep">Sleep: The Sleep object to update.</param>
        /// <returns>Sleep: The updated Sleep object.</returns>
        Task<Sleep> UpdateSleep(Sleep sleep);

        /// <summary>
        /// Removes the Sleep object with a given Id.
        /// </summary>
        /// <param name="sleepId">int: The id of the Sleep object to remove.</param>
        /// <returns>bool: True if the Sleep object was successfully removed.</returns>
        Task<bool> DeleteSleepItem(int sleepId);

        /// <summary>
        /// Gets the List of Sleep objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny.</param>
        /// <param name="accessLevel">int: The access level of the user.</param>
        /// <returns>List of Sleep objects.</returns>
        Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets a CalendarItem with a given Id.
        /// </summary>
        /// <param name="eventId">int: The Id of the CalendarItem object.</param>
        /// <returns>CalendarItem</returns>
        Task<CalendarItem> GetCalendarItem(int eventId);

        /// <summary>
        /// Adds a new CalendarItem object.
        /// </summary>
        /// <param name="eventItem">CalendarItem: The new CalendarItem object to be added.</param>
        /// <returns>CalendarItem: The CalendarItem object that was added.</returns>
        Task<CalendarItem> AddCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Updates a CalendarItem object.
        /// </summary>
        /// <param name="eventItem">CalendarItem: The CalendarItem object to be updated.</param>
        /// <returns>CalendarItem: The updated CalendarItem object.</returns>
        Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Removes the CalendarItem object with a given Id.
        /// </summary>
        /// <param name="eventId">int: The Id of the CalendarItem to remove.</param>
        /// <returns>bool: True if the CalendarItem object was successfully removed.</returns>
        Task<bool> DeleteCalendarItem(int eventId);

        /// <summary>
        /// Gets the list of CalendarItem objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of CalendarItem objects.</returns>
        Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the next 5 upcoming events in the progeny's calendar.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of CalendarItem objects.</returns>
        Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Contact with a given Id.
        /// </summary>
        /// <param name="contactId">int: The Id of the Contact.</param>
        /// <returns>Contact.</returns>
        Task<Contact> GetContact(int contactId);

        /// <summary>
        /// Adds a new Contact.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to add.</param>
        /// <returns>Contact: The Contact object that was added.</returns>
        Task<Contact> AddContact(Contact contact);

        /// <summary>
        /// Updates a Contact. The Contact with the same Id will be updated.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to update.</param>
        /// <returns>Contact: The updated Contact object.</returns>
        Task<Contact> UpdateContact(Contact contact);

        /// <summary>
        /// Removes the Contact with the given Id.
        /// </summary>
        /// <param name="contactId">int: The Id of the Contact object to remove.</param>
        /// <returns>bool: True if the Contact was successfully removed.</returns>
        Task<bool> DeleteContact(int contactId);

        /// <summary>
        /// Gets the list of Contact objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Contact objects.</returns>
        Task<List<Contact>> GetContactsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the address with a given Id.
        /// </summary>
        /// <param name="addressId">int: The Id of the address.</param>
        /// <returns></returns>
        Task<Address> GetAddress(int addressId);

        /// <summary>
        /// Adds a new Address.
        /// </summary>
        /// <param name="address">Address: The Address object to add.</param>
        /// <returns>Address: The added Address object.</returns>
        Task<Address> AddAddress(Address address);

        /// <summary>
        /// Updates an Address. The Address with the same Id will be updated.
        /// </summary>
        /// <param name="address">Address: The Address object to update.</param>
        /// <returns>Address: The updated Address object.</returns>
        Task<Address> UpdateAddress(Address address);

        /// <summary>
        /// Gets the Friend with the given Id.
        /// </summary>
        /// <param name="friendId">int: The Id of the Friend.</param>
        /// <returns>Friend</returns>
        Task<Friend> GetFriend(int friendId);

        /// <summary>
        /// Adds a new Friend.
        /// </summary>
        /// <param name="friend">Friend: The Friend object to add.</param>
        /// <returns>Friend: The Friend object that was added.</returns>
        Task<Friend> AddFriend(Friend friend);

        /// <summary>
        /// Updates a Friend. The Friend with the same Id will be updated.
        /// </summary>
        /// <param name="friend">Friend: The Friend object to update.</param>
        /// <returns>Friend: The updated Friend.</returns>
        Task<Friend> UpdateFriend(Friend friend);

        /// <summary>
        /// Removes the Friend with a given Id.
        /// </summary>
        /// <param name="friendId">int: The Id of the Friend to remove.</param>
        /// <returns>bool: True if the Friend was successfully removed.</returns>
        Task<bool> DeleteFriend(int friendId);

        /// <summary>
        /// Gets the list of Friend objects for a given progeny that the user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns></returns>
        Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Location with a given Id.
        /// </summary>
        /// <param name="locationId">int: The Id of the Location.</param>
        /// <returns>Location</returns>
        Task<Location> GetLocation(int locationId);

        /// <summary>
        /// Adds a new Location.
        /// </summary>
        /// <param name="location">Location: The Location to be added.</param>
        /// <returns>Location: The Location object that was added.</returns>
        Task<Location> AddLocation(Location location);

        /// <summary>
        /// Updates a Location. The Location with the same Id will be updated.
        /// </summary>
        /// <param name="location">Location: The Location to update.</param>
        /// <returns>Location: The updated Location object.</returns>
        Task<Location> UpdateLocation(Location location);

        /// <summary>
        /// Removes the Location with a given Id.
        /// </summary>
        /// <param name="locationId">int: The Id of the Location to remove.</param>
        /// <returns>bool: True if the Location was successfully removed.</returns>
        Task<bool> DeleteLocation(int locationId);

        /// <summary>
        /// Gets the list of Locations for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Location objects.</returns>
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
