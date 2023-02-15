using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Services
{
    public class ViewModelSetupService : IViewModelSetupService
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;

        public ViewModelSetupService(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        public async Task<BaseItemsViewModel> SetupViewModel(int languageId, string userEmail, int progenyId)
        {
           BaseItemsViewModel viewModel = new BaseItemsViewModel();
           viewModel.LanguageId = languageId;
           viewModel.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
           viewModel.SetCurrentProgenyId(progenyId);
           viewModel.CurrentProgeny = await _progenyHttpClient.GetProgeny(viewModel.CurrentProgenyId);
           viewModel.CurrentProgenyAccessList = await _userAccessHttpClient.GetProgenyAccessList(viewModel.CurrentProgenyId);
           viewModel.SetCurrentUsersAccessLevel();

           return viewModel;
        }

        public async Task<List<SelectListItem>> GetProgenySelectList(UserInfo userInfo)
        {
            List<SelectListItem> progenyList = new List<SelectListItem>();
            List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userInfo.UserEmail);
            if (accessList.Any())
            {
                foreach (Progeny prog in accessList)
                {
                    SelectListItem selItem = new SelectListItem()
                    {
                        Text = accessList.Single(p => p.Id == prog.Id).NickName,
                        Value = prog.Id.ToString()
                    };
                    if (prog.Id == userInfo.ViewChild)
                    {
                        selItem.Selected = true;
                    }

                    progenyList.Add(selItem);
                }
            }

            return progenyList;
        }
    }
}
