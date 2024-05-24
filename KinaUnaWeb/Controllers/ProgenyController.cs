using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.FamilyViewModels;
using KinaUnaWeb.Models.TypeScriptModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class ProgenyController(
        IProgenyHttpClient progenyHttpClient,
        IMediaHttpClient mediaHttpClient,
        ImageStore imageStore,
        IUserInfosHttpClient userInfosHttpClient,
        ITimelineHttpClient timelineHttpClient,
        ICalendarsHttpClient calendarsHttpClient,
        IWordsHttpClient wordsHttpClient,
        ISkillsHttpClient skillsHttpClient,
        IFriendsHttpClient friendsHttpClient,
        IMeasurementsHttpClient measurementsHttpClient,
        ISleepHttpClient sleepHttpClient,
        INotesHttpClient notesClient,
        IContactsHttpClient contactsHttpClient,
        IVaccinationsHttpClient vaccinationsHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IAutoSuggestsHttpClient autoSuggestsHttpClient)
        : Controller
    {
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Family");
        }

        [HttpGet]
        public async Task<IActionResult> AddProgeny()
        {
            ProgenyViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie()
            };
            string userEmail = User.GetEmail();
            model.CurrentUser = await userInfosHttpClient.GetUserInfo(userEmail);
            model.Admins = model.CurrentUser.UserEmail.ToUpper();
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddProgeny(ProgenyViewModel model)
        {
            Progeny prog = new()
            {
                BirthDay = model.BirthDay,
                Admins = model.Admins.ToUpper(),
                Name = model.Name,
                NickName = model.NickName,
                PictureLink = model.PictureLink,
                TimeZone = model.TimeZone
            };
            // Todo: Check if the progeny exists.

            if (model.File != null)
            {
                await using Stream stream = model.File.OpenReadStream();
                prog.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Progeny);
            }
            else
            {
                prog.PictureLink = Constants.WebAppUrl + "/photodb/childcareicon.jpg"; // Todo: Find better image
            }

            await progenyHttpClient.AddProgeny(prog);
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditProgeny(int progenyId)
        {
            ProgenyViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie()
            };
            string userEmail = User.GetEmail();
            model.CurrentUser = await userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await progenyHttpClient.GetProgeny(progenyId);

            model.ProgenyId = prog.Id;
            model.Name = prog.Name;
            model.NickName = prog.NickName;
            model.BirthDay = prog.BirthDay;
            model.TimeZone = prog.TimeZone;
            model.Admins = prog.Admins.ToUpper();
            model.PictureLink = prog.PictureLink;
            model.PictureLink = imageStore.UriFor(prog.PictureLink, BlobContainers.Progeny);
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditProgeny(ProgenyViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(userEmail);
            Progeny prog = await progenyHttpClient.GetProgeny(model.ProgenyId);
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

            if (model.File != null && model.File.Name != string.Empty)
            {
                string oldPictureLink = prog.PictureLink;
                await using (Stream stream = model.File.OpenReadStream())
                {
                    prog.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Progeny);
                }

                await imageStore.DeleteImage(oldPictureLink, BlobContainers.Progeny);
            }
            await progenyHttpClient.UpdateProgeny(prog);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteProgeny(int progenyId)
        {
            string userEmail = User.GetEmail();
            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(userEmail);
            Progeny prog = await progenyHttpClient.GetProgeny(progenyId);
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
            string userEmail = User.GetEmail();
            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(userEmail);
            Progeny prog = await progenyHttpClient.GetProgeny(model.Id);
            if (!prog.IsInAdminList(userinfo.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            List<Picture> photoList = await mediaHttpClient.GetPictureList(model.Id, (int)AccessLevel.Private, userinfo.Timezone);
            if (photoList.Any())
            {
                foreach (Picture picture in photoList)
                {

                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(picture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo);
                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await mediaHttpClient.DeletePicture(picture.PictureId);
                }
            }

            List<Video> videoList = await mediaHttpClient.GetVideoList(model.Id, 0, userinfo.Timezone);
            if (videoList.Any())
            {
                foreach (Video video in videoList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(video.VideoId.ToString(), (int)KinaUnaTypes.TimeLineType.Video);
                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await mediaHttpClient.DeleteVideo(video.VideoId);
                }
            }

            List<CalendarItem> eventsList = await calendarsHttpClient.GetCalendarList(model.Id, 0);
            if (eventsList.Any())
            {
                foreach (CalendarItem evt in eventsList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(evt.EventId.ToString(), (int)KinaUnaTypes.TimeLineType.Calendar);
                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await calendarsHttpClient.DeleteCalendarItem(evt.EventId);
                }
            }

            List<VocabularyItem> vocabList = await wordsHttpClient.GetWordsList(model.Id, 0);
            if (vocabList.Any())
            {
                foreach (VocabularyItem voc in vocabList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(voc.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await wordsHttpClient.DeleteWord(voc.WordId);
                }
            }

            List<Skill> skillList = await skillsHttpClient.GetSkillsList(model.Id, 0);
            if (skillList.Any())
            {
                foreach (Skill skill in skillList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(skill.SkillId.ToString(), (int)KinaUnaTypes.TimeLineType.Skill);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await skillsHttpClient.DeleteSkill(skill.SkillId);
                }
            }

            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.Id, 0);

            if (friendsList.Any())
            {
                foreach (Friend friend in friendsList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(friend.FriendId.ToString(), (int)KinaUnaTypes.TimeLineType.Friend);
                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await imageStore.DeleteImage(friend.PictureLink);

                    await friendsHttpClient.DeleteFriend(friend.FriendId);
                }
            }

            List<Measurement> measurementsList = await measurementsHttpClient.GetMeasurementsList(model.Id, 0);

            if (measurementsList.Any())
            {
                foreach (Measurement measurement in measurementsList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(measurement.MeasurementId.ToString(), (int)KinaUnaTypes.TimeLineType.Measurement);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await measurementsHttpClient.DeleteMeasurement(measurement.MeasurementId);
                }
            }

            List<Sleep> sleepList = await sleepHttpClient.GetSleepList(model.Id, 0);
            
            if (sleepList.Any())
            {
                foreach (Sleep sleep in sleepList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(sleep.SleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await sleepHttpClient.DeleteSleepItem(sleep.SleepId);
                }
            }

            List<Note> notesList = await notesClient.GetNotesList(model.Id, 0);

            if (notesList.Any())
            {
                foreach (Note note in notesList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(note.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }
                    // Todo: Delete content added from notes

                    await notesClient.DeleteNote(note.NoteId);
                }
            }

            List<Contact> contactsList = await contactsHttpClient.GetContactsList(model.Id, 0);
            
            if (contactsList.Any())
            {
                foreach (Contact contact in contactsList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(contact.ContactId.ToString(), (int)KinaUnaTypes.TimeLineType.Contact);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await contactsHttpClient.DeleteContact(contact.ContactId);
                    
                    await imageStore.DeleteImage(contact.PictureLink);
                }
            }

            List<Vaccination> vaccinationsList = await vaccinationsHttpClient.GetVaccinationsList(model.Id, 0);

            if (vaccinationsList.Any())
            {
                foreach (Vaccination vaccination in vaccinationsList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(vaccination.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination);

                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await vaccinationsHttpClient.DeleteVaccination(vaccination.VaccinationId);
                }
            }

            List<Location> locationsList = await locationsHttpClient.GetLocationsList(model.Id, 0);
            if (locationsList.Any())
            {
                foreach (Location location in locationsList)
                {
                    TimeLineItem tItem = await timelineHttpClient.GetTimeLineItem(location.LocationId.ToString(), (int)KinaUnaTypes.TimeLineType.Location);
                    if (tItem != null)
                    {
                        await timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    }

                    await locationsHttpClient.DeleteLocation(location.LocationId);
                }
            }

            await progenyHttpClient.DeleteProgeny(model.Id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> GetAllProgenyTags([FromBody] AutoSuggestList suggestionsList)
        {
            suggestionsList.Suggestions = await autoSuggestsHttpClient.GetTagsList(suggestionsList.ProgenyId, 0);


            return Json(suggestionsList);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllProgenyContexts([FromBody] AutoSuggestList suggestionsList)
        {
            suggestionsList.Suggestions = await autoSuggestsHttpClient.GetContextsList(suggestionsList.ProgenyId, 0);
            
            return Json(suggestionsList);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllProgenyLocations([FromBody] AutoSuggestList suggestionsList)
        {
            suggestionsList.Suggestions = await autoSuggestsHttpClient.GetLocationsList(suggestionsList.ProgenyId, 0);

            return Json(suggestionsList);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllProgenyCategories([FromBody] AutoSuggestList suggestionsList)
        {
            suggestionsList.Suggestions = await autoSuggestsHttpClient.GetCategoriesList(suggestionsList.ProgenyId, 0);
            
            return Json(suggestionsList);
        }
    }
}