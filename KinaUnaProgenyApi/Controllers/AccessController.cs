using KinaUnaProgenyApi.Data;
using KinaUnaProgenyApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public AccessController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/access
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<UserAccess> resultList = await _context.UserAccessDb.AsNoTracking().ToListAsync();
            if (resultList.Any())
            {
                foreach (UserAccess ua in resultList)
                {
                    ua.Progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                }
            }
            return Ok(resultList);
        }

        // GET api/access/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == id).ToListAsync();
            if (accessList.Any())
            {
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                    ua.User = new ApplicationUser();
                    UserInfo userinfo =
                        await _context.UserInfoDb.SingleOrDefaultAsync(
                            u => u.UserEmail.ToUpper() == ua.UserId.ToUpper());
                    if (userinfo != null)
                    {
                        ua.User.FirstName = userinfo.FirstName;
                        ua.User.MiddleName = userinfo.MiddleName;
                        ua.User.LastName = userinfo.LastName;
                        ua.User.UserName = userinfo.UserName;
                    }

                    ua.User.Email = ua.UserId;

                }
                return Ok(accessList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/access/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccess(int id)
        {
            UserAccess result = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
            result.Progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == result.ProgenyId);
            return Ok(result);
        }

        // POST api/access
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserAccess value)
        {
            UserAccess userAccess = new UserAccess();
            userAccess.ProgenyId = value.ProgenyId;
            userAccess.AccessLevel = value.AccessLevel;
            userAccess.UserId = value.UserId;
            userAccess.CanContribute = value.CanContribute;

            _context.UserAccessDb.Add(userAccess);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccess), new {id = userAccess.AccessId });
        }

        // PUT api/access/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UserAccess value)
        {
            UserAccess userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);

            if (userAccess == null)
            {
                return NotFound();
            }
            userAccess.ProgenyId = value.ProgenyId;
            userAccess.AccessLevel = value.AccessLevel;
            userAccess.UserId = value.UserId;
            userAccess.CanContribute = value.CanContribute;

            _context.UserAccessDb.Update(userAccess);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccess), new {id = userAccess.AccessId });
        }

        // DELETE api/access/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserAccess userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);
            if (userAccess != null)
            {
                _context.UserAccessDb.Remove(userAccess);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        // GET api/access/progenylistbyuser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUser(string id)
        {
            List<Progeny> result = new List<Progeny>();
            List<UserAccess> userAccessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper()).ToListAsync();

            if (userAccessList.Any())
            {
                foreach (UserAccess ua in userAccessList)
                {
                    Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                    result.Add(prog);
                }

                return Ok(result);
            }
            else
            {
                return NotFound();
            }
        }

        // GET api/access/accesslistbyuser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AccessListByUser(string id)
        {
            List<UserAccess> userAccessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper()).ToListAsync();
            if (userAccessList.Any())
            {
                foreach (UserAccess ua in userAccessList)
                {
                    ua.Progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                }
                return Ok(userAccessList);
            }
            else
            {
                return NotFound();
            }
        }

        // GET api/access/adminlistbyuser/[useremail]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AdminListByUser(string id)
        {
            List<UserAccess> userAccessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper() && u.AccessLevel == 0).ToListAsync();
            List<Progeny> progenyList = new List<Progeny>();
            if (userAccessList.Any())
            {
                foreach (UserAccess ua in userAccessList)
                {
                    Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                    progenyList.Add(progeny);
                }

                if (progenyList.Any())
                {
                    return Ok(progenyList);
                }

                return Ok();
            }
            else
            {
                return Ok();
            }
        }

        [HttpGet("[action]/{oldEmail}/{newEmail}")]
        public async Task<IActionResult> UpdateAccessListEmailChange(string oldEmail, string newEmail)
        {
            List<UserAccess> userAccessList = await _context.UserAccessDb.Where(u => u.UserId.ToUpper() == oldEmail.ToUpper()).ToListAsync();
            if (userAccessList.Any())
            {
                foreach (UserAccess ua in userAccessList)
                {
                    ua.UserId = newEmail;
                }

                _context.UserAccessDb.UpdateRange(userAccessList);
                await _context.SaveChangesAsync();
                return Ok(userAccessList);
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

            HttpClient userAccessHttpClient = new HttpClient();

            userAccessHttpClient.BaseAddress = new Uri("https://kinauna.com");
            userAccessHttpClient.DefaultRequestHeaders.Accept.Clear();
            userAccessHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string userAccessApiPath = "/api/azureexport/useraccessexport";
            var userAccessUri = "https://kinauna.com" + userAccessApiPath;

            var userAccessResponseString = await userAccessHttpClient.GetStringAsync(userAccessUri);

            List<UserAccess> userAccessList = JsonConvert.DeserializeObject<List<UserAccess>>(userAccessResponseString);
            List<UserAccess> userAccessItems = new List<UserAccess>();
            foreach (UserAccess ua in userAccessList)
            {
                UserAccess userAccess = new UserAccess();
                userAccess.ProgenyId = ua.ProgenyId;
                userAccess.AccessLevel = ua.AccessLevel;
                userAccess.UserId = ua.UserId;
                if (ua.AccessLevel == 0)
                {
                    userAccess.CanContribute = true;
                }
                else
                {
                    userAccess.CanContribute = false;
                }
                await _context.UserAccessDb.AddAsync(userAccess);
                userAccessItems.Add(userAccess);

            }
            await _context.SaveChangesAsync();

            return Ok(userAccessItems);
        }
    }
}
