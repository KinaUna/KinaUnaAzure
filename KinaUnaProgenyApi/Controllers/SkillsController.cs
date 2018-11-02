using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Data;
using KinaUnaProgenyApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient skillsHttpClient = new HttpClient();
            
            skillsHttpClient.BaseAddress = new Uri("https://kinauna.com");
            skillsHttpClient.DefaultRequestHeaders.Accept.Clear();
            skillsHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string skillsApiPath = "/api/azureexport/skillsexport";
            var skillsUri = "https://kinauna.com" + skillsApiPath;

            var skillsResponseString = await skillsHttpClient.GetStringAsync(skillsUri);

            List<Skill> skillsList = JsonConvert.DeserializeObject<List<Skill>>(skillsResponseString);
            List<Skill> skillsItems = new List<Skill>();
            foreach (Skill value in skillsList)
            {
                Skill skillItem = new Skill();
                skillItem.AccessLevel = value.AccessLevel;
                skillItem.Author = value.Author;
                skillItem.Category = value.Category;
                skillItem.Name = value.Name;
                skillItem.ProgenyId = value.ProgenyId;
                skillItem.Description = value.Description;
                skillItem.SkillAddedDate = value.SkillAddedDate;
                skillItem.SkillFirstObservation = value.SkillFirstObservation;
                await _context.SkillsDb.AddAsync(skillItem);
                skillsItems.Add(skillItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(skillsItems);
        }
    }
}
