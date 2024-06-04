using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
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
        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Skill> skillsList = await skillService.GetSkillsList(id);
            skillsList = skillsList.Where(s => s.AccessLevel >= accessLevel).ToList();
            if (skillsList.Count != 0)
            {
                return Ok(skillsList);
            }
            return NotFound();

        }

        // GET api/skills/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSkillItem(int id)
        {
            Skill result = await skillService.GetSkill(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

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

            //UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            //string notificationTitle = "Skill edited for " + progeny.NickName;
            //string notificationMessage = userInfo.FullName() + " edited a skill for " + progeny.NickName;

            //await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            //await webNotificationsService.SendSkillNotification(skillItem, userInfo, notificationTitle);

            return Ok(skillItem);
        }

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

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetSkillMobile(int id)
        {
            Skill result = await skillService.GetSkill(id);
            if (result == null) return NotFound();
            
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();

        }

        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetSkillsListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Skill> allItems = await skillService.GetSkillsList(progenyId);
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
