using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly ProgenyDbContext _context;
        
        public AccessController(IDataService dataService, ProgenyDbContext context)
        {
            _dataService = dataService;
            _context = context;
        }
        
        // GET api/access/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id)
        {
            List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(id);
            
            if (accessList.Any())
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

                bool allowedAccess = false;
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await _dataService.GetProgeny(ua.ProgenyId);
                    
                    ua.User = new ApplicationUser();
                    UserInfo userinfo = await _dataService.GetUserInfoByEmail(ua.UserId);
                    if (userinfo != null)
                    {
                        ua.User.FirstName = userinfo.FirstName;
                        ua.User.MiddleName = userinfo.MiddleName;
                        ua.User.LastName = userinfo.LastName;
                        ua.User.UserName = userinfo.UserName;
                    }
                    ua.User.Email = ua.UserId;
                    if (ua.User.Email.ToUpper() == userEmail.ToUpper())
                    {
                        allowedAccess = true;
                    }
                }

                if (!allowedAccess)
                {
                    return Unauthorized();
                }

                return Ok(accessList);
            }

            return NotFound();
        }

        // GET api/access/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAccess(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess result = await _dataService.GetUserAccess(id); // await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
            result.Progeny = await _dataService.GetProgeny(result.ProgenyId); // await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == result.ProgenyId);
            if (result.Progeny.Admins.ToUpper().Contains(User.GetEmail().ToUpper()) || result.UserId.ToUpper() == userEmail.ToUpper())
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/access
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserAccess value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            if (prog != null)
            {
                // Check if user is allowed to add users for this child.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }
            
            
            UserAccess userAccess = new UserAccess();
            userAccess.ProgenyId = value.ProgenyId;
            userAccess.AccessLevel = value.AccessLevel;
            userAccess.UserId = value.UserId;
            userAccess.CanContribute = value.CanContribute;

            _context.UserAccessDb.Add(userAccess);
            await _context.SaveChangesAsync();
            if (userAccess.AccessLevel == (int) AccessLevel.Private)
            {
                await _dataService.SetProgenyUserIsAdmin(userAccess.UserId);
            }

            await _dataService.SetProgenyUserAccessList(userAccess.ProgenyId);
            await _dataService.SetUsersUserAccessList(userAccess.UserId);
            await _dataService.SetUserAccess(userAccess.AccessId);

            return CreatedAtAction(nameof(GetAccess), new {id = userAccess.AccessId });
        }

        // PUT api/access/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] UserAccess value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            if (prog != null)
            {
                // Check if user is allowed to edit user access for this child.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

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

            if (userAccess.AccessLevel == (int)AccessLevel.Private)
            {
                await _dataService.SetProgenyUserIsAdmin(userAccess.UserId);
            }

            await _dataService.SetProgenyUserAccessList(userAccess.ProgenyId);
            await _dataService.SetUsersUserAccessList(userAccess.UserId);
            await _dataService.SetUserAccess(userAccess.AccessId);

            return CreatedAtAction(nameof(GetAccess), new {id = userAccess.AccessId });
        }

        // DELETE api/access/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            
            UserAccess userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id);
            if (userAccess != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == userAccess.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete users for this child.
                    string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                _context.UserAccessDb.Remove(userAccess);
                await _context.SaveChangesAsync();
                await _dataService.RemoveUserAccess(userAccess.AccessId, userAccess.ProgenyId, userAccess.UserId);
                return NoContent();
            }
            
            return NotFound();
        }

        // GET api/access/progenylistbyuser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<Progeny> result = new List<Progeny>();
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id); // await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper()).ToListAsync();

                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny prog = await _dataService.GetProgeny(ua.ProgenyId); // await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                        result.Add(prog);
                    }

                    return Ok(result);
                }
            }
            
            return NotFound();
        }

        // GET api/access/accesslistbyuser/[userid]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AccessListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id); // await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper()).ToListAsync();
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        ua.Progeny = await _dataService.GetProgeny(ua.ProgenyId); // await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                    }
                    return Ok(userAccessList);
                }
            }

            return NotFound();
        }

        // GET api/access/adminlistbyuser/[useremail]
        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> AdminListByUser(string id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == id.ToUpper())
            {
                List<UserAccess> userAccessList = await _dataService.GetUsersUserAccessList(id); // await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == id.ToUpper() && u.AccessLevel == 0).ToListAsync();
                userAccessList = userAccessList.Where(u => u.AccessLevel == 0).ToList();
                List<Progeny> progenyList = new List<Progeny>();
                if (userAccessList.Any())
                {
                    foreach (UserAccess ua in userAccessList)
                    {
                        Progeny progeny = await _dataService.GetProgeny(ua.ProgenyId); // await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                        progenyList.Add(progeny);
                    }

                    if (progenyList.Any())
                    {
                        return Ok(progenyList);
                    }

                    return Ok();
                }
            }

            return Ok();
        }

        [HttpGet("[action]/{oldEmail}/{newEmail}")]
        public async Task<IActionResult> UpdateAccessListEmailChange(string oldEmail, string newEmail)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (userEmail.ToUpper() == oldEmail.ToUpper())
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
            }

            return NotFound();
        }
    }
}
