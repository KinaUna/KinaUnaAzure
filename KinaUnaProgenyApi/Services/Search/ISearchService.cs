using KinaUna.Data.Models;
using KinaUna.Data.Models.Search;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.Search
{
    /// <summary>
    /// Service interface for searching entities across progenies and families.
    /// All search methods respect user access permissions.
    /// </summary>
    public interface ISearchService
    {
        Task<SearchResponse<TimeLineItem>> QuickSearch(SearchRequest request, UserInfo currentUserInfo);
        /// <summary>
        /// Searches calendar items by title, notes, location, and context.
        /// </summary>
        Task<SearchResponse<CalendarItem>> SearchCalendarItems(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches contacts by name fields, email, notes, website, context, and tags.
        /// </summary>
        Task<SearchResponse<Contact>> SearchContacts(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches friends by name, description, context, notes, and tags.
        /// </summary>
        Task<SearchResponse<Friend>> SearchFriends(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches Kanban boards by title, description, tags, and context.
        /// </summary>
        Task<SearchResponse<KanbanBoard>> SearchKanbanBoards(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches locations by name, address fields, notes, and tags.
        /// </summary>
        Task<SearchResponse<Location>> SearchLocations(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches measurements by eye color and hair color.
        /// </summary>
        Task<SearchResponse<Measurement>> SearchMeasurements(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches notes by title, content, and category.
        /// </summary>
        Task<SearchResponse<Note>> SearchNotes(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches pictures by tags and location.
        /// </summary>
        Task<SearchResponse<Picture>> SearchPictures(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches skills by name, description, and category.
        /// </summary>
        Task<SearchResponse<Skill>> SearchSkills(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches sleep records by sleep notes.
        /// </summary>
        Task<SearchResponse<Sleep>> SearchSleepRecords(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches TodoItems by title, description, notes, tags, context, and location.
        /// </summary>
        Task<SearchResponse<TodoItem>> SearchTodoItems(SearchRequest request, UserInfo currentUserInfo);

        ///// <summary>
        ///// Searches users connected to the current user through shared progenies or families.
        ///// Searches by email, username, and name fields.
        ///// </summary>
        //Task<SearchResponse<UserInfo>> SearchUserInfos(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches vaccinations by name, description, and notes.
        /// </summary>
        Task<SearchResponse<Vaccination>> SearchVaccinations(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches videos by tags and location.
        /// </summary>
        Task<SearchResponse<Video>> SearchVideos(SearchRequest request, UserInfo currentUserInfo);

        /// <summary>
        /// Searches vocabulary items by word, description, language, and sounds-like.
        /// </summary>
        Task<SearchResponse<VocabularyItem>> SearchVocabularyItems(SearchRequest request, UserInfo currentUserInfo);
    }
}
