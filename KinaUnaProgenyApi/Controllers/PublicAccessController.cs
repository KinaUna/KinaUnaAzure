using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
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
        private readonly IProgenyService _progenyService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ICalendarService _calendarService;
        private readonly IContactService _contactService;
        private readonly IFriendService _friendService;
        private readonly ILocationService _locationService;
        private readonly ITimelineService _timelineService;
        private readonly IMeasurementService _measurementService;
        private readonly INoteService _noteService;
        private readonly ISkillService _skillService;
        private readonly ISleepService _sleepService;
        private readonly IVaccinationService _vaccinationService;
        private readonly IVocabularyService _vocabularyService;

        public PublicAccessController(ImageStore imageStore, IProgenyService progenyService, IUserInfoService userInfoService, IUserAccessService userAccessService, ICalendarService calendarService,
            IContactService contactService, IFriendService friendService, ILocationService locationService, ITimelineService timelineService, IMeasurementService measurementService, INoteService noteService,
            ISkillService skillService, ISleepService sleepService, IVaccinationService vaccinationService, IVocabularyService vocabularyService)
        {
            _imageStore = imageStore;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _calendarService = calendarService;
            _contactService = contactService;
            _friendService = friendService;
            _locationService = locationService;
            _timelineService = timelineService;
            _measurementService = measurementService;
            _noteService = noteService;
            _skillService = skillService;
            _sleepService = sleepService;
            _vaccinationService = vaccinationService;
            _vocabularyService = vocabularyService;
        }
        // GET api/publicaccess
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            Progeny prog = await _progenyService.GetProgeny(Constants.DefaultChildId);
            List<Progeny> resultList = new List<Progeny>(); 
            resultList.Add(prog);

            return Ok(resultList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            Progeny result = await _progenyService.GetProgeny(Constants.DefaultChildId);
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Progeny);
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Access(int id)
        {
            List<UserAccess> accessList = await _userAccessService.GetProgenyUserAccessList(Constants.DefaultChildId); 
            if (accessList.Any())
            {
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await _progenyService.GetProgeny(ua.ProgenyId);
                    ua.User = new ApplicationUser();
                    UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(ua.UserId);
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
            Progeny prog = await _progenyService.GetProgeny(Constants.DefaultChildId); 
            result.Add(prog);
            return Ok(result);

        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> EventList(int progenyId, int accessLevel)
        {
            var model = await _calendarService.GetCalendarList(Constants.DefaultChildId);
            model = model.Where(e => e.EndTime > DateTime.UtcNow && e.AccessLevel >= 5).OrderBy(e => e.StartTime).ToList();
            model = model.Take(5).ToList();

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}/{count}/{start}")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start = 0)
        {
            List<TimeLineItem> timeLineList = await _timelineService.GetTimeLineList(Constants.DefaultChildId);
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
            CalendarItem result = await _calendarService.GetCalendarItem(id);
            if (result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }
            CalendarItem calItem = new CalendarItem();
            calItem.ProgenyId = Constants.DefaultChildId;
            calItem.AccessLevel = 5;
            calItem.Title = "Launch of KinaUna.com";
            UserInfo adminInfo = await _userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
            calItem.Author = adminInfo?.UserId ?? "Unknown Author";
            calItem.StartTime = new DateTime(2018, 2, 18, 21, 02, 0);
            calItem.EndTime = new DateTime(2018, 2, 18, 22, 02, 0);
            return Ok(calItem);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyCalendarMobile(int id, int accessLevel = 5)
        {
            List<CalendarItem> calendarList = await _calendarService.GetCalendarList(Constants.DefaultChildId);
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
            Contact result = await _contactService.GetContact(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Contact();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Active = true;
                UserInfo adminInfo = await _userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
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
                result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Contacts);
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyContactsMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await _contactService.GetContactsList(Constants.DefaultChildId); 
            contactsList = contactsList.Where(c => c.AccessLevel >= 5).ToList();
            if (contactsList.Any())
            {
                foreach (Contact cont in contactsList)
                {
                    if (!cont.PictureLink.ToLower().StartsWith("http"))
                    {
                        cont.PictureLink = _imageStore.UriFor(cont.PictureLink, BlobContainers.Contacts);
                    }
                }
                return Ok(contactsList);
            }
            else
            {
                return Ok(new List<Contact>());
            }

        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyFriendsMobile(int id, int accessLevel = 5)
        {
            List<Friend> friendsList = await _friendService.GetFriendsList(Constants.DefaultChildId);
            friendsList = friendsList.Where(c => c.AccessLevel >= 5).ToList();
            if (friendsList.Any())
            {
                foreach (Friend frn in friendsList)
                {
                    if (!frn.PictureLink.ToLower().StartsWith("http"))
                    {
                        frn.PictureLink = _imageStore.UriFor(frn.PictureLink, BlobContainers.Friends);
                    }
                }
                return Ok(friendsList);
            }
            else
            {
                return Ok(new List<Contact>());
            }

        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await _locationService.GetLocation(id);
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
            VocabularyItem result = await _vocabularyService.GetVocabularyItem(id);
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
            Skill result = await _skillService.GetSkill(id); 
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
            Friend result = await _friendService.GetFriend(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Friend();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                UserInfo adminInfo = await _userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail); 
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.Name = adminInfo?.UserName ?? "Unknown";
                result.FriendAddedDate = DateTime.UtcNow;
                result.FriendSince = DateTime.UtcNow;
                result.Type = 1;
                result.PictureLink = Constants.ProfilePictureUrl;
            }
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Friends);
            }
            return Ok(result);
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetMeasurementMobile(int id)
        {
            Measurement result = await _measurementService.GetMeasurement(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Measurement();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                result.Circumference = 0;
                UserInfo adminInfo = await _userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
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
            Sleep result = await _sleepService.GetSleep(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Sleep();
                result.AccessLevel = 5;
                result.ProgenyId = Constants.DefaultChildId;
                UserInfo adminInfo = await _userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
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
            var model = await _sleepService.GetSleepList(Constants.DefaultChildId); 
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
            List<Sleep> sList = await _sleepService.GetSleepList(Constants.DefaultChildId);
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
            List<Sleep> sList = await _sleepService.GetSleepList(Constants.DefaultChildId);
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
            Note result = await _noteService.GetNote(id); 
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
            Vaccination result = await _vaccinationService.GetVaccination(id);
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

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyYearAgo(int id, int accessLevel = 5)
        {
            List<TimeLineItem> timeLineList = await _timelineService.GetTimeLineList(Constants.DefaultChildId); 
            timeLineList = timeLineList
                .Where(t => t.AccessLevel >= 5 && t.ProgenyTime.Year < DateTime.UtcNow.Year && t.ProgenyTime.Month == DateTime.UtcNow.Month && t.ProgenyTime.Day == DateTime.UtcNow.Day).OrderBy(t => t.ProgenyTime).ToList();
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

        [HttpGet("[action]")]
        public async Task<IActionResult> GetNotesListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Note> allItems = await _noteService.GetNotesList(Constants.DefaultChildId);
            allItems = allItems.Where(n => n.AccessLevel == 5).OrderBy(v => v.CreatedDate).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int noteCounter = 1;
            int notesCount = allItems.Count;
            foreach (Note note in allItems)
            {
                if (sortBy == 1)
                {
                    note.NoteNumber = notesCount - noteCounter + 1;
                }
                else
                {
                    note.NoteNumber = noteCounter;
                }

                noteCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            NotesListPage model = new NotesListPage();
            model.NotesList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSleepListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Sleep> allItems = await _sleepService.GetSleepList(Constants.DefaultChildId);
            allItems = allItems.Where(s => s.AccessLevel == 5).OrderBy(s => s.SleepStart).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int sleepCounter = 1;
            int slpCount = allItems.Count;
            foreach (Sleep slp in allItems)
            {
                if (sortBy == 1)
                {
                    slp.SleepNumber = slpCount - sleepCounter + 1;
                }
                else
                {
                    slp.SleepNumber = sleepCounter;
                }

                sleepCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            SleepListPage model = new SleepListPage();
            model.SleepList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet("[action]/{sleepId}/{accessLevel}/{sortOrder}")]
        public async Task<IActionResult> GetSleepDetails(int sleepId, int accessLevel, int sortOrder)
        {
            
            Sleep currentSleep = await _sleepService.GetSleep(sleepId);
            if (currentSleep != null && currentSleep.ProgenyId == Constants.DefaultChildId)
            {
                string userTimeZone = Constants.DefaultTimezone;
                List<Sleep> sList = await _sleepService.GetSleepList(currentSleep.ProgenyId);
                List<Sleep> sleepList = new List<Sleep>();
                foreach (Sleep s in sList)
                {
                    if (s.AccessLevel >= accessLevel)
                    {
                        sleepList.Add(s);
                    }
                }

                if (sortOrder == 0)
                {
                    sleepList = sleepList.OrderBy(s => s.SleepStart).ToList();
                }
                else
                {
                    sleepList = sleepList.OrderByDescending(s => s.SleepStart).ToList();
                }

                List<Sleep> model = new List<Sleep>();

                model.Add(currentSleep);
                int currentSleepIndex = sleepList.IndexOf(currentSleep);
                if (currentSleepIndex > 0)
                {
                    model.Add(sleepList[currentSleepIndex - 1]);
                }
                else
                {
                    model.Add(sleepList[sleepList.Count - 1]);
                }

                if (sleepList.Count < currentSleepIndex + 1)
                {
                    model.Add(sleepList[currentSleepIndex + 1]);
                }
                else
                {
                    model.Add(sleepList[0]);
                }

                foreach (Sleep s in model)
                {
                    DateTimeOffset sOffset = new DateTimeOffset(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                    DateTimeOffset eOffset = new DateTimeOffset(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                    s.SleepDuration = eOffset - sOffset;
                }

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> LocationsList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id == Constants.DefaultChildId)
            {
                List<Location> locationsList = await _locationService.GetLocationsList(id);
                locationsList = locationsList.Where(l => l.AccessLevel == 5).ToList();
                if (locationsList.Any())
                {
                    return Ok(locationsList);
                }

                return NotFound();
            }

            return Unauthorized();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetLocationsListPage([FromQuery] int pageSize = 8,
            [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId,
            [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Location> allItems = await _locationService.GetLocationsList(progenyId);
            allItems = allItems.OrderBy(v => v.Date).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int locationCounter = 1;
            int locationsCount = allItems.Count;
            foreach (Location location in allItems)
            {
                if (sortBy == 1)
                {
                    location.LocationNumber = locationsCount - locationCounter + 1;
                }
                else
                {
                    location.LocationNumber = locationCounter;
                }

                locationCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            LocationsListPage model = new LocationsListPage();
            model.LocationsList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetMeasurementsListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Measurement> allItems = await _measurementService.GetMeasurementsList(progenyId);
            allItems = allItems.OrderBy(m => m.Date).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int measurementsCounter = 1;
            int measurementsCount = allItems.Count;
            foreach (Measurement mes in allItems)
            {
                if (sortBy == 1)
                {
                    mes.MeasurementNumber = measurementsCount - measurementsCounter + 1;
                }
                else
                {
                    mes.MeasurementNumber = measurementsCounter;
                }

                measurementsCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            MeasurementsListPage model = new MeasurementsListPage();
            model.MeasurementsList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> MeasurementsList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id == Constants.DefaultChildId)
            {
                List<Measurement> measurementsList = await _measurementService.GetMeasurementsList(id);
                measurementsList = measurementsList.Where(m => m.AccessLevel >= accessLevel).ToList();
                if (measurementsList.Any())
                {
                    return Ok(measurementsList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSkillsListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Skill> allItems = await _skillService.GetSkillsList(progenyId);
            allItems = allItems.OrderBy(s => s.SkillFirstObservation).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int skillsCounter = 1;
            int skillsCount = allItems.Count;
            foreach (Skill skill in allItems)
            {
                if (sortBy == 1)
                {
                    skill.SkillNumber = skillsCount - skillsCounter + 1;
                }
                else
                {
                    skill.SkillNumber = skillsCounter;
                }

                skillsCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            SkillsListPage model = new SkillsListPage();
            model.SkillsList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVocabularyListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<VocabularyItem> allItems = await _vocabularyService.GetVocabularyList(progenyId);
            allItems = allItems.OrderBy(v => v.Date).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int vocabularyCounter = 1;
            int vocabularyCount = allItems.Count;
            foreach (VocabularyItem word in allItems)
            {
                if (sortBy == 1)
                {
                    word.VocabularyItemNumber = vocabularyCount - vocabularyCounter + 1;
                }
                else
                {
                    word.VocabularyItemNumber = vocabularyCounter;
                }

                vocabularyCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            VocabularyListPage model = new VocabularyListPage();
            model.VocabularyList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> VocabularyList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id == Constants.DefaultChildId)
            {
                List<VocabularyItem> wordList = await _vocabularyService.GetVocabularyList(id);
                wordList = wordList.Where(w => w.AccessLevel >= accessLevel).ToList();
                if (wordList.Any())
                {
                    return Ok(wordList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> VaccinationsList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id == Constants.DefaultChildId)
            {
                List<Vaccination> vaccinationsList = await _vaccinationService.GetVaccinationsList(id);
                vaccinationsList = vaccinationsList.Where(v => v.AccessLevel >= accessLevel).ToList();
                if (vaccinationsList.Any())
                {
                    return Ok(vaccinationsList);
                }

                return NotFound();
            }

            return Unauthorized();
        }
    }
}