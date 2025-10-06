using KinaUna.Data.Extensions;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services.HttpClients;
using System;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Provides methods for generating item specific content for TimeLineItems.
    /// </summary>
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
        ISleepHttpClient sleepHttpClient,
        ITodoItemsHttpClient todoItemsHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IFamiliesHttpClient familiesHttpClient)
        : ITimeLineItemsService
    {
        /// <summary>
        /// Generates a TimeLineItemPartialViewModel object for a given TimeLineItemViewModel.
        /// The TimeLineItemPartialViewModel is used to provide type specific models for partial views in the TimeLine.
        /// </summary>
        /// <param name="model">The TimeLineItemViewModel to generate a ViewModel for.</param>
        /// <returns>TimeLineItemPartialViewModel</returns>
        public async Task<TimeLineItemPartialViewModel> GetTimeLineItemPartialViewModel(TimeLineItemViewModel model)
        {
            string id = model.ItemId.ToString();
            bool idParse = int.TryParse(id, out int itemId);
            
            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Photo)
            {
                if (idParse)
                {
                    PictureViewModelRequest pictureViewModelRequest = new()
                    {
                        PictureId = itemId,
                        SortOrder = 1,
                        TimeZone = model.CurrentUser.Timezone,
                        TagFilter = model.TagFilter
                    };

                    PictureViewModel picture = await mediaHttpClient.GetPictureViewModel(pictureViewModelRequest);
                    if (picture != null && picture.PictureId > 0)
                    {
                        string pictureUrl = "/Pictures/File?id=" + picture.PictureId + "&size=600";
                        picture.PictureLink = pictureUrl;
                        picture.CommentsCount = picture.CommentsList.Count;
                        picture.Progeny = await progenyHttpClient.GetProgeny(picture.ProgenyId);
                        return new TimeLineItemPartialViewModel("_TimeLinePhotoPartial", picture);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Video)
            {
                if (idParse)
                {
                    VideoViewModelRequest videoViewModelRequest = new()
                    {
                        VideoId = itemId,
                        SortOrder = 1,
                        TimeZone = model.CurrentUser.Timezone
                    };
                    VideoViewModel video = await mediaHttpClient.GetVideoViewModel(videoViewModelRequest);
                    if (video != null && video.VideoId > 0)
                    {
                        video.CommentsCount = video.CommentsList.Count;
                        video.Progeny = await progenyHttpClient.GetProgeny(video.ProgenyId);
                        return new TimeLineItemPartialViewModel("_TimeLineVideoPartial", video);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Calendar)
            {
                if (idParse)
                {
                    CalendarItem eventItem = await calendarsHttpClient.GetCalendarItem(itemId);
                    if (eventItem != null && eventItem.EventId > 0)
                    {
                        if (!eventItem.StartTime.HasValue || !eventItem.EndTime.HasValue) return new TimeLineItemPartialViewModel("_TimeLineEventPartial", eventItem);

                        eventItem.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        eventItem.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        if (eventItem.ProgenyId > 0)
                        {
                            eventItem.Progeny = await progenyHttpClient.GetProgeny(eventItem.ProgenyId);
                        }
                        if (eventItem.FamilyId > 0)
                        {
                            eventItem.Family = await familiesHttpClient.GetFamily(eventItem.FamilyId);
                        }

                        if (eventItem.RecurrenceRuleId <= 0 || model.ItemYear <= 0 || model.ItemMonth <= 0 || model.ItemDay <= 0) return new TimeLineItemPartialViewModel("_TimeLineEventPartial", eventItem);
                        
                        TimeSpan eventDuration = eventItem.EndTime.Value - eventItem.StartTime.Value;
                        eventItem.StartTime = new DateTime(model.ItemYear, model.ItemMonth, model.ItemDay, eventItem.StartTime.Value.Hour, eventItem.StartTime.Value.Minute, eventItem.StartTime.Value.Second);
                        eventItem.EndTime = eventItem.StartTime.Value + eventDuration;
                        return new TimeLineItemPartialViewModel("_TimeLineEventPartial", eventItem);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Vocabulary)
            {
                if (idParse)
                {
                    VocabularyItem vocabularyItem = await wordsHttpClient.GetWord(itemId);
                    if (vocabularyItem != null && vocabularyItem.WordId > 0)
                    {
                        if (vocabularyItem.Date != null)
                        {
                            vocabularyItem.Date = TimeZoneInfo.ConvertTimeFromUtc(vocabularyItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

                        }
                        vocabularyItem.Progeny = await progenyHttpClient.GetProgeny(vocabularyItem.ProgenyId);

                        return new TimeLineItemPartialViewModel("_TimeLineVocabularyPartial", vocabularyItem);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Skill)
            {
                if (idParse)
                {
                    Skill skill = await skillsHttpClient.GetSkill(itemId);
                    if (skill != null && skill.SkillId > 0)
                    {
                        skill.Progeny = await progenyHttpClient.GetProgeny(skill.ProgenyId);

                        return new TimeLineItemPartialViewModel("_TimeLineSkillPartial", skill);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Friend)
            {
                if (idParse)
                {
                    Friend friend = await friendsHttpClient.GetFriend(itemId);
                    if (friend != null && friend.FriendId > 0)
                    {
                        friend.PictureLink = friend.GetProfilePictureUrl();
                        friend.Progeny = await progenyHttpClient.GetProgeny(friend.ProgenyId);
                        return new TimeLineItemPartialViewModel("_TimeLineFriendPartial", friend);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Measurement)
            {
                if (idParse)
                {
                    Measurement measurement = await measurementsHttpClient.GetMeasurement(itemId);
                    if (measurement != null && measurement.MeasurementId > 0)
                    {
                        measurement.Progeny = await progenyHttpClient.GetProgeny(measurement.ProgenyId);
                        return new TimeLineItemPartialViewModel("_TimeLineMeasurementPartial", measurement);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Sleep)
            {
                if (idParse)
                {
                    Sleep sleep = await sleepHttpClient.GetSleepItem(itemId);
                    if (sleep != null && sleep.SleepId > 0)
                    {
                        sleep.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        sleep.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        DateTimeOffset sOffset = new(sleep.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(sleep.SleepStart));
                        DateTimeOffset eOffset = new(sleep.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone).GetUtcOffset(sleep.SleepEnd));
                        sleep.SleepDuration = eOffset - sOffset;
                        sleep.Progeny = await progenyHttpClient.GetProgeny(sleep.ProgenyId);

                        return new TimeLineItemPartialViewModel("_TimeLineSleepPartial", sleep);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Note)
            {
                if (idParse)
                {
                    Note note = await notesHttpClient.GetNote(itemId);
                    if (note != null && note.NoteId > 0)
                    {
                        note.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        note.Progeny = await progenyHttpClient.GetProgeny(note.ProgenyId);

                        return new TimeLineItemPartialViewModel("_TimeLineNotePartial", note);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Contact)
            {
                if (idParse)
                {
                    Contact contact = await contactsHttpClient.GetContact(itemId);
                    if (contact != null && contact.ContactId > 0)
                    {
                        contact.DateAdded ??= DateTime.UtcNow;

                        contact.PictureLink = contact.GetProfilePictureUrl();
                        if (contact.ProgenyId > 0)
                        {
                            contact.Progeny = await progenyHttpClient.GetProgeny(contact.ProgenyId);
                        }

                        if (contact.FamilyId > 0)
                        {
                            contact.Family = await familiesHttpClient.GetFamily(contact.FamilyId);
                        }

                        return new TimeLineItemPartialViewModel("_TimeLineContactPartial", contact);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Vaccination)
            {
                if (idParse)
                {
                    Vaccination vaccination = await vaccinationsHttpClient.GetVaccination(itemId);
                    if (vaccination != null && vaccination.VaccinationId > 0)
                    {
                        vaccination.Progeny = await progenyHttpClient.GetProgeny(vaccination.ProgenyId);

                        return new TimeLineItemPartialViewModel("_TimeLineVaccinationPartial", vaccination);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.Location)
            {
                if (idParse)
                {
                    Location location = await locationsHttpClient.GetLocation(itemId);
                    if (location != null && location.LocationId > 0)
                    {
                        if (location.ProgenyId > 0)
                        {
                            location.Progeny = await progenyHttpClient.GetProgeny(location.ProgenyId);
                        }

                        if (location.FamilyId > 0)
                        {
                            location.Family = await familiesHttpClient.GetFamily(location.FamilyId);
                        }

                        return new TimeLineItemPartialViewModel("_TimeLineLocationPartial", location);
                    }
                }
            }

            if (model.TypeId == (int)KinaUnaTypes.TimeLineType.TodoItem)
            {
                if (idParse)
                {
                    TodoItem todoItem = await todoItemsHttpClient.GetTodoItem(itemId);
                    if (todoItem != null && todoItem.TodoItemId > 0)
                    {
                        if (todoItem.DueDate.HasValue)
                        {
                            todoItem.DueDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.DueDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        }

                        if (todoItem.CompletedDate.HasValue)
                        {
                            todoItem.CompletedDate = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CompletedDate.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                        }
                        todoItem.CreatedTime = TimeZoneInfo.ConvertTimeFromUtc(todoItem.CreatedTime, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));

                        if (todoItem.ProgenyId > 0)
                        {
                            todoItem.Progeny = await progenyHttpClient.GetProgeny(todoItem.ProgenyId);
                        }

                        if (todoItem.FamilyId > 0)
                        {
                            todoItem.Family = await familiesHttpClient.GetFamily(todoItem.FamilyId);
                        }

                        return new TimeLineItemPartialViewModel("_TimeLineTodoItemPartial", todoItem);
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
