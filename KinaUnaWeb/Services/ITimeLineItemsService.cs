using System.Threading.Tasks;
using KinaUnaWeb.Models.ItemViewModels;

namespace KinaUnaWeb.Services;

public interface ITimeLineItemsService
{
    Task<TimeLineItemPartialViewModel> GetTimeLineItemPartialViewModel(TimeLineItemViewModel model);
}