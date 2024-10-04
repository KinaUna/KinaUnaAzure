using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Services;

/// <summary>
/// Service for setting up the BaseItemsViewModel and related entities.
/// Gets the frequently used ViewModel properties, such as LanguageId, CurrentUser, CurrentProgeny, CurrentProgenyAccessList and CurrentUsersAccessLevel properties.
/// </summary>
public interface IViewModelSetupService
{
    /// <summary>
    /// Sets up the BaseItemsViewModel for the given user and Progeny and language.
    /// First checks the cache for a cached ViewModel, if not found, generates a new one via API calls.
    /// </summary>
    /// <param name="languageId">The language Id set for the current user.</param>
    /// <param name="userEmail">The user's email address.</param>
    /// <param name="progenyId">The ProgenyId for the Progeny.</param>
    /// <returns>BaseItemsViewModel</returns>
    Task<BaseItemsViewModel> SetupViewModel(int languageId, string userEmail, int progenyId);

    /// <summary>
    /// Generates a SelectListItem list of Progeny for the given user.
    /// </summary>
    /// <param name="userInfo">The user's UserInfo data.</param>
    /// <returns>List of SelectListItem objects.</returns>
    Task<List<SelectListItem>> GetProgenySelectList(UserInfo userInfo);

    /// <summary>
    /// Generates a SelectListItem list of offset times for setting reminders.
    /// </summary>
    /// <param name="languageId">The user's language.</param>
    /// <returns>List of SelectListItems.</returns>
    Task<List<SelectListItem>> CreateReminderOffsetSelectListItems(int languageId);
}