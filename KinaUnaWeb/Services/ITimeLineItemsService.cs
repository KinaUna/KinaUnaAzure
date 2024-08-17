using System.Threading.Tasks;
using KinaUnaWeb.Models.ItemViewModels;

namespace KinaUnaWeb.Services;

/// <summary>
/// Provides methods for generating item specific content for TimeLineItems.
/// </summary>
public interface ITimeLineItemsService
{
    /// <summary>
    /// Generates a TimeLineItemPartialViewModel object for a given TimeLineItemViewModel.
    /// The TimeLineItemPartialViewModel is used to provide type specific models for partial views in the TimeLine.
    /// </summary>
    /// <param name="model">The TimeLineItemViewModel to generate a ViewModel for.</param>
    /// <returns>TimeLineItemPartialViewModel</returns>
    Task<TimeLineItemPartialViewModel> GetTimeLineItemPartialViewModel(TimeLineItemViewModel model);
}