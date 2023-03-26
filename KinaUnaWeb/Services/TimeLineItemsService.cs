using System;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.ItemViewModels;

namespace KinaUnaWeb.Services
{
    public class TimeLineItemsService : ITimeLineItemsService
    {
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
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;

        public TimeLineItemsService(IMediaHttpClient mediaHttpClient, ImageStore imageStore, IWordsHttpClient wordsHttpClient, IVaccinationsHttpClient vaccinationsHttpClient,
            ISkillsHttpClient skillsHttpClient, INotesHttpClient notesHttpClient, IMeasurementsHttpClient measurementsHttpClient, ILocationsHttpClient locationsHttpClient, IFriendsHttpClient friendsHttpClient,
            IContactsHttpClient contactsHttpClient, ICalendarsHttpClient calendarsHttpClient, ISleepHttpClient sleepHttpClient)
        {
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
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
        }

        public async Task<TimeLineItemPartialViewModel> GetTimeLineItemPartialViewModel(TimeLineItemViewModel model)
        {
            string id = model.ItemId.ToString();
            bool idParse = int.TryParse(id, out int itemId);
            
            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Photo)
            {
                if (idParse)
                {
                    PictureViewModel picture = await _mediaHttpClient.GetPictureViewModel(itemId, 0, 1, model.CurrentUser.Timezone, model.TagFilter);
                    if (picture != null && picture.PictureId > 0)
                    {
                        picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                        picture.CommentsCount = picture.CommentsList.Count;
                        return new TimeLineItemPartialViewModel("TimeLinePhotoPartial", picture);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Video)
            {
                if (idParse)
                {
                    VideoViewModel video = await _mediaHttpClient.GetVideoViewModel(itemId, 0, 1, model.CurrentUser.Timezone);
                    if (video != null && video.VideoId > 0)
                    {
                        video.CommentsCount = video.CommentsList.Count;
                        return new TimeLineItemPartialViewModel("TimeLineVideoPartial", video);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Calendar)
            {
                if (idParse)
                {
                    CalendarItem evt = await _calendarsHttpClient.GetCalendarItem(itemId);
                    if (evt != null && evt.EventId > 0)
                    {
                        if (evt.StartTime.HasValue && evt.EndTime.HasValue)
                        {
                            evt.StartTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                            evt.EndTime = TimeZoneInfo.ConvertTimeFromUtc(evt.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        }
                        return new TimeLineItemPartialViewModel("TimeLineEventPartial", evt);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Vocabulary)
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
                        return new TimeLineItemPartialViewModel("TimeLineVocabularyPartial", voc);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Skill)
            {
                if (idParse)
                {
                    Skill skl = await _skillsHttpClient.GetSkill(itemId);
                    if (skl != null && skl.SkillId > 0)
                    {
                        return new TimeLineItemPartialViewModel("TimeLineSkillPartial", skl);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Friend)
            {
                if (idParse)
                {
                    Friend frn = await _friendsHttpClient.GetFriend(itemId);
                    if (frn != null && frn.FriendId > 0)
                    {
                        frn.PictureLink = _imageStore.UriFor(frn.PictureLink, "friends");
                        return new TimeLineItemPartialViewModel("TimeLineFriendPartial", frn);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Measurement)
            {
                if (idParse)
                {
                    Measurement mes = await _measurementsHttpClient.GetMeasurement(itemId);
                    if (mes != null && mes.MeasurementId > 0)
                    {
                        return new TimeLineItemPartialViewModel("TimeLineMeasurementPartial", mes);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Sleep)
            {
                if (idParse)
                {
                    Sleep slp = await _sleepHttpClient.GetSleepItem(itemId);
                    if (slp != null && slp.SleepId > 0)
                    {
                        slp.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        slp.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        DateTimeOffset sOffset = new(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepStart));
                        DateTimeOffset eOffset = new(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepEnd));
                        slp.SleepDuration = eOffset - sOffset;

                        return new TimeLineItemPartialViewModel("TimeLineSleepPartial", slp);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Note)
            {
                if (idParse)
                {
                    Note nte = await _notesHttpClient.GetNote(itemId);
                    if (nte != null && nte.NoteId > 0)
                    {
                        nte.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(nte.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        return new TimeLineItemPartialViewModel("TimeLineNotePartial", nte);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Contact)
            {
                if (idParse)
                {
                    Contact cnt = await _contactsHttpClient.GetContact(itemId);
                    if (cnt != null && cnt.ContactId > 0)
                    {
                        if (cnt.DateAdded == null)
                        {
                            cnt.DateAdded = DateTime.UtcNow;
                        }

                        cnt.PictureLink = _imageStore.UriFor(cnt.PictureLink, "contacts");

                        return new TimeLineItemPartialViewModel("TimeLineContactPartial", cnt);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Vaccination)
            {
                if (idParse)
                {
                    Vaccination vac = await _vaccinationsHttpClient.GetVaccination(itemId);
                    if (vac != null && vac.VaccinationId > 0)
                    {
                        return new TimeLineItemPartialViewModel("TimeLineVaccinationPartial", vac);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Location)
            {
                if (idParse)
                {
                    Location loc = await _locationsHttpClient.GetLocation(itemId);
                    if (loc != null && loc.LocationId > 0)
                    {
                        return new TimeLineItemPartialViewModel("TimeLineLocationPartial", loc);
                    }
                }
            }

            Note failNote = new()
            {
                CreatedDate = DateTime.UtcNow,
                Title = "Error, content not found."
            };

            return new TimeLineItemPartialViewModel("TimeLineNotePartial", failNote);
        }
    }
}
