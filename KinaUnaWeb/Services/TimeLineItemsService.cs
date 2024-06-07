using System;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Services
{
    public class TimeLineItemsService(
        IMediaHttpClient mediaHttpClient,
        IWordsHttpClient wordsHttpClient,
        IVaccinationsHttpClient vaccinationsHttpClient,
        ISkillsHttpClient skillsHttpClient,
        INotesHttpClient notesHttpClient,
        IMeasurementsHttpClient measurementsHttpClient,
        ILocationsHttpClient locationsHttpClient,
        IFriendsHttpClient friendsHttpClient,
        IContactsHttpClient contactsHttpClient,
        ICalendarsHttpClient calendarsHttpClient,
        ISleepHttpClient sleepHttpClient)
        : ITimeLineItemsService
    {
        public async Task<TimeLineItemPartialViewModel> GetTimeLineItemPartialViewModel(TimeLineItemViewModel model)
        {
            string id = model.ItemId.ToString();
            bool idParse = int.TryParse(id, out int itemId);
            
            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Photo)
            {
                if (idParse)
                {
                    PictureViewModel picture = await mediaHttpClient.GetPictureViewModel(itemId, 0, 1, model.CurrentUser.Timezone, model.TagFilter);
                    if (picture != null && picture.PictureId > 0)
                    {
                        string pictureUrl = "/Pictures/File?id=" + picture.PictureId + "&size=600";
                        picture.PictureLink = pictureUrl;
                        picture.CommentsCount = picture.CommentsList.Count;
                        return new TimeLineItemPartialViewModel("_TimeLinePhotoPartial", picture);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Video)
            {
                if (idParse)
                {
                    VideoViewModel video = await mediaHttpClient.GetVideoViewModel(itemId, 0, 1, model.CurrentUser.Timezone);
                    if (video != null && video.VideoId > 0)
                    {
                        video.CommentsCount = video.CommentsList.Count;
                        return new TimeLineItemPartialViewModel("_TimeLineVideoPartial", video);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Calendar)
            {
                if (idParse)
                {
                    CalendarItem evt = await calendarsHttpClient.GetCalendarItem(itemId);
                    if (evt != null && evt.EventId > 0)
                    {
                        if (!evt.StartTime.HasValue || !evt.EndTime.HasValue) return new TimeLineItemPartialViewModel("_TimeLineEventPartial", evt);

                        evt.StartTime = TimeZoneInfo.ConvertTimeFromUtc(evt.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        evt.EndTime = TimeZoneInfo.ConvertTimeFromUtc(evt.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        return new TimeLineItemPartialViewModel("_TimeLineEventPartial", evt);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Vocabulary)
            {
                if (idParse)
                {
                    VocabularyItem voc = await wordsHttpClient.GetWord(itemId);
                    if (voc != null && voc.WordId > 0)
                    {
                        if (voc.Date != null)
                        {
                            voc.Date = TimeZoneInfo.ConvertTimeFromUtc(voc.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

                        }
                        return new TimeLineItemPartialViewModel("_TimeLineVocabularyPartial", voc);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Skill)
            {
                if (idParse)
                {
                    Skill skl = await skillsHttpClient.GetSkill(itemId);
                    if (skl != null && skl.SkillId > 0)
                    {
                        return new TimeLineItemPartialViewModel("_TimeLineSkillPartial", skl);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Friend)
            {
                if (idParse)
                {
                    Friend frn = await friendsHttpClient.GetFriend(itemId);
                    if (frn != null && frn.FriendId > 0)
                    {
                        frn.PictureLink = frn.GetProfilePictureUrl();
                        return new TimeLineItemPartialViewModel("_TimeLineFriendPartial", frn);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Measurement)
            {
                if (idParse)
                {
                    Measurement mes = await measurementsHttpClient.GetMeasurement(itemId);
                    if (mes != null && mes.MeasurementId > 0)
                    {
                        return new TimeLineItemPartialViewModel("_TimeLineMeasurementPartial", mes);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Sleep)
            {
                if (idParse)
                {
                    Sleep slp = await sleepHttpClient.GetSleepItem(itemId);
                    if (slp != null && slp.SleepId > 0)
                    {
                        slp.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        slp.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(slp.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        DateTimeOffset sOffset = new(slp.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepStart));
                        DateTimeOffset eOffset = new(slp.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(slp.SleepEnd));
                        slp.SleepDuration = eOffset - sOffset;

                        return new TimeLineItemPartialViewModel("_TimeLineSleepPartial", slp);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Note)
            {
                if (idParse)
                {
                    Note nte = await notesHttpClient.GetNote(itemId);
                    if (nte != null && nte.NoteId > 0)
                    {
                        nte.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(nte.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        return new TimeLineItemPartialViewModel("_TimeLineNotePartial", nte);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Contact)
            {
                if (idParse)
                {
                    Contact cnt = await contactsHttpClient.GetContact(itemId);
                    if (cnt != null && cnt.ContactId > 0)
                    {
                        cnt.DateAdded ??= DateTime.UtcNow;

                        cnt.PictureLink = cnt.GetProfilePictureUrl();

                        return new TimeLineItemPartialViewModel("_TimeLineContactPartial", cnt);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Vaccination)
            {
                if (idParse)
                {
                    Vaccination vac = await vaccinationsHttpClient.GetVaccination(itemId);
                    if (vac != null && vac.VaccinationId > 0)
                    {
                        return new TimeLineItemPartialViewModel("_TimeLineVaccinationPartial", vac);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Location)
            {
                if (idParse)
                {
                    Location loc = await locationsHttpClient.GetLocation(itemId);
                    if (loc != null && loc.LocationId > 0)
                    {
                        return new TimeLineItemPartialViewModel("_TimeLineLocationPartial", loc);
                    }
                }
            }

            Note failNote = new()
            {
                CreatedDate = DateTime.UtcNow,
                Title = "Error, content not found."
            };

            return new TimeLineItemPartialViewModel("_TimeLineNotePartial", failNote);
        }
    }
}
