using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Utilities;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for retrieving auto suggest lists.
    /// </summary>
    /// <param name="userAccessService"></param>
    /// <param name="calendarService"></param>
    /// <param name="contactService"></param>
    /// <param name="friendService"></param>
    /// <param name="noteService"></param>
    /// <param name="skillService"></param>
    /// <param name="picturesService"></param>
    /// <param name="videosService"></param>
    /// <param name="locationService"></param>
    /// <param name="vocabularyService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AutoSuggestsController(
        IUserAccessService userAccessService,
        ICalendarService calendarService,
        IContactService contactService,
        IFriendService friendService,
        INoteService noteService,
        ISkillService skillService,
        IPicturesService picturesService,
        IVideosService videosService,
        ILocationService locationService,
        IVocabularyService vocabularyService)
        : ControllerBase
    {
        /// <summary>
        /// Provides a list of strings for category auto suggest inputs for a given Progeny.
        /// Only returns categories with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny</param>
        /// <param name="accessLevel">The user's access level for this Progeny</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null)
            {
                if (id != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                userAccess = await userAccessService.GetProgenyUserAccessForUser(Constants.DefaultChildId, userEmail);
            }

            if (userAccess.AccessLevel > accessLevel)
            {
                accessLevel = userAccess.AccessLevel;
            }

            AutoSuggestListBuilder autoSuggestListBuilder = new AutoSuggestListBuilder();
            
            List<Note> allNotes = await noteService.GetNotesList(id);
            allNotes = allNotes.Where(p => p.AccessLevel >= accessLevel).ToList();
            autoSuggestListBuilder.AddItemsToCategoriesList(allNotes);

            List<Skill> allSkills = await skillService.GetSkillsList(id);
            allSkills = allSkills.Where(p => p.AccessLevel >= accessLevel).ToList();
            autoSuggestListBuilder.AddItemsToCategoriesList(allSkills);
            
            List<string> autoSuggestList = autoSuggestListBuilder.GetCategoriesList(); 
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Provides a list of strings for context auto suggest inputs for a given Progeny.
        /// Only returns contexts with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny</param>
        /// <param name="accessLevel"></param>
        /// <returns>List of string</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetContextAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<string> autoSuggestList = [];

            List<Friend> allFriends = await friendService.GetFriendsList(id);
            allFriends = allFriends.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Friend friendItem in allFriends)
            {
                if (string.IsNullOrEmpty(friendItem.Context)) continue;

                List<string> tagsList = [.. friendItem.Context.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<CalendarItem> allCalendarItems = await calendarService.GetCalendarList(id, accessLevel);
            foreach (CalendarItem calendarItem in allCalendarItems)
            {
                if (string.IsNullOrEmpty(calendarItem.Context)) continue;

                List<string> tagsList = [.. calendarItem.Context.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Contact> allContacts = await contactService.GetContactsList(id);
            allContacts = allContacts.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Contact contactItem in allContacts)
            {
                if (string.IsNullOrEmpty(contactItem.Context)) continue;

                List<string> tagsList = [.. contactItem.Context.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for location auto suggest inputs for a given Progeny.
        /// Only returns locations with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny.</param>
        /// <param name="accessLevel">The user's access level.</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> allPictures = await picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (Picture picture in allPictures)
            {
                if (string.IsNullOrEmpty(picture.Location)) continue;

                List<string> locationsList = [.. picture.Location.Split(',')];
                foreach (string locationString in locationsList)
                {
                    if (!string.IsNullOrEmpty(locationString) && !autoSuggestList.Contains(locationString.Trim()))
                    {
                        autoSuggestList.Add(locationString.Trim());
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Location)) continue;

                List<string> locationsList = [.. video.Location.Split(',')];
                foreach (string locationString in locationsList)
                {
                    if (!string.IsNullOrEmpty(locationString) && !autoSuggestList.Contains(locationString.Trim()))
                    {
                        autoSuggestList.Add(locationString.Trim());
                    }
                }
            }

            List<CalendarItem> allCalendarItems = await calendarService.GetCalendarList(id, accessLevel);
            foreach (CalendarItem calendarItem in allCalendarItems)
            {
                if (string.IsNullOrEmpty(calendarItem.Location)) continue;

                List<string> locationsList = [.. calendarItem.Location.Split(',')];
                foreach (string locationString in locationsList)
                {
                    if (!string.IsNullOrEmpty(locationString) && !autoSuggestList.Contains(locationString.Trim()))
                    {
                        autoSuggestList.Add(locationString.Trim());
                    }
                }
            }

            List<Location> allLocations = await locationService.GetLocationsList(id);
            allLocations = allLocations.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Location locationItem in allLocations)
            {
                if (string.IsNullOrEmpty(locationItem.Name)) continue;

                List<string> locationsList = [.. locationItem.Name.Split(',')];
                foreach (string locationString in locationsList)
                {
                    if (!string.IsNullOrEmpty(locationString) && !autoSuggestList.Contains(locationString.Trim()))
                    {
                        autoSuggestList.Add(locationString.Trim());
                    }
                }
            }

            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for tag auto suggest inputs for a given Progeny.
        /// Only returns tags with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny.</param>
        /// <param name="accessLevel"></param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> allPictures = await picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (Picture picture in allPictures)
            {
                if (string.IsNullOrEmpty(picture.Tags)) continue;

                List<string> tagsList = [.. picture.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Tags)) continue;

                List<string> tagsList = [.. video.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Location> allLocations = await locationService.GetLocationsList(id);
            allLocations = allLocations.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Location location in allLocations)
            {
                if (string.IsNullOrEmpty(location.Tags)) continue;

                List<string> tagsList = [.. location.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Friend> allFriends = await friendService.GetFriendsList(id);
            allFriends = allFriends.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Friend friend in allFriends)
            {
                if (string.IsNullOrEmpty(friend.Tags)) continue;

                List<string> tagsList = [.. friend.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Contact> allContacts = await contactService.GetContactsList(id);
            allContacts = allContacts.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Contact contact in allContacts)
            {
                if (string.IsNullOrEmpty(contact.Tags)) continue;

                List<string> tagsList = [.. contact.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    string trimmedTagString = tagString.Trim();
                    if (!string.IsNullOrEmpty(trimmedTagString) && !autoSuggestList.Contains(trimmedTagString))
                    {
                        autoSuggestList.Add(trimmedTagString);
                    }
                }
            }

            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for language auto suggest inputs when adding or editing a VocabularyItem for a given Progeny.
        /// Only returns languages with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny.</param>
        /// <param name="accessLevel"></param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetVocabularyLanguagesSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<VocabularyItem> allVocabularyItems = await vocabularyService.GetVocabularyList(id);
            allVocabularyItems = allVocabularyItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (VocabularyItem vocabularyItem in allVocabularyItems)
            {
                if (string.IsNullOrEmpty(vocabularyItem.Language)) continue;

                List<string> languageList = [.. vocabularyItem.Language.Split(',')];
                foreach (string languageString in languageList)
                {
                    if (!autoSuggestList.Contains(languageString.Trim()))
                    {
                        autoSuggestList.Add(languageString.Trim());
                    }
                }
            }
            
            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }
    }
}
