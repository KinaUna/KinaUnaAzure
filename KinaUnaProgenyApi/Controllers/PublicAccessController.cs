using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PublicAccessController : ControllerBase
    {
        private readonly ImageStore _imageStore;
        private readonly IDataService _dataService;

        public PublicAccessController(ImageStore imageStore, IDataService dataService)
        {
            _imageStore = imageStore;
            _dataService = dataService;
        }
        // GET api/publicaccess
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            Progeny prog = await _dataService.GetProgeny(Constants.DefaultChildId);
            List<Progeny> resultList = new List<Progeny>(); //_context.ProgenyDb.AsNoTracking().Where(p => p.Id == Constants.DefaultChildId).ToListAsync();
            resultList.Add(prog);

            return Ok(resultList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            Progeny result = await _dataService.GetProgeny(Constants.DefaultChildId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == Constants.DefaultChildId);
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
            List<UserAccess> accessList = await _dataService.GetProgenyUserAccessList(Constants.DefaultChildId); // await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == Constants.DefaultChildId).ToListAsync();
            if (accessList.Any())
            {
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await _dataService.GetProgeny(ua.ProgenyId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == ua.ProgenyId);
                    ua.User = new ApplicationUser();
                    UserInfo userinfo = await _dataService.GetUserInfoByEmail(ua.UserId); // _context.UserInfoDb.SingleOrDefaultAsync( u => u.UserEmail.ToUpper() == ua.UserId.ToUpper());
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
            Progeny prog = await _dataService.GetProgeny(Constants.DefaultChildId); // _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == Constants.DefaultChildId);
            result.Add(prog);
            return Ok(result);

        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> EventList(int progenyId, int accessLevel)
        {
            var model = await _dataService.GetCalendarList(Constants.DefaultChildId); // _context.CalendarDb.Where(e => e.ProgenyId == Constants.DefaultChildId && e.EndTime > DateTime.UtcNow && e.AccessLevel >= 5).ToListAsync();
            model = model.Where(e => e.EndTime > DateTime.UtcNow && e.AccessLevel >= 5).OrderBy(e => e.StartTime).ToList();
            model = model.Take(5).ToList();

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start = 0)
        {
            List<TimeLineItem> timeLineList = await _dataService.GetTimeLineList(Constants.DefaultChildId); // await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == Constants.DefaultChildId && t.AccessLevel >= 5 && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToListAsync();
            timeLineList = timeLineList.Where(t => t.AccessLevel >= 5 && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime).ToList();
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
            CalendarItem result = await _dataService.GetCalendarItem(id); // await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
            if (result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }
            CalendarItem calItem = new CalendarItem();
            calItem.ProgenyId = Constants.DefaultChildId;
            calItem.AccessLevel = 5;
            calItem.Title = "Launch of KinaUna.com";
            UserInfo adminInfo = await _dataService.GetUserInfoByEmail(Constants.DefaultUserEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.AdminEmail.ToUpper());
            calItem.Author = adminInfo?.UserId ?? "Unknown Author";
            calItem.StartTime = new DateTime(2018, 2, 18, 21, 02, 0);
            calItem.EndTime = new DateTime(2018, 2, 18, 22, 02, 0);
            return Ok(calItem);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyCalendarMobile(int id, int accessLevel = 5)
        {
            List<CalendarItem> calendarList = await _dataService.GetCalendarList(Constants.DefaultChildId); // _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == Constants.DefaultChildId && c.AccessLevel >= 5).ToListAsync();
            calendarList = calendarList.Where(c => c.AccessLevel >= 5).ToList();
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
            Contact result = await _dataService.GetContact(id); // await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Contact();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Active = true;
                UserInfo adminInfo = await _dataService.GetUserInfoByEmail(Constants.DefaultUserEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.AdminEmail.ToUpper());
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.DisplayName = adminInfo?.UserName ?? "Unknown";
                result.FirstName = adminInfo?.FirstName ?? "Unknown";
                result.MiddleName = adminInfo?.MiddleName ?? "Unknown";
                result.LastName = adminInfo?.LastName ?? "Unknown";
                result.Email1 = Constants.SupportEmail;
                result.PictureLink = Constants.ProfilePictureUrl;
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
            List<Contact> contactsList = await _dataService.GetContactsList(Constants.DefaultChildId); // _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == Constants.DefaultChildId && c.AccessLevel >= 5).ToListAsync();
            contactsList = contactsList.Where(c => c.AccessLevel >= 5).ToList();
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
            Location result = await _dataService.GetLocation(id); // _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Location();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Name = Constants.AppName;
                result.Latitude = 0.0;
                result.Longitude = 0.0;
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVocabularyItemMobile(int id)
        {
            VocabularyItem result = await _dataService.GetVocabularyItem(id); // _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new VocabularyItem();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Word = Constants.AppName;
                result.DateAdded = DateTime.UtcNow;
                result.Date = DateTime.UtcNow;
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSkillMobile(int id)
        {
            Skill result = await _dataService.GetSkill(id); // _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Skill();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Name = "Launch website";
                result.SkillAddedDate = DateTime.UtcNow;
                result.SkillFirstObservation = DateTime.UtcNow;
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetFriendMobile(int id)
        {
            Friend result = await _dataService.GetFriend(id); // _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Friend();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                UserInfo adminInfo = await _dataService.GetUserInfoByEmail(Constants.DefaultUserEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.AdminEmail.ToUpper());
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.Name = adminInfo?.UserName ?? "Unknown";
                result.FriendAddedDate = DateTime.UtcNow;
                result.FriendSince = DateTime.UtcNow;
                result.Type = 1;
                result.PictureLink = Constants.ProfilePictureUrl;
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
            Measurement result = await _dataService.GetMeasurement(id); // _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Measurement();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Circumference = 0;
                UserInfo adminInfo = await _dataService.GetUserInfoByEmail(Constants.DefaultUserEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.AdminEmail.ToUpper());
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.CreatedDate = DateTime.UtcNow;
                result.Height = 1;
                result.Weight = 1;
                result.Date = DateTime.UtcNow;
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetSleepMobile(int id)
        {
            Sleep result = await _dataService.GetSleep(id); // _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Sleep();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                UserInfo adminInfo = await _dataService.GetUserInfoByEmail(Constants.DefaultUserEmail); // _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.AdminEmail.ToUpper());
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.CreatedDate = DateTime.UtcNow;
                result.SleepStart = DateTime.UtcNow - TimeSpan.FromHours(1);
                result.SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(2);
                result.SleepRating = 3;
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}/{start}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            var model = await _dataService.GetSleepList(Constants.DefaultChildId); //_context.SleepDb.Where(s => s.ProgenyId == Constants.DefaultChildId && s.AccessLevel >= 5).ToListAsync());
            model = model.Where(s => s.AccessLevel >= 5).ToList();
            model = model.OrderByDescending(s => s.SleepStart).ToList();
            model = model.Skip(start).Take(25).ToList();
            return Ok(model);
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            string userTimeZone = Constants.DefaultTimezone;
            SleepStatsModel model = new SleepStatsModel();
            model.SleepTotal = TimeSpan.Zero;
            model.SleepLastYear = TimeSpan.Zero;
            model.SleepLastMonth = TimeSpan.Zero;
            List<Sleep> sList = await _dataService.GetSleepList(Constants.DefaultChildId); // _context.SleepDb.Where(s => s.ProgenyId == Constants.DefaultChildId).ToListAsync();
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
            string userTimeZone = Constants.DefaultTimezone;
            List<Sleep> sList = await _dataService.GetSleepList(Constants.DefaultChildId); // _context.SleepDb.Where(s => s.ProgenyId == Constants.DefaultChildId).ToListAsync();
            List<Sleep> chartList = new List<Sleep>();
            foreach (Sleep chartItem in sList)
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

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await _dataService.GetNote(id); // _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Note();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Content = "Sample Note";
                result.CreatedDate = DateTime.UtcNow;
                result.Title = "Sample";
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVaccinationMobile(int id)
        {
            Vaccination result = await _dataService.GetVaccination(id); // _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Vaccination();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.VaccinationDate = DateTime.UtcNow;
                result.VaccinationName = "Test vaccination";
            }
            return Ok(result);
        }
    }
}