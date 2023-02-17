using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Services;

public interface IViewModelSetupService
{
    Task<BaseItemsViewModel> SetupViewModel(int languageId, string userEmail, int progenyId);
    Task<List<SelectListItem>> GetProgenySelectList(UserInfo userInfo);
    Task<TimeLineViewModel> GetLatestPostTimeLineModel(int progenyId, int accessLevel, int languageId);
    Task<TimeLineViewModel> GetYearAgoPostsTimeLineModel(int progenyId, int accessLevel, int languageId);
}