using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Utilities;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryAutoSuggestList(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            AutoSuggestListBuilder autoSuggestListBuilder = new();
            
            List<Note> allNotes = await noteService.GetNotesList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToCategoriesList(allNotes);

            List<Skill> allSkills = await skillService.GetSkillsList(id, accessLevelResult.Value);
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
        /// <returns>List of string</returns>
        [Route("[action]/{id:int}")]
        [HttpGet]
        public async Task<IActionResult> GetContextAutoSuggestList(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            AutoSuggestListBuilder autoSuggestListBuilder = new();

            List<Friend> allFriends = await friendService.GetFriendsList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToContextsList(allFriends);
            
            List<CalendarItem> allCalendarItems = await calendarService.GetCalendarList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToContextsList(allCalendarItems);

            List<Contact> allContacts = await contactService.GetContactsList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToContextsList(allContacts);
            
            List<string> autoSuggestList = autoSuggestListBuilder.GetContextsList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for location auto suggest inputs for a given Progeny.
        /// Only returns locations with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny.</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            AutoSuggestListBuilder autoSuggestListBuilder = new();

            List<Picture> allPictures = await picturesService.GetPicturesList(id, accessLevelResult.Value); 
            autoSuggestListBuilder.AddItemsToLocationsList(allPictures);
            
            List<Video> allVideos = await videosService.GetVideosList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToLocationsList(allVideos);

            List<CalendarItem> allCalendarItems = await calendarService.GetCalendarList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToLocationsList(allCalendarItems);

            List<Location> allLocations = await locationService.GetLocationsList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToLocationsList(allLocations);

            List<string> autoSuggestList = autoSuggestListBuilder.GetLocationsList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for tag auto suggest inputs for a given Progeny.
        /// Only returns tags with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny.</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            AutoSuggestListBuilder autoSuggestListBuilder = new();

            List<Picture> allPictures = await picturesService.GetPicturesList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToTagsList(allPictures);

            List<Video> allVideos = await videosService.GetVideosList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToTagsList(allVideos);

            List<Location> allLocations = await locationService.GetLocationsList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToTagsList(allLocations);

            List<Friend> allFriends = await friendService.GetFriendsList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToTagsList(allFriends);

            List<Contact> allContacts = await contactService.GetContactsList(id, accessLevelResult.Value);
            autoSuggestListBuilder.AddItemsToTagsList(allContacts);

            List<string> autoSuggestList = autoSuggestListBuilder.GetTagsList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for language auto suggest inputs when adding or editing a VocabularyItem for a given Progeny.
        /// Only returns languages with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="id">The id of the Progeny.</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{id:int}")]
        [HttpGet]
        public async Task<IActionResult> GetVocabularyLanguagesSuggestList(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<VocabularyItem> allVocabularyItems = await vocabularyService.GetVocabularyList(id, accessLevelResult.Value);
            
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
