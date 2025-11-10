using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Skill items.
    /// </summary>
    /// <param name="userInfoService"></param>
    /// <param name="timelineService"></param>
    /// <param name="skillService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SkillsController(
        IUserInfoService userInfoService,
        ITimelineService timelineService,
        ISkillService skillService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Gets all Skills for a given Progeny that a user can access.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Skill items for.</param>
        /// <returns>List of Skill items.</returns>
        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<Skill> skillsList = await skillService.GetSkillsList(id, currentUserInfo);
            
            return Ok(skillsList);
        }

        /// <summary>
        /// Gets a single Skill item with the given SkillId.
        /// </summary>
        /// <param name="id">The SkillId of the Skill item to get.</param>
        /// <returns>Skill object with the provided SkillId.</returns>
        // GET api/skills/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSkillItem(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Skill skill = await skillService.GetSkill(id, currentUserInfo);
            if (skill == null)
            {
                return NotFound();
            }

            return Ok(skill);
        }

        /// <summary>
        /// Adds a new Skill item to the database.
        /// Also creates a TimeLineItem for the new Skill item and sends notifications to users who have access to the Skill item.
        /// </summary>
        /// <param name="value">The Skill to add.</param>
        /// <returns>The added Skill item.</returns>
        // POST api/vocabulary
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Skill value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            
            value.Author = User.GetUserId();
            value.CreatedBy = User.GetUserId();
            value.ModifiedBy = User.GetUserId();

            Skill skillItem = await skillService.AddSkill(value, currentUserInfo);
            if (skillItem == null || skillItem.SkillId == 0)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = new();
            timeLineItem.CopySkillPropertiesForAdd(skillItem);
            _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);

            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId, currentUserInfo);
            string notificationTitle = "Skill added for " + progeny.NickName;
            
            await webNotificationsService.SendSkillNotification(skillItem, currentUserInfo, notificationTitle);

            skillItem = await skillService.GetSkill(skillItem.SkillId, currentUserInfo);

            return Ok(skillItem);
        }

        /// <summary>
        /// Updates a Skill item in the database.
        /// Also updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The SkillId of the Skill to update.</param>
        /// <param name="value">Skill item with the updated properties.</param>
        /// <returns>The updated Skill object.</returns>
        // PUT api/skills/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Skill value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Skill skillItem = await skillService.GetSkill(id, currentUserInfo);
            if (skillItem == null)
            {
                return NotFound();
            }
            
            value.ModifiedBy = User.GetUserId();

            skillItem = await skillService.UpdateSkill(value, currentUserInfo);
            if (skillItem == null || skillItem.SkillId == 0)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(skillItem.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill, currentUserInfo);
            if (timeLineItem == null) return Ok(skillItem);

            timeLineItem.CopySkillPropertiesForUpdate(skillItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);

            skillItem = await skillService.GetSkill(skillItem.SkillId, currentUserInfo);

            return Ok(skillItem);
        }

        /// <summary>
        /// Deletes a Skill item from the database.
        /// Also deletes the corresponding TimeLineItem and sends notifications to users who have admin access to the Skill item.
        /// </summary>
        /// <param name="id">The SkillId of the Skill item to delete.</param>
        /// <returns>NoContentResult. UnauthorizedResult if the user doesn't have access to the Skill item, NotFoundResult if the Skill doesn't exist.</returns>
        // DELETE api/skills/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Skill skillItem = await skillService.GetSkill(id, currentUserInfo);
            if (skillItem == null) return NotFound();
            
            skillItem.ModifiedBy = User.GetUserId();

            Skill deletedSkill = await skillService.DeleteSkill(skillItem, currentUserInfo);
            if (deletedSkill == null) return Unauthorized();
            
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(skillItem.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }
            if (timeLineItem == null) return NoContent();

            Progeny progeny = await progenyService.GetProgeny(skillItem.ProgenyId, currentUserInfo);
            string notificationTitle = "Skill deleted for " + progeny.NickName;
            
            skillItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await webNotificationsService.SendSkillNotification(skillItem, currentUserInfo, notificationTitle);

            return NoContent();

        }
        
        /// <summary>
        /// Gets a SkillListPage for displaying Skills in a paged list.
        /// </summary>
        /// <param name="pageSize">The number of Skills per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId for the Progeny to show skills for.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first, 1 = newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetSkillsListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int sortBy = 1)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<Skill> allItems = await skillService.GetSkillsList(progenyId, currentUserInfo);
            allItems = [.. allItems.OrderBy(s => s.SkillFirstObservation)];

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int skillsCounter = 1;
            int skillsCount = allItems.Count;
            foreach (Skill skill in allItems)
            {
                if (sortBy == 1)
                {
                    skill.SkillNumber = skillsCount - skillsCounter + 1;
                }
                else
                {
                    skill.SkillNumber = skillsCounter;
                }

                skillsCounter++;
            }

            List<Skill> itemsOnPage = [.. allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)];

            SkillsListPage model = new()
            {
                SkillsList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }
    }
}
