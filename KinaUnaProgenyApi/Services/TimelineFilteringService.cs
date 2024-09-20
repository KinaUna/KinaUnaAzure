using System;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Filters TimeLineItems based on tags, categories, contexts and keywords.
    /// </summary>
    /// <param name="progenyContext"></param>
    /// <param name="mediaContext"></param>
    public class TimelineFilteringService(ProgenyDbContext progenyContext, MediaDbContext mediaContext) : ITimelineFilteringService
    {
        /// <summary>
        /// Filters TimeLineItems based on tags.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="tags">Comma separated list of tags.</param>
        /// <returns>List of TimeLineItems that contain any of the tags.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithTags(List<TimeLineItem> timeLineItems, string tags)
        {
            if (string.IsNullOrEmpty(tags)) return timeLineItems;

            List<string> tagsList = [.. tags.Split(',')];
            tagsList = tagsList.Select(t => t.Trim()).ToList();

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
                        if (!picture.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) continue;
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
                        if (!video.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) continue;
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
                        if (!friend.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) continue;
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
                        if (!contact.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) continue;
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
                        if (!location.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
            }

            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters TimeLineItems based on categories.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="categories">Comma separated list of categories.</param>
        /// <returns>List of TimeLineItems that contain any of the categories</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithCategories(List<TimeLineItem> timeLineItems, string categories)
        {
            if (string.IsNullOrEmpty(categories)) return timeLineItems;

            List<string> categoriesList = [.. categories.Split(',')];
            categoriesList = categoriesList.Select(t => t.Trim()).ToList();

            List<TimeLineItem> filteredTimeLineItems = [];
            foreach (TimeLineItem timeLineItem in timeLineItems)
            {
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Skill)
                {
                    bool skillIdValid = int.TryParse(timeLineItem.ItemId, out int skillId);
                    if (!skillIdValid) continue;
                    Skill skill = await progenyContext.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == skillId);
                    if (skill == null || skill.Category == null) continue;
                    foreach (string category in categoriesList)
                    {
                        if (!skill.Category.Contains(category, StringComparison.CurrentCultureIgnoreCase)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
                
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Note)
                {
                    bool noteIdValid = int.TryParse(timeLineItem.ItemId, out int noteId);
                    if (!noteIdValid) continue;
                    Note note = await progenyContext.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == noteId);
                    if (note == null || note.Category == null) continue;
                    foreach (string category in categoriesList)
                    {
                        if (!note.Category.Contains(category, StringComparison.CurrentCultureIgnoreCase)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
            }

            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters TimeLineItems based on contexts.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="contexts">Comma separated list of contexts</param>
        /// <returns>List of TimeLineItems that contain any of the contexts.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithContexts(List<TimeLineItem> timeLineItems, string contexts)
        {
            if (string.IsNullOrEmpty(contexts)) return timeLineItems;

            List<string> contextsList = [.. contexts.Split(',')];
            contextsList = contextsList.Select(t => t.Trim()).ToList();

            List<TimeLineItem> filteredTimeLineItems = [];
            foreach (TimeLineItem timeLineItem in timeLineItems)
            {
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar)
                {
                    bool calendarIdValid = int.TryParse(timeLineItem.ItemId, out int calendarId);
                    if (!calendarIdValid) continue;
                    CalendarItem calendarItem = await progenyContext.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == calendarId);
                    if (calendarItem == null || calendarItem.Context == null) continue;
                    foreach (string context in contextsList)
                    {
                        if (!calendarItem.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
                
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Friend)
                {
                    bool friendIdValid = int.TryParse(timeLineItem.ItemId, out int friendId);
                    if (!friendIdValid) continue;
                    Friend friend = await progenyContext.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == friendId);
                    if (friend == null || friend.Context == null) continue;
                    foreach (string context in contextsList)
                    {
                        if (!friend.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
                
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Contact)
                {
                    bool contactIdValid = int.TryParse(timeLineItem.ItemId, out int contactId);
                    if (!contactIdValid) continue;
                    Contact contact = await progenyContext.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == contactId);
                    if (contact == null || contact.Context == null) continue;
                    foreach (string context in contextsList)
                    {
                        if (!contact.Context.Contains(context, StringComparison.CurrentCultureIgnoreCase)) continue;
                        filteredTimeLineItems.Add(timeLineItem);
                        break;
                    }
                }
            }

            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters TimeLineItems based on keywords.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="keywords">Comma separated list of keywords.</param>
        /// <returns>List of TimeLineItems that contain any of the keywords.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithKeyword(List<TimeLineItem> timeLineItems, string keywords)
        {
            if (string.IsNullOrEmpty(keywords)) return timeLineItems;
            List<string> keywordsList = [.. keywords.Split(',')];
            keywordsList = keywordsList.Select(t => t.Trim()).ToList();

            List<TimeLineItem> filteredTimeLineItems = [];
            foreach (TimeLineItem timeLineItem in timeLineItems)
            {
                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Photo)
                {
                    bool pictureIdValid = int.TryParse(timeLineItem.ItemId, out int pictureId);
                    if (!pictureIdValid) continue;
                    Picture picture = await mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == pictureId);
                    if (picture == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((picture.Tags != null && picture.Tags.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (picture.Location != null && picture.Location.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Video)
                {
                    bool videoIdValid = int.TryParse(timeLineItem.ItemId, out int videoId);
                    if (!videoIdValid) continue;
                    Video video = await mediaContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == videoId);
                    if (video == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((video.Tags != null && video.Tags.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (video.Location != null && video.Location.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar)
                {
                    bool calendarIdValid = int.TryParse(timeLineItem.ItemId, out int calendarId);
                    if (!calendarIdValid) continue;
                    CalendarItem calendarItem = await progenyContext.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == calendarId);
                    if (calendarItem == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((calendarItem.Context != null && calendarItem.Context.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)) 
                            || (calendarItem.Location != null && calendarItem.Location.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Vocabulary)
                {
                    bool wordIdValid = int.TryParse(timeLineItem.ItemId, out int wordId);
                    if (!wordIdValid) continue;
                    VocabularyItem vocabularyItem = await progenyContext.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == wordId);
                    if (vocabularyItem == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((vocabularyItem.Word != null && vocabularyItem.Word.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (vocabularyItem.Language != null && vocabularyItem.Language.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Skill)
                {
                    bool skillIdValid = int.TryParse(timeLineItem.ItemId, out int skillId);
                    if (!skillIdValid) continue;
                    Skill skill = await progenyContext.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == skillId);
                    if (skill == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((skill.Name != null && skill.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)) 
                            || (skill.Category != null && skill.Category.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Friend)
                {
                    bool friendIdValid = int.TryParse(timeLineItem.ItemId, out int friendId);
                    if (!friendIdValid) continue;
                    Friend friend = await progenyContext.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == friendId);
                    if (friend == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((friend.Tags != null && friend.Tags.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (friend.Context != null && friend.Context.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
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
                    foreach (string keyword in keywordsList)
                    {
                        if ((note.Title != null && note.Title.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (note.Content != null && note.Content.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (note.Category != null && note.Category.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Contact)
                {
                    bool contactIdValid = int.TryParse(timeLineItem.ItemId, out int contactId);
                    if (!contactIdValid) continue;
                    Contact contact = await progenyContext.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == contactId);
                    if (contact == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((contact.Tags != null && contact.Tags.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (contact.Context != null && contact.Context.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Vaccination)
                {
                    bool vaccinationIdValid = int.TryParse(timeLineItem.ItemId, out int vaccinationId);
                    if (!vaccinationIdValid) continue;
                    Vaccination vaccination = await progenyContext.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == vaccinationId);
                    if (vaccination == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if (vaccination.VaccinationName != null && vaccination.VaccinationName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }

                if (timeLineItem.ItemType == (int)KinaUnaTypes.TimeLineType.Location)
                {
                    bool locationIdValid = int.TryParse(timeLineItem.ItemId, out int locationId);
                    if (!locationIdValid) continue;
                    Location location = await progenyContext.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == locationId);
                    if (location == null) continue;
                    foreach (string keyword in keywordsList)
                    {
                        if ((location.Tags != null && location.Tags.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                            || (location.Name != null && location.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            filteredTimeLineItems.Add(timeLineItem);
                            break;
                        }
                    }
                }
            }

            return filteredTimeLineItems;
        }
    }
}
