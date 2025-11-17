using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ProgeniesViewModels;
using KinaUnaWeb.Models.TypeScriptModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Models.FamiliesViewModels;

namespace KinaUnaWeb.Controllers
{
    public class ProgenyController(
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient,
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
        IAutoSuggestsHttpClient autoSuggestsHttpClient,
        IViewModelSetupService viewModelSetupService)
        : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Families");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int progenyId)
        {
            ProgenyInfo progenyInfo = await progenyHttpClient.GetProgenyInfo(progenyId);
            if (progenyInfo == null || progenyInfo.ProgenyId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId, 0, false);
            
            ProgenyDetailsViewModel model = new(baseModel)
            {
                ProgenyInfo = progenyInfo
            };
            
            return PartialView("_ProgenyDetailsPartial", model);
        }


        /// <summary>
        /// Profile picture for a Progeny. If the Progeny has no picture or the user has no access to the picture, a default image is returned.
        /// Images are stored in Azure Blob Storage, this provides a static URL for profile pictures.
        /// </summary>
        /// <param name="id">The Id of the Progeny to get the profile picture for.</param>
        /// <returns>FileContentResult with the image data.</returns>
        [AllowAnonymous]
        public async Task<FileContentResult> ProfilePicture(int id)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), id, 0, false);
            
            if (baseModel.CurrentProgeny == null || baseModel.CurrentProgeny.Id == 0 || string.IsNullOrEmpty(baseModel.CurrentProgeny.PictureLink))
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("868b62e2-6978-41a1-97dc-1cc1116f65a6.jpg");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/jpeg");
            }

            MemoryStream fileContent = await imageStore.GetStream(baseModel.CurrentProgeny.PictureLink, BlobContainers.Progeny);
            byte[] fileContentBytes = fileContent.ToArray();

            return new FileContentResult(fileContentBytes, baseModel.CurrentProgeny.GetPictureFileContentType());
        }

        /// <summary>
        /// Page for adding a new Progeny.
        /// </summary>
        /// <returns>View with ProgenyViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> AddProgeny()
        {
            // Todo: Check if user is allowed to add Progeny.
            ProgenyViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie()
            };
            string userEmail = User.GetEmail();
            model.CurrentUser = await userInfosHttpClient.GetUserInfo(userEmail);
            model.Admins = model.CurrentUser.UserEmail.ToUpper();
            
            return PartialView("_AddProgenyPartial", model);
        }

        /// <summary>
        /// Handles the POST request for adding a new Progeny.
        /// </summary>
        /// <param name="model">ProgenyViewModel with the properties of the Progeny to add.</param>
        /// <returns>Redirects to Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddProgeny(ProgenyViewModel model)
        {
            // Todo: Check if user is allowed to add Progeny.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            if (userEmail == Constants.DefaultUserEmail)
            {
                return RedirectToAction("Index", "Home");
            }

            model.CurrentUser = await userInfosHttpClient.GetUserInfo(userEmail);

            Progeny progeny = new()
            {
                BirthDay = model.BirthDay,
                Admins = model.Admins.ToUpper(),
                Name = model.Name,
                NickName = model.NickName,
                PictureLink = model.PictureLink,
                TimeZone = model.TimeZone,
                Email = model.Email
            };
            
            if (model.File != null)
            {
                await using Stream stream = model.File.OpenReadStream();
                string fileFormat = Path.GetExtension(model.File.FileName);
                progeny.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Progeny, fileFormat);
            }
            else
            {
                progeny.PictureLink = Constants.WebAppUrl + "/photodb/childcareicon.jpg"; // Todo: Find better image
            }

            await progenyHttpClient.AddProgeny(progeny);
            model.PictureLink = progeny.GetProfilePictureUrl();

            return PartialView("_ProgenyAddedPartial", model);
        }

        /// <summary>
        /// Page for editing a Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to edit.</param>
        /// <returns>View with ProgenyViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditProgeny(int progenyId)
        {
            ProgenyViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie()
            };
            string userEmail = User.GetEmail();
            model.CurrentUser = await userInfosHttpClient.GetUserInfo(userEmail);

            Progeny progeny = await progenyHttpClient.GetProgeny(progenyId);
            if (progeny == null || progeny.Id == 0 || progeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            model.ProgenyId = progeny.Id;
            model.Name = progeny.Name;
            model.NickName = progeny.NickName;
            model.BirthDay = progeny.BirthDay;
            model.TimeZone = progeny.TimeZone;
            model.Admins = progeny.Admins.ToUpper();
            model.PictureLink = progeny.GetProfilePictureUrl();
            model.Email = progeny.Email;

            model.ProgenyInfo = await progenyHttpClient.GetProgenyInfo(progenyId);

            return PartialView("_EditProgenyPartial", model);
        }

        /// <summary>
        /// Handles the POST request for editing a Progeny.
        /// </summary>
        /// <param name="model">ProgenyViewModel with the updated Progeny properties.</param>
        /// <returns>Redirects to Index.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditProgeny(ProgenyViewModel model)
        {
            Progeny progeny = await progenyHttpClient.GetProgeny(model.ProgenyId);
            if (progeny == null || progeny.Id == 0 || progeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            progeny.BirthDay = model.BirthDay;
            progeny.Admins = model.Admins.ToUpper();
            progeny.Name = model.Name;
            progeny.NickName = model.NickName;
            progeny.TimeZone = model.TimeZone;
            progeny.Email = model.Email;

            if (model.File != null && model.File.Name != string.Empty)
            {
                await using Stream stream = model.File.OpenReadStream();
                string fileFormat = Path.GetExtension(model.File.FileName);
                progeny.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Progeny, fileFormat);
            }
            
            await progenyHttpClient.UpdateProgeny(progeny);

            model.ProgenyInfo = await progenyHttpClient.UpdateProgenyInfo(model.ProgenyInfo);

            model.PictureLink = progeny.GetProfilePictureUrl();

            return PartialView("_ProgenyUpdatedPartial", model);
        }

        /// <summary>
        /// Page for deleting a Progeny.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to delete.</param>
        /// <returns>View with Progeny as model.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteProgeny(int progenyId)
        {
            Progeny progeny = await progenyHttpClient.GetProgeny(progenyId);
            if (progeny == null || progeny.Id == 0 || progeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            return View(progeny);
        }

        // Todo: This will take a relative long time, and may time out. Use split it up and use ajax to delete Progeny and related data.
        /// <summary>
        /// Handles the POST request for deleting a Progeny.
        /// Also deletes all related data for the Progeny.
        /// </summary>
        /// <param name="model">Progeny object with the properties of the Progeny to delete.</param>
        /// <returns>Redirects to Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgeny(Progeny model)
        {
            string userEmail = User.GetEmail();
            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(userEmail);
            Progeny progeny = await progenyHttpClient.GetProgeny(model.Id);
            if (progeny == null || progeny.Id == 0 || progeny.ProgenyPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            List<Picture> photoList = await mediaHttpClient.GetPictureList(model.Id, userinfo.Timezone);
            if (photoList.Count != 0)
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

            List<Video> videoList = await mediaHttpClient.GetVideoList(model.Id, userinfo.Timezone);
            if (videoList.Count != 0)
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

            CalendarItemsRequest calendarItemsRequest = new()
            {
                ProgenyIds = [model.Id]
            };

            List<CalendarItem> eventsList = await calendarsHttpClient.GetProgeniesCalendarList(calendarItemsRequest);
            if (eventsList.Count != 0)
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

            List<VocabularyItem> vocabList = await wordsHttpClient.GetWordsList(model.Id);
            if (vocabList.Count != 0)
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

            List<Skill> skillList = await skillsHttpClient.GetSkillsList(model.Id);
            if (skillList.Count != 0)
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

            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.Id);

            if (friendsList.Count != 0)
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

            List<Measurement> measurementsList = await measurementsHttpClient.GetMeasurementsList(model.Id);

            if (measurementsList.Count != 0)
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

            List<Sleep> sleepList = await sleepHttpClient.GetSleepList(model.Id);
            
            if (sleepList.Count != 0)
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

            List<Note> notesList = await notesClient.GetNotesList(model.Id);

            if (notesList.Count != 0)
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
            
            if (contactsList.Count != 0)
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

            List<Vaccination> vaccinationsList = await vaccinationsHttpClient.GetVaccinationsList(model.Id);

            if (vaccinationsList.Count != 0)
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
            if (locationsList.Count != 0)
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

        /// <summary>
        /// Gets all tags from all items that have tags for a Progeny.
        /// </summary>
        /// <param name="suggestionsList">AutoSuggestList with ProgenyId of the Progeny to get tags for.</param>
        /// <returns>AutoSuggestList object.</returns>
        [HttpPost]
        public async Task<IActionResult> GetAllProgenyTags([FromBody] AutoSuggestList suggestionsList)
        {
            if (suggestionsList.Progenies.Count == 0)
            {
                suggestionsList.Progenies = [Constants.DefaultChildId];
            }
            
            List<string> tagsList = [];
            foreach (int progenyId in suggestionsList.Progenies)
            {
                tagsList.AddRange(await autoSuggestsHttpClient.GetTagsList(progenyId, 0));
            }

            foreach (int familyId in suggestionsList.Families)
            {
                tagsList.AddRange(await autoSuggestsHttpClient.GetTagsList(0, familyId));
            }

            suggestionsList.Suggestions = [.. tagsList.Distinct()];


            return Json(suggestionsList);
        }

        /// <summary>
        /// Gets all contexts from all items that have contexts for a Progeny.
        /// </summary>
        /// <param name="suggestionsList">AutoSuggestList with ProgenyId of the Progeny to get contexts for.</param>
        /// <returns>AutoSuggestList object</returns>
        [HttpPost]
        public async Task<IActionResult> GetAllProgenyContexts([FromBody] AutoSuggestList suggestionsList)
        {
            if (suggestionsList.Progenies.Count == 0)
            {
                suggestionsList.Progenies = [Constants.DefaultChildId];
            }

            List<string> contextsList = [];
            foreach (int progenyId in suggestionsList.Progenies)
            {
                contextsList.AddRange(await autoSuggestsHttpClient.GetContextsList(progenyId, 0));
            }
            foreach (int familyId in suggestionsList.Families)
            {
                contextsList.AddRange(await autoSuggestsHttpClient.GetContextsList(0, familyId));
            }

            suggestionsList.Suggestions = [.. contextsList.Distinct()];


            return Json(suggestionsList);
        }

        /// <summary>
        /// Gets all locations from all items that have locations for a Progeny.
        /// </summary>
        /// <param name="suggestionsList">AutoSuggestList with ProgenyId of the Progeny to get locations for.</param>
        /// <returns>AutoSuggestList object</returns>
        [HttpPost]
        public async Task<IActionResult> GetAllProgenyLocations([FromBody] AutoSuggestList suggestionsList)
        {
            if (suggestionsList.Progenies.Count == 0)
            {
                suggestionsList.Progenies = [Constants.DefaultChildId];
            }

            List<string> locationsList = [];
            foreach (int progenyId in suggestionsList.Progenies)
            {
                locationsList.AddRange(await autoSuggestsHttpClient.GetLocationsList(progenyId, 0));
            }
            foreach (int familyId in suggestionsList.Families)
            {
                locationsList.AddRange(await autoSuggestsHttpClient.GetLocationsList(0, familyId));
            }

            suggestionsList.Suggestions = [.. locationsList.Distinct()];


            return Json(suggestionsList);
        }

        /// <summary>
        /// Gets all categories from all items that have categories for a Progeny.
        /// </summary>
        /// <param name="suggestionsList">AutoSuggestList with ProgenyId of the Progeny to get categories for.</param>
        /// <returns>AutoSuggestList object</returns>
        [HttpPost]
        public async Task<IActionResult> GetAllProgenyCategories([FromBody] AutoSuggestList suggestionsList)
        {
            if (suggestionsList.Progenies.Count == 0)
            {
                suggestionsList.Progenies = [Constants.DefaultChildId];
            }

            List<string> categoriesList = [];
            foreach (int progenyId in suggestionsList.Progenies)
            {
                categoriesList.AddRange(await autoSuggestsHttpClient.GetCategoriesList(progenyId));
            }
            
            suggestionsList.Suggestions = [.. categoriesList.Distinct()];


            return Json(suggestionsList);
        }

        /// <summary>
        /// Gets all languages from all VocabularyItems for a Progeny.
        /// </summary>
        /// <param name="suggestionsList">AutoSuggestList with ProgenyId of the Progeny to get languages for.</param>
        /// <returns>AutoSuggestList object</returns>
        [HttpPost]
        public async Task<IActionResult> GetAllProgenyVocabularyLanguages([FromBody] AutoSuggestList suggestionsList)
        {
            if (suggestionsList.Progenies.Count == 0)
            {
                suggestionsList.Progenies = [Constants.DefaultChildId];
            }

            List<string> languagesList = [];
            foreach (int progenyId in suggestionsList.Progenies)
            {
                languagesList.AddRange(await autoSuggestsHttpClient.GetVocabularyLanguageList(progenyId));
            }

            suggestionsList.Suggestions = [.. languagesList.Distinct()];


            return Json(suggestionsList);
        }

        [HttpGet]
        public async Task<IActionResult> OtherPeopleAndPets()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamiliesViewModel model = new(baseModel);
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OtherPeopleAndPetsList()
        {
            List<Progeny> allMyProgenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.View);
            
            return Json(allMyProgenies);
        }

        [HttpGet]
        public async Task<IActionResult> OtherPeopleElement(int progenyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId, 0, false);
            baseModel.CurrentProgeny = await progenyHttpClient.GetProgeny(progenyId);
            if (baseModel.CurrentProgeny == null || baseModel.CurrentProgeny.Id == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            return PartialView("_OtherPeopleElementPartial", baseModel);
        }

        [HttpGet]
        public async Task<IActionResult> AddOtherPersonToFamily(int progenyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), progenyId, 0, false);
            if (baseModel.CurrentProgeny == null || baseModel.CurrentProgeny.Id == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            
            AddProgenyToFamilyViewModel model = new(baseModel)
            {
                FamilyList = await viewModelSetupService.GetFamilySelectList()
            };

            List<Family> progenysFamilies = await familiesHttpClient.GetFamiliesForProgeny(progenyId);
            foreach (Family family in progenysFamilies)
            {
                model.FamilyList = model.FamilyList.Where(f => f.Value != family.FamilyId.ToString()).ToList();
            }

            model.SetMemberTypeList();
            
            return PartialView("_AddOtherPersonToFamilyPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOtherPersonToFamily([FromForm] AddProgenyToFamilyViewModel model)
        {
            Progeny progenyToAddToFamily = await progenyHttpClient.GetProgeny(model.CurrentProgenyId);
            if (progenyToAddToFamily == null || progenyToAddToFamily.Id == 0)
            {
                return NotFound();
            }
            Family familyToAddProgenyTo = await familiesHttpClient.GetFamily(model.CurrentFamilyId);
            if (familyToAddProgenyTo == null || familyToAddProgenyTo.FamilyId == 0)
            {
                return NotFound();
            }
            if(familyToAddProgenyTo.FamilyPermission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }
            FamilyMember newFamilyMember = new()
            {
                ProgenyId = progenyToAddToFamily.Id,
                FamilyId = familyToAddProgenyTo.FamilyId,
                MemberType = model.MemberType,
                Email = progenyToAddToFamily.Email,
                PermissionLevel = PermissionLevel.View,  // Todo: Let user select permission level?
            };
            FamilyMember addedFamilyMember = await familiesHttpClient.AddFamilyMember(newFamilyMember);

            return Json(addedFamilyMember);
        }
    }
}