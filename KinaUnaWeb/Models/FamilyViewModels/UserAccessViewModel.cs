using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class UserAccessViewModel: BaseItemsViewModel
    {
        public int AccessId { get; set; }
        public string ProgenyName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int AccessLevel { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }

        public UserAccessViewModel()
        {
            ProgenyList = [];
        }

        public UserAccessViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ProgenyName = CurrentProgeny.Name;
            Email = "";
            AccessLevel = (int)KinaUna.Data.Models.AccessLevel.Users;
            UserId = "";
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accList = new();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;

            AccessLevelListEn[AccessLevel].Selected = true;
            AccessLevelListDa[AccessLevel].Selected = true;
            AccessLevelListDe[AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetUserAccessItem(UserAccess userAccess, UserInfo userInfo)
        {
            UserId = userAccess.UserId;
            AccessId = userAccess.AccessId;
            AccessLevel = userAccess.AccessLevel;
            Email = userAccess.UserId;
            UserName = "No user found";
            FirstName = "No user found";
            MiddleName = "No user found";
            LastName = "No user found";
            
            if (userInfo != null)
            {
                UserName = userInfo.UserName;
                FirstName = userInfo.FirstName;
                MiddleName = userInfo.MiddleName;
                LastName = userInfo.LastName;
            }
        }

        public UserAccess CreateUserAccess()
        {
            UserAccess userAccess = new()
            {
                AccessId = AccessId,
                ProgenyId = CurrentProgenyId,
                UserId = Email,
                AccessLevel = AccessLevel
            };

            return userAccess;
        }
    }
}
