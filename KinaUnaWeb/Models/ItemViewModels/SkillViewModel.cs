using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillViewModel
    {
        public int SkillId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DateTime? SkillFirstObservation { get; set; }
        public DateTime SkillAddedDate { get; set; }
        public string Author { get; set; }
        public int ProgenyId { get; set; }
        public Progeny Progeny { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public int AccessLevel { get; set; }

        public SkillViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
