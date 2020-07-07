using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SkillsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;
        private readonly AzureNotifications _azureNotifications;

        public SkillsController(ProgenyDbContext context, IDataService dataService, AzureNotifications azureNotifications)
        {
            _context = context;
            _dataService = dataService;
            _azureNotifications = azureNotifications;
        }
        
        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Skill> skillsList = await _dataService.GetSkillsList(id);
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
            Skill result = await _dataService.GetSkill(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
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
            
            _context.SkillsDb.Add(skillItem);
            await _context.SaveChangesAsync();
            await _dataService.SetSkill(skillItem.SkillId);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = skillItem.ProgenyId;
            tItem.AccessLevel = skillItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Skill;
            tItem.ItemId = skillItem.SkillId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            if (userinfo != null)
            {
                tItem.CreatedBy = userinfo.UserId;
            }
            tItem.CreatedTime = DateTime.UtcNow;
            if (skillItem.SkillFirstObservation != null)
            {
                tItem.ProgenyTime = skillItem.SkillFirstObservation.Value;
            }

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

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
            Skill skillItem = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == id);
            if (skillItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
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

            _context.SkillsDb.Update(skillItem);
            await _context.SaveChangesAsync();
            await _dataService.SetSkill(skillItem.SkillId);

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == skillItem.SkillId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Skill);
            if (tItem != null)
            {
                if (skillItem.SkillFirstObservation != null)
                {
                    tItem.ProgenyTime = skillItem.SkillFirstObservation.Value;
                }
                tItem.AccessLevel = skillItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
            string title = "Skill edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a skill for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(skillItem);
        }

        // DELETE api/skills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Skill skillItem = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == id);
            if (skillItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == skillItem.ProgenyId);
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

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == skillItem.SkillId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Skill);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.SkillsDb.Remove(skillItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveSkill(skillItem.SkillId, skillItem.ProgenyId);

                UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
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
            Skill result = await _dataService.GetSkill(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

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
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Skill> allItems = await _dataService.GetSkillsList(progenyId);
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

            var itemsOnPage = allItems
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
