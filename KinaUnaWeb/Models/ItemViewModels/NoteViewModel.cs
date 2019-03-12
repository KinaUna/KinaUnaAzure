using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NoteViewModel
    {
        public int NoteId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public DateTime CreatedDate { get; set; }
        public int AccessLevel { get; set; }
        public int ProgenyId { get; set; }
        public string Owner { get; set; }
        public string PathName { get; set; }

        public Progeny Progeny { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public bool IsAdmin { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public NoteViewModel()
        {
            ProgenyList = new List<SelectListItem>();
            AccessLevelList aclList = new AccessLevelList();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }
    }
}
