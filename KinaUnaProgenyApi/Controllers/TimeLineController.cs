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
    public class TimeLineController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;

        public TimeLineController(ProgenyDbContext context, IDataService dataService)
        {
            _context = context;
            _dataService = dataService;
        }
        
        // GET api/timeline/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<TimeLineItem> timeLineList = await _dataService.GetTimeLineList(id); // await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel && t.ProgenyTime < DateTime.UtcNow).ToListAsync();
                timeLineList = timeLineList.Where(t => t.AccessLevel >= accessLevel && t.ProgenyTime < DateTime.UtcNow).ToList();
                if (timeLineList.Any())
                {
                    return Ok(timeLineList);
                }
                return Ok(new List<TimeLineItem>());
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start =0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<TimeLineItem> timeLineList = await _dataService.GetTimeLineList(id); // await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToListAsync();
                timeLineList = timeLineList
                    .Where(t => t.AccessLevel >= accessLevel && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToList();
                if (timeLineList.Any())
                {
                    timeLineList.Reverse();

                    return Ok(timeLineList.Skip(start).Take(count));
                }
                else
                {
                    return Ok(new List<TimeLineItem>());
                }
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyYearAgo(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<TimeLineItem> timeLineList = await _dataService.GetTimeLineList(id); // await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToListAsync();
                DateTime yearAgo = DateTime.Today.AddYears(-1);
                timeLineList = timeLineList
                    .Where(t => t.AccessLevel >= accessLevel && t.ProgenyTime.Date == yearAgo.Date).OrderBy(t => t.ProgenyTime).ToList();
                if (timeLineList.Any())
                {
                    timeLineList.Reverse();

                    return Ok(timeLineList);
                }
                else
                {
                    return Ok(new List<TimeLineItem>());
                }
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{itemId}/{itemType}")]
        public async Task<IActionResult> GetTimeLineItemByItemId(string itemId, int itemType)
        {
            TimeLineItem result = await _dataService.GetTimeLineItemByItemId(itemId, itemType); // await _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == itemId && t.ItemType == itemType);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // GET api/timeline/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTimeLineItem(int id)
        {
            TimeLineItem result = await _dataService.GetTimeLineItem(id); // await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(u => u.TimeLineId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TimeLineItem value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add timeline items for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.ProgenyId = value.ProgenyId;
            timeLineItem.AccessLevel = value.AccessLevel;
            timeLineItem.CreatedBy = value.CreatedBy;
            timeLineItem.CreatedTime = value.CreatedTime;
            timeLineItem.ItemId = value.ItemId;
            timeLineItem.ItemType = value.ItemType;
            timeLineItem.ProgenyTime = value.ProgenyTime;

            _context.TimeLineDb.Add(timeLineItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(timeLineItem.TimeLineId);

            return Ok(timeLineItem);
        }

        // PUT api/timeline/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] TimeLineItem value)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.TimeLineId == id);
            if (timeLineItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit timeline items for this child.
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            timeLineItem.ProgenyId = value.ProgenyId;
            timeLineItem.AccessLevel = value.AccessLevel;
            timeLineItem.CreatedBy = value.CreatedBy;
            timeLineItem.CreatedTime = value.CreatedTime;
            timeLineItem.ItemId = value.ItemId;
            timeLineItem.ItemType = value.ItemType;
            timeLineItem.ProgenyTime = value.ProgenyTime;
            
            _context.TimeLineDb.Update(timeLineItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(timeLineItem.TimeLineId);
            return Ok(timeLineItem);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.TimeLineId == id);
            if (timeLineItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == timeLineItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete timeline items for this child.
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                _context.TimeLineDb.Remove(timeLineItem);
                await _context.SaveChangesAsync();

                await _dataService.RemoveTimeLineItem(timeLineItem.TimeLineId, timeLineItem.ItemType, timeLineItem.ProgenyId);

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}/{year}/{month}/{day}")]
        public async Task<IActionResult> ProgenyLatestMobile(int id, int accessLevel = 5, int count = 5, int start = 0, int year = 0, int month = 0, int day = 0)
        {
            Progeny prog = await _dataService.GetProgeny(id); // await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            if (prog != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
                if (userAccess != null)
                {
                    // Check if the correct access level is used. If not use the one retrieved from database.
                    if (accessLevel < userAccess.AccessLevel)
                    {
                        accessLevel = userAccess.AccessLevel;
                    }
                    DateTime startDate;
                    if (year != 0 && month != 0 && day != 0)
                    {
                        startDate = new DateTime(year, month, day, 23, 59, 59, DateTimeKind.Utc);

                    }
                    else
                    {
                        startDate = DateTime.UtcNow;
                    }

                    List<TimeLineItem> timeLineList = await _dataService.GetTimeLineList(id); // await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == id && t.AccessLevel >= accessLevel && t.ProgenyTime < startDate).OrderBy(t => t.ProgenyTime).ToListAsync();
                    timeLineList = timeLineList.Where(t => t.AccessLevel >= accessLevel && t.ProgenyTime < startDate)
                        .OrderBy(t => t.ProgenyTime).ToList();
                    if (timeLineList.Any())
                    {
                        timeLineList.Reverse();

                        return Ok(timeLineList.Skip(start).Take(count));
                    }
                }
                
            }

            return Ok(new List<TimeLineItem>());
        }
    }
}
