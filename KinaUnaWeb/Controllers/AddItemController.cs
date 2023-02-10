using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUnaWeb.Models.HomeViewModels;

namespace KinaUnaWeb.Controllers
{
    [Authorize]
    public class AddItemController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IWordsHttpClient  _wordsHttpClient;
        private readonly IVaccinationsHttpClient _vaccinationsHttpClient;
        private readonly ISkillsHttpClient _skillsHttpClient;
        private readonly INotesHttpClient _notesHttpClient;
        private readonly IMeasurementsHttpClient _measurementsHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IFriendsHttpClient _friendsHttpClient;
        private readonly IContactsHttpClient _contactsHttpClient;
        private readonly ICalendarsHttpClient _calendarsHttpClient;
        private readonly ISleepHttpClient _sleepHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly ImageStore _imageStore;
        private readonly WebDbContext _context;
        private readonly string _defaultUser = Constants.DefaultUserEmail;
        private readonly IPushMessageSender _pushMessageSender;

        public AddItemController(IProgenyHttpClient progenyHttpClient, ImageStore imageStore, WebDbContext context, IPushMessageSender pushMessageSender,
            IUserInfosHttpClient userInfosHttpClient, IWordsHttpClient wordsHttpClient, IVaccinationsHttpClient vaccinationsHttpClient, ISkillsHttpClient skillsHttpClient,
            INotesHttpClient notesHttpClient, IMeasurementsHttpClient measurementsHttpClient, ILocationsHttpClient locationsHttpClient, IFriendsHttpClient friendsHttpClient, IContactsHttpClient contactsHttpClient,
            ICalendarsHttpClient calendarsHttpClient, ISleepHttpClient sleepHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
            _context = context; // Todo: replace _context with httpClients
            _pushMessageSender = pushMessageSender;
            _userInfosHttpClient = userInfosHttpClient;
            _wordsHttpClient = wordsHttpClient;
            _vaccinationsHttpClient = vaccinationsHttpClient;
            _skillsHttpClient = skillsHttpClient;
            _notesHttpClient = notesHttpClient;
            _measurementsHttpClient = measurementsHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _friendsHttpClient = friendsHttpClient;
            _contactsHttpClient = contactsHttpClient;
            _calendarsHttpClient = calendarsHttpClient;
            _sleepHttpClient = sleepHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }
        public IActionResult Index()
        {
            AboutViewModel model = new AboutViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            return View(model);
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            NoteViewModel model = new NoteViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName, Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            model.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.PathName = model.CurrentUser.UserId;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(NoteViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<Progeny> progAdminList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
            if (!progAdminList.Any())
            {
                // Todo: Show that no children are available to add note for.
                return RedirectToAction("Index");
            }
            
            Note noteItem = new Note();
            noteItem.Title = model.Title;
            noteItem.ProgenyId = model.ProgenyId;
            noteItem.CreatedDate = TimeZoneInfo.ConvertTimeToUtc(model.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            noteItem.Content = model.Content;
            noteItem.Category = model.Category;
            noteItem.AccessLevel = model.AccessLevel;
            noteItem.Owner = model.CurrentUser.UserId;

            await _notesHttpClient.AddNote(noteItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= noteItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new note was added for " + progeny.NickName;
                        notification.Link = "/Notes?childId=" + model.ProgenyId;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + progeny.Id);
                    }
                }
            }

            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            NoteViewModel model = new NoteViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Note note = await _notesHttpClient.GetNote(itemId);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.NoteId = note.NoteId;
            model.ProgenyId = note.ProgenyId;
            model.AccessLevel = note.AccessLevel;
            model.Category = note.Category;
            model.Title = note.Title;
            model.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.Content = _imageStore.UpdateBlobLinks(note.Content);
            model.Owner = note.Owner;
            if (model.Owner.Contains("@"))
            {
                model.Owner = model.CurrentUser.UserId;
            }
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            model.PathName = model.CurrentUser.UserId;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(NoteViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                Note editedNote = new Note();
                editedNote.NoteId = model.NoteId;
                editedNote.ProgenyId = model.ProgenyId;
                editedNote.AccessLevel = model.AccessLevel;
                editedNote.Category = model.Category;
                editedNote.Title = model.Title;
                editedNote.CreatedDate = TimeZoneInfo.ConvertTimeToUtc(model.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                editedNote.Content = model.Content;
                editedNote.Owner = model.Owner;

                await _notesHttpClient.UpdateNote(editedNote);
            }
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            NoteViewModel model = new NoteViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Note = await _notesHttpClient.GetNote(itemId);
            model.Note.Content = _imageStore.UpdateBlobLinks(model.Note.Content);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Note.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(NoteViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Note note = await _notesHttpClient.GetNote(model.Note.NoteId);
            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _notesHttpClient.DeleteNote(note.NoteId);
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> AddEvent()
        {
            CalendarItemViewModel model = new CalendarItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.AllDay = false;
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                
                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(CalendarItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            CalendarItem eventItem = new CalendarItem();
            eventItem.ProgenyId = model.ProgenyId;
            eventItem.Title = model.Title;
            eventItem.Notes = model.Notes;
            if (model.StartTime != null && model.EndTime != null)
            {
                eventItem.StartTime = TimeZoneInfo.ConvertTimeToUtc(model.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                eventItem.EndTime = TimeZoneInfo.ConvertTimeToUtc(model.EndTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            else
            {
                return View();
            }
            eventItem.Location = model.Location;
            eventItem.Context = model.Context;
            eventItem.AllDay = model.AllDay;
            eventItem.AccessLevel = model.AccessLevel;
            eventItem.Author = model.CurrentUser.UserId;
            eventItem = await _calendarsHttpClient.AddCalendarItem(eventItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }

            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= eventItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        if (eventItem.StartTime != null)
                        {
                            DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            string eventTimeString = "\r\nStart: " + startTime.ToString("dd-MMM-yyyy HH:mm");

                            if (eventItem.EndTime != null)
                            {
                                DateTime endTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value,
                                    TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                                eventTimeString = eventTimeString + "\r\nEnd: " + endTime.ToString("dd-MMM-yyyy HH:mm");
                            }

                            WebNotification notification = new WebNotification();
                            notification.To = uaUserInfo.UserId;
                            notification.From = authorName;
                            notification.Message = eventItem.Title + eventTimeString;
                            notification.DateTime = DateTime.UtcNow;
                            notification.Icon = model.CurrentUser.ProfilePicture;
                            notification.Title = "A new calendar event was added for " + progeny.NickName;
                            notification.Link = "/Calendar/ViewEvent?eventId=" + eventItem.EventId + "&childId=" + progeny.Id;
                            notification.Type = "Notification";
                            await _context.WebNotificationsDb.AddAsync(notification);
                            await _context.SaveChangesAsync();

                            await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                                notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacalendar" + progeny.Id);
                        }
                    }
                }
            }

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int itemId)
        {

            CalendarItemViewModel model = new CalendarItemViewModel();

            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            CalendarItem eventItem = await _calendarsHttpClient.GetCalendarItem(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = eventItem.ProgenyId;
            model.Progeny = prog;
            model.EventId = eventItem.EventId;
            model.AccessLevel = eventItem.AccessLevel;
            model.Author = eventItem.Author;
            model.Title = eventItem.Title;
            model.Notes = eventItem.Notes;
            if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
            {
                model.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                model.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.Location = eventItem.Location;
            model.Context = eventItem.Context;
            model.AllDay = eventItem.AllDay;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(CalendarItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                CalendarItem editedEvent = new CalendarItem();
                editedEvent.ProgenyId = model.ProgenyId;
                editedEvent.EventId = model.EventId;
                editedEvent.AccessLevel = model.AccessLevel;
                editedEvent.Author = model.Author;
                editedEvent.Title = model.Title;
                editedEvent.Notes = model.Notes;
                if (model.StartTime.HasValue && model.EndTime.HasValue)
                {
                    editedEvent.StartTime = TimeZoneInfo.ConvertTimeToUtc(model.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    editedEvent.EndTime = TimeZoneInfo.ConvertTimeToUtc(model.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedEvent.Location = model.Location;
                editedEvent.Context = model.Context;
                editedEvent.AllDay = model.AllDay;
                await _calendarsHttpClient.UpdateCalendarItem(editedEvent);
            }
            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int itemId)
        {
            CalendarItemViewModel model = new CalendarItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            model.CalendarItem = await _calendarsHttpClient.GetCalendarItem(itemId);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(CalendarItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.CalendarItem = await _calendarsHttpClient.GetCalendarItem(model.CalendarItem.EventId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.CalendarItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _calendarsHttpClient.DeleteCalendarItem(model.CalendarItem.EventId);

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> AddVocabulary()
        {
            VocabularyItemViewModel model = new VocabularyItemViewModel();

            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            model.Progeny = accessList[0];

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVocabulary(VocabularyItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }


            VocabularyItem vocabItem = new VocabularyItem();
            vocabItem.Word = model.Word;
            vocabItem.ProgenyId = model.ProgenyId;
            vocabItem.DateAdded = DateTime.UtcNow;
            if (model.Date == null)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            vocabItem.Date = model.Date;
            vocabItem.Description = model.Description;
            vocabItem.Language = model.Language;
            vocabItem.SoundsLike = model.SoundsLike;
            vocabItem.AccessLevel = model.AccessLevel;
            vocabItem.Author = model.CurrentUser.UserId;

            await _wordsHttpClient.AddWord(vocabItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= vocabItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string vocabTimeString;
                        DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabItem.Date.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy");
                        
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Word: " + vocabItem.Word + "\r\nLanguage: " + vocabItem.Language + vocabTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new word was added for " + progeny.NickName;
                        notification.Link = "/Vocabulary?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavocabulary" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> EditVocabulary(int itemId)
        {

            VocabularyItemViewModel model = new VocabularyItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            VocabularyItem vocab = await _wordsHttpClient.GetWord(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vocab.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.ProgenyId = vocab.ProgenyId;
            model.AccessLevel = vocab.AccessLevel;
            model.Author = vocab.Author;
            model.Date = vocab.Date;
            if (model.Date == null)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.DateAdded = vocab.DateAdded;
            model.Description = vocab.Description;
            model.Language = vocab.Language;
            model.SoundsLike = vocab.SoundsLike;
            model.Word = vocab.Word;
            model.WordId = vocab.WordId;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVocabulary(VocabularyItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                VocabularyItem editedWord = new VocabularyItem();
                editedWord.Author = model.Author;
                editedWord.ProgenyId = model.ProgenyId;
                editedWord.AccessLevel = model.AccessLevel;
                editedWord.Date = model.Date;
                if (editedWord.Date == null)
                {
                    editedWord.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedWord.DateAdded = model.DateAdded;
                editedWord.Description = model.Description;
                editedWord.Language = model.Language;
                editedWord.SoundsLike = model.SoundsLike;
                editedWord.Word = model.Word;
                editedWord.WordId = model.WordId;

                await _wordsHttpClient.UpdateWord(editedWord);

            }
            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVocabulary(int itemId)
        {
            VocabularyItemViewModel model = new VocabularyItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.VocabularyItem = await _wordsHttpClient.GetWord(itemId);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(model.VocabularyItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVocabulary(VocabularyItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            VocabularyItem vocab = await _wordsHttpClient.GetWord(model.VocabularyItem.WordId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vocab.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _wordsHttpClient.DeleteWord(vocab.WordId);
            return RedirectToAction("Index", "Vocabulary");
        }


        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            SkillViewModel model = new SkillViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            

            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(SkillViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Skill skillItem = new Skill();
            skillItem.ProgenyId = model.ProgenyId;
            skillItem.Category = model.Category;
            skillItem.Description = model.Description;
            skillItem.Name = model.Name;
            skillItem.SkillAddedDate = DateTime.UtcNow;
            if (model.SkillFirstObservation == null)
            {
                model.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            skillItem.SkillFirstObservation = model.SkillFirstObservation;
            skillItem.AccessLevel = model.AccessLevel;
            skillItem.Author = model.CurrentUser.UserId;

            await _skillsHttpClient.AddSkill(skillItem);
            
            string authorName = "";
            if (!String.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!String.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!String.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= skillItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy");
                        
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new skill was added for " + progeny.NickName;
                        notification.Link = "/Skills?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaskill" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> EditSkill(int itemId)
        {
            SkillViewModel model = new SkillViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Skill skill = await _skillsHttpClient.GetSkill(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(skill.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = skill.ProgenyId;
            model.AccessLevel = skill.AccessLevel;
            model.Author = skill.Author;
            model.Category = skill.Category;
            model.Description = skill.Description;
            model.Name = skill.Name;
            model.SkillAddedDate = skill.SkillAddedDate;
            if (skill.SkillFirstObservation == null)
            {
                skill.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.SkillFirstObservation = skill.SkillFirstObservation;
            model.SkillId = skill.SkillId;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(SkillViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Skill editedSkill = new Skill();

                editedSkill.ProgenyId = model.ProgenyId;
                editedSkill.AccessLevel = model.AccessLevel;
                editedSkill.Author = model.Author;
                editedSkill.Category = model.Category;
                editedSkill.Description = model.Description;
                editedSkill.Name = model.Name;
                editedSkill.SkillAddedDate = model.SkillAddedDate;
                if (model.SkillFirstObservation == null)
                {
                    model.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedSkill.SkillFirstObservation = model.SkillFirstObservation;
                editedSkill.SkillId = model.SkillId;

                await _skillsHttpClient.UpdateSkill(editedSkill);
            }

            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSkill(int itemId)
        {
            SkillViewModel model = new SkillViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Skill = await _skillsHttpClient.GetSkill(itemId);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Skill.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(SkillViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Skill skill = await _skillsHttpClient.GetSkill(model.Skill.SkillId);
            Progeny prog = await _progenyHttpClient.GetProgeny(skill.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _skillsHttpClient.DeleteSkill(skill.SkillId);

            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> AddFriend()
        {
            FriendViewModel model = new FriendViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            List<string> tagsList = new List<string>();
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
           
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);

                        List<Friend> friendsList1 = await _friendsHttpClient.GetFriendsList(prog.Id, 0);
                        foreach (Friend frn in friendsList1)
                        {
                            if (!string.IsNullOrEmpty(frn.Tags))
                            {
                                List<string> fvmTags = frn.Tags.Split(',').ToList();
                                foreach (string tagstring in fvmTags)
                                {
                                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                    {
                                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
            }
            tagItems = tagItems + "]";
            ViewBag.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
                model.FriendTypeListEn = model.FriendTypeListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
                model.FriendTypeListEn = model.FriendTypeListDa;
            }

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddFriend(FriendViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            Friend friendItem = new Friend();
            friendItem.ProgenyId = model.ProgenyId;
            friendItem.Description = model.Description;
            friendItem.FriendAddedDate = DateTime.UtcNow;
            if (model.FriendSince == null)
            {
                model.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            friendItem.FriendSince = model.FriendSince;
            friendItem.Name = model.Name;
            friendItem.AccessLevel = model.AccessLevel;
            friendItem.Type = model.Type;
            friendItem.Context = model.Context;
            friendItem.Notes = model.Notes;
            friendItem.Author = model.CurrentUser.UserId;
            if (!string.IsNullOrEmpty(model.Tags))
            {
                friendItem.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (model.File != null)
            {
                using (Stream stream = model.File.OpenReadStream())
                {
                    friendItem.PictureLink = await _imageStore.SaveImage(stream, "friends");

                }
            }
            else
            {
                friendItem.PictureLink = Constants.ProfilePictureUrl;
            }

            await _friendsHttpClient.AddFriend(friendItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= friendItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new friend was added for " + progeny.NickName;
                        notification.Link = "/Friends?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunafriend" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> EditFriend(int itemId)
        {
            FriendViewModel model = new FriendViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Friend friend = await _friendsHttpClient.GetFriend(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = friend.ProgenyId;
            model.AccessLevel = friend.AccessLevel;
            model.Author = friend.Author;
            if (friend.FriendSince == null)
            {
                friend.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.FriendAddedDate = friend.FriendAddedDate;
            model.Description = friend.Description;
            model.Name = friend.Name;
            model.FriendId = friend.FriendId;
            model.FriendSince = friend.FriendSince;
            model.PictureLink = friend.PictureLink;
            if (!friend.PictureLink.ToLower().StartsWith("http"))
            {
                model.PictureLink = _imageStore.UriFor(friend.PictureLink, "friends");
            }
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            model.Type = friend.Type;
            model.FriendTypeListEn[model.Type].Selected = true;
            model.FriendTypeListDa[model.Type].Selected = true;
            model.FriendTypeListDe[model.Type].Selected = true;
            model.Context = friend.Context;
            model.Notes = friend.Notes;
            model.Tags = friend.Tags;

            List<string> tagsList = new List<string>();
            List<Friend> friendsList1 = await _friendsHttpClient.GetFriendsList(model.ProgenyId, 0);
            foreach (Friend frn in friendsList1)
            {
                if (!string.IsNullOrEmpty(frn.Tags))
                {
                    List<string> fvmTags = frn.Tags.Split(',').ToList();
                    foreach (string tagstring in fvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems = tagItems + "]";
            }
            
            ViewBag.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
                model.FriendTypeListEn = model.FriendTypeListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
                model.FriendTypeListEn = model.FriendTypeListDa;
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditFriend(FriendViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Friend editedFriend = await _friendsHttpClient.GetFriend(model.FriendId);
                editedFriend.AccessLevel = model.AccessLevel;
                editedFriend.Author = model.Author;
                editedFriend.Description = model.Description;
                editedFriend.Name = model.Name;
                editedFriend.FriendId = model.FriendId;
                if (model.FriendSince == null)
                {
                    model.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedFriend.FriendSince = model.FriendSince;
                editedFriend.Type = model.Type;
                editedFriend.Context = model.Context;
                editedFriend.Notes = model.Notes;
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    editedFriend.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
                }
                if (model.File != null && model.File.Name != string.Empty)
                {
                    string oldPictureLink = model.PictureLink;
                    model.FileName = model.File.FileName;
                    using (Stream stream = model.File.OpenReadStream())
                    {
                        editedFriend.PictureLink = await _imageStore.SaveImage(stream, "friends");
                    }

                    if (!oldPictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(oldPictureLink, "friends");
                    }
                }

                await _friendsHttpClient.UpdateFriend(editedFriend);
            }
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFriend(int itemId)
        {
            FriendViewModel model = new FriendViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Friend = await _friendsHttpClient.GetFriend(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Friend.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFriend(FriendViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Friend friend = await _friendsHttpClient.GetFriend(model.Friend.FriendId);
            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _friendsHttpClient.DeleteFriend(friend.FriendId);
            
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeasurement(MeasurementViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            Measurement measurementItem = new Measurement();
            measurementItem.ProgenyId = model.ProgenyId;
            measurementItem.CreatedDate = DateTime.UtcNow;
            measurementItem.Date = model.Date;
            measurementItem.Height = model.Height;
            measurementItem.Weight = model.Weight;
            measurementItem.Circumference = model.Circumference;
            measurementItem.HairColor = model.HairColor;
            measurementItem.EyeColor = model.EyeColor;
            measurementItem.AccessLevel = model.AccessLevel;
            measurementItem.Author = model.CurrentUser.UserId;

            await _measurementsHttpClient.AddMeasurement(measurementItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= measurementItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new measurement was added for " + progeny.NickName;
                        notification.Link = "/Measurements?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> EditMeasurement(int itemId)
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Measurement measurement = await _measurementsHttpClient.GetMeasurement(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.ProgenyId = measurement.ProgenyId;
            model.MeasurementId = measurement.MeasurementId;
            model.AccessLevel = measurement.AccessLevel;
            model.Author = measurement.Author;
            model.CreatedDate = measurement.CreatedDate;
            model.Date = measurement.Date;
            model.Height = measurement.Height;
            model.Weight = measurement.Weight;
            model.Circumference = measurement.Circumference;
            model.HairColor = measurement.HairColor;
            model.EyeColor = measurement.EyeColor;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeasurement(MeasurementViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(userinfo.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Measurement editedMeasurement = new Measurement();
            editedMeasurement.ProgenyId = model.ProgenyId;
            editedMeasurement.MeasurementId = model.MeasurementId;
            editedMeasurement.AccessLevel = model.AccessLevel;
            editedMeasurement.Author = model.Author;
            editedMeasurement.CreatedDate = model.CreatedDate;
            editedMeasurement.Date = model.Date;
            editedMeasurement.Height = model.Height;
            editedMeasurement.Weight = model.Weight;
            editedMeasurement.Circumference = model.Circumference;
            editedMeasurement.HairColor = model.HairColor;
            editedMeasurement.EyeColor = model.EyeColor;

            await _measurementsHttpClient.UpdateMeasurement(editedMeasurement);

            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMeasurement(int itemId)
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Measurement = await _measurementsHttpClient.GetMeasurement(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Measurement.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(MeasurementViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Measurement measurement = await _measurementsHttpClient.GetMeasurement(model.Measurement.MeasurementId);
            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _measurementsHttpClient.DeleteMeasurement(measurement.MeasurementId);
            
            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> AddContact()
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<string> tagsList = new List<string>();

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            foreach (Progeny item in accessList)
            {
                List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(item.Id, 0);
                foreach (Contact cont in contactsList1)
                {
                    if (!string.IsNullOrEmpty(cont.Tags))
                    {
                        List<string> cvmTags = cont.Tags.Split(',').ToList();
                        foreach (string tagstring in cvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            ViewBag.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Contact contactItem = new Contact();
            contactItem.FirstName = model.FirstName;
            contactItem.MiddleName = model.MiddleName;
            contactItem.LastName = model.LastName;
            contactItem.DisplayName = model.DisplayName;
            contactItem.Email1 = model.Email1;
            contactItem.Email2 = model.Email2;
            contactItem.PhoneNumber = model.PhoneNumber;
            contactItem.MobileNumber = model.MobileNumber;
            contactItem.Notes = model.Notes;
            contactItem.Website = model.Website;
            contactItem.Active = true;
            contactItem.Context = model.Context;
            contactItem.AccessLevel = model.AccessLevel;
            contactItem.Author = model.CurrentUser.UserId;
            contactItem.ProgenyId = model.ProgenyId;
            contactItem.DateAdded = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(model.Tags))
            {
                contactItem.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            if (model.File != null)
            {
                model.FileName = model.File.FileName;
                using (Stream stream = model.File.OpenReadStream())
                {
                    model.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                }
            }
            else
            {
                contactItem.PictureLink = Constants.ProfilePictureUrl;
            }

            if (model.AddressLine1 + model.AddressLine2 + model.City + model.Country + model.PostalCode + model.State !=
                "")
            {
                Address address = new Address();
                address.AddressLine1 = model.AddressLine1;
                address.AddressLine2 = model.AddressLine2;
                address.City = model.City;
                address.PostalCode = model.PostalCode;
                address.State = model.State;
                address.Country = model.Country;
                contactItem.Address = address;
            }

            await _contactsHttpClient.AddContact(contactItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= contactItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new contact was added for " + progeny.NickName;
                        notification.Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacontact" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> EditContact(int itemId)
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Contact contact = await _contactsHttpClient.GetContact(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ContactId = contact.ContactId;
            model.Active = contact.Active;
            model.ProgenyId = contact.ProgenyId;
            model.AccessLevel = contact.AccessLevel;
            model.Author = contact.Author;
            model.FirstName = contact.FirstName;
            model.MiddleName = contact.MiddleName;
            model.LastName = contact.LastName;
            model.DisplayName = contact.DisplayName;
            if (contact.AddressIdNumber != null)
            {
                model.AddressIdNumber = contact.AddressIdNumber;
                model.Address = await _locationsHttpClient.GetAddress(model.AddressIdNumber.Value);
                model.AddressLine1 = model.Address.AddressLine1;
                model.AddressLine2 = model.Address.AddressLine2;
                model.City = model.Address.City;
                model.PostalCode = model.Address.PostalCode;
                model.State = model.Address.State;
                model.Country = model.Address.Country;
            }
            model.Email1 = contact.Email1;
            model.Email2 = contact.Email2;
            model.PhoneNumber = contact.PhoneNumber;
            model.MobileNumber = contact.MobileNumber;
            model.Website = contact.Website;
            model.Notes = contact.Notes;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            model.Context = contact.Context;
            model.Notes = contact.Notes;
            model.PictureLink = contact.PictureLink;
            if (!contact.PictureLink.ToLower().StartsWith("http"))
            {
                model.PictureLink = _imageStore.UriFor(contact.PictureLink, "contacts");
            }
            DateTime tempTime = contact.DateAdded ?? DateTime.UtcNow;
            model.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(tempTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.Tags = contact.Tags;

            List<string> tagsList = new List<string>();
            List<Contact> contactsList1 = await _contactsHttpClient.GetContactsList(model.ProgenyId, 0);
            foreach (Contact cont in contactsList1)
            {
                if (!string.IsNullOrEmpty(cont.Tags))
                {
                    List<string> cvmTags = cont.Tags.Split(',').ToList();
                    foreach (string tagstring in cvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems = tagItems + "]";
            }

            ViewBag.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditContact(ContactViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Contact editedContact = await _contactsHttpClient.GetContact(model.ContactId);
                editedContact.ContactId = model.ContactId;
                editedContact.ProgenyId = model.ProgenyId;
                editedContact.Active = model.Active;
                editedContact.AccessLevel = model.AccessLevel;
                editedContact.Author = model.Author;
                editedContact.FirstName = model.FirstName;
                editedContact.MiddleName = model.MiddleName;
                editedContact.LastName = model.LastName;
                editedContact.DisplayName = model.DisplayName;
                editedContact.DateAdded = model.DateAdded;
                editedContact.AddressIdNumber = model.AddressIdNumber;
                if (model.AddressLine1 + model.AddressLine2 + model.City + model.Country + model.PostalCode +
                    model.State != "")
                {
                    Address address = new Address();
                    address.AddressLine1 = model.AddressLine1;
                    address.AddressLine2 = model.AddressLine2;
                    address.City = model.City;
                    address.PostalCode = model.PostalCode;
                    address.State = model.State;
                    address.Country = model.Country;
                    editedContact.Address = address;
                }
                
                editedContact.Email1 = model.Email1;
                editedContact.Email2 = model.Email2;
                editedContact.PhoneNumber = model.PhoneNumber;
                editedContact.MobileNumber = model.MobileNumber;
                editedContact.Notes = model.Notes;
                editedContact.Context = model.Context;
                editedContact.Website = model.Website;
                if (model.File != null && model.File.Name != string.Empty)
                {
                    string oldPictureLink = model.PictureLink;
                    model.FileName = model.File.FileName;
                    using (Stream stream = model.File.OpenReadStream())
                    {
                        editedContact.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                    }

                    if (!oldPictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(oldPictureLink, "contacts");
                    }
                }
                
                if (editedContact.DateAdded == null)
                {
                    editedContact.DateAdded = DateTime.UtcNow;
                }
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    editedContact.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
                }

                await _contactsHttpClient.UpdateContact(editedContact);
            }

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteContact(int itemId)
        {
            ContactViewModel model = new ContactViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Contact = await _contactsHttpClient.GetContact(itemId);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Contact.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(ContactViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Contact contact = await _contactsHttpClient.GetContact(model.Contact.ContactId);
            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            await _contactsHttpClient.DeleteContact(contact.ContactId);

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> AddVaccination()
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVaccination(VaccinationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Vaccination vacItem = new Vaccination();
            vacItem.VaccinationName = model.VaccinationName;
            vacItem.ProgenyId = model.ProgenyId;
            vacItem.VaccinationDescription = model.VaccinationDescription;
            vacItem.VaccinationDate = model.VaccinationDate;
            vacItem.Notes = model.Notes;
            vacItem.AccessLevel = model.AccessLevel;
            vacItem.Author = model.CurrentUser.UserId;

            await _vaccinationsHttpClient.AddVaccination(vacItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= vacItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + vacItem.VaccinationName + "\r\nContext: " + vacItem.VaccinationDate.ToString("dd-MMM-yyyy");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new vaccination was added for " + progeny.NickName;
                        notification.Link = "/Vaccinations?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> EditVaccination(int itemId)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vaccination.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.VaccinationId = vaccination.VaccinationId;
            model.ProgenyId = vaccination.ProgenyId;
            model.AccessLevel = vaccination.AccessLevel;
            model.Author = vaccination.Author;
            model.VaccinationName = vaccination.VaccinationName;
            model.VaccinationDate = vaccination.VaccinationDate;
            model.VaccinationDescription = vaccination.VaccinationDescription;
            model.Notes = vaccination.Notes;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVaccination(VaccinationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Vaccination editedVaccination = new Vaccination();
                editedVaccination.VaccinationId = model.VaccinationId;
                editedVaccination.ProgenyId = model.ProgenyId;
                editedVaccination.AccessLevel = model.AccessLevel;
                editedVaccination.Author = model.Author;
                editedVaccination.VaccinationName = model.VaccinationName;
                editedVaccination.VaccinationDate = model.VaccinationDate;
                editedVaccination.VaccinationDescription = model.VaccinationDescription;
                editedVaccination.Notes = model.Notes;

                await _vaccinationsHttpClient.UpdateVaccination(editedVaccination);
            }
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVaccination(int itemId)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            model.Vaccination = await _vaccinationsHttpClient.GetVaccination(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Vaccination.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVaccination(VaccinationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Vaccination vaccination = await _vaccinationsHttpClient.GetVaccination(model.Vaccination.VaccinationId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vaccination.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _vaccinationsHttpClient.DeleteVaccination(vaccination.VaccinationId);
            
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> AddSleep()
        {
            SleepViewModel model = new SleepViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSleep(SleepViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Sleep sleepItem = new Sleep();
            sleepItem.ProgenyId = model.ProgenyId;
            sleepItem.Progeny = prog;
            sleepItem.CreatedDate = DateTime.UtcNow;
            if (model.SleepStart.HasValue && model.SleepEnd.HasValue)
            {
                sleepItem.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepStart.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                sleepItem.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepEnd.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            sleepItem.SleepRating = model.SleepRating;
            if (sleepItem.SleepRating == 0)
            {
                sleepItem.SleepRating = 3;
            }
            sleepItem.SleepNotes = model.SleepNotes;
            sleepItem.AccessLevel = model.AccessLevel;
            sleepItem.Author = model.CurrentUser.UserId;

            sleepItem = await _sleepHttpClient.AddSleep(sleepItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(sleepItem.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= sleepItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        DateTime sleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        DateTime sleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " +sleepEnd.ToString("dd-MMM-yyyy HH:mm");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "Sleep Added for " + prog.NickName;
                        notification.Link = "/Sleep?childId=" + prog.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title, notification.Message,
                            Constants.WebAppUrl + notification.Link, "kinaunasleep" + prog.Id);
                    }
                }
            }
            
            // Todo: send notification to others.
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> EditSleep(int itemId)
        {
            SleepViewModel model = new SleepViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Sleep sleep = await _sleepHttpClient.GetSleepItem(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = sleep.ProgenyId;
            model.Progeny = prog;
            model.SleepId = sleep.SleepId;
            model.AccessLevel = sleep.AccessLevel;
            model.Author = sleep.Author;
            model.CreatedDate = sleep.CreatedDate;
            model.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.SleepRating = sleep.SleepRating;
            if (model.SleepRating == 0)
            {
                model.SleepRating = 3;
            }
            model.SleepNotes = sleep.SleepNotes;
            model.Progeny = prog;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            ViewBag.RatingList = new List<SelectListItem>();
            SelectListItem selItem1 = new SelectListItem();
            selItem1.Text = "1";
            selItem1.Value = "1";
            SelectListItem selItem2 = new SelectListItem();
            selItem2.Text = "2";
            selItem2.Value = "2";
            SelectListItem selItem3 = new SelectListItem();
            selItem3.Text = "3";
            selItem3.Value = "3";
            SelectListItem selItem4 = new SelectListItem();
            selItem4.Text = "4";
            selItem4.Value = "4";
            SelectListItem selItem5 = new SelectListItem();
            selItem5.Text = "5";
            selItem5.Value = "5";
            ViewBag.RatingList.Add(selItem1);
            ViewBag.RatingList.Add(selItem2);
            ViewBag.RatingList.Add(selItem3);
            ViewBag.RatingList.Add(selItem4);
            ViewBag.RatingList.Add(selItem5);
            ViewBag.RatingList[model.SleepRating - 1].Selected = true;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSleep(SleepViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Sleep editedSleep = new Sleep();
                editedSleep.ProgenyId = model.ProgenyId;
                editedSleep.Progeny = prog;
                editedSleep.SleepId = model.SleepId;
                editedSleep.AccessLevel = model.AccessLevel;
                editedSleep.Author = model.Author;
                editedSleep.CreatedDate = model.CreatedDate;
                if (model.SleepStart.HasValue && model.SleepEnd.HasValue)
                {
                    editedSleep.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepStart.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    editedSleep.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepEnd.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedSleep.SleepRating = model.SleepRating;
                if (editedSleep.SleepRating == 0)
                {
                    editedSleep.SleepRating = 3;
                }
                editedSleep.SleepNotes = model.SleepNotes;
                await _sleepHttpClient.UpdateSleep(editedSleep);
            }
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSleep(int itemId)
        {
            SleepViewModel model = new SleepViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            model.Sleep = await _sleepHttpClient.GetSleepItem(itemId);
 
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Sleep.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSleep(SleepViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Sleep sleep = await _sleepHttpClient.GetSleepItem(model.Sleep.SleepId);
            
            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _sleepHttpClient.DeleteSleepItem(sleep.SleepId);

            return RedirectToAction("Index", "Sleep");
        }

        public async Task<IActionResult> AddLocation()
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<string> tagsList = new List<string>();
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    model.Progeny = accessList[0];
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                            model.Progeny = prog;
                        }

                        model.ProgenyList.Add(selItem);

                        List<Location> locList1 = _context.LocationsDb.Where(l => l.ProgenyId == prog.Id).ToList();
                        foreach (Location loc in locList1)
                        {
                            if (!string.IsNullOrEmpty(loc.Tags))
                            {
                                List<string> locTags = loc.Tags.Split(',').ToList();
                                foreach (string tagstring in locTags)
                                {
                                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                    {
                                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            model.TagsList = tagItems;
            model.Latitude = 30.94625288456589;
            model.Longitude = -54.10861860580418;
            model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.Progeny = prog;
            Location locItem = new Location();
            locItem.Latitude = model.Latitude;
            locItem.Longitude = model.Longitude;
            locItem.Name = model.Name;
            locItem.HouseNumber = model.HouseNumber;
            locItem.StreetName = model.StreetName;
            locItem.District = model.District;
            locItem.City = model.City;
            locItem.PostalCode = model.PostalCode;
            locItem.County = model.County;
            locItem.State = model.State;
            locItem.Country = model.Country;
            locItem.Notes = model.Notes;
            if (model.Date.HasValue)
            {
                locItem.Date = TimeZoneInfo.ConvertTimeToUtc(model.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
            }
            if (!string.IsNullOrEmpty(model.Tags))
            {
                locItem.Tags = model.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            locItem.ProgenyId = model.ProgenyId;
            locItem.DateAdded = DateTime.UtcNow;
            locItem.Author = model.CurrentUser.UserId;
            locItem.AccessLevel = model.AccessLevel;

            await _locationsHttpClient.AddLocation(locItem);
            
            string authorName = "";
            if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
            {
                authorName = model.CurrentUser.FirstName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
            {
                authorName = authorName + " " + model.CurrentUser.MiddleName;
            }
            if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
            {
                authorName = authorName + " " + model.CurrentUser.LastName;
            }

            authorName = authorName.Trim();
            if (string.IsNullOrEmpty(authorName))
            {
                authorName = model.CurrentUser.UserName;
            }
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= locItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        DateTime tempDate = DateTime.UtcNow;
                        if (locItem.Date.HasValue)
                        {
                            tempDate = TimeZoneInfo.ConvertTimeFromUtc(locItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
                        }

                        string dateString = tempDate.ToString("dd-MMM-yyyy");
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + locItem.Name + "\r\nDate: " + dateString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new location was added for " + progeny.NickName;
                        notification.Link = "/Locations?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunalocation" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> EditLocation(int itemId)
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<string> tagsList = new List<string>();
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }
            
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny chld in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == chld.Id).NickName,
                            Value = chld.Id.ToString()
                        };
                        if (chld.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);

                        List<Location> locList1 = await _locationsHttpClient.GetLocationsList(chld.Id, 0);
                        foreach (Location loc in locList1)
                        {
                            if (!string.IsNullOrEmpty(loc.Tags))
                            {
                                List<string> locTags = loc.Tags.Split(',').ToList();
                                foreach (string tagstring in locTags)
                                {
                                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                    {
                                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Location locItem = await _locationsHttpClient.GetLocation(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            model.Progeny = prog;
            model.LocationId = locItem.LocationId;
            model.Latitude = locItem.Latitude;
            model.Longitude = locItem.Longitude;
            model.Name = locItem.Name;
            model.HouseNumber = locItem.HouseNumber;
            model.StreetName = locItem.StreetName;
            model.District = locItem.District;
            model.City = locItem.City;
            model.PostalCode = locItem.PostalCode;
            model.County = locItem.County;
            model.State = locItem.State;
            model.Country = locItem.Country;
            model.Notes = locItem.Notes;
            if (locItem.Date.HasValue)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(locItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            model.Tags = locItem.Tags;
            model.ProgenyId = locItem.ProgenyId;
            model.DateAdded = locItem.DateAdded;
            model.Author = locItem.Author;
            model.AccessLevel = locItem.AccessLevel;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            model.TagsList = tagItems;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            Location locItem = await _locationsHttpClient.GetLocation(model.LocationId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            model.Progeny = prog;
            locItem.Latitude = model.Latitude;
            locItem.Longitude = model.Longitude;
            locItem.Name = model.Name;
            locItem.HouseNumber = model.HouseNumber;
            locItem.StreetName = model.StreetName;
            locItem.District = model.District;
            locItem.City = model.City;
            locItem.PostalCode = model.PostalCode;
            locItem.County = model.County;
            locItem.State = model.State;
            locItem.Country = model.Country;
            locItem.Notes = model.Notes;
            if (model.Date.HasValue)
            {
                locItem.Date = TimeZoneInfo.ConvertTimeToUtc(model.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            if (!String.IsNullOrEmpty(model.Tags))
            {
                locItem.Tags = model.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            locItem.AccessLevel = model.AccessLevel;

            await _locationsHttpClient.UpdateLocation(locItem);
            
            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> DeleteLocation(int itemId)
        {
            LocationViewModel model = new LocationViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Location = await _locationsHttpClient.GetLocation(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Location.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(LocationViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Location locItem = await _locationsHttpClient.GetLocation(model.Location.LocationId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _locationsHttpClient.DeleteLocation(locItem.LocationId);
            return RedirectToAction("Index", "Locations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFile(FileItem model)
        {
            
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult> SaveRtfFile(IList<IFormFile> UploadFiles)
        {
            try
            {
                foreach (IFormFile file in UploadFiles)
                {
                    if (UploadFiles.Any())
                    {
                        string filename;
                        await using (Stream stream = file.OpenReadStream())
                        {
                            filename = await _imageStore.SaveImage(stream, BlobContainers.Notes);
                        }

                        string resultName = _imageStore.UriFor(filename, BlobContainers.Notes);
                        Response.Clear();
                        Response.ContentType = "application/json; charset=utf-8";
                        Response.Headers.Add("name", resultName);
                        Response.StatusCode = 204;
                    }
                }
            }
            catch (Exception)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = 204;
            }
            return Content("");
        }
    }
}