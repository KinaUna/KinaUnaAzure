using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaWeb.Controllers
{
    public class ProgenyController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly WebDbContext _context;
        private readonly ImageStore _imageStore;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public ProgenyController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, WebDbContext context, ImageStore imagestore)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _context = context; // Todo: Replace _context with httpClient
            _imageStore = imagestore;
        }
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Family");
        }

        [HttpGet]
        public async Task<IActionResult> AddProgeny()
        {
            ProgenyViewModel model = new ProgenyViewModel();
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo currentUser = await _progenyHttpClient.GetUserInfo(userEmail);
            model.Admins = currentUser.UserEmail.ToUpper();
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddProgeny(ProgenyViewModel model)
        {
            Progeny prog = new Progeny();
            prog.BirthDay = model.BirthDay;
            prog.Admins = model.Admins.ToUpper();
            prog.Name = model.Name;
            prog.NickName = model.NickName;
            prog.PictureLink = model.PictureLink;
            prog.TimeZone = model.TimeZone;
            // Todo: Check if the progeny exists.

            if (model.File != null)
            {
                using (var stream = model.File.OpenReadStream())
                {
                    prog.PictureLink = await _imageStore.SaveImage(stream, "progeny");

                }
            }
            else
            {
                prog.PictureLink = Constants.WebAppUrl + "/photodb/childcareicon.jpg"; // Todo: Find better image
            }

            await _progenyHttpClient.AddProgeny(prog);
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditProgeny(int progenyId)
        {
            ProgenyViewModel model = new ProgenyViewModel();
            Progeny prog = await _progenyHttpClient.GetProgeny(progenyId);

            model.ProgenyId = prog.Id;
            model.Name = prog.Name;
            model.NickName = prog.NickName;
            model.BirthDay = prog.BirthDay;
            model.TimeZone = prog.TimeZone;
            model.Admins = prog.Admins.ToUpper();
            model.PictureLink = prog.PictureLink;
            if (!prog.PictureLink.ToLower().StartsWith("http"))
            {
                model.PictureLink = _imageStore.UriFor(prog.PictureLink, "progeny");
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditProgeny(ProgenyViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(userinfo.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            prog.BirthDay = model.BirthDay;
            prog.Admins = model.Admins.ToUpper();
            prog.Name = model.Name;
            prog.NickName = model.NickName;
            prog.TimeZone = model.TimeZone;
            // Todo: check if fields are valid.

            if (model.File != null && model.File.Name != String.Empty)
            {
                string oldPictureLink = prog.PictureLink;
                using (var stream = model.File.OpenReadStream())
                {
                    prog.PictureLink = await _imageStore.SaveImage(stream, "progeny");
                }

                if (!oldPictureLink.ToLower().StartsWith("http") && !String.IsNullOrEmpty(oldPictureLink))
                {
                    await _imageStore.DeleteImage(oldPictureLink, "progeny");
                }
            }
            await _progenyHttpClient.UpdateProgeny(prog);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteProgeny(int progenyId)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            Progeny prog = await _progenyHttpClient.GetProgeny(progenyId);
            if (!prog.IsInAdminList(userinfo.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            return View(prog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgeny(Progeny model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Id);
            if (!prog.IsInAdminList(userinfo.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            List<Picture> photoList = await _mediaHttpClient.GetPictureList(model.Id, (int)AccessLevel.Private, userinfo.Timezone);
            if (photoList.Any())
            {
                foreach (Picture picture in photoList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == picture.PictureId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Photo);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    await _mediaHttpClient.DeletePicture(picture.PictureId);
                }
            }

            List<Video> videoList = await _mediaHttpClient.GetVideoList(model.Id, 0, userinfo.Timezone);
            if (videoList.Any())
            {
                foreach (Video video in videoList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == video.VideoId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Video);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    await _mediaHttpClient.DeleteVideo(video.VideoId);
                }
            }

            List<CalendarItem> eventsList = _context.CalendarDb.Where(e => e.ProgenyId == model.Id).ToList();
            if (eventsList.Any())
            {
                foreach (CalendarItem evt in eventsList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == evt.EventId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }
                    _context.CalendarDb.Remove(evt);
                    await _context.SaveChangesAsync();
                }
            }

            List<VocabularyItem> vocabList = _context.VocabularyDb.Where(v => v.ProgenyId == model.Id).ToList();
            if (vocabList.Any())
            {
                foreach (VocabularyItem voc in vocabList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == voc.WordId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Vocabulary);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }
                    _context.VocabularyDb.Remove(voc);
                    await _context.SaveChangesAsync();
                }
            }

            List<Skill> skillList = _context.SkillsDb.Where(s => s.ProgenyId == model.Id).ToList();
            if (skillList.Any())
            {
                foreach (Skill skill in skillList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == skill.SkillId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Skill);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }
                    _context.SkillsDb.Remove(skill);
                    await _context.SaveChangesAsync();
                }
            }

            List<Friend> friendsList = _context.FriendsDb.Where(f => f.ProgenyId == model.Id).ToList();
            if (friendsList.Any())
            {
                foreach (Friend friend in friendsList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == friend.FriendId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Friend);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    if (!friend.PictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(friend.PictureLink);
                    }
                    _context.FriendsDb.Remove(friend);
                    await _context.SaveChangesAsync();
                }
            }

            List<Measurement> measurementsList =
                _context.MeasurementsDb.Where(m => m.ProgenyId == model.Id).ToList();
            if (measurementsList.Any())
            {
                foreach (Measurement measurement in measurementsList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == measurement.MeasurementId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Measurement);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    _context.MeasurementsDb.Remove(measurement);
                    await _context.SaveChangesAsync();
                }
            }

            List<Sleep> sleepList = _context.SleepDb.Where(s => s.ProgenyId == model.Id).ToList();
            if (sleepList.Any())
            {
                foreach (Sleep sleep in sleepList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == sleep.SleepId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Sleep);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    _context.SleepDb.Remove(sleep);
                    await _context.SaveChangesAsync();
                }
            }

            List<Note> notesList = _context.NotesDb.Where(n => n.ProgenyId == model.Id).ToList();
            if (notesList.Any())
            {
                foreach (Note note in notesList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == note.NoteId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Note);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }
                    // Todo: Delete content add from notes
                    _context.NotesDb.Remove(note);
                    await _context.SaveChangesAsync();
                }
            }

            List<Contact> contactsList = _context.ContactsDb.Where(c => c.ProgenyId == model.Id).ToList();
            if (contactsList.Any())
            {
                foreach (Contact contact in contactsList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == contact.ContactId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Contact);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    _context.ContactsDb.Remove(contact);
                    if (contact.AddressIdNumber != null)
                    {
                        Address address = await _context.AddressDb.SingleAsync(a => a.AddressId == contact.AddressIdNumber);
                        _context.AddressDb.Remove(address);
                    }

                    await _context.SaveChangesAsync();

                    if (!contact.PictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(contact.PictureLink);
                    }
                }
            }

            List<Vaccination> vaccinationsList =
                _context.VaccinationsDb.Where(v => v.ProgenyId == model.Id).ToList();
            if (vaccinationsList.Any())
            {
                foreach (Vaccination vaccination in vaccinationsList)
                {
                    TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                        t.ItemId == vaccination.VaccinationId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Vaccination);
                    if (tItem != null)
                    {
                        _context.TimeLineDb.Remove(tItem);
                        await _context.SaveChangesAsync();
                    }

                    _context.VaccinationsDb.Remove(vaccination);
                    await _context.SaveChangesAsync();
                }
            }

            await _progenyHttpClient.DeleteProgeny(model.Id);
            return RedirectToAction("Index");
        }
    }
}