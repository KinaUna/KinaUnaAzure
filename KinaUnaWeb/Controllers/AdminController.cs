using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUnaWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using Measurement = KinaUnaWeb.Models.Measurement;

namespace KinaUnaWeb.Controllers
{
    public class AdminController: Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly WebDbContext _context;
        private readonly IApplicationLifetime _appLifetime;
        private readonly ILogger _logger;
        private readonly IHubContext<WebNotificationHub> _hubContext;
        private readonly IPushMessageSender _pushMessageSender;

        public AdminController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore,
            IConfiguration configuration, IHttpContextAccessor httpContextAccessor, WebDbContext context,
            IBackgroundTaskQueue queue, IApplicationLifetime appLifetime, ILoggerFactory loggerFactory,
            IHubContext<WebNotificationHub> hubContext, IPushMessageSender pushMessageSender)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            Queue = queue;
            _appLifetime = appLifetime;
            _logger = loggerFactory.CreateLogger<AdminController>();
            _hubContext = hubContext;
            _pushMessageSender = pushMessageSender;
        }
        public IBackgroundTaskQueue Queue { get; }

        public IActionResult Index()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public async Task<IActionResult> ImportLocations()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Location> allLocations = new List<Location>();
            string locationsApiPath = "api/locations/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var locationsResponse = await progenyClient.GetAsync(locationsApiPath).ConfigureAwait(false);
            if (locationsResponse.IsSuccessStatusCode)
            {
                var locationsAsString = await locationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allLocations = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
            }
            
            return View(allLocations);
        }

        public async Task<IActionResult> ImportPictures()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Picture> allPictures = new List<Picture>();
            string clientUri = _configuration.GetValue<string>("MediaApiServer");
            string accessToken = string.Empty;

            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;
            // get access token
            accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string pictureApiPath = "/api/pictures/syncall/";
            var pictureUri = clientUri + pictureApiPath;

            var pictureResponseString = await pictureHttpClient.GetStringAsync(pictureUri);
            
            allPictures = JsonConvert.DeserializeObject<List<Picture>>(pictureResponseString);
            
            return View(allPictures);
        }

        public async Task<IActionResult> ImportComments()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Comment> allComments = new List<Comment>();
            string clientUri = _configuration.GetValue<string>("MediaApiServer");
            string accessToken = string.Empty;

            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;
            // get access token
            accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient commentsHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                commentsHttpClient.SetBearerToken(accessToken);
            }
            commentsHttpClient.BaseAddress = new Uri(clientUri);
            commentsHttpClient.DefaultRequestHeaders.Accept.Clear();
            commentsHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string commentsApiPath = "/api/comments/syncall/";
            var commentsUri = clientUri + commentsApiPath;

            var commentsResponseString = await commentsHttpClient.GetStringAsync(commentsUri);

            allComments = JsonConvert.DeserializeObject<List<Comment>>(commentsResponseString);

            return View(allComments);
        }

        public async Task<IActionResult> ImportVideos()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Video> allVideos = new List<Video>();
            string clientUri = _configuration.GetValue<string>("MediaApiServer");
            string accessToken = string.Empty;

            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;
            // get access token
            accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string videoApiPath = "/api/videos/syncall/";
            var videoUri = clientUri + videoApiPath;

            var videoResponseString = await pictureHttpClient.GetStringAsync(videoUri);

            allVideos = JsonConvert.DeserializeObject<List<Video>>(videoResponseString);

            return View(allVideos);
        }

        public async Task<IActionResult> ImportCalendars()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<CalendarItem> allCalendarItems = new List<CalendarItem>();
            string calendarApiPath = "api/calendar/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var calendarResponse = await progenyClient.GetAsync(calendarApiPath).ConfigureAwait(false);
            if (calendarResponse.IsSuccessStatusCode)
            {
                var calendarItemsAsString = await calendarResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allCalendarItems = JsonConvert.DeserializeObject<List<CalendarItem>>(calendarItemsAsString);
            }

            return View(allCalendarItems);
        }

        public async Task<IActionResult> ImportVocabulary()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<VocabularyItem> allVocabularyItems = new List<VocabularyItem>();
            string vocabularyApiPath = "api/vocabulary/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var vocabularyResponse = await progenyClient.GetAsync(vocabularyApiPath).ConfigureAwait(false);
            if (vocabularyResponse.IsSuccessStatusCode)
            {
                var vocabularyItemsAsString = await vocabularyResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allVocabularyItems = JsonConvert.DeserializeObject<List<VocabularyItem>>(vocabularyItemsAsString);
            }

            return View(allVocabularyItems);
        }

        public async Task<IActionResult> ImportSkills()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Skill> allSkillsItems = new List<Skill>();
            string skillsApiPath = "api/skills/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var skillsResponse = await progenyClient.GetAsync(skillsApiPath).ConfigureAwait(false);
            if (skillsResponse.IsSuccessStatusCode)
            {
                var skillsItemsAsString = await skillsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allSkillsItems = JsonConvert.DeserializeObject<List<Skill>>(skillsItemsAsString);
            }

            return View(allSkillsItems);
        }

        public async Task<IActionResult> ImportFriends()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Friend> allFriendsItems = new List<Friend>();
            string skillsApiPath = "api/friends/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var friendsResponse = await progenyClient.GetAsync(skillsApiPath).ConfigureAwait(false);
            if (friendsResponse.IsSuccessStatusCode)
            {
                var friendsItemsAsString = await friendsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allFriendsItems = JsonConvert.DeserializeObject<List<Friend>>(friendsItemsAsString);
            }

            return View(allFriendsItems);
        }

        public async Task<IActionResult> ImportTimeLine()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<TimeLineItem> allTimeLineItems = new List<TimeLineItem>();
            string timeLineApiPath = "api/timeline/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var timeLineResponse = await progenyClient.GetAsync(timeLineApiPath).ConfigureAwait(false);
            if (timeLineResponse.IsSuccessStatusCode)
            {
                var timeLineItemsAsString = await timeLineResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allTimeLineItems = JsonConvert.DeserializeObject<List<TimeLineItem>>(timeLineItemsAsString);
            }

            return View(allTimeLineItems);
        }

        public async Task<IActionResult> ImportMeasurements()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Measurement> allMeasurementItems = new List<Measurement>();
            string measurementsApiPath = "api/measurements/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var measurementsResponse = await progenyClient.GetAsync(measurementsApiPath).ConfigureAwait(false);
            if (measurementsResponse.IsSuccessStatusCode)
            {
                var measurementsItemsAsString = await measurementsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allMeasurementItems = JsonConvert.DeserializeObject<List<Measurement>>(measurementsItemsAsString);
            }

            return View(allMeasurementItems);
        }

        public async Task<IActionResult> ImportSleep()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Sleep> allSleepItems = new List<Sleep>();
            string sleepApiPath = "api/sleep/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var sleepResponse = await progenyClient.GetAsync(sleepApiPath).ConfigureAwait(false);
            if (sleepResponse.IsSuccessStatusCode)
            {
                var sleepItemsAsString = await sleepResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allSleepItems = JsonConvert.DeserializeObject<List<Sleep>>(sleepItemsAsString);
            }

            return View(allSleepItems);
        }

        public async Task<IActionResult> ImportNotes()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Note> allNoteItems = new List<Note>();
            string notesApiPath = "api/notes/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var notesResponse = await progenyClient.GetAsync(notesApiPath).ConfigureAwait(false);
            if (notesResponse.IsSuccessStatusCode)
            {
                var noteItemsAsString = await notesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allNoteItems = JsonConvert.DeserializeObject<List<Note>>(noteItemsAsString);
            }

            return View(allNoteItems);
        }

        public async Task<IActionResult> ImportContacts()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Contact> allContactItems = new List<Contact>();
            string contactsApiPath = "api/contacts/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var contactsResponse = await progenyClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (contactsResponse.IsSuccessStatusCode)
            {
                var contactItemsAsString = await contactsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allContactItems = JsonConvert.DeserializeObject<List<Contact>>(contactItemsAsString);
            }

            return View(allContactItems);
        }

        public async Task<IActionResult> ImportAddresses()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Address> allAddressItems = new List<Address>();
            string addressesApiPath = "api/addresses/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var addressesResponse = await progenyClient.GetAsync(addressesApiPath).ConfigureAwait(false);
            if (addressesResponse.IsSuccessStatusCode)
            {
                var addressItemsAsString = await addressesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allAddressItems = JsonConvert.DeserializeObject<List<Address>>(addressItemsAsString);
            }

            return View(allAddressItems);
        }

        public async Task<IActionResult> ImportVaccinations()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Vaccination> allVaccinationItems = new List<Vaccination>();
            string vaccinationsApiPath = "api/vaccinations/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var vaccinationsResponse = await progenyClient.GetAsync(vaccinationsApiPath).ConfigureAwait(false);
            if (vaccinationsResponse.IsSuccessStatusCode)
            {
                var vaccinationItemsAsString = await vaccinationsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allVaccinationItems = JsonConvert.DeserializeObject<List<Vaccination>>(vaccinationItemsAsString);
            }

            return View(allVaccinationItems);
        }

        public async Task<IActionResult> ImportUserAccess()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<UserAccess> allUserAccessItems = new List<UserAccess>();
            string userAccessApiPath = "api/access/syncall";
            var progenyClient = await _progenyHttpClient.GetClient();
            var userAccessResponse = await progenyClient.GetAsync(userAccessApiPath).ConfigureAwait(false);
            if (userAccessResponse.IsSuccessStatusCode)
            {
                var userAccessItemsAsString = await userAccessResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                allUserAccessItems = JsonConvert.DeserializeObject<List<UserAccess>>(userAccessItemsAsString);
            }

            return View(allUserAccessItems);
        }

        public async Task<IActionResult> UpdateTimeLine()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? "testuser@kinauna.com";
            string myEmail = "per.mogensen@gmail.com";
            if (userEmail.ToUpper() != myEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            List<int> itemsCount = new List<int>();
            itemsCount.Add(0);

            
            
            List<Picture> pictures = await _mediaHttpClient.GetAllPictures();
            foreach (Picture pic in pictures)
            {
                if (pic.PictureTime != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = pic.ProgenyId;
                    tItem.ProgenyTime = pic.PictureTime.Value;
                    if (pic.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (pic.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = pic.AccessLevel;
                    tItem.ItemId = pic.PictureId.ToString();
                    tItem.CreatedTime = pic.PictureTime.Value;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Photo;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }

            }

            List<Video> videos = await _mediaHttpClient.GetAllVideos();
            foreach (Video vid in videos)
            {
                if (vid.VideoTime != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = vid.ProgenyId;
                    tItem.ProgenyTime = vid.VideoTime.Value;
                    if (vid.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (vid.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = vid.AccessLevel;
                    tItem.ItemId = vid.VideoId.ToString();
                    tItem.CreatedTime = vid.VideoTime.Value;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Video;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }

            }

            List<CalendarItem> calendarEvents = _context.CalendarDb.ToList();
            foreach (CalendarItem evt in calendarEvents)
            {
                if (evt.StartTime != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = evt.ProgenyId;
                    tItem.ProgenyTime = evt.StartTime.Value;
                    if (evt.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (evt.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = evt.AccessLevel;
                    tItem.ItemId = evt.EventId.ToString();
                    tItem.CreatedTime = evt.StartTime.Value;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Calendar;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }

            }

            List<VocabularyItem> vocList = _context.VocabularyDb.ToList();
            foreach (VocabularyItem voc in vocList)
            {
                if (voc.Date != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = voc.ProgenyId;
                    tItem.ProgenyTime = voc.Date.Value;
                    if (voc.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (voc.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = voc.AccessLevel;
                    tItem.ItemId = voc.WordId.ToString();
                    tItem.CreatedTime = voc.DateAdded;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }

            }

            List<Skill> skillsList = _context.SkillsDb.ToList();
            foreach (Skill skl in skillsList)
            {
                if (skl.SkillFirstObservation != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = skl.ProgenyId;
                    tItem.ProgenyTime = skl.SkillFirstObservation.Value;
                    if (skl.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (skl.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = skl.AccessLevel;
                    tItem.ItemId = skl.SkillId.ToString();
                    tItem.CreatedTime = skl.SkillAddedDate;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Skill;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }
            }

            List<Friend> friendsList = _context.FriendsDb.ToList();
            foreach (Friend frn in friendsList)
            {
                if (frn.FriendSince != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = frn.ProgenyId;
                    tItem.ProgenyTime = frn.FriendSince.Value;
                    if (frn.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (frn.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = frn.AccessLevel;
                    tItem.ItemId = frn.FriendId.ToString();
                    tItem.CreatedTime = frn.FriendAddedDate;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Friend;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }
            }

            List<Measurement> measurementsList = _context.MeasurementsDb.ToList();
            foreach (Measurement mea in measurementsList)
            {
                TimeLineItem tItem = new TimeLineItem();
                tItem.ProgenyId = mea.ProgenyId;
                tItem.ProgenyTime = mea.Date;
                if (mea.ProgenyId == 1)
                {
                    tItem.CreatedBy = userinfo.UserId;
                }
                else
                {
                    if (mea.ProgenyId == 2)
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                    else
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                }
                tItem.AccessLevel = mea.AccessLevel;
                tItem.ItemId = mea.MeasurementId.ToString();
                tItem.CreatedTime = mea.CreatedDate;
                tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Measurement;
                await _context.TimeLineDb.AddAsync(tItem);
                await _context.SaveChangesAsync();
                itemsCount[0] = itemsCount[0] + 1;
            }

            List<Sleep> sleepList = _context.SleepDb.ToList();
            foreach (Sleep slp in sleepList)
            {
                if (slp.SleepStart != null)
                {
                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = slp.ProgenyId;
                    tItem.ProgenyTime = slp.SleepStart;
                    if (slp.ProgenyId == 1)
                    {
                        tItem.CreatedBy = userinfo.UserId;
                    }
                    else
                    {
                        if (slp.ProgenyId == 2)
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                        else
                        {
                            UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                            tItem.CreatedBy = usr.UserId;
                        }
                    }
                    tItem.AccessLevel = slp.AccessLevel;
                    tItem.ItemId = slp.SleepId.ToString();
                    tItem.CreatedTime = slp.CreatedDate;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Sleep;
                    await _context.TimeLineDb.AddAsync(tItem);
                    await _context.SaveChangesAsync();
                    itemsCount[0] = itemsCount[0] + 1;
                }
            }

            List<Note> notesList = _context.NotesDb.ToList();
            foreach (Note nte in notesList)
            {
                TimeLineItem tItem = new TimeLineItem();
                tItem.ProgenyId = nte.ProgenyId;
                tItem.ProgenyTime = nte.CreatedDate;
                if (nte.ProgenyId == 1)
                {
                    tItem.CreatedBy = userinfo.UserId;
                }
                else
                {
                    if (nte.ProgenyId == 2)
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                    else
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                }
                tItem.AccessLevel = nte.AccessLevel;
                tItem.ItemId = nte.NoteId.ToString();
                tItem.CreatedTime = nte.CreatedDate;
                tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Note;
                await _context.TimeLineDb.AddAsync(tItem);
                await _context.SaveChangesAsync();
                itemsCount[0] = itemsCount[0] + 1;
            }

            List<Contact> contactsList = _context.ContactsDb.ToList();
            foreach (Contact cnt in contactsList)
            {
                TimeLineItem tItem = new TimeLineItem();
                tItem.ProgenyId = cnt.ProgenyId;
                if (cnt.ProgenyId == 1)
                {
                    tItem.ProgenyTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2016, 8, 5, 22, 21, 0), TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
                else
                {
                    tItem.ProgenyTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2018, 2, 18, 18, 2, 0), TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }

                if (cnt.ProgenyId == 1)
                {
                    tItem.CreatedBy = userinfo.UserId;
                }
                else
                {
                    if (cnt.ProgenyId == 2)
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                    else
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                }
                tItem.AccessLevel = cnt.AccessLevel;
                tItem.ItemId = cnt.ContactId.ToString();
                tItem.CreatedTime = tItem.ProgenyTime;
                tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Contact;
                await _context.TimeLineDb.AddAsync(tItem);
                await _context.SaveChangesAsync();
                itemsCount[0] = itemsCount[0] + 1;
            }

            List<Vaccination> vaccinationsList = _context.VaccinationsDb.ToList();
            foreach (Vaccination vcn in vaccinationsList)
            {
                TimeLineItem tItem = new TimeLineItem();
                tItem.ProgenyId = vcn.ProgenyId;
                tItem.ProgenyTime = vcn.VaccinationDate;
                if (vcn.ProgenyId == 1)
                {
                    tItem.CreatedBy = userinfo.UserId;
                }
                else
                {
                    if (vcn.ProgenyId == 2)
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("per.mogensen@live.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                    else
                    {
                        UserInfo usr = await _progenyHttpClient.GetUserInfo("tuelpi@hotmail.com");
                        tItem.CreatedBy = usr.UserId;
                    }
                }
                tItem.AccessLevel = vcn.AccessLevel;
                tItem.ItemId = vcn.VaccinationId.ToString();
                tItem.CreatedTime = tItem.ProgenyTime;
                tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vaccination;
                await _context.TimeLineDb.AddAsync(tItem);
                await _context.SaveChangesAsync();
                itemsCount[0] = itemsCount[0] + 1;
            }


            return View(itemsCount);
        }

        public async Task<IActionResult> ShowPicturesNotDownloaded()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            List<Picture> pictures = await _mediaHttpClient.GetAllPictures();
            int model = pictures.Where(p => p.PictureLink.ToLower().StartsWith("http")).ToList().Count;
            return View(model);
        }

        public async Task<IActionResult> DownloadPictures()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            List<Picture> allPictures = await _mediaHttpClient.GetAllPictures();
            List<Picture> notDownloaded = allPictures.Where(p => p.PictureLink.ToLower().StartsWith("http")).ToList();
            ViewBag.Remaining = notDownloaded.Count -500;
            notDownloaded = notDownloaded.Take(500).ToList();
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            string clientUri = _configuration.GetValue<string>("MediaApiServer");
            Queue.QueueBackgroundWorkItem(async token =>
            {
                List<Picture> picturesList = notDownloaded;

                var guid = Guid.NewGuid().ToString();

                int count = 0;
                foreach (Picture pic in picturesList)
                {
                    count++;
                    _logger.LogInformation(
                        $"Queued Background Task Download Pictures {guid} is running. {count}/{picturesList.Count}");
                    await Task.Delay(TimeSpan.FromSeconds(10), token);

                    await UpdatePicture(pic, accessToken, clientUri);

                }

                _logger.LogInformation(
                    $"Queued Background Task Download Pictures {guid} is complete. {count}/{picturesList.Count}");
            });


            return View(notDownloaded);
        }

        public async Task UpdatePicture(Picture pic, string accessToken, string clientUri)
        {

            HttpClient httpClient = new HttpClient();


            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                return;
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string picturesApiPath = "/api/pictures/downloadpicture/" + pic.PictureId;
            await httpClient.GetAsync(picturesApiPath).ConfigureAwait(false);

        }

        public async Task<IActionResult> DownloadFriendsPictures()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            List<Friend> notDownloaded = await _context.FriendsDb.Where(f => f.PictureLink.ToLower().StartsWith("http")).ToListAsync();
            ViewBag.Remaining = notDownloaded.Count;
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            Queue.QueueBackgroundWorkItem(async token =>
            {
                List<Friend> friendsList = await _context.FriendsDb.Where(f => f.PictureLink.ToLower().StartsWith("http")).ToListAsync();
                var guid = Guid.NewGuid().ToString();

                int count = 0;
                foreach (Friend frn in friendsList)
                {
                    count++;
                    _logger.LogInformation(
                        $"Queued Background Task Download Friends Pictures {guid} is running. {count}/{friendsList.Count}");
                    await Task.Delay(TimeSpan.FromSeconds(10), token);

                    await UpdateFriend(frn, accessToken, clientUri);
                    
                }

                _logger.LogInformation(
                    $"Queued Background Task Download Friends Pictures {guid} is complete. {count}/{friendsList.Count}");
            });


            return View(notDownloaded);
        }

        public async Task UpdateFriend(Friend friend, string accessToken, string clientUri)
        {

            HttpClient httpClient = new HttpClient();


            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                return;
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string friendsApiPath = "/api/friends/downloadpicture/" + friend.FriendId;
            await httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);

        }

        public async Task<IActionResult> DownloadContactsPictures()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            List<Contact> notDownloaded = await _context.ContactsDb.Where(c => c.PictureLink.ToLower().StartsWith("http")).ToListAsync();
            ViewBag.Remaining = notDownloaded.Count;
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            Queue.QueueBackgroundWorkItem(async token =>
            {
                List<Contact> contactList = await _context.ContactsDb.Where(c => c.PictureLink.ToLower().StartsWith("http")).ToListAsync();
                var guid = Guid.NewGuid().ToString();
                
                int count = 0;
                foreach (Contact cnt in contactList)
                {
                    count++;
                    _logger.LogInformation(
                        $"Queued Background Task Download Contacts Pictures {guid} is running. {count}/{contactList.Count}");
                    await Task.Delay(TimeSpan.FromSeconds(10), token);

                    await UpdateContact(cnt, accessToken, clientUri);
                }
                
                _logger.LogInformation(
                    $"Queued Background Task Download Contacts Pictures {guid} is complete. {count}/{contactList.Count}");
            });
            
            
            return View(notDownloaded);
        }

        public async Task UpdateContact(Contact contact, string accessToken, string clientUri)
        {
            HttpClient httpClient = new HttpClient();
            
            
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                return;
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string contactsApiPath = "/api/contacts/downloadpicture/" + contact.ContactId;
            await httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            
        }

        public IActionResult SendAdminMessage()
        {
            WebNotification model = new WebNotification();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendAdminMessage(WebNotification notification)
        {
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = User.FindFirst("timezone")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            if (userEmail.ToUpper() == "PER.MOGENSEN@GMAIL.COM")
            {
                if (notification.To == "OnlineUsers")
                {
                    notification.DateTime = DateTime.UtcNow;
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));
                }
                else
                {
                    UserInfo userinfo = new UserInfo();
                    if (notification.To.Contains('@'))
                    {
                        userinfo = await _progenyHttpClient.GetUserInfo(notification.To);
                        notification.To = userinfo.UserId;
                    }
                    else
                    {
                        userinfo = await _progenyHttpClient.GetUserInfoByUserId(notification.To);
                    }

                    notification.DateTime = DateTime.UtcNow;
                    await _context.WebNotificationsDb.AddAsync(notification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.User(userinfo.UserId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));

                    WebNotification webNotification = new WebNotification();
                    webNotification.Title = "Notification Sent" ;
                    webNotification.Message = "To: " + notification.To + "<br/>From: " + notification.From + "<br/><br/>Message: <br/>" + notification.Message;
                    webNotification.From = "KinaUna.com Notification System";
                    webNotification.Type = "Notification";
                    webNotification.DateTime = DateTime.UtcNow;
                    webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await _hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
                }
            }

            notification.Title = "Notification Added";
            return View(notification);
        }

        public IActionResult SendPush()
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            PushNotification notification = new PushNotification();
            notification.UserId = userId;
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> SendPush(PushNotification notification)
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != "PER.MOGENSEN@GMAIL.COM")
            {
                return RedirectToAction("Index", "Home");
            }

            UserInfo userinfo = new UserInfo();
            if (notification.UserId.Contains('@'))
            {
                userinfo = await _progenyHttpClient.GetUserInfo(notification.UserId);
                notification.UserId = userinfo.UserId;
            }

            await _pushMessageSender.SendMessage(notification.UserId, notification.Title, notification.Message,
                notification.Link, "kinaunapush");
            notification.Title = "Message Sent";
            return View(notification);
        }
    }
}
