using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class SkillExtensions
    {
        public static void CopyPropertiesForUpdate(this Skill currentSkillItem, Skill otherSkillItem )
        {
            currentSkillItem.AccessLevel = otherSkillItem.AccessLevel;
            currentSkillItem.Author = otherSkillItem.Author;
            currentSkillItem.Description = otherSkillItem.Description;
            currentSkillItem.Category = otherSkillItem.Category;
            currentSkillItem.Name = otherSkillItem.Name;
            currentSkillItem.Progeny = otherSkillItem.Progeny;
            currentSkillItem.SkillFirstObservation = otherSkillItem.SkillFirstObservation;
            currentSkillItem.SkillNumber = otherSkillItem.SkillNumber;
        }

        public static void CopyPropertiesForAdd(this Skill currentSkillItem, Skill otherSkillItem)
        {
            currentSkillItem.AccessLevel = otherSkillItem.AccessLevel;
            currentSkillItem.Author = otherSkillItem.Author;
            currentSkillItem.Category = otherSkillItem.Category;
            currentSkillItem.Name = otherSkillItem.Name;
            currentSkillItem.ProgenyId = otherSkillItem.ProgenyId;
            currentSkillItem.Description = otherSkillItem.Description;
            currentSkillItem.SkillAddedDate = DateTime.UtcNow;
            currentSkillItem.SkillFirstObservation = otherSkillItem.SkillFirstObservation;
        }
    }
}
