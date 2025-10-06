using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class SkillViewModel: BaseItemsViewModel
    {
        public Skill SkillItem { get; set; } = new Skill();
        
        public SkillViewModel()
        {
            ProgenyList = [];
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
        
        public Skill CreateSkill()
        {
            Skill skillItem = new()
            {
                SkillId = SkillItem.SkillId,
                ProgenyId = SkillItem.ProgenyId,
                Category = SkillItem.Category,
                Description = SkillItem.Description,
                Name = SkillItem.Name,
                SkillAddedDate = SkillItem.SkillAddedDate,
                ItemPermissionsDtoList = JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString)
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

        public void SetPropertiesFromSkillItem(Skill skill)
        {
            SkillItem.ProgenyId = skill.ProgenyId;
            SkillItem.AccessLevel = skill.AccessLevel;
            SkillItem.Description = skill.Description;
            SkillItem.Category = skill.Category;
            SkillItem.Name = skill.Name;
            SkillItem.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(skill.SkillFirstObservation?? DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            SkillItem.SkillId = skill.SkillId;
            SkillItem.ItemPerMission = skill.ItemPerMission;
        }
    }
}
