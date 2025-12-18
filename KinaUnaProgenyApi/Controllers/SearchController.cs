using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.Search;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// Provides API endpoints for searching entities across progenies and families.
    /// All endpoints respect user access permissions.
    /// </summary>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController(
        ISearchService searchService,
        IUserInfoService userInfoService) : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> QuickSearch([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<TimeLineItem> result = await searchService.QuickSearch(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches calendar items by title, notes, location, and context.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching calendar items the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> CalendarItems([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<CalendarItem> result = await searchService.SearchCalendarItems(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches contacts by name fields, email, notes, website, context, and tags.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching contacts the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Contacts([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Contact> result = await searchService.SearchContacts(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches friends by name, description, context, notes, and tags.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching friends the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Friends([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Friend> result = await searchService.SearchFriends(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches Kanban boards by title, description, tags, and context.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching Kanban boards the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> KanbanBoards([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<KanbanBoard> result = await searchService.SearchKanbanBoards(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches locations by name, address fields, notes, and tags.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching locations the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Locations([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Location> result = await searchService.SearchLocations(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches measurements by eye color and hair color.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching measurements the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Measurements([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Measurement> result = await searchService.SearchMeasurements(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches notes by title, content, and category.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching notes the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Notes([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Note> result = await searchService.SearchNotes(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches pictures by tags and location.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching pictures the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Pictures([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Picture> result = await searchService.SearchPictures(request, currentUserInfo);
            return Ok(result);
        }
        
        /// <summary>
        /// Searches skills by name, description, and category.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching skills the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Skills([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Skill> result = await searchService.SearchSkills(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches sleep records by sleep notes.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching sleep records the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> SleepRecords([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Sleep> result = await searchService.SearchSleepRecords(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches todo items by title, description, notes, tags, context, and location.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching todo items the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> TodoItems([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<TodoItem> result = await searchService.SearchTodoItems(request, currentUserInfo);
            return Ok(result);
        }
        
        /// <summary>
        /// Searches vaccinations by name, description, and notes.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching vaccinations the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Vaccinations([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Vaccination> result = await searchService.SearchVaccinations(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches videos by tags and location.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching videos the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Videos([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<Video> result = await searchService.SearchVideos(request, currentUserInfo);
            return Ok(result);
        }

        /// <summary>
        /// Searches vocabulary items by word, description, language, and sounds-like.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>A list of matching vocabulary items the user has access to.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> VocabularyItems([FromBody] SearchRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (currentUserInfo == null) return Unauthorized();

            SearchResponse<VocabularyItem> result = await searchService.SearchVocabularyItems(request, currentUserInfo);
            return Ok(result);
        }
    }
}
