using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public class TimelineFilteringService(ProgenyDbContext progenyContext, MediaDbContext mediaContext) : ITimelineFilteringService
    {
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithTags(List<TimeLineItem> timeLineItems, string tags)
        {
            if (string.IsNullOrEmpty(tags)) return timeLineItems;

            List<string> tagsList = [.. tags.Split(',')];

            List<TimeLineItem> filteredTimeLineItems = [];
            foreach (TimeLineItem timeLineItem in timeLineItems)
            {
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Photo)
                {
                    bool pictureIdValid = int.TryParse(timeLineItem.ItemId, out int pictureId);
                    if (!pictureIdValid) continue;
                    Picture picture = await mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == pictureId);
                    if (picture == null || picture.Tags == null) continue;
                    foreach (string tag in tagsList)
                    {
                        if (!picture.Tags.Contains(tag)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Video)
                {
                    bool videoIdValid = int.TryParse(timeLineItem.ItemId, out int videoId);
                    if (!videoIdValid) continue;
                    Video video = await mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == videoId);
                    if (video == null || video.Tags == null) continue;
                    foreach (string tag in tagsList)
                    {
                        if (!video.Tags.Contains(tag)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Friend)
                {
                    bool friendIdValid = int.TryParse(timeLineItem.ItemId, out int friendId);
                    if (!friendIdValid) continue;
                    Friend friend = await progenyContext.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == friendId);
                    if (friend == null || friend.Tags == null) continue;
                    foreach (string tag in tagsList)
                    {
                        if (!friend.Tags.Contains(tag)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Contact)
                {
                    bool contactIdValid = int.TryParse(timeLineItem.ItemId, out int contactId);
                    if (!contactIdValid) continue;
                    Contact contact = await progenyContext.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == contactId);
                    if (contact == null || contact.Tags == null) continue;
                    foreach (string tag in tagsList)
                    {
                        if (!contact.Tags.Contains(tag)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Location)
                {
                    bool locationIdValid = int.TryParse(timeLineItem.ItemId, out int locationId);
                    if (!locationIdValid) continue;
                    Location location = await progenyContext.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == locationId);
                    if (location == null || location.Tags == null) continue;
                    foreach (string tag in tagsList)
                    {
                        if (!location.Tags.Contains(tag)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
            }

            return filteredTimeLineItems;
        }

        public async Task<List<TimeLineItem>> GetTimeLineItemsWithKeyword(List<TimeLineItem> timeLineItems, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return timeLineItems;

            List<TimeLineItem> filteredTimeLineItems = [];
            foreach (TimeLineItem timeLineItem in timeLineItems)
            {
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Photo)
                {
                    bool pictureIdValid = int.TryParse(timeLineItem.ItemId, out int pictureId);
                    if (!pictureIdValid) continue;
                    Picture picture = await mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == pictureId);
                    if (picture == null) continue;
                    if (picture.Tags.Contains(keyword) || picture.Location.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Video)
                {
                    bool videoIdValid = int.TryParse(timeLineItem.ItemId, out int videoId);
                    if (!videoIdValid) continue;
                    Video video = await mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == videoId);
                    if (video == null) continue;
                    if (video.Tags.Contains(keyword) || video.Location.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar)
                {
                    bool calendarIdValid = int.TryParse(timeLineItem.ItemId, out int calendarId);
                    if (!calendarIdValid) continue;
                    CalendarItem calendarItem = await progenyContext.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == calendarId);
                    if (calendarItem == null) continue;
                    if (calendarItem.Context.Contains(keyword) || calendarItem.Location.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Vocabulary)
                {
                    bool wordIdValid = int.TryParse(timeLineItem.ItemId, out int wordId);
                    if (!wordIdValid) continue;
                    VocabularyItem vocabularyItem = await progenyContext.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == wordId);
                    if (vocabularyItem == null) continue;
                    if (vocabularyItem.Word.Contains(keyword) || vocabularyItem.Language.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Skill)
                {
                    bool skillIdValid = int.TryParse(timeLineItem.ItemId, out int skillId);
                    if (!skillIdValid) continue;
                    Skill skill = await progenyContext.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == skillId);
                    if (skill == null) continue;
                    if (skill.Name.Contains(keyword) || skill.Category.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Friend)
                {
                    bool friendIdValid = int.TryParse(timeLineItem.ItemId, out int friendId);
                    if (!friendIdValid) continue;
                    Friend friend = await progenyContext.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == friendId);
                    if (friend == null) continue;
                    if (friend.Tags.Contains(keyword) || friend.Context.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Measurement)
                {
                    continue;
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Sleep)
                {
                    continue;
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Note)
                {
                    bool noteIdValid = int.TryParse(timeLineItem.ItemId, out int noteId);
                    if (!noteIdValid) continue;
                    Note note = await progenyContext.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == noteId);
                    if (note == null) continue;
                    if (note.Category.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Contact)
                {
                    bool contactIdValid = int.TryParse(timeLineItem.ItemId, out int contactId);
                    if (!contactIdValid) continue;
                    Contact contact = await progenyContext.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == contactId);
                    if (contact == null) continue;
                    if (contact.Tags.Contains(keyword) || contact.Context.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Vaccination)
                {
                    bool vaccinationIdValid = int.TryParse(timeLineItem.ItemId, out int vaccinationId);
                    if (!vaccinationIdValid) continue;
                    Vaccination vaccination = await progenyContext.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == vaccinationId);
                    if (vaccination == null) continue;
                    if (vaccination.VaccinationName.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Location)
                {
                    bool locationIdValid = int.TryParse(timeLineItem.ItemId, out int locationId);
                    if (!locationIdValid) continue;
                    Location location = await progenyContext.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == locationId);
                    if (location == null) continue;
                    if (location.Tags.Contains(keyword) || location.Name.Contains(keyword))
                    {
                        filteredTimeLineItems.Add(timeLineItem);
                    }
                }
            }

            return filteredTimeLineItems;
        }
    }
}
