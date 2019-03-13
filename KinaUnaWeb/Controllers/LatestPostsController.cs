using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaWeb.Controllers
{
    public class LatestPostsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly string _defaultUser = Constants.DefaultUserEmail;
        
        public LatestPostsController(WebDbContext context, IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore)
        {
            _context = context;  // Todo: replace _context with httpClient
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
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
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            
            string id = model.ItemId.ToString();
            int type = model.TypeId;
            int itemId;
            bool idParse = int.TryParse(id, out itemId);
            if (type == (int)KinaUnaTypes.TimeLineType.Photo)
            {
                if (idParse)
                {
                    PictureViewModel picture = await _mediaHttpClient.GetPictureViewModel(itemId, 0, 1, userinfo.Timezone);
                    if (picture != null)
                    {
                        if (!picture.PictureLink.StartsWith("https://"))
                        {
                            picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                        }
                        picture.CommentsCount = picture.CommentsList.Count;
                        return PartialView("TimeLinePhotoPartial", picture);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Video)
            {
                if (idParse)
                {
                    VideoViewModel video = await _mediaHttpClient.GetVideoViewModel(itemId, 0, 1, userinfo.Timezone);
                    if (video != null)
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
                    CalendarItem evt = _context.CalendarDb.AsNoTracking().SingleOrDefault(e => e.EventId == itemId);
                    if (evt != null)
                    {
                        if (evt.StartTime.HasValue && evt.EndTime.HasValue)
                        {
                            evt.StartTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                            evt.EndTime = TimeZoneInfo.ConvertTimeFromUtc(evt.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        }
                        return PartialView("TimeLineEventPartial", evt);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Vocabulary)
            {
                if (idParse)
                {
                    VocabularyItem voc = _context.VocabularyDb.AsNoTracking().SingleOrDefault(v => v.WordId == itemId);
                    if (voc != null)
                    {
                        if (voc.Date != null)
                        {
                            voc.Date = TimeZoneInfo.ConvertTimeFromUtc(voc.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));

                        }
                        return PartialView("TimeLineVocabularyPartial", voc);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Skill)
            {
                if (idParse)
                {
                    Skill skl = _context.SkillsDb.AsNoTracking().SingleOrDefault(s => s.SkillId == itemId);
                    if (skl != null)
                    {
                        return PartialView("TimeLineSkillPartial", skl);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Friend)
            {
                if (idParse)
                {
                    Friend frn = _context.FriendsDb.AsNoTracking().SingleOrDefault(f => f.FriendId == itemId);
                    if (frn != null)
                    {
                        if (!frn.PictureLink.StartsWith("https://"))
                        {
                            frn.PictureLink = _imageStore.UriFor(frn.PictureLink, "friends");
                        }
                        return PartialView("TimeLineFriendPartial", frn);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Measurement)
            {
                if (idParse)
                {
                    Measurement mes = _context.MeasurementsDb.AsNoTracking().SingleOrDefault(m => m.MeasurementId == itemId);
                    if (mes != null)
                    {
                        return PartialView("TimeLineMeasurementPartial", mes);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Sleep)
            {
                if (idParse)
                {
                    Sleep slp = _context.SleepDb.AsNoTracking().SingleOrDefault(s => s.SleepId == itemId);
                    if (slp != null)
                    {
                        slp.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        slp.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        DateTimeOffset sOffset = new DateTimeOffset(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(slp.SleepStart));
                        DateTimeOffset eOffset = new DateTimeOffset(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone).GetUtcOffset(slp.SleepEnd));
                        slp.SleepDuration = eOffset - sOffset;
                        
                        return PartialView("TimeLineSleepPartial", slp);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Note)
            {
                if (idParse)
                {
                    Note nte = _context.NotesDb.AsNoTracking().SingleOrDefault(n => n.NoteId == itemId);
                    if (nte != null)
                    {
                        nte.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(nte.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                        return PartialView("TimeLineNotePartial", nte);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Contact)
            {
                if (idParse)
                {
                    Contact cnt = _context.ContactsDb.AsNoTracking().SingleOrDefault(c => c.ContactId == itemId);
                    if (cnt != null)
                    {
                        if (cnt.DateAdded == null)
                        {
                            if (!cnt.PictureLink.StartsWith("https://"))
                            {
                                cnt.PictureLink = _imageStore.UriFor(cnt.PictureLink, "contacts");
                            }

                            cnt.DateAdded = new DateTime(1900, 1, 1);
                        }
                        return PartialView("TimeLineContactPartial", cnt);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Vaccination)
            {
                if (idParse)
                {
                    Vaccination vac = _context.VaccinationsDb.AsNoTracking().SingleOrDefault(v => v.VaccinationId == itemId);
                    if (vac != null)
                    {
                        return PartialView("TimeLineVaccinationPartial", vac);
                    }
                }
            }

            if (type == (int)KinaUnaTypes.TimeLineType.Location)
            {
                if (idParse)
                {
                    Location loc = _context.LocationsDb.AsNoTracking().SingleOrDefault(l => l.LocationId == itemId);
                    if (loc != null)
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