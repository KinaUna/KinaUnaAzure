using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Models.ViewModels;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KinaUnaProgenyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PublicAccessController(
        IImageStore imageStore,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ICalendarService calendarService,
        IContactService contactService,
        IFriendService friendService,
        ILocationService locationService,
        ITimelineService timelineService,
        IMeasurementService measurementService,
        INoteService noteService,
        ISkillService skillService,
        ISleepService sleepService,
        IVaccinationService vaccinationService,
        IVocabularyService vocabularyService,
        IPicturesService picturesService,
        IVideosService videosService,
        ICommentsService commentsService,
        IConfiguration configuration)
        : ControllerBase
    {
        /// <summary>
        /// Gets the default Progeny's information. For displaying content when a user isn't logged in.
        /// </summary>
        /// <returns>Progeny object for the default Progeny.</returns>
        // GET api/publicaccess
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            Progeny prog = await progenyService.GetProgeny(Constants.DefaultChildId);
            List<Progeny> resultList = [prog];

            return Ok(resultList);
        }

        /// <summary>
        /// Gets the default Progeny's information. For displaying content when a user isn't logged in.
        /// Includes profile picture link with SAS token.
        /// </summary>
        /// <returns>Progeny object for the default Progeny.</returns>
        [HttpGet("{id:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetProgeny(int id)
        {
            Progeny result = await progenyService.GetProgeny(Constants.DefaultChildId);
            result.PictureLink = imageStore.UriFor(result.PictureLink, BlobContainers.Progeny);

            return Ok(result);
        }

        /// <summary>
        /// Gets the access list for the default Progeny. For displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id">The ProgenyId.</param>
        /// <returns>Progeny object.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> Access(int id)
        {
            List<UserAccess> accessList = await userAccessService.GetProgenyUserAccessList(Constants.DefaultChildId);
            if (accessList.Count != 0)
            {
                foreach (UserAccess ua in accessList)
                {
                    ua.Progeny = await progenyService.GetProgeny(ua.ProgenyId);
                    ua.User = new UserInfo();
                    UserInfo userinfo = await userInfoService.GetUserInfoByEmail(ua.UserId);
                    if (userinfo != null)
                    {
                        ua.User = userinfo;
                    }

                }
                return Ok(accessList);
            }

            return NotFound();

        }

        /// <summary>
        /// Gets a List of Progeny items for a user. Dummy for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id">ProgenyId, not actually used.</param>
        /// <returns>List of Progeny</returns>
        [HttpGet("[action]/{id}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> ProgenyListByUser(string id)
        {
            List<Progeny> result = [];
            Progeny prog = await progenyService.GetProgeny(Constants.DefaultChildId);
            result.Add(prog);
            return Ok(result);

        }

        /// <summary>
        /// Gets a list of the default Progeny's CalendarItems, for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="progenyId">ProgenyId</param>
        /// <param name="accessLevel">The current user's access level.</param>
        /// <returns>List of CalendarItems</returns>
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> EventList(int progenyId, int accessLevel)
        {
            List<CalendarItem> model = await calendarService.GetCalendarList(Constants.DefaultChildId);
            model = [.. model.Where(e => e.EndTime > DateTime.UtcNow && e.AccessLevel >= 5).OrderBy(e => e.StartTime)];
            model = model.Take(5).ToList();

            return Ok(model);
        }

        /// <summary>
        /// Gets the latest items for the default Progeny. For displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id">ProgenyId</param>
        /// <param name="accessLevel">The user's current access level.</param>
        /// <param name="count">The number of TimeLineItems to get.</param>
        /// <param name="start">The number of TimeLineItems to skip.</param>
        /// <returns>List of TimeLineItems.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}/{count:int}/{start:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start = 0)
        {
            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(Constants.DefaultChildId);
            timeLineList = [.. timeLineList.Where(t => t.AccessLevel >= 5 && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime)];
            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();

            return Ok(timeLineList.Skip(start).Take(count));

        }

        /// <summary>
        /// Gets a dummy CalendarItem for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetCalendarItemMobile(int id)
        {
            CalendarItem result = await calendarService.GetCalendarItem(id);
            if (result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }
            CalendarItem calItem = new()
            {
                ProgenyId = Constants.DefaultChildId,
                AccessLevel = 5,
                Title = "Launch of KinaUna.com"
            };
            UserInfo adminInfo = await userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
            calItem.Author = adminInfo?.UserId ?? "Unknown Author";
            calItem.StartTime = new DateTime(2018, 2, 18, 21, 02, 0);
            calItem.EndTime = new DateTime(2018, 2, 18, 22, 02, 0);
            return Ok(calItem);
        }

        /// <summary>
        /// Gets a dummy list of CalendarItems for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> ProgenyCalendarMobile(int id, int accessLevel = 5)
        {
            List<CalendarItem> calendarList = await calendarService.GetCalendarList(Constants.DefaultChildId);
            calendarList = calendarList.Where(c => c.AccessLevel >= 5).ToList();
            if (calendarList.Count != 0)
            {
                return Ok(calendarList);
            }

            return NotFound();

        }

        /// <summary>
        /// Gets a dummy Contact for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact result = await contactService.GetContact(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Contact
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId,
                    Active = true
                };
                UserInfo adminInfo = await userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.DisplayName = adminInfo?.UserName ?? "Unknown";
                result.FirstName = adminInfo?.FirstName ?? "Unknown";
                result.MiddleName = adminInfo?.MiddleName ?? "Unknown";
                result.LastName = adminInfo?.LastName ?? "Unknown";
                result.Email1 = Constants.SupportEmail;
                result.PictureLink = Constants.ProfilePictureUrl;
            }
            result.PictureLink = imageStore.UriFor(result.PictureLink, BlobContainers.Contacts);
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Contact, for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> ProgenyContactsMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await contactService.GetContactsList(Constants.DefaultChildId);
            contactsList = contactsList.Where(c => c.AccessLevel >= 5).ToList();
            if (contactsList.Count == 0) return Ok(new List<Contact>());

            foreach (Contact cont in contactsList)
            {
                cont.PictureLink = imageStore.UriFor(cont.PictureLink, BlobContainers.Contacts);
            }
            return Ok(contactsList);

        }

        /// <summary>
        /// Gets a dummy Friend object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> ProgenyFriendsMobile(int id, int accessLevel = 5)
        {
            List<Friend> friendsList = await friendService.GetFriendsList(Constants.DefaultChildId);
            friendsList = friendsList.Where(c => c.AccessLevel >= 5).ToList();
            if (friendsList.Count == 0) return Ok(new List<Contact>());

            foreach (Friend frn in friendsList)
            {
                frn.PictureLink = imageStore.UriFor(frn.PictureLink, BlobContainers.Friends);
            }
            return Ok(friendsList);

        }

        /// <summary>
        /// Gets a dummy Location object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await locationService.GetLocation(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Location
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId,
                    Name = Constants.AppName,
                    Latitude = 0.0,
                    Longitude = 0.0
                };
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Location object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetVocabularyItemMobile(int id)
        {
            VocabularyItem result = await vocabularyService.GetVocabularyItem(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new VocabularyItem
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId,
                    Word = Constants.AppName,
                    DateAdded = DateTime.UtcNow,
                    Date = DateTime.UtcNow
                };
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Skill object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetSkillMobile(int id)
        {
            Skill result = await skillService.GetSkill(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Skill
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId,
                    Name = "Launch website",
                    SkillAddedDate = DateTime.UtcNow,
                    SkillFirstObservation = DateTime.UtcNow
                };
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Friend object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetFriendMobile(int id)
        {
            Friend result = await friendService.GetFriend(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Friend
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId
                };
                UserInfo adminInfo = await userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
                result.Author = adminInfo?.UserId ?? "Unknown Author";
                result.Name = adminInfo?.UserName ?? "Unknown";
                result.FriendAddedDate = DateTime.UtcNow;
                result.FriendSince = DateTime.UtcNow;
                result.Type = 1;
                result.PictureLink = Constants.ProfilePictureUrl;
            }

            result.PictureLink = imageStore.UriFor(result.PictureLink, BlobContainers.Friends);
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Measurement object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetMeasurementMobile(int id)
        {
            Measurement result = await measurementService.GetMeasurement(id);
            if (result.ProgenyId == Constants.DefaultChildId) return Ok(result);

            result = new Measurement
            {
                AccessLevel = 5,
                ProgenyId = Constants.DefaultChildId,
                Circumference = 0
            };
            UserInfo adminInfo = await userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
            result.Author = adminInfo?.UserId ?? "Unknown Author";
            result.CreatedDate = DateTime.UtcNow;
            result.Height = 1;
            result.Weight = 1;
            result.Date = DateTime.UtcNow;
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Sleep object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetSleepMobile(int id)
        {
            Sleep result = await sleepService.GetSleep(id);
            if (result.ProgenyId == Constants.DefaultChildId) return Ok(result);

            result = new Sleep
            {
                AccessLevel = 5,
                ProgenyId = Constants.DefaultChildId
            };
            UserInfo adminInfo = await userInfoService.GetUserInfoByEmail(Constants.DefaultUserEmail);
            result.Author = adminInfo?.UserId ?? "Unknown Author";
            result.CreatedDate = DateTime.UtcNow;
            result.SleepStart = DateTime.UtcNow - TimeSpan.FromHours(1);
            result.SleepEnd = DateTime.UtcNow + TimeSpan.FromHours(2);
            result.SleepRating = 3;
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy List of Sleep items for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}/{start:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            List<Sleep> model = await sleepService.GetSleepList(Constants.DefaultChildId);
            model = model.Where(s => s.AccessLevel >= 5).ToList();
            model = [.. model.OrderByDescending(s => s.SleepStart)];
            model = model.Skip(start).Take(25).ToList();
            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy SleepStatsModel object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            const string userTimeZone = Constants.DefaultTimezone;
            SleepStatsModel model = new()
            {
                SleepTotal = TimeSpan.Zero,
                SleepLastYear = TimeSpan.Zero,
                SleepLastMonth = TimeSpan.Zero
            };
            List<Sleep> sList = await sleepService.GetSleepList(Constants.DefaultChildId);
            List<Sleep> sleepList = [];
            DateTime yearAgo = new(DateTime.UtcNow.Year - 1, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
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
                    DateTimeOffset sOffset = new(s.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                    DateTimeOffset eOffset = new(s.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                    s.SleepDuration = eOffset - sOffset;

                    model.SleepTotal += s.SleepDuration;
                    if (isLessThanYear)
                    {
                        model.SleepLastYear += s.SleepDuration;
                    }

                    if (isLessThanMonth)
                    {
                        model.SleepLastMonth += s.SleepDuration;
                    }

                    if (s.AccessLevel >= accessLevel)
                    {
                        sleepList.Add(s);
                    }
                }
                sleepList = [.. sleepList.OrderBy(s => s.SleepStart)];

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

        /// <summary>
        /// Gets a dummy List of Sleep items for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet("[action]/{progenyId:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            const string userTimeZone = Constants.DefaultTimezone;
            List<Sleep> sList = await sleepService.GetSleepList(Constants.DefaultChildId);
            List<Sleep> chartList = [];
            foreach (Sleep chartItem in sList)
            {
                double durationStartDate = 0.0;
                if (chartItem.SleepStart.Date == chartItem.SleepEnd.Date)
                {
                    durationStartDate += chartItem.SleepDuration.TotalMinutes;
                    Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                    if (slpItem != null)
                    {
                        slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                    }
                    else
                    {

                        Sleep newSleep = new()
                        {
                            SleepStart = chartItem.SleepStart,
                            SleepDuration = TimeSpan.FromMinutes(durationStartDate)
                        };
                        chartList.Add(newSleep);
                    }
                }
                else
                {
                    DateTimeOffset sOffset = new(chartItem.SleepStart,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepStart));
                    DateTimeOffset s2Offset = new(chartItem.SleepStart.Date + TimeSpan.FromDays(1),
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                            .GetUtcOffset(chartItem.SleepStart.Date + TimeSpan.FromDays(1)));
                    DateTimeOffset eOffset = new(chartItem.SleepEnd,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(chartItem.SleepEnd));
                    DateTimeOffset e2Offset = new(chartItem.SleepEnd.Date,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone)
                            .GetUtcOffset(chartItem.SleepEnd.Date));
                    TimeSpan sDateDuration = s2Offset - sOffset;
                    TimeSpan eDateDuration = eOffset - e2Offset;
                    durationStartDate = chartItem.SleepDuration.TotalMinutes - (eDateDuration.TotalMinutes);
                    double durationEndDate = chartItem.SleepDuration.TotalMinutes - sDateDuration.TotalMinutes;
                    Sleep slpItem = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepStart.Date);
                    if (slpItem != null)
                    {
                        slpItem.SleepDuration += TimeSpan.FromMinutes(durationStartDate);
                    }
                    else
                    {
                        Sleep newSleep = new()
                        {
                            SleepStart = chartItem.SleepStart,
                            SleepDuration = TimeSpan.FromMinutes(durationStartDate)
                        };
                        chartList.Add(newSleep);
                    }

                    Sleep slpItem2 = chartList.SingleOrDefault(s => s.SleepStart.Date == chartItem.SleepEnd.Date);
                    if (slpItem2 != null)
                    {
                        slpItem2.SleepDuration += TimeSpan.FromMinutes(durationEndDate);
                    }
                    else
                    {
                        Sleep newSleep = new()
                        {
                            SleepStart = chartItem.SleepEnd,
                            SleepDuration = TimeSpan.FromMinutes(durationEndDate)
                        };
                        chartList.Add(newSleep);
                    }
                }
            }

            List<Sleep> model = [.. chartList.OrderBy(s => s.SleepStart)];

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy Note object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await noteService.GetNote(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Note
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId,
                    Content = "Sample Note",
                    CreatedDate = DateTime.UtcNow,
                    Title = "Sample"
                };
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy Vaccination object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetVaccinationMobile(int id)
        {
            Vaccination result = await vaccinationService.GetVaccination(id);
            if (result.ProgenyId != Constants.DefaultChildId)
            {
                result = new Vaccination
                {
                    AccessLevel = 5,
                    ProgenyId = Constants.DefaultChildId,
                    VaccinationDate = DateTime.UtcNow,
                    VaccinationName = "Test vaccination"
                };
            }
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy List of TimeLine objects for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> ProgenyYearAgo(int id, int accessLevel = 5)
        {
            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(Constants.DefaultChildId);
            timeLineList = [.. timeLineList
                .Where(t => t.AccessLevel >= 5 && t.ProgenyTime.Year < DateTime.UtcNow.Year && t.ProgenyTime.Month == DateTime.UtcNow.Month && t.ProgenyTime.Day == DateTime.UtcNow.Day).OrderBy(t => t.ProgenyTime)];
            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();
            return Ok(timeLineList);

        }

        /// <summary>
        /// Gets a dummy NotesListPage object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetNotesListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Note> allItems = await noteService.GetNotesList(Constants.DefaultChildId);
            allItems = [.. allItems.Where(n => n.AccessLevel == 5).OrderBy(v => v.CreatedDate)];

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

            List<Note> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            NotesListPage model = new()
            {
                NotesList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy SleepListPage object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetSleepListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Sleep> allItems = await sleepService.GetSleepList(Constants.DefaultChildId);
            allItems = [.. allItems.Where(s => s.AccessLevel == 5).OrderBy(s => s.SleepStart)];

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

            List<Sleep> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            SleepListPage model = new()
            {
                SleepList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy List of Sleep objects for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="sleepId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        [HttpGet("[action]/{sleepId:int}/{accessLevel:int}/{sortOrder:int}")]
        public async Task<IActionResult> GetSleepDetails(int sleepId, int accessLevel, int sortOrder)
        {

            Sleep currentSleep = await sleepService.GetSleep(sleepId);
            if (currentSleep == null || currentSleep.ProgenyId != Constants.DefaultChildId) return Unauthorized();

            const string userTimeZone = Constants.DefaultTimezone;
            List<Sleep> sList = await sleepService.GetSleepList(currentSleep.ProgenyId);
            List<Sleep> sleepList = [];
            foreach (Sleep s in sList)
            {
                if (s.AccessLevel >= accessLevel)
                {
                    sleepList.Add(s);
                }
            }

            if (sortOrder == 0)
            {
                sleepList = [.. sleepList.OrderBy(s => s.SleepStart)];
            }
            else
            {
                sleepList = [.. sleepList.OrderByDescending(s => s.SleepStart)];
            }

            List<Sleep> model = [currentSleep];

            int currentSleepIndex = sleepList.IndexOf(currentSleep);
            if (currentSleepIndex > 0)
            {
                model.Add(sleepList[currentSleepIndex - 1]);
            }
            else
            {
                model.Add(sleepList[^1]);
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
                DateTimeOffset sOffset = new(s.SleepStart,
                    TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepStart));
                DateTimeOffset eOffset = new(s.SleepEnd,
                    TimeZoneInfo.FindSystemTimeZoneById(userTimeZone).GetUtcOffset(s.SleepEnd));
                s.SleepDuration = eOffset - sOffset;
            }

            return Ok(model);

        }

        /// <summary>
        /// Gets a dummy List of Location objects for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> LocationsList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id != Constants.DefaultChildId) return Unauthorized();

            List<Location> locationsList = await locationService.GetLocationsList(id);
            locationsList = locationsList.Where(l => l.AccessLevel == 5).ToList();
            if (locationsList.Count != 0)
            {
                return Ok(locationsList);
            }

            return NotFound();

        }

        /// <summary>
        /// Gets a dummy LocationListPage object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
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

            List<Location> allItems = await locationService.GetLocationsList(progenyId);
            allItems = [.. allItems.OrderBy(v => v.Date)];

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

            List<Location> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            LocationsListPage model = new()
            {
                LocationsList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy MeasurementsListPage object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetMeasurementsListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Measurement> allItems = await measurementService.GetMeasurementsList(progenyId);
            allItems = [.. allItems.OrderBy(m => m.Date)];

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

            List<Measurement> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            MeasurementsListPage model = new()
            {
                MeasurementsList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy List of Measurement objects for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> MeasurementsList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id != Constants.DefaultChildId) return Unauthorized();

            List<Measurement> measurementsList = await measurementService.GetMeasurementsList(id);
            measurementsList = measurementsList.Where(m => m.AccessLevel >= accessLevel).ToList();
            if (measurementsList.Count != 0)
            {
                return Ok(measurementsList);
            }
            return NotFound();

        }

        /// <summary>
        /// Gets a dummy SkillsListPage object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetSkillsListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Skill> allItems = await skillService.GetSkillsList(progenyId);
            allItems = [.. allItems.OrderBy(s => s.SkillFirstObservation)];

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

            List<Skill> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            SkillsListPage model = new()
            {
                SkillsList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy VocabularyListPage object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> GetVocabularyListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            if (progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<VocabularyItem> allItems = await vocabularyService.GetVocabularyList(progenyId);
            allItems = [.. allItems.OrderBy(v => v.Date)];

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

            List<VocabularyItem> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            VocabularyListPage model = new()
            {
                VocabularyList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy List of VocabularyItem objects for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> VocabularyList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id != Constants.DefaultChildId) return Unauthorized();

            List<VocabularyItem> wordList = await vocabularyService.GetVocabularyList(id);
            wordList = wordList.Where(w => w.AccessLevel >= accessLevel).ToList();
            if (wordList.Count != 0)
            {
                return Ok(wordList);
            }
            return NotFound();

        }

        /// <summary>
        /// Gets a dummy List of Vaccination objects for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> VaccinationsList(int id, [FromQuery] int accessLevel = 5)
        {
            if (id != Constants.DefaultChildId) return Unauthorized();

            List<Vaccination> vaccinationsList = await vaccinationService.GetVaccinationsList(id);
            vaccinationsList = vaccinationsList.Where(v => v.AccessLevel >= accessLevel).ToList();
            if (vaccinationsList.Count != 0)
            {
                return Ok(vaccinationsList);
            }

            return NotFound();

        }

        /// <summary>
        /// Gets a dummy Random Picture object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> RandomPictureMobile(int progenyId, int accessLevel)
        {
            List<Picture> picturesList = await picturesService.GetPicturesList(Constants.DefaultChildId);
            picturesList = picturesList.Where(p => p.AccessLevel >= 5).ToList();
            if (picturesList.Count != 0)
            {
                Random r = new();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                picture.PictureLink = imageStore.UriFor(picture.PictureLink);
                picture.PictureLink1200 = imageStore.UriFor(picture.PictureLink1200);
                picture.PictureLink600 = imageStore.UriFor(picture.PictureLink600);

                return Ok(picture);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),
                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };

            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets a dummy Picture object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET api/pictures/5
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetPictureMobile(int id)
        {
            Picture result = await picturesService.GetPicture(id);
            if (result != null)
            {
                if (result.ProgenyId == Constants.DefaultChildId)
                {
                    result.PictureLink = imageStore.UriFor(result.PictureLink);
                    result.PictureLink1200 = imageStore.UriFor(result.PictureLink1200);
                    result.PictureLink600 = imageStore.UriFor(result.PictureLink600);
                    return Ok(result);
                }

            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),
                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };

            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        /// <summary>
        /// Gets a dummy PicturePageViewModel object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="tagFilter"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> PageMobile([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems;
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await picturesService.GetPicturesList(Constants.DefaultChildId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5 && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
            }
            else
            {
                allItems = await picturesService.GetPicturesList(2);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5).OrderBy(p => p.PictureTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            List<string> tagsList = [];
            foreach (Picture pic in allItems)
            {
                if (sortBy == 1)
                {
                    pic.PictureNumber = picCount - pictureCounter + 1;
                }
                else
                {
                    pic.PictureNumber = pictureCounter;
                }

                pictureCounter++;
                if (string.IsNullOrEmpty(pic.Tags)) continue;

                List<string> pvmTags = [.. pic.Tags.Split(',')];
                foreach (string tagstring in pvmTags)
                {
                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                    {
                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                    }
                }
            }

            List<Picture> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
                pic.PictureLink = imageStore.UriFor(pic.PictureLink);
                pic.PictureLink1200 = imageStore.UriFor(pic.PictureLink1200);
                pic.PictureLink600 = imageStore.UriFor(pic.PictureLink600);
            }
            PicturePageViewModel model = new()
            {
                PicturesList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy,
                TagFilter = tagFilter
            };
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy PictureViewModel object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> PictureViewModelMobile(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {

            Picture picture = await picturesService.GetPicture(id);

            if (picture == null || picture.ProgenyId != Constants.DefaultChildId) return NotFound();
            
            PictureViewModel model = new()
            {
                PictureId = picture.PictureId,
                PictureTime = picture.PictureTime,
                ProgenyId = picture.ProgenyId,
                Owners = picture.Owners,
                PictureLink = picture.PictureLink1200
            };
            model.PictureLink = imageStore.UriFor(model.PictureLink);
            model.AccessLevel = picture.AccessLevel;
            model.Author = picture.Author;
            model.CommentThreadNumber = picture.CommentThreadNumber;
            model.Tags = picture.Tags;
            model.Location = picture.Location;
            model.Latitude = picture.Latitude;
            model.Longtitude = picture.Longtitude;
            model.Altitude = picture.Altitude;
            model.PictureNumber = 1;
            model.PictureCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
            {
                int currentIndex = 0;
                int indexer = 0;
                foreach (Picture pic in pictureList)
                {
                    if (pic.PictureId == picture.PictureId)
                    {
                        currentIndex = indexer;
                    }
                    indexer++;
                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
                model.PictureNumber = currentIndex + 1;
                model.PictureCount = pictureList.Count;
                if (currentIndex > 0)
                {
                    model.PrevPicture = pictureList[currentIndex - 1].PictureId;
                }
                else
                {
                    model.PrevPicture = pictureList.Last().PictureId;
                }

                if (currentIndex + 1 < pictureList.Count)
                {
                    model.NextPicture = pictureList[currentIndex + 1].PictureId;
                }
                else
                {
                    model.NextPicture = pictureList.First().PictureId;
                }

                if (sortBy == 1)
                {
                    (model.PrevPicture, model.NextPicture) = (model.NextPicture, model.PrevPicture);
                }

            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            model.TagsList = tagItems;
            return Ok(model);

        }

        /// <summary>
        /// Gets a dummy Video object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetVideoMobile(int id)
        {
            Video result = await videosService.GetVideo(id);
            if (result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            // Todo: Create default video item
            result = await videosService.GetVideo(204);
            return Ok(result);
        }

        /// <summary>
        /// Gets a dummy VideoPageViewModel object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="progenyId"></param>
        /// <param name="accessLevel"></param>
        /// <param name="tagFilter"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mobile clients.")]
        public async Task<IActionResult> VideoPageMobile([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)

        {
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Video> allItems;
            if (tagFilter != "")
            {
                allItems = await videosService.GetVideosList(Constants.DefaultChildId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5 && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.VideoTime)];
            }
            else
            {
                allItems = await videosService.GetVideosList(Constants.DefaultChildId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= 5).OrderBy(p => p.VideoTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int videoCounter = 1;
            int vidCount = allItems.Count;
            List<string> tagsList = [];
            foreach (Video vid in allItems)
            {
                if (sortBy == 1)
                {
                    vid.VideoNumber = vidCount - videoCounter + 1;
                }
                else
                {
                    vid.VideoNumber = videoCounter;
                }

                videoCounter++;
                if (!string.IsNullOrEmpty(vid.Tags))
                {
                    List<string> pvmTags = [.. vid.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }

                if (vid.Duration == null) continue;

                vid.DurationHours = vid.Duration.Value.Hours.ToString();
                vid.DurationMinutes = vid.Duration.Value.Minutes.ToString();
                vid.DurationSeconds = vid.Duration.Value.Seconds.ToString();
                if (vid.DurationSeconds.Length == 1)
                {
                    vid.DurationSeconds = "0" + vid.DurationSeconds;
                }

                if (vid.Duration.Value.Hours == 0) continue;

                if (vid.DurationMinutes.Length == 1)
                {
                    vid.DurationMinutes = "0" + vid.DurationMinutes;
                }
            }

            List<Video> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Video vid in itemsOnPage)
            {
                vid.Comments = await commentsService.GetCommentsList(vid.CommentThreadNumber);
            }
            VideoPageViewModel model = new()
            {
                VideosList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy,
                TagFilter = tagFilter
            };
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        /// <summary>
        /// Gets a dummy VideoViewModel object for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="accessLevel"></param>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> VideoViewModelMobile(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {

            Video video = await videosService.GetVideo(id);

            if (video == null || video.ProgenyId != Constants.DefaultChildId) return NotFound();
            
            VideoViewModel model = new()
            {
                VideoId = video.VideoId,
                VideoType = video.VideoType,
                VideoTime = video.VideoTime,
                Duration = video.Duration,
                ProgenyId = video.ProgenyId,
                Owners = video.Owners,
                VideoLink = video.VideoLink,
                ThumbLink = video.ThumbLink,
                AccessLevel = video.AccessLevel,
                Author = video.Author
            };
            model.AccessLevelListEn[video.AccessLevel].Selected = true;
            model.AccessLevelListDa[video.AccessLevel].Selected = true;
            model.AccessLevelListDe[video.AccessLevel].Selected = true;
            model.CommentThreadNumber = video.CommentThreadNumber;
            model.Tags = video.Tags;
            model.VideoNumber = 1;
            model.VideoCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(video.CommentThreadNumber);
            model.Location = video.Location;
            model.Longtitude = video.Longtitude;
            model.Latitude = video.Latitude;
            model.Altitude = video.Latitude;
            model.TagsList = "";
            List<string> tagsList = [];
            List<Video> videosList = await videosService.GetVideosList(video.ProgenyId);
            videosList = [.. videosList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.VideoTime)];
            if (videosList.Count != 0)
            {
                int currentIndex = 0;
                int indexer = 0;
                foreach (Video vid in videosList)
                {
                    if (vid.VideoId == video.VideoId)
                    {
                        currentIndex = indexer;
                    }
                    indexer++;
                    if (string.IsNullOrEmpty(vid.Tags)) continue;

                    List<string> pvmTags = [.. vid.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
                model.VideoNumber = currentIndex + 1;
                model.VideoCount = videosList.Count;
                if (currentIndex > 0)
                {
                    model.PrevVideo = videosList[currentIndex - 1].VideoId;
                }
                else
                {
                    model.PrevVideo = videosList.Last().VideoId;
                }

                if (currentIndex + 1 < videosList.Count)
                {
                    model.NextVideo = videosList[currentIndex + 1].VideoId;
                }
                else
                {
                    model.NextVideo = videosList.First().VideoId;
                }

                if (sortBy == 1)
                {
                    (model.NextVideo, model.PrevVideo) = (model.PrevVideo, model.NextVideo);
                }

            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            model.TagsList = tagItems;

            return Ok(model);

        }

        /// <summary>
        /// Gets a dummy string of tags for displaying content when a user isn't logged in.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> PictureTagsList(int id)
        {
            string tagListString = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(id);
            if (pictureList.Count != 0)
            {
                foreach (Picture pic in pictureList)
                {
                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }
            else
            {
                return Ok(tagListString);
            }

            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            tagListString = tagItems;
            return Ok(tagListString);
        }
    }
}