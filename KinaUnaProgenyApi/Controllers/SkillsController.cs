using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
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

        public SkillsController(ProgenyDbContext context, IDataService dataService)
        {
            _context = context;
            _dataService = dataService;
        }
        
        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Skill> skillsList = await _dataService.GetSkillsList(id); // await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == id && s.AccessLevel >= accessLevel).ToListAsync();
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
            Skill result = await _dataService.GetSkill(id); // await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
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

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
            Skill result = await _dataService.GetSkill(id); // await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }
    }
}
