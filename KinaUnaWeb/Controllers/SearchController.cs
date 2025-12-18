using KinaUna.Data.Extensions;
using KinaUna.Data.Models.Search;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.Search;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Controller for handling search functionality across all entity types.
    /// </summary>
    [Authorize]
    public class SearchController(ISearchHttpClient searchHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        /// <summary>
        /// Displays the main search page.
        /// </summary>
        /// <returns>The search page view.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            SearchViewModel model = new(baseModel);

            // Default to searching all progenies and families the user has access to
            if (model.CurrentUser != null)
            {
                model.ProgenyIds = model.CurrentUser.ProgenyList?.Select(p => p.Id).ToList() ?? [];
                model.FamilyIds = model.CurrentUser.FamilyList?.Select(f => f.FamilyId).ToList() ?? [];
            }

            // Default to all entity types selected
            model.SelectedEntityTypes = SearchViewModel.AvailableEntityTypes;

            return View(model);
        }

        /// <summary>
        /// Performs a search across selected entity types.
        /// </summary>
        /// <param name="model">The search view model containing query and filters.</param>
        /// <returns>The search results view.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SearchViewModel model)
        {
            BaseItemsViewModel baseItemsViewModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            model.SetBaseProperties(baseItemsViewModel);

            if (string.IsNullOrWhiteSpace(model.Query))
            {
                return View(model);
            }

            // If no entity types selected, search all
            if (model.SelectedEntityTypes == null || model.SelectedEntityTypes.Count == 0)
            {
                model.SelectedEntityTypes = SearchViewModel.AvailableEntityTypes;
            }

            SearchRequest request = new()
            {
                Query = model.Query,
                ProgenyIds = model.ProgenyIds ?? [],
                FamilyIds = model.FamilyIds ?? [],
                Skip = model.Skip,
                NumberOfItems = model.NumberOfItems,
                Sort = model.Sort
            };

            // Execute searches in parallel for selected entity types
            List<Task> searchTasks = [];

            if (model.SelectedEntityTypes.Contains("CalendarItems"))
            {
                searchTasks.Add(SearchCalendarItemsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Contacts"))
            {
                searchTasks.Add(SearchContactsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Friends"))
            {
                searchTasks.Add(SearchFriendsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("KanbanBoards"))
            {
                searchTasks.Add(SearchKanbanBoardsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Locations"))
            {
                searchTasks.Add(SearchLocationsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Measurements"))
            {
                searchTasks.Add(SearchMeasurementsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Notes"))
            {
                searchTasks.Add(SearchNotesAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Pictures"))
            {
                searchTasks.Add(SearchPicturesAsync(model, request));
            }
            
            if (model.SelectedEntityTypes.Contains("Skills"))
            {
                searchTasks.Add(SearchSkillsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("SleepRecords"))
            {
                searchTasks.Add(SearchSleepRecordsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("TodoItems"))
            {
                searchTasks.Add(SearchTodoItemsAsync(model, request));
            }
            
            if (model.SelectedEntityTypes.Contains("Vaccinations"))
            {
                searchTasks.Add(SearchVaccinationsAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("Videos"))
            {
                searchTasks.Add(SearchVideosAsync(model, request));
            }

            if (model.SelectedEntityTypes.Contains("VocabularyItems"))
            {
                searchTasks.Add(SearchVocabularyItemsAsync(model, request));
            }

            await Task.WhenAll(searchTasks);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch()
        {

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            SearchViewModel model = new(baseModel);

            // Default to searching all progenies and families the user has access to
            if (model.CurrentUser != null)
            {
                model.ProgenyIds = model.CurrentUser.ProgenyList?.Select(p => p.Id).ToList() ?? [];
                model.FamilyIds = model.CurrentUser.FamilyList?.Select(f => f.FamilyId).ToList() ?? [];
            }

            // Default to all entity types selected
            model.SelectedEntityTypes = SearchViewModel.AvailableEntityTypes;

            return PartialView("_QuickSearchPartial", model);
        }

        /// <summary>
        /// Performs a search across selected entity types.
        /// </summary>
        /// <param name="model">The search view model containing query and filters.</param>
        /// <returns>The search results view.</returns>
        [HttpPost]
        public async Task<IActionResult> QuickSearch([FromBody] SearchViewModel model)
        {
            BaseItemsViewModel baseItemsViewModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            model.SetBaseProperties(baseItemsViewModel);

            if (string.IsNullOrWhiteSpace(model.Query))
            {
                return Json(model.TimelineItemsResults);
            }
            // Default to searching all progenies and families the user has access to
            if (model.CurrentUser != null)
            {
                model.ProgenyIds = model.CurrentUser.ProgenyList?.Select(p => p.Id).ToList() ?? [];
                model.FamilyIds = model.CurrentUser.FamilyList?.Select(f => f.FamilyId).ToList() ?? [];
            }
            
            SearchRequest request = new()
            {
                Query = model.Query,
                ProgenyIds = model.ProgenyIds ?? [],
                FamilyIds = model.FamilyIds ?? [],
                Skip = model.Skip,
                NumberOfItems = model.NumberOfItems,
                Sort = model.Sort
            };
            
            model.TimelineItemsResults = await searchHttpClient.QuickSearch(request);

            return Json(model.TimelineItemsResults);
        }

        #region Individual Search Actions (for AJAX calls)

        /// <summary>
        /// Searches calendar items and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchCalendarItems([FromBody] SearchRequest request)
        {
            SearchResponse<CalendarItem> result = await searchHttpClient.SearchCalendarItems(request);
            return Json(result);
        }

        /// <summary>
        /// Searches contacts and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchContacts([FromBody] SearchRequest request)
        {
            SearchResponse<Contact> result = await searchHttpClient.SearchContacts(request);
            return Json(result);
        }

        /// <summary>
        /// Searches friends and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchFriends([FromBody] SearchRequest request)
        {
            SearchResponse<Friend> result = await searchHttpClient.SearchFriends(request);
            return Json(result);
        }

        /// <summary>
        /// Searches Kanban boards and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchKanbanBoards([FromBody] SearchRequest request)
        {
            SearchResponse<KanbanBoard> result = await searchHttpClient.SearchKanbanBoards(request);
            return Json(result);
        }

        /// <summary>
        /// Searches locations and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchLocations([FromBody] SearchRequest request)
        {
            SearchResponse<Location> result = await searchHttpClient.SearchLocations(request);
            return Json(result);
        }

        /// <summary>
        /// Searches measurements and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchMeasurements([FromBody] SearchRequest request)
        {
            SearchResponse<Measurement> result = await searchHttpClient.SearchMeasurements(request);
            return Json(result);
        }

        /// <summary>
        /// Searches notes and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchNotes([FromBody] SearchRequest request)
        {
            SearchResponse<Note> result = await searchHttpClient.SearchNotes(request);
            return Json(result);
        }

        /// <summary>
        /// Searches pictures and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchPictures([FromBody] SearchRequest request)
        {
            SearchResponse<Picture> result = await searchHttpClient.SearchPictures(request);
            return Json(result);
        }
        
        /// <summary>
        /// Searches skills and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchSkills([FromBody] SearchRequest request)
        {
            SearchResponse<Skill> result = await searchHttpClient.SearchSkills(request);
            return Json(result);
        }

        /// <summary>
        /// Searches sleep records and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchSleepRecords([FromBody] SearchRequest request)
        {
            SearchResponse<Sleep> result = await searchHttpClient.SearchSleepRecords(request);
            return Json(result);
        }

        /// <summary>
        /// Searches TodoItems and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchTodoItems([FromBody] SearchRequest request)
        {
            SearchResponse<TodoItem> result = await searchHttpClient.SearchTodoItems(request);
            return Json(result);
        }

        /// <summary>
        /// Searches vaccinations and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchVaccinations([FromBody] SearchRequest request)
        {
            SearchResponse<Vaccination> result = await searchHttpClient.SearchVaccinations(request);
            return Json(result);
        }

        /// <summary>
        /// Searches videos and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchVideos([FromBody] SearchRequest request)
        {
            SearchResponse<Video> result = await searchHttpClient.SearchVideos(request);
            return Json(result);
        }

        /// <summary>
        /// Searches vocabulary items and returns JSON results.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SearchVocabularyItems([FromBody] SearchRequest request)
        {
            SearchResponse<VocabularyItem> result = await searchHttpClient.SearchVocabularyItems(request);
            return Json(result);
        }

        #endregion

        #region Private Helper Methods

        private async Task SearchCalendarItemsAsync(SearchViewModel model, SearchRequest request)
        {
            model.CalendarItemResults = await searchHttpClient.SearchCalendarItems(request);
        }

        private async Task SearchContactsAsync(SearchViewModel model, SearchRequest request)
        {
            model.ContactResults = await searchHttpClient.SearchContacts(request);
        }

        private async Task SearchFriendsAsync(SearchViewModel model, SearchRequest request)
        {
            model.FriendResults = await searchHttpClient.SearchFriends(request);
        }

        private async Task SearchKanbanBoardsAsync(SearchViewModel model, SearchRequest request)
        {
            model.KanbanBoardResults = await searchHttpClient.SearchKanbanBoards(request);
        }

        private async Task SearchLocationsAsync(SearchViewModel model, SearchRequest request)
        {
            model.LocationResults = await searchHttpClient.SearchLocations(request);
        }

        private async Task SearchMeasurementsAsync(SearchViewModel model, SearchRequest request)
        {
            model.MeasurementResults = await searchHttpClient.SearchMeasurements(request);
        }

        private async Task SearchNotesAsync(SearchViewModel model, SearchRequest request)
        {
            model.NoteResults = await searchHttpClient.SearchNotes(request);
        }

        private async Task SearchPicturesAsync(SearchViewModel model, SearchRequest request)
        {
            model.PictureResults = await searchHttpClient.SearchPictures(request);
        }
        
        private async Task SearchSkillsAsync(SearchViewModel model, SearchRequest request)
        {
            model.SkillResults = await searchHttpClient.SearchSkills(request);
        }

        private async Task SearchSleepRecordsAsync(SearchViewModel model, SearchRequest request)
        {
            model.SleepResults = await searchHttpClient.SearchSleepRecords(request);
        }

        private async Task SearchTodoItemsAsync(SearchViewModel model, SearchRequest request)
        {
            model.TodoItemResults = await searchHttpClient.SearchTodoItems(request);
        }


        private async Task SearchVaccinationsAsync(SearchViewModel model, SearchRequest request)
        {
            model.VaccinationResults = await searchHttpClient.SearchVaccinations(request);
        }

        private async Task SearchVideosAsync(SearchViewModel model, SearchRequest request)
        {
            model.VideoResults = await searchHttpClient.SearchVideos(request);
        }

        private async Task SearchVocabularyItemsAsync(SearchViewModel model, SearchRequest request)
        {
            model.VocabularyItemResults = await searchHttpClient.SearchVocabularyItems(request);
        }

        #endregion
    }
}
