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
    public class SkillsController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ITimelineService _timelineService;
        private readonly ISkillService _skillService;
        private readonly IProgenyService _progenyService;
        private readonly AzureNotifications _azureNotifications;

        public SkillsController(AzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService, ITimelineService timelineService, ISkillService skillService, IProgenyService progenyService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _skillService = skillService;
            _progenyService = progenyService;
        }

        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Skill> skillsList = await _skillService.GetSkillsList(id);
                skillsList = skillsList.Where(s => s.AccessLevel >= accessLevel).ToList();
                if (skillsList.Any())
                {
                    return Ok(skillsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/skills/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSkillItem(int id)
        {
            Skill result = await _skillService.GetSkill(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add skills for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Skill skillItem = new Skill();
            skillItem.AccessLevel = value.AccessLevel;
            skillItem.Author = value.Author;
            skillItem.Category = value.Category;
            skillItem.Name = value.Name;
            skillItem.ProgenyId = value.ProgenyId;
            skillItem.Description = value.Description;
            skillItem.SkillAddedDate = DateTime.UtcNow;
            skillItem.SkillFirstObservation = value.SkillFirstObservation;

            skillItem = await _skillService.AddSkill(skillItem);
            

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = skillItem.ProgenyId;
            tItem.AccessLevel = skillItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Skill;
            tItem.ItemId = skillItem.SkillId.ToString();
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            if (userinfo != null)
            {
                tItem.CreatedBy = userinfo.UserId;
            }
            tItem.CreatedTime = DateTime.UtcNow;
            if (skillItem.SkillFirstObservation != null)
            {
                tItem.ProgenyTime = skillItem.SkillFirstObservation.Value;
            }

            _ = await _timelineService.AddTimeLineItem(tItem);

            string title = "Skill added for " + prog.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName +
                                 " added a new skill for " + prog.NickName;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(skillItem);
        }

        // PUT api/skills/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Skill value)
        {
            Skill skillItem = await _skillService.GetSkill(id);
            if (skillItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(skillItem.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit skills for this child.
                if (!prog.IsInAdminList(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            skillItem.AccessLevel = value.AccessLevel;
            skillItem.Author = value.Author;
            skillItem.Category = value.Category;
            skillItem.Name = value.Name;
            skillItem.ProgenyId = value.ProgenyId;
            skillItem.Description = value.Description;
            skillItem.SkillAddedDate = DateTime.UtcNow;
            skillItem.SkillFirstObservation = value.SkillFirstObservation;

            skillItem = await _skillService.UpdateSkill(skillItem);
            
            TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(skillItem.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill);
            if (tItem != null)
            {
                if (skillItem.SkillFirstObservation != null)
                {
                    tItem.ProgenyTime = skillItem.SkillFirstObservation.Value;
                }
                tItem.AccessLevel = skillItem.AccessLevel;
                _ = await _timelineService.UpdateTimeLineItem(tItem);
            }

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Skill edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a skill for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(skillItem);
        }

        // DELETE api/skills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Skill skillItem = await _skillService.GetSkill(id);
            if (skillItem != null)
            {
                // Check if child exists.
                Progeny prog = await _progenyService.GetProgeny(skillItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete skills for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(skillItem.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill);
                if (tItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(tItem);
                }

                _ = await _skillService.DeleteSkill(skillItem);
                
                UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string title = "Skill deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a skill for " + prog.NickName + ". Measurement date: " + skillItem.Name;
                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSkillMobile(int id)
        {
            Skill result = await _skillService.GetSkill(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSkillsListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Skill> allItems = await _skillService.GetSkillsList(progenyId);
            allItems = allItems.OrderBy(s => s.SkillFirstObservation).ToList();

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

            SkillsListPage model = new SkillsListPage();
            model.SkillsList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }
    }
}
