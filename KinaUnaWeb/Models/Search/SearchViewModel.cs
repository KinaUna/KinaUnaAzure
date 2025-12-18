using KinaUna.Data.Models.Search;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.Search
{
    /// <summary>
    /// View model for the search page, containing search results across all entity types.
    /// </summary>
    public class SearchViewModel : BaseItemsViewModel
    {
        public SearchViewModel()
        {
            
        }
        public SearchViewModel(BaseItemsViewModel baseViewModel)
        {
            SetBaseProperties(baseViewModel);
        }

        /// <summary>
        /// The search query string.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// List of Progeny IDs to search within.
        /// </summary>
        public List<int> ProgenyIds { get; set; } = [];

        /// <summary>
        /// List of Family IDs to search within.
        /// </summary>
        public List<int> FamilyIds { get; set; } = [];

        /// <summary>
        /// Which entity types to include in the search.
        /// </summary>
        public List<string> SelectedEntityTypes { get; set; } = [];

        /// <summary>
        /// Number of items to skip for pagination.
        /// </summary>
        public int Skip { get; set; } = 0;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int NumberOfItems { get; set; } = 25;

        /// <summary>
        /// Sort order. 0 = newest first, 1 = oldest first.
        /// </summary>
        public int Sort { get; set; } = 0;

        /// <summary>
        /// Search results for calendar items.
        /// </summary>
        public SearchResponse<CalendarItem> CalendarItemResults { get; set; } = new();

        /// <summary>
        /// Search results for contacts.
        /// </summary>
        public SearchResponse<Contact> ContactResults { get; set; } = new();

        /// <summary>
        /// Search results for friends.
        /// </summary>
        public SearchResponse<Friend> FriendResults { get; set; } = new();

        /// <summary>
        /// Search results for Kanban boards.
        /// </summary>
        public SearchResponse<KanbanBoard> KanbanBoardResults { get; set; } = new();

        /// <summary>
        /// Search results for locations.
        /// </summary>
        public SearchResponse<Location> LocationResults { get; set; } = new();

        /// <summary>
        /// Search results for measurements.
        /// </summary>
        public SearchResponse<Measurement> MeasurementResults { get; set; } = new();

        /// <summary>
        /// Search results for notes.
        /// </summary>
        public SearchResponse<Note> NoteResults { get; set; } = new();

        /// <summary>
        /// Search results for pictures.
        /// </summary>
        public SearchResponse<Picture> PictureResults { get; set; } = new();

        /// <summary>
        /// Search results for progenies.
        /// </summary>
        public SearchResponse<Progeny> ProgenyResults { get; set; } = new();

        /// <summary>
        /// Search results for progeny info.
        /// </summary>
        public SearchResponse<ProgenyInfo> ProgenyInfoResults { get; set; } = new();

        /// <summary>
        /// Search results for skills.
        /// </summary>
        public SearchResponse<Skill> SkillResults { get; set; } = new();

        /// <summary>
        /// Search results for sleep records.
        /// </summary>
        public SearchResponse<Sleep> SleepResults { get; set; } = new();

        /// <summary>
        /// Search results for TodoItems.
        /// </summary>
        public SearchResponse<TodoItem> TodoItemResults { get; set; } = new();

        /// <summary>
        /// Search results for user infos.
        /// </summary>
        public SearchResponse<UserInfo> UserInfoResults { get; set; } = new();

        /// <summary>
        /// Search results for vaccinations.
        /// </summary>
        public SearchResponse<Vaccination> VaccinationResults { get; set; } = new();

        /// <summary>
        /// Search results for videos.
        /// </summary>
        public SearchResponse<Video> VideoResults { get; set; } = new();

        /// <summary>
        /// Search results for vocabulary items.
        /// </summary>
        public SearchResponse<VocabularyItem> VocabularyItemResults { get; set; } = new();

        public SearchResponse<TimeLineItem> TimelineItemsResults { get; set; } = new();

        /// <summary>
        /// Total number of results across all entity types.
        /// </summary>
        public int TotalResultsCount =>
            CalendarItemResults.TotalCount +
            ContactResults.TotalCount +
            FriendResults.TotalCount +
            KanbanBoardResults.TotalCount +
            LocationResults.TotalCount +
            MeasurementResults.TotalCount +
            NoteResults.TotalCount +
            PictureResults.TotalCount +
            ProgenyResults.TotalCount +
            ProgenyInfoResults.TotalCount +
            SkillResults.TotalCount +
            SleepResults.TotalCount +
            TodoItemResults.TotalCount +
            UserInfoResults.TotalCount +
            VaccinationResults.TotalCount +
            VideoResults.TotalCount +
            VocabularyItemResults.TotalCount;

        /// <summary>
        /// Available entity types for filtering.
        /// </summary>
        public static List<string> AvailableEntityTypes =>
        [
            "CalendarItems",
            "Contacts",
            "Friends",
            "KanbanBoards",
            "Locations",
            "Measurements",
            "Notes",
            "Pictures",
            "Skills",
            "SleepRecords",
            "TodoItems",
            "Vaccinations",
            "Videos",
            "VocabularyItems"
        ];
    }
}
