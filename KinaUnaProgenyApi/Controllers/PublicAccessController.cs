using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PublicAccessController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly ImageStore _imageStore;

        public PublicAccessController(ProgenyDbContext context, ImageStore imageStore)
        {
            _context = context;
            _imageStore = imageStore;

        }
        // GET api/publicaccess
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Progeny> resultList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Id == 2).ToListAsync();
            return Ok(resultList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            Progeny result = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == 2);
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, "progeny");
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Access(int id)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == 2).ToListAsync();
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

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> ProgenyListByUser(string id)
        {
            List<Progeny> result = new List<Progeny>();
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == 2);
            result.Add(prog);
            return Ok(result);
            
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> EventList(int progenyId, int accessLevel)
        {
            var model = await _context.CalendarDb
                .Where(e => e.ProgenyId == 2 && e.EndTime > DateTime.UtcNow && e.AccessLevel >= 5).ToListAsync();
            model = model.OrderBy(e => e.StartTime).ToList();
            model = model.Take(5).ToList();

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start = 0)
        {
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == 2 && t.AccessLevel >= 5 && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToListAsync();
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

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetCalendarItemMobile(int id)
        {
            CalendarItem result = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
            if (result.ProgenyId == 2)
            {
                return Ok(result);
            }
            result = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == 50);
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyCalendarMobile(int id, int accessLevel = 5)
        {
            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == 2 && c.AccessLevel >= 5).ToListAsync();
            if (calendarList.Any())
            {
                return Ok(calendarList);
            }
            else
            {
                return NotFound();
            }

        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact result = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == 9);
            }
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, "contacts");
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyContactsMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == 2 && c.AccessLevel >= 5).ToListAsync();
            if (contactsList.Any())
            {
                foreach (Contact cont in contactsList)
                {
                    if (!cont.PictureLink.ToLower().StartsWith("http"))
                    {
                        cont.PictureLink = _imageStore.UriFor(cont.PictureLink, "contacts");
                    }
                }
                return Ok(contactsList);
            }
            else
            {
                return Ok(new List<Contact>());
            }

        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == 12);
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVocabularyItemMobile(int id)
        {
            VocabularyItem result = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == 45);
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSkillMobile(int id)
        {
            Skill result = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == 18);
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetFriendMobile(int id)
        {
            Friend result = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == 25);
            }
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, "friends");
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetMeasurementMobile(int id)
        {
            Measurement result = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == 10);
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSleepMobile(int id)
        {
            Sleep result = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == 591);
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}/{start}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            var model = await _context.SleepDb
                .Where(s => s.ProgenyId == 2 && s.AccessLevel >= 5).ToListAsync();

            model = model.OrderByDescending(s => s.SleepStart).ToList();
            model = model.Skip(start).Take(25).ToList();
            return Ok(model);
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            string userTimeZone = "Romance Standard Time";
            SleepStatsModel model = new SleepStatsModel();
            model.SleepTotal = TimeSpan.Zero;
            model.SleepLastYear = TimeSpan.Zero;
            model.SleepLastMonth = TimeSpan.Zero;
            List<Sleep> sList = await _context.SleepDb.Where(s => s.ProgenyId == 2).ToListAsync();
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

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            string userTimeZone = "Romance Standard Time";
            List<Sleep> sList = await _context.SleepDb.Where(s => s.ProgenyId == 2).ToListAsync();
            List<Sleep> chartList = new List<Sleep>();
            foreach (Sleep chartItem in sList)
            {
                double durationStartDate = 0.0;
                if (chartItem.SleepStart.Date == chartItem.SleepEnd.Date)
                {
                    durationStartDate = durationStartDate + chartItem.SleepDuration.TotalMinutes;
                    Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                    if ( slpItem != null)
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
                    if ( slpItem != null)
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

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == 50);
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVaccinationMobile(int id)
        {
            Vaccination result = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (result.ProgenyId != 2)
            {
                result = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == 8);
            }
            return Ok(result);
        }
    }
}