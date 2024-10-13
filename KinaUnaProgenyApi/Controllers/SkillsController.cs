using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Skill items.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="timelineService"></param>
    /// <param name="skillService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SkillsController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
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
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>List of Skill items.</returns>
        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<Skill> skillsList = await skillService.GetSkillsList(id, accessLevelResult.Value);
            
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
            Skill skill = await skillService.GetSkill(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(skill.ProgenyId, userEmail, skill.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
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
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            value.Author = User.GetUserId();

            Skill skillItem = await skillService.AddSkill(value);


            TimeLineItem timeLineItem = new();
            timeLineItem.CopySkillPropertiesForAdd(skillItem);
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Skill added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new skill for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendSkillNotification(skillItem, userInfo, notificationTitle);

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
            Skill skillItem = await skillService.GetSkill(id);
            if (skillItem == null)
            {
                return NotFound();
            }

            Progeny progeny = await progenyService.GetProgeny(skillItem.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            skillItem = await skillService.UpdateSkill(value);

            

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(skillItem.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill);
            if (timeLineItem == null) return Ok(skillItem);

            timeLineItem.CopySkillPropertiesForUpdate(skillItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

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
            Skill skillItem = await skillService.GetSkill(id);
            if (skillItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(skillItem.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(skillItem.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            _ = await skillService.DeleteSkill(skillItem);

            if (timeLineItem == null) return NoContent();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Skill deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FirstName + " " + userInfo.MiddleName + " " + userInfo.LastName + " deleted a skill for " + progeny.NickName + ". Measurement date: " + skillItem.Name;

            skillItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendSkillNotification(skillItem, userInfo, notificationTitle);

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
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(progenyId, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Skill> allItems = await skillService.GetSkillsList(progenyId, accessLevelResult.Value);
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

            List<Skill> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

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
