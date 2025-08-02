using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public Skill SkillItem { get; set; } = new Skill();
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        
        public SkillViewModel()
        {
            ProgenyList = [];
            AccessLevelList aclList = new();
            AccessLevelListEn = aclList.AccessLevelListEn;
            AccessLevelListDa = aclList.AccessLevelListDa;
            AccessLevelListDe = aclList.AccessLevelListDe;
        }

        public SkillViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public void SetProgenyList()
        {
            SkillItem.ProgenyId = CurrentProgenyId;
            foreach (SelectListItem item in ProgenyList)
            {
                if (item.Value == CurrentProgenyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[SkillItem.AccessLevel].Selected = true;
            AccessLevelListDa[SkillItem.AccessLevel].Selected = true;
            AccessLevelListDe[SkillItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public Skill CreateSkill()
        {
            Skill skillItem = new()
            {
                SkillId = SkillItem.SkillId,
                ProgenyId = SkillItem.ProgenyId,
                Category = SkillItem.Category,
                Description = SkillItem.Description,
                Name = SkillItem.Name,
                SkillAddedDate = SkillItem.SkillAddedDate
            };
            
            if (SkillItem.SkillFirstObservation.HasValue)
            {
                skillItem.SkillFirstObservation = TimeZoneInfo.ConvertTimeToUtc(SkillItem.SkillFirstObservation.Value, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            }
            else
            {
                skillItem.SkillFirstObservation = DateTime.UtcNow;
            }
            
            skillItem.AccessLevel = SkillItem.AccessLevel;
            skillItem.Author = CurrentUser.UserId;

            return skillItem;
        }

        public void SetPropertiesFromSkillItem(Skill skill, bool isAdmin)
        {
            SkillItem.ProgenyId = skill.ProgenyId;
            SkillItem.AccessLevel = skill.AccessLevel;
            SkillItem.Description = skill.Description;
            SkillItem.Category = skill.Category;
            SkillItem.Name = skill.Name;
            SkillItem.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(skill.SkillFirstObservation?? DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            SkillItem.SkillId = skill.SkillId;
            IsCurrentUserProgenyAdmin = isAdmin;
        }
    }
}
