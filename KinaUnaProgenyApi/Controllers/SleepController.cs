using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Constants = KinaUna.Data.Constants;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SleepController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public SleepController(ProgenyDbContext context)
        {
            _context = context;

        }
        
        // GET api/sleep/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail();
            UserAccess userAccess = _context.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == id && s.AccessLevel >= accessLevel).ToListAsync();
                if (sleepList.Any())
                {
                    return Ok(sleepList);
                }
            }

            return NotFound();
        }

        // GET api/sleep/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSleepItem(int id)
        {
            
            Sleep result = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            if (result.AccessLevel == (int) AccessLevel.Public || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            string userEmail = User.GetEmail();
            UserAccess userAccess = _context.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/sleep
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Sleep value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail();
            if (prog != null)
            {
                // Check if user is allowed to add sleep for this child.
                
                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Sleep sleepItem = new Sleep();
            sleepItem.AccessLevel = value.AccessLevel;
            sleepItem.Author = value.Author;
            sleepItem.SleepNotes = value.SleepNotes;
            sleepItem.SleepRating = value.SleepRating;
            sleepItem.ProgenyId = value.ProgenyId;
            sleepItem.SleepStart = value.SleepStart;
            sleepItem.SleepEnd = value.SleepEnd;
            sleepItem.CreatedDate = DateTime.UtcNow;

            _context.SleepDb.Add(sleepItem);
            await _context.SaveChangesAsync();

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = sleepItem.ProgenyId;
            tItem.AccessLevel = sleepItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Sleep;
            tItem.ItemId = sleepItem.SleepId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            tItem.CreatedBy = userinfo?.UserId ?? "Unknown";
            tItem.CreatedTime = DateTime.UtcNow;
            tItem.ProgenyTime = sleepItem.SleepStart;

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();

            return Ok(sleepItem);
        }

        // PUT api/sleep/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Sleep value)
        {
            Sleep sleepItem = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleepItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail();
            if (prog != null)
            {
                // Check if user is allowed to edit sleep for this child.

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            sleepItem.AccessLevel = value.AccessLevel;
            sleepItem.Author = value.Author;
            sleepItem.SleepNotes = value.SleepNotes;
            sleepItem.SleepRating = value.SleepRating;
            sleepItem.ProgenyId = value.ProgenyId;
            sleepItem.SleepStart = value.SleepStart;
            sleepItem.SleepEnd = value.SleepEnd;
            sleepItem.CreatedDate = value.CreatedDate;

            _context.SleepDb.Update(sleepItem);
            await _context.SaveChangesAsync();

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == sleepItem.SleepId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Sleep);
            if (tItem != null)
            {
                tItem.ProgenyTime = sleepItem.SleepStart;
                tItem.AccessLevel = sleepItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
            }

            return Ok(sleepItem);
        }

        // DELETE api/sleep/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Sleep sleepItem = await _context.SleepDb.SingleOrDefaultAsync(s => s.SleepId == id);
            if (sleepItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == sleepItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete sleep for this child.
                    string userEmail = User.GetEmail();
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
                    t.ItemId == sleepItem.SleepId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Sleep);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                }

                _context.SleepDb.Remove(sleepItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSleepMobile(int id)
        {
            
            Sleep result = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userEmail.ToUpper() && u.ProgenyId == result.ProgenyId);
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}/{start}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userEmail.ToUpper() && u.ProgenyId == progenyId);
            if (userAccess != null)
            {
                List<Sleep> result = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId && s.AccessLevel >= accessLevel).ToListAsync();
                result = result.OrderByDescending(s => s.SleepStart).ToList();
                if (start != -1)
                {
                    result = result.Skip(start).Take(25).ToList();
                }

                return Ok(result);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userEmail.ToUpper() && u.ProgenyId == progenyId);
            if (userAccess != null)
            {
                string userTimeZone = Constants.DefaultTimezone;
                SleepStatsModel model = new SleepStatsModel();
                model.SleepTotal = TimeSpan.Zero;
                model.SleepLastYear = TimeSpan.Zero;
                model.SleepLastMonth = TimeSpan.Zero;
                List<Sleep> sList = await _context.SleepDb.Where(s => s.ProgenyId == progenyId).ToListAsync();
                List<Sleep> sleepList = new List<Sleep>();
                DateTime yearAgo = new DateTime(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
                DateTime monthAgo = DateTime.UtcNow - TimeSpan.FromDays(30);
                if (sList.Count != 0)
                {
                    foreach (Sleep s in sList)
                    {

                        bool isLessThanYear = s.SleepEnd > yearAgo;
                        bool isLessThanMonth = s.SleepEnd > monthAgo;
                        s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                        DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                        DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                        s.SleepDuration = eOffset - sOffset;

                        model.SleepTotal = model.SleepTotal + s.SleepDuration;
                        if (isLessThanYear)
                        {
                            model.SleepLastYear = model.SleepLastYear + s.SleepDuration;
                        }

                        if (isLessThanMonth)
                        {
                            model.SleepLastMonth = model.SleepLastMonth + s.SleepDuration;
                        }

                        if (s.AccessLevel >= accessLevel)
                        {
                            sleepList.Add(s);
                        }
                    }
                    sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();

                    model.TotalAverage = model.SleepTotal / (DateTime.UtcNow - sleepList.First().SleepStart).TotalDays;
                    model.LastYearAverage = model.SleepLastYear / (DateTime.UtcNow - yearAgo).TotalDays;
                    model.LastMonthAverage = model.SleepLastMonth / 30;

                }
                else
                {
                    model.TotalAverage = TimeSpan.Zero;
                    model.LastYearAverage = TimeSpan.Zero;
                    model.LastMonthAverage = TimeSpan.Zero;
                }

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userEmail.ToUpper() && u.ProgenyId == progenyId);
            if (userAccess != null)
            {
                string userTimeZone = Constants.DefaultTimezone;
                List<Sleep> sList = await _context.SleepDb.Where(s => s.ProgenyId == progenyId).ToListAsync();
                List<Sleep> sleepList = new List<Sleep>();
                foreach (Sleep s in sList)
                {
                    s.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    s.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                    DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                    s.SleepDuration = eOffset - sOffset;

                    if (s.AccessLevel >= accessLevel)
                    {
                        sleepList.Add(s);
                    }
                }
                sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();

                List<Sleep> chartList = new List<Sleep>();
                foreach (Sleep chartItem in sleepList)
                {
                    double durationStartDate = 0.0;
                    if (chartItem.SleepStart.Date == chartItem.SleepEnd.Date)
                    {
                        durationStartDate = durationStartDate + chartItem.SleepDuration.TotalMinutes;
                        Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                        if (slpItem != null)
                        {
                            slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                        }
                        else
                        {
                            Sleep newSleep = new Sleep();
                            newSleep.SleepStart = chartItem.SleepStart;
                            newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                            chartList.Add(newSleep);
                        }
                    }
                    else
                    {
                        DateTimeOffset sOffset = new DateTimeOffset(chartItem.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepStart));
                        DateTimeOffset s2Offset = new DateTimeOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1),
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                                .GetUtcOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1)));
                        DateTimeOffset eOffset = new DateTimeOffset(chartItem.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepEnd));
                        DateTimeOffset e2Offset = new DateTimeOffset(chartItem.SleepEnd.Date,
                            TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                                .GetUtcOffset(chartItem.SleepEnd.Date));
                        TimeSpan sDateDuration = s2Offset - sOffset;
                        TimeSpan eDateDuration = eOffset - e2Offset;
                        durationStartDate = chartItem.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                        var durationEndDate = chartItem.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;
                        Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                        if (slpItem != null)
                        {
                            slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                        }
                        else
                        {
                            Sleep newSleep = new Sleep();
                            newSleep.SleepStart = chartItem.SleepStart;
                            newSleep.SleepDuration = TimeSpan.FromMinutes(durationStartDate);
                            chartList.Add(newSleep);
                        }

                        Sleep slpItem2 = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepEnd.Date);
                        if (slpItem2 != null)
                        {
                            slpItem2.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                        }
                        else
                        {
                            Sleep newSleep = new Sleep();
                            newSleep.SleepStart = chartItem.SleepEnd;
                            newSleep.SleepDuration = TimeSpan.FromMinutes(durationEndDate);
                            chartList.Add(newSleep);
                        }
                    }
                }

                List<Sleep> model = chartList.OrderBy(s => s.SleepStart).ToList();

                return Ok(model);
            }

            return Unauthorized();
        }
    }
}
