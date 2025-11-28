using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Utilities;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for retrieving auto suggest lists.
    /// </summary>
    /// <param name="calendarService"></param>
    /// <param name="contactService"></param>
    /// <param name="friendService"></param>
    /// <param name="noteService"></param>
    /// <param name="skillService"></param>
    /// <param name="picturesService"></param>
    /// <param name="videosService"></param>
    /// <param name="locationService"></param>
    /// <param name="vocabularyService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AutoSuggestsController(
        ICalendarService calendarService,
        IContactService contactService,
        IFriendService friendService,
        INoteService noteService,
        ISkillService skillService,
        IPicturesService picturesService,
        IVideosService videosService,
        ILocationService locationService,
        IVocabularyService vocabularyService,
        ITodosService todosService,
        IKanbanBoardsService kanbanBoardsService,
        IUserInfoService userInfoService)
        : ControllerBase
    {
        /// <summary>
        /// Provides a list of strings for category auto suggest inputs for a given Progeny.
        /// Only returns categories with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="progenyId">The progenyId of the Progeny</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{progenyId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryAutoSuggestList(int progenyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            AutoSuggestListBuilder autoSuggestListBuilder = new();
            
            List<Note> allNotes = await noteService.GetNotesList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToCategoriesList(allNotes);

            List<Skill> allSkills = await skillService.GetSkillsList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToCategoriesList(allSkills);
            
            List<string> autoSuggestList = autoSuggestListBuilder.GetCategoriesList(); 
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Provides a list of strings for context auto suggest inputs for a given Progeny.
        /// Only returns contexts with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="progenyId">The progenyId of the Progeny</param>
        /// <param name="familyId">The progenyId of the Family, if any. Default is 0.</param>
        /// <returns>List of string</returns>
        [Route("[action]/{progenyId:int}/{familyId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetContextAutoSuggestList(int progenyId, int familyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            AutoSuggestListBuilder autoSuggestListBuilder = new();

            List<Friend> allFriends = await friendService.GetFriendsList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToContextsList(allFriends);
            
            List<CalendarItem> allCalendarItems = await calendarService.GetCalendarList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToContextsList(allCalendarItems);

            List<Contact> allContacts = await contactService.GetContactsList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToContextsList(allContacts);

            List<TodoItem> allTodos = await todosService.GetTodosList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToContextsList(allTodos);
            
            List<KanbanBoard> allKanbanBoards = await kanbanBoardsService.GetKanbanBoardsListForProgenyOrFamily(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToContextsList(allKanbanBoards);
            
            List<string> autoSuggestList = autoSuggestListBuilder.GetContextsList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for location auto suggest inputs for a given Progeny.
        /// Only returns locations with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="progenyId">The progenyId of the Progeny.</param>
        /// <param name="familyId">The progenyId of the Family, if any. Default is 0.</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{progenyId:int}/{familyId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int progenyId, int familyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            AutoSuggestListBuilder autoSuggestListBuilder = new();
            
            List<CalendarItem> allCalendarItems = await calendarService.GetCalendarList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToLocationsList(allCalendarItems);

            List<Location> allLocations = await locationService.GetLocationsList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToLocationsList(allLocations);

            List<TodoItem> allTodoItems = await todosService.GetTodosList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToLocationsList(allTodoItems);

            List<Picture> allPictures = await picturesService.GetPicturesList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToLocationsList(allPictures);

            List<Video> allVideos = await videosService.GetVideosList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToLocationsList(allVideos);

            List<string> autoSuggestList = autoSuggestListBuilder.GetLocationsList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for tag auto suggest inputs for a given Progeny.
        /// Only returns tags with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="progenyId">The progenyId of the Progeny.</param>
        /// <param name="familyId"></param>
        /// <returns>List of string.</returns>
        [Route("[action]/{progenyId:int}/{familyId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int progenyId, int familyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            AutoSuggestListBuilder autoSuggestListBuilder = new();
            
            List<Location> allLocations = await locationService.GetLocationsList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allLocations);

            List<Friend> allFriends = await friendService.GetFriendsList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allFriends);

            List<Contact> allContacts = await contactService.GetContactsList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allContacts);

            List<TodoItem> allTodoItems = await todosService.GetTodosList(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allTodoItems);

            List<KanbanBoard> allKanbanBoards = await kanbanBoardsService.GetKanbanBoardsListForProgenyOrFamily(progenyId, familyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allKanbanBoards);
            
            List<Picture> allPictures = await picturesService.GetPicturesList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allPictures);

            List<Video> allVideos = await videosService.GetVideosList(progenyId, currentUserInfo);
            autoSuggestListBuilder.AddItemsToTagsList(allVideos);

            List<string> autoSuggestList = autoSuggestListBuilder.GetTagsList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        /// <summary>
        /// Returns a list of strings for language auto suggest inputs when adding or editing a VocabularyItem for a given Progeny.
        /// Only returns languages with an access level equal to or higher than the accessLevel parameter.
        /// </summary>
        /// <param name="progenyId">The progenyId of the Progeny.</param>
        /// <returns>List of string.</returns>
        [Route("[action]/{progenyId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetVocabularyLanguagesSuggestList(int progenyId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<VocabularyItem> allVocabularyItems = await vocabularyService.GetVocabularyList(progenyId, currentUserInfo);
            
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
            
            autoSuggestList = [.. autoSuggestList.Distinct()];
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }
    }
}
