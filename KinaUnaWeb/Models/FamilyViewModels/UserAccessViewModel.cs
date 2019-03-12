using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class UserAccessViewModel
    {
        public int AccessId { get; set; }
        public int ProgenyId { get; set; }
        public Progeny Progeny { get; set; }
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
            ProgenyList = new List<SelectListItem>();
            AccessLevelList accList  = new AccessLevelList();
            AccessLevelListEn = accList.AccessLevelListEn;
            AccessLevelListDa = accList.AccessLevelListDa;
            AccessLevelListDe = accList.AccessLevelListDe;

        }
    }
}
