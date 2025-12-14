using KinaUna.Data.Models.Search;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients.Search
{
    /// <summary>
    /// Provides methods for searching entities via the Progeny API.
    /// </summary>
    public interface ISearchHttpClient
    {
        /// <summary>
        /// Searches calendar items by title, notes, location, and context.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching CalendarItems.</returns>
        Task<SearchResponse<CalendarItem>> SearchCalendarItems(SearchRequest request);

        /// <summary>
        /// Searches contacts by name fields, email, notes, website, context, and tags.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Contacts.</returns>
        Task<SearchResponse<Contact>> SearchContacts(SearchRequest request);

        /// <summary>
        /// Searches friends by name, description, context, notes, and tags.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Friends.</returns>
        Task<SearchResponse<Friend>> SearchFriends(SearchRequest request);

        /// <summary>
        /// Searches Kanban boards by title, description, tags, and context.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching KanbanBoards.</returns>
        Task<SearchResponse<KanbanBoard>> SearchKanbanBoards(SearchRequest request);

        /// <summary>
        /// Searches locations by name, address fields, notes, and tags.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Locations.</returns>
        Task<SearchResponse<Location>> SearchLocations(SearchRequest request);

        /// <summary>
        /// Searches measurements by eye color and hair color.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Measurements.</returns>
        Task<SearchResponse<Measurement>> SearchMeasurements(SearchRequest request);

        /// <summary>
        /// Searches notes by title, content, and category.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Notes.</returns>
        Task<SearchResponse<Note>> SearchNotes(SearchRequest request);

        /// <summary>
        /// Searches pictures by tags and location.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Pictures.</returns>
        Task<SearchResponse<Picture>> SearchPictures(SearchRequest request);
        
        /// <summary>
        /// Searches skills by name, description, and category.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Skills.</returns>
        Task<SearchResponse<Skill>> SearchSkills(SearchRequest request);

        /// <summary>
        /// Searches sleep records by sleep notes.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Sleep records.</returns>
        Task<SearchResponse<Sleep>> SearchSleepRecords(SearchRequest request);

        /// <summary>
        /// Searches todo items by title, description, notes, tags, context, and location.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching TodoItems.</returns>
        Task<SearchResponse<TodoItem>> SearchTodoItems(SearchRequest request);
        
        /// <summary>
        /// Searches vaccinations by name, description, and notes.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Vaccinations.</returns>
        Task<SearchResponse<Vaccination>> SearchVaccinations(SearchRequest request);

        /// <summary>
        /// Searches videos by tags and location.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching Videos.</returns>
        Task<SearchResponse<Video>> SearchVideos(SearchRequest request);

        /// <summary>
        /// Searches vocabulary items by word, description, language, and sounds-like.
        /// </summary>
        /// <param name="request">The search request containing query and filters.</param>
        /// <returns>SearchResponse containing matching VocabularyItems.</returns>
        Task<SearchResponse<VocabularyItem>> SearchVocabularyItems(SearchRequest request);
    }
}
