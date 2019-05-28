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
        /// Gets a user's information from the UserId.
        /// </summary>
        /// <param name="userId">string: The user's UserId (ApplicationUser.Id and UserInfo.UserId).</param>
        /// <returns>UserInfo</returns>
        Task<UserInfo> GetUserInfoByUserId(string userId);

        /// <summary>
        /// Gets progeny from the progeny's id.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
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
        /// <param name="progenyId">int: The Id of the progeny to be removed (Progeny.Id).</param>
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
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
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
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the latest 5 posts (progeny time, not added time) for a progeny, that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's Id (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyLatestPosts(int progenyId, int accessLevel);

        /// <summary>
        /// Gets all the posts from today's date last year (progeny time, not added time), that the user has access to.
        /// </summary>
        /// <param name="progenyId">int: The progeny's id.</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of TimeLineItem objects.</returns>
        Task<List<TimeLineItem>> GetProgenyYearAgo(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the UserAccess with a given UserAccess Id.
        /// </summary>
        /// <param name="userAccessId">int: The Id of the UserAccess (UserAccess.AccessId).</param>
        /// <returns>UserAccess</returns>
        Task<UserAccess> GetUserAccess(int userAccessId);

        /// <summary>
        /// Adds a new UserAccess.
        /// </summary>
        /// <param name="userAccess">UserAccess: The UserAccess object to be added.</param>
        /// <returns>UserAccess: The UserAccess object that was added.</returns>
        Task<UserAccess> AddUserAccess(UserAccess userAccess);

        /// <summary>
        /// Updates a UserAccess object. The UserAccess with the same AccessId will be updated.
        /// </summary>
        /// <param name="userAccess">UserAccess: The UserAccess object to be updated.</param>
        /// <returns>UserAccess: The updated UserAccess object.</returns>
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);

        /// <summary>
        /// Removes a UserAccess object.
        /// </summary>
        /// <param name="userAccessId">int: The UserAccess object's Id (UserAccess.AccessId).</param>
        /// <returns>bool: True if the UserAccess object was successfully removed.</returns>
        Task<bool> DeleteUserAccess(int userAccessId);

        /// <summary>
        /// Updates a UserInfo object. The UserInfo with the same Id will be updated.
        /// </summary>
        /// <param name="userinfo">UserInfo: The UserInfo object to update.</param>
        /// <returns>UserInfo: The updated UserInfo object.</returns>
        Task<UserInfo> UpdateUserInfo(UserInfo userinfo);

        /// <summary>
        /// Gets a Sleep with a given SleepId.
        /// </summary>
        /// <param name="sleepId">int: The Id of the sleep object (Sleep.SleepId).</param>
        /// <returns>Sleep</returns>
        Task<Sleep> GetSleepItem(int sleepId);

        /// <summary>
        /// Adds a new Sleep object.
        /// </summary>
        /// <param name="sleep">Sleep: The Sleep object to be added.</param>
        /// <returns>Sleep: The added sleep object.</returns>
        Task<Sleep> AddSleep(Sleep sleep);

        /// <summary>
        /// Updates a Sleep object. The Sleep with the same SleepId will be updated.
        /// </summary>
        /// <param name="sleep">Sleep: The Sleep object to update.</param>
        /// <returns>Sleep: The updated Sleep object.</returns>
        Task<Sleep> UpdateSleep(Sleep sleep);

        /// <summary>
        /// Removes the Sleep object with a given SleepId.
        /// </summary>
        /// <param name="sleepId">int: The id of the Sleep object to remove (Sleep.SleepId).</param>
        /// <returns>bool: True if the Sleep object was successfully removed.</returns>
        Task<bool> DeleteSleepItem(int sleepId);

        /// <summary>
        /// Gets the List of Sleep objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The access level of the user.</param>
        /// <returns>List of Sleep objects.</returns>
        Task<List<Sleep>> GetSleepList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets a CalendarItem with a given EventId.
        /// </summary>
        /// <param name="eventId">int: The Id of the CalendarItem object (CalendarItem.EventId).</param>
        /// <returns>CalendarItem</returns>
        Task<CalendarItem> GetCalendarItem(int eventId);

        /// <summary>
        /// Adds a new CalendarItem object.
        /// </summary>
        /// <param name="eventItem">CalendarItem: The new CalendarItem object to be added.</param>
        /// <returns>CalendarItem: The CalendarItem object that was added.</returns>
        Task<CalendarItem> AddCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Updates a CalendarItem object. The CalendarItem with the same EventId will be updated.
        /// </summary>
        /// <param name="eventItem">CalendarItem: The CalendarItem object to be updated.</param>
        /// <returns>CalendarItem: The updated CalendarItem object.</returns>
        Task<CalendarItem> UpdateCalendarItem(CalendarItem eventItem);

        /// <summary>
        /// Removes the CalendarItem object with a given EventId.
        /// </summary>
        /// <param name="eventId">int: The Id of the CalendarItem to remove.</param>
        /// <returns>bool: True if the CalendarItem object was successfully removed.</returns>
        Task<bool> DeleteCalendarItem(int eventId);

        /// <summary>
        /// Gets the list of CalendarItem objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of CalendarItem objects.</returns>
        Task<List<CalendarItem>> GetCalendarList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the next 5 upcoming events in the progeny's calendar.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of CalendarItem objects.</returns>
        Task<List<CalendarItem>> GetUpcomingEvents(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Contact with a given ContactId.
        /// </summary>
        /// <param name="contactId">int: The Id of the Contact (Contact.ContactId).</param>
        /// <returns>Contact.</returns>
        Task<Contact> GetContact(int contactId);

        /// <summary>
        /// Adds a new Contact.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to add.</param>
        /// <returns>Contact: The Contact object that was added.</returns>
        Task<Contact> AddContact(Contact contact);

        /// <summary>
        /// Updates a Contact. The Contact with the same ContactId will be updated.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to update.</param>
        /// <returns>Contact: The updated Contact object.</returns>
        Task<Contact> UpdateContact(Contact contact);

        /// <summary>
        /// Removes the Contact with the given ContactId.
        /// </summary>
        /// <param name="contactId">int: The Id of the Contact object to remove (Contact.ContactId).</param>
        /// <returns>bool: True if the Contact was successfully removed.</returns>
        Task<bool> DeleteContact(int contactId);

        /// <summary>
        /// Gets the list of Contact objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Contact objects.</returns>
        Task<List<Contact>> GetContactsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the address with a given AddressId.
        /// </summary>
        /// <param name="addressId">int: The Id of the address (Address.AddressId).</param>
        /// <returns>Address</returns>
        Task<Address> GetAddress(int addressId);

        /// <summary>
        /// Adds a new Address.
        /// </summary>
        /// <param name="address">Address: The Address object to add.</param>
        /// <returns>Address: The added Address object.</returns>
        Task<Address> AddAddress(Address address);

        /// <summary>
        /// Updates an Address. The Address with the same AddressId will be updated.
        /// </summary>
        /// <param name="address">Address: The Address object to update.</param>
        /// <returns>Address: The updated Address object.</returns>
        Task<Address> UpdateAddress(Address address);

        /// <summary>
        /// Gets the Friend with the given FriendId.
        /// </summary>
        /// <param name="friendId">int: The Id of the Friend (Friend.FriendId).</param>
        /// <returns>Friend</returns>
        Task<Friend> GetFriend(int friendId);

        /// <summary>
        /// Adds a new Friend.
        /// </summary>
        /// <param name="friend">Friend: The Friend object to add.</param>
        /// <returns>Friend: The Friend object that was added.</returns>
        Task<Friend> AddFriend(Friend friend);

        /// <summary>
        /// Updates a Friend. The Friend with the same FriendId will be updated.
        /// </summary>
        /// <param name="friend">Friend: The Friend object to update.</param>
        /// <returns>Friend: The updated Friend.</returns>
        Task<Friend> UpdateFriend(Friend friend);

        /// <summary>
        /// Removes the Friend with a given FriendId.
        /// </summary>
        /// <param name="friendId">int: The Id of the Friend to remove (Friend.FriendId).</param>
        /// <returns>bool: True if the Friend was successfully removed.</returns>
        Task<bool> DeleteFriend(int friendId);

        /// <summary>
        /// Gets the list of Friend objects for a given progeny that the user has access to.
        /// </summary>
        /// <param name="progenyId">int: The id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns></returns>
        Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">int: The Id of the Location (Location.LocationId).</param>
        /// <returns>Location</returns>
        Task<Location> GetLocation(int locationId);

        /// <summary>
        /// Adds a new Location.
        /// </summary>
        /// <param name="location">Location: The Location to be added.</param>
        /// <returns>Location: The Location object that was added.</returns>
        Task<Location> AddLocation(Location location);

        /// <summary>
        /// Updates a Location. The Location with the same LocationId will be updated.
        /// </summary>
        /// <param name="location">Location: The Location to update.</param>
        /// <returns>Location: The updated Location object.</returns>
        Task<Location> UpdateLocation(Location location);

        /// <summary>
        /// Removes the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">int: The Id of the Location to remove (Location.LocationId).</param>
        /// <returns>bool: True if the Location was successfully removed.</returns>
        Task<bool> DeleteLocation(int locationId);

        /// <summary>
        /// Gets the list of Locations for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Location objects.</returns>
        Task<List<Location>> GetLocationsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Measurement with the given MeasurementId.
        /// </summary>
        /// <param name="measurementId">int: The Measurement Id (Measurement.MeasurementId).</param>
        /// <returns>Measurement: The Measurement object.</returns>
        Task<Measurement> GetMeasurement(int measurementId);

        /// <summary>
        /// Adds a new Measurement. 
        /// </summary>
        /// <param name="measurement">Measurement: The Measurement object to be added.</param>
        /// <returns>Measurement: The Measurement object that was added.</returns>
        Task<Measurement> AddMeasurement(Measurement measurement);

        /// <summary>
        /// Updates a Measurement. The Measurement with the same MeasurementId will be updated.
        /// </summary>
        /// <param name="measurement">Measurement: The Measurement to update.</param>
        /// <returns>Measurement: The updated Measurement object.</returns>
        Task<Measurement> UpdateMeasurement(Measurement measurement);

        /// <summary>
        /// Removes the Measurement with a given MeasurementId.
        /// </summary>
        /// <param name="measurementId">int: The Id of the Measurement to remove (Measurement.MeasurementId).</param>
        /// <returns>bool: True if the Measurement was successfully removed.</returns>
        Task<bool> DeleteMeasurement(int measurementId);

        /// <summary>
        /// Gets the list of Measurements for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Measurement objects.</returns>
        Task<List<Measurement>> GetMeasurementsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Note with the given NoteId.
        /// </summary>
        /// <param name="noteId">int: The Id of the Note (Note.NoteId).</param>
        /// <returns>Note: The Note object.</returns>
        Task<Note> GetNote(int noteId);

        /// <summary>
        /// Adds a new Note.
        /// </summary>
        /// <param name="note">Note: The new Note to add.</param>
        /// <returns>Note</returns>
        Task<Note> AddNote(Note note);

        /// <summary>
        /// Updates a Note. The Note with the same NoteId will be updated.
        /// </summary>
        /// <param name="note">Note: The Note to update.</param>
        /// <returns>Note: The updated Note object.</returns>
        Task<Note> UpdateNote(Note note);

        /// <summary>
        /// Removes the Note with a given NoteId.
        /// </summary>
        /// <param name="noteId">int: The Id of the Note to remove (Note.NoteId).</param>
        /// <returns>bool: True if the Note was successfully removed.</returns>
        Task<bool> DeleteNote(int noteId);

        /// <summary>
        /// Gets a progeny's list of Notes for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Note objects.</returns>
        Task<List<Note>> GetNotesList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Skill with the given SkillId.
        /// </summary>
        /// <param name="skillId">int: The Id of the Skill (Skill.SkillId).</param>
        /// <returns>Skill: The Skill object.</returns>
        Task<Skill> GetSkill(int skillId);

        /// <summary>
        /// Adds a new Skill.
        /// </summary>
        /// <param name="skill">Skill: The new Skill to add.</param>
        /// <returns>Skill</returns>
        Task<Skill> AddSkill(Skill skill);

        /// <summary>
        /// Updates a Skill. The Skill with the same SkillId will be updated.
        /// </summary>
        /// <param name="skill">Skill: The Skill to update.</param>
        /// <returns>Skill: The updated Skill object.</returns>
        Task<Skill> UpdateSkill(Skill skill);

        /// <summary>
        /// Removes the Skill with a given SkillId.
        /// </summary>
        /// <param name="skillId">int: The Id of the Skill to remove (Skill.SkillId).</param>
        /// <returns>bool: True if the Skill was successfully removed.</returns>
        Task<bool> DeleteSkill(int skillId);

        /// <summary>
        /// Gets a progeny's list of Skills that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Skill objects.</returns>
        Task<List<Skill>> GetSkillsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">int: The Id of the Vaccination (Vaccination.VaccinationId).</param>
        /// <returns>Vaccination: The Vaccination object.</returns>
        Task<Vaccination> GetVaccination(int vaccinationId);

        /// <summary>
        /// Adds a new Vaccination.
        /// </summary>
        /// <param name="vaccination">Vaccination: The new Vaccination to add.</param>
        /// <returns>Vaccination</returns>
        Task<Vaccination> AddVaccination(Vaccination vaccination);

        /// <summary>
        /// Updates a Vaccination. The Vaccination with the same VaccinationId will be updated.
        /// </summary>
        /// <param name="vaccination">Vaccination: The Vaccination to update.</param>
        /// <returns>Vaccination: The updated Vaccination.</returns>
        Task<Vaccination> UpdateVaccination(Vaccination vaccination);

        /// <summary>
        /// Removes the Vaccination with the given VaccinationId.
        /// </summary>
        /// <param name="vaccinationId">int: The Id of the Vaccination to remove (Vaccination.VaccinationId).</param>
        /// <returns>bool: True if the Vaccination was successfully removed.</returns>
        Task<bool> DeleteVaccination(int vaccinationId);

        /// <summary>
        /// Gets a progeny's list of Vaccinations that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Vaccination objects.</returns>
        Task<List<Vaccination>> GetVaccinationsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">int: The Id of the VocabularyItem (VocabularyItem.WordId).</param>
        /// <returns>VocabularyItem.</returns>
        Task<VocabularyItem> GetWord(int wordId);

        /// <summary>
        /// Adds a new VocabularyItem.
        /// </summary>
        /// <param name="word">VocabularyItem: The new VocabularyItem to add.</param>
        /// <returns>VocabularyItem</returns>
        Task<VocabularyItem> AddWord(VocabularyItem word);

        /// <summary>
        /// Updates a VocabularyItem. The VocabularyItem with the same WordId will be updated.
        /// </summary>
        /// <param name="word">VocabularyItem: The VocabularyItem to update.</param>
        /// <returns>VocabularyItem: The updated VocabularyItem object.</returns>
        Task<VocabularyItem> UpdateWord(VocabularyItem word);

        /// <summary>
        /// Removes the VocabularyItem with the given WordId.
        /// </summary>
        /// <param name="wordId">int: The Id of the VocabularyItem to remove (VocabularyItem.WordId).</param>
        /// <returns>bool: True if the VocabularyItem was successfully removed.</returns>
        Task<bool> DeleteWord(int wordId);

        /// <summary>
        /// Gets a progeny's list of VocabularyItems that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of VocabularyItem objects.</returns>
        Task<List<VocabularyItem>> GetWordsList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets the TimeLineItem with the given type and the Id (Not the TimeLineItem.TimeLineId, but the type's own Id property).
        /// </summary>
        /// <param name="itemId">int: The item's Id. That is PictureId, VideoId, NoteId, WordId, etc.</param>
        /// <param name="itemType">int: The type of the item. Defined in the KinaUnaTypes.TimeLineType enum.</param>
        /// <returns>TimeLineItem</returns>
        Task<TimeLineItem> GetTimeLineItem(string itemId, int itemType);

        /// <summary>
        /// Adds a new TimeLineItem.
        /// </summary>
        /// <param name="timeLineItem">TimeLineItem: The new TimeLineItem to add.</param>
        /// <returns>TimeLineItem</returns>
        Task<TimeLineItem> AddTimeLineItem(TimeLineItem timeLineItem);

        /// <summary>
        /// Updates a TimeLineItem. The TimeLineItem with the same TimeLineId will be updated.
        /// </summary>
        /// <param name="timeLineItem">TimeLineItem: The TimeLineItem to update.</param>
        /// <returns>TimeLineItem: The updated TimeLineItem.</returns>
        Task<TimeLineItem> UpdateTimeLineItem(TimeLineItem timeLineItem);

        /// <summary>
        /// Removes the TimeLineItem with the given TimeLineId
        /// </summary>
        /// <param name="timeLineItemId">int: The TimeLineId of the TimeLineItem to remove (TimeLineItem.TimeLineId).</param>
        /// <returns>bool: True if the TimeLineItem was successfully removed.</returns>
        Task<bool> DeleteTimeLineItem(int timeLineItemId);

        /// <summary>
        /// Gets a progeny's list of TimeLineItems that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns></returns>
        Task<List<TimeLineItem>> GetTimeline(int progenyId, int accessLevel);

        /// <summary>
        /// Sets the ViewChild for a given user.
        /// </summary>
        /// <param name="userId">string: The user's UserId (UserInfo.UserId or ApplicationUser.Id).</param>
        /// <param name="userinfo">UserInfo: The user's UserInfo.</param>
        /// <returns></returns>
        Task SetViewChild(string userId, UserInfo userinfo);
    }
}
