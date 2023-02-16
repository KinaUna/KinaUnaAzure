﻿using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class TimelineController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly ITimelineHttpClient _timelineHttpClient;
        private readonly IWordsHttpClient _wordsHttpClient;
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
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        
        public TimelineController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, ITimelineHttpClient timelineHttpClient, 
            IWordsHttpClient wordsHttpClient, IVaccinationsHttpClient vaccinationsHttpClient, ISkillsHttpClient skillsHttpClient, INotesHttpClient notesHttpClient, IMeasurementsHttpClient measurementsHttpClient,
            ILocationsHttpClient locationsHttpClient, IFriendsHttpClient friendsHttpClient, IContactsHttpClient contactsHttpClient, ICalendarsHttpClient calendarsHttpClient, ISleepHttpClient sleepHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _timelineHttpClient = timelineHttpClient;
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

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int sortBy = 1, int items = 0)
        {
            TimeLineViewModel model = new TimeLineViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);


            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            model.SortBy = sortBy;
            
            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                userAccessLevel = (int)AccessLevel.Private;
            }

            
            model.TimeLineItems = new List<TimeLineItem>();
            model.TimeLineItems = await _timelineHttpClient.GetTimeline(childId, userAccessLevel);
            if (sortBy == 1)
            {
                model.TimeLineItems = model.TimeLineItems.OrderByDescending(t => t.ProgenyTime).ToList();
            }
            else
            {
                model.TimeLineItems = model.TimeLineItems.OrderBy(t => t.ProgenyTime).ToList();
            }

            model.Progeny = progeny;
            model.Items = items;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLinePhotoPartial(PictureViewModel model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVideoPartial(VideoViewModel model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineEventPartial(CalendarItem model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVocabularyPartial(VocabularyItem model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSkillPartial(Skill model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineFriendPartial(Friend model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineMeasurementPartial(Measurement model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineSleepPartial(Sleep model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineNotePartial(Note model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineContactPartial(Contact model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineVaccinationPartial(Vaccination model)
        {

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TimeLineLocationPartial(Location model)
        {

            return View(model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> GetTimeLineItem(TimeLineItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            string id = model.ItemId.ToString();
            int type = model.TypeId;
            int itemId;
            bool idParse = int.TryParse(id, out itemId);
            if (type == (int)KinaUnaTypes.TimeLineType.Photo)
            {
                if (idParse)
                {
                    PictureViewModel picture = await _mediaHttpClient.GetPictureViewModel(itemId, 0, 1, model.CurrentUser.Timezone);
                    if (picture != null && picture.PictureId > 0)
                    {
                        picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                        picture.CommentsCount = picture.CommentsList.Count;
                        return PartialView("TimeLinePhotoPartial", picture);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Video)
            {
                if (idParse)
                {
                    VideoViewModel video = await _mediaHttpClient.GetVideoViewModel(itemId, 0, 1, model.CurrentUser.Timezone);
                    if (video != null && video.VideoId > 0)
                    {
                        video.CommentsCount = video.CommentsList.Count;
                        return PartialView("TimeLineVideoPartial", video);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Calendar)
            {
                if (idParse)
                {
                    CalendarItem evt = await _calendarsHttpClient.GetCalendarItem(itemId);
                    if (evt != null && evt.EventId > 0 && evt.StartTime.HasValue && evt.EndTime.HasValue)
                    {
                        evt.StartTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        evt.EndTime = TimeZoneInfo.ConvertTimeFromUtc(evt.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        return PartialView("TimeLineEventPartial", evt);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Vocabulary)
            {
                if (idParse)
                {
                    VocabularyItem voc = await _wordsHttpClient.GetWord(itemId);
                    if (voc != null && voc.WordId > 0)
                    {
                        if (voc.Date != null)
                        {
                            voc.Date = TimeZoneInfo.ConvertTimeFromUtc(voc.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

                        }
                        return PartialView("TimeLineVocabularyPartial", voc);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Skill)
            {
                if (idParse)
                {
                    Skill skl = await _skillsHttpClient.GetSkill(itemId);
                    if (skl != null && skl.SkillId > 0)
                    {
                        return PartialView("TimeLineSkillPartial", skl);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Friend)
            {
                if (idParse)
                {
                    Friend frn = await _friendsHttpClient.GetFriend(itemId);
                    if (frn != null && frn.FriendId > 0)
                    {
                        frn.PictureLink = _imageStore.UriFor(frn.PictureLink, "friends");
                        return PartialView("TimeLineFriendPartial", frn);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Measurement)
            {
                if (idParse)
                {
                    Measurement mes = await _measurementsHttpClient.GetMeasurement(itemId);
                    if (mes != null && mes.MeasurementId > 0)
                    {
                        return PartialView("TimeLineMeasurementPartial", mes);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Sleep)
            {
                if (idParse)
                {
                    Sleep slp = await _sleepHttpClient.GetSleepItem(itemId);
                    if (slp != null && slp.SleepId > 0)
                    {
                        slp.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        slp.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        DateTimeOffset sOffset = new DateTimeOffset(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepStart));
                        DateTimeOffset eOffset = new DateTimeOffset(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepEnd));
                        slp.SleepDuration = eOffset - sOffset;
                        return PartialView("TimeLineSleepPartial", slp);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Note)
            {
                if (idParse)
                {
                    Note nte = await _notesHttpClient.GetNote(itemId);
                    if (nte != null && nte.NoteId > 0)
                    {
                        nte.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(nte.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        return PartialView("TimeLineNotePartial", nte);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Contact)
            {
                if (idParse)
                {
                    Contact cnt = await _contactsHttpClient.GetContact(itemId);
                    if (cnt != null && cnt.ContactId > 0)
                    {
                        cnt.PictureLink = _imageStore.UriFor(cnt.PictureLink, "contacts");
                        if (cnt.DateAdded == null)
                        {
                            cnt.DateAdded = DateTime.UtcNow;
                        }
                        return PartialView("TimeLineContactPartial", cnt);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Vaccination)
            {
                if (idParse)
                {
                    Vaccination vac = await _vaccinationsHttpClient.GetVaccination(itemId);
                    if (vac != null && vac.VaccinationId > 0)
                    {
                        return PartialView("TimeLineVaccinationPartial", vac);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Location)
            {
                if (idParse)
                {
                    Location loc = await _locationsHttpClient.GetLocation(itemId);
                    if (loc != null && loc.LocationId > 0)
                    {
                        return PartialView("TimeLineLocationPartial", loc);
                    }
                }
            }

            Note failNote = new Note();
            failNote.CreatedDate = DateTime.UtcNow;
            failNote.Title = "Error, content not found.";

            return PartialView("TimeLineNotePartial", failNote);
        }
    }
}