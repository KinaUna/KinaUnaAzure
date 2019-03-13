using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
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

        public SkillsController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/skills
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Skill> resultList = await _context.SkillsDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/skills/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == id && s.AccessLevel >= accessLevel).ToListAsync();
            if (skillsList.Any())
            {
                return Ok(skillsList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/skills/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSkillItem(int id)
        {
            Skill result = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);

            return Ok(result);
        }

        // POST api/vocabulary
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Skill value)
        {
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

            return Ok(skillItem);
        }

        // DELETE api/skills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Skill skillItem = await _context.SkillsDb.SingleOrDefaultAsync(s => s.SkillId == id);
            if (skillItem != null)
            {
                _context.SkillsDb.Remove(skillItem);
                await _context.SaveChangesAsync();
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
            Skill result = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);

            return Ok(result);
        }
    }
}
