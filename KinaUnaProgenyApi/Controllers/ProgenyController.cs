using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
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
    public class ProgenyController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly ImageStore _imageStore;

        public ProgenyController(ProgenyDbContext context, ImageStore imageStore)
        {
            _context = context;
            _imageStore = imageStore;

        }
        // GET api/progeny
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Progeny> resultList = await _context.ProgenyDb.AsNoTracking().ToListAsync();
            return Ok(resultList);
        }

        // GET api/progeny/parent/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Parent(string id)
        {
            List<Progeny> progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(id)).ToListAsync();
            if (progenyList.Any())
            {
                return Ok(progenyList);
            }
            else
            {
                return NotFound();
            }
        }

        // GET api/progeny/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            Progeny result = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Mobile(int id)
        {
            Progeny result = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, "progeny");
            }
            return Ok(result);
        }

        // POST api/progeny
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Progeny value)
        {
            Progeny progeny = new Progeny();
            progeny.Name = value.Name;
            progeny.NickName = value.NickName;
            progeny.BirthDay = value.BirthDay;
            progeny.TimeZone = value.TimeZone;
            progeny.Admins = value.Admins;
            progeny.PictureLink = value.PictureLink;

            _context.ProgenyDb.Add(progeny);
            await _context.SaveChangesAsync();
            if (progeny.Admins.Contains(','))
            {
                List<string> adminList = progeny.Admins.Split(',').ToList();
                foreach (string adminEmail in adminList)
                {
                    UserAccess ua = new UserAccess();
                    ua.AccessLevel = 0;
                    ua.ProgenyId = progeny.Id;
                    ua.UserId = adminEmail.Trim();

                    _context.UserAccessDb.Add(ua);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                UserAccess ua = new UserAccess();
                ua.AccessLevel = 0;
                ua.ProgenyId = progeny.Id;
                ua.UserId = progeny.Admins.Trim();

                _context.UserAccessDb.Add(ua);
                await _context.SaveChangesAsync();
            }

            return Ok(progeny);
        }

        // PUT api/progeny/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Progeny value)
        {
            Progeny progeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == id);

            if (progeny == null)
            {
                return NotFound();
            }

            progeny.Admins = value.Admins;
            progeny.BirthDay = value.BirthDay;
            progeny.Name = value.Name;
            progeny.NickName = value.NickName;
            progeny.PictureLink = value.PictureLink;
            progeny.TimeZone = value.TimeZone;
            
            _context.ProgenyDb.Update(progeny);
            await _context.SaveChangesAsync();

            return Ok(progeny);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Progeny progeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == id);
            if (progeny != null)
            {
                // Todo: Delete content associated with progeny.
                if (!progeny.PictureLink.ToLower().StartsWith("http") && !String.IsNullOrEmpty(progeny.PictureLink))
                {
                    await _imageStore.DeleteImage(progeny.PictureLink, "progeny");
                }
                _context.ProgenyDb.Remove(progeny);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
            
        }
    }
}
