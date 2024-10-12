using System;
using KinaUna.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUnaProgenyApi.Services.CalendarServices;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Filters TimeLineItems based on tags, categories, contexts and keywords.
    /// </summary>
    public class TimelineFilteringService(IPicturesService picturesService, IVideosService videosService, IFriendService friendService,
        IContactService contactService, ILocationService locationService, ISkillService skillService, INoteService noteService,
        ICalendarService calendarService, IVocabularyService vocabularyService, IVaccinationService vaccinationService) : ITimelineFilteringService
    {
        /// <summary>
        /// Filters TimeLineItems based on tags.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="tags">Comma separated list of tags.</param>
        /// <param name="accessLevel">The required access level to view the items.</param>
        /// <returns>List of TimeLineItems that contain any of the tags.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithTags(List<TimeLineItem> timeLineItems, string tags, int accessLevel)
        {
            if (string.IsNullOrEmpty(tags)) return timeLineItems;
            
            List<TimeLineItem> filteredTimeLineItems = [];

            List<string> tagsList = [.. tags.Split(',')];
            tagsList = tagsList.Select(t => t.Trim()).ToList();

            int progenyId = timeLineItems.FirstOrDefault()?.ProgenyId ?? Constants.DefaultChildId;

            foreach (string tag in tagsList)
            {
                List<Picture> allPictureItems = await picturesService.GetPicturesWithTag(progenyId, tag, accessLevel);
                List<TimeLineItem> allTimeLinePictureItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Photo).ToList();
                List<TimeLineItem> allTimeLinePictureItemsInTimeLineItems = allTimeLinePictureItems.Where(t => allPictureItems.Any(p => p.PictureId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLinePictureItemsInTimeLineItems);

                List<Video> allVideoItems = await videosService.GetVideosWithTag(progenyId, tag, accessLevel);
                List<TimeLineItem> allTimeLineVideoItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Video).ToList();
                List<TimeLineItem> allTimeLineVideoItemsInTimeLineItems = allTimeLineVideoItems.Where(t => allVideoItems.Any(v => v.VideoId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineVideoItemsInTimeLineItems);

                List<Friend> allFriendItems = await friendService.GetFriendsWithTag(progenyId, tag, accessLevel);
                List<TimeLineItem> allTimeLineFriendItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Friend).ToList();
                List<TimeLineItem> allTimeLineFriendItemsInTimeLineItems = allTimeLineFriendItems.Where(t => allFriendItems.Any(f => f.FriendId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineFriendItemsInTimeLineItems);

                List<Contact> allContactItems = await contactService.GetContactsWithTag(progenyId, tag, accessLevel);
                List<TimeLineItem> allTimeLineContactItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Contact).ToList();
                List<TimeLineItem> allTimeLineContactItemsInTimeLineItems = allTimeLineContactItems.Where(t => allContactItems.Any(c => c.ContactId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineContactItemsInTimeLineItems);

                List<Location> allLocationItems = await locationService.GetLocationsWithTag(progenyId, tag, accessLevel);
                List<TimeLineItem> allTimeLineLocationItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Location).ToList();
                List<TimeLineItem> allTimeLineLocationItemsInTimeLineItems = allTimeLineLocationItems.Where(t => allLocationItems.Any(l => l.LocationId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineLocationItemsInTimeLineItems);

            }
            
            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters TimeLineItems based on categories.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="categories">Comma separated list of categories.</param>
        /// <param name="accessLevel">The required access level to view the items.</param>
        /// <returns>List of TimeLineItems that contain any of the categories</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithCategories(List<TimeLineItem> timeLineItems, string categories, int accessLevel)
        {
            if (string.IsNullOrEmpty(categories)) return timeLineItems;

            List<string> categoriesList = [.. categories.Split(',')];
            categoriesList = categoriesList.Select(t => t.Trim()).ToList();

            List<TimeLineItem> filteredTimeLineItems = [];

            int progenyId = timeLineItems.FirstOrDefault()?.ProgenyId ?? Constants.DefaultChildId;
            foreach (string category in categoriesList)
            {
                List<Skill> allSkillItems = await skillService.GetSkillsWithCategory(progenyId, category, accessLevel);
                List<TimeLineItem> allTimeLineSkillItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Skill).ToList();
                List<TimeLineItem> allTimeLineSkillItemsInTimeLineItems = allTimeLineSkillItems.Where(t => allSkillItems.Any(s => s.SkillId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineSkillItemsInTimeLineItems);

                List<Note> allNoteItems = await noteService.GetNotesWithCategory(progenyId, category, accessLevel);
                List<TimeLineItem> allTimeLineNoteItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Note).ToList();
                List<TimeLineItem> allTimeLineNoteItemsInTimeLineItems = allTimeLineNoteItems.Where(t => allNoteItems.Any(n => n.NoteId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineNoteItemsInTimeLineItems);
            }
            
            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters TimeLineItems based on contexts.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="contexts">Comma separated list of contexts</param>
        /// <param name="accessLevel">The required access level to view the items.</param>
        /// <returns>List of TimeLineItems that contain any of the contexts.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithContexts(List<TimeLineItem> timeLineItems, string contexts, int accessLevel)
        {
            if (string.IsNullOrEmpty(contexts)) return timeLineItems;

            List<string> contextsList = [.. contexts.Split(',')];
            contextsList = contextsList.Select(t => t.Trim()).ToList();

            List<TimeLineItem> filteredTimeLineItems = [];

            int progenyId = timeLineItems.FirstOrDefault()?.ProgenyId ?? Constants.DefaultChildId;

            foreach (string context in contextsList)
            {
                List<CalendarItem> allCalendarItems = await calendarService.GetCalendarItemsWithContext(progenyId, context, accessLevel);
                List<TimeLineItem> allTimeLineCalendarItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Calendar).ToList();
                List<TimeLineItem> allTimeLineCalendarItemsInTimeLineItems = allTimeLineCalendarItems.Where(t => allCalendarItems.Any(c => c.EventId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineCalendarItemsInTimeLineItems);

                List<Friend> allFriendItems = await friendService.GetFriendsWithContext(progenyId, context, accessLevel);
                List<TimeLineItem> allTimeLineFriendItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Friend).ToList();
                List<TimeLineItem> allTimeLineFriendItemsInTimeLineItems = allTimeLineFriendItems.Where(t => allFriendItems.Any(f => f.FriendId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineFriendItemsInTimeLineItems);

                List<Contact> allContactItems = await contactService.GetContactsWithContext(progenyId, context, accessLevel);
                List<TimeLineItem> allTimeLineContactItems = timeLineItems.Where(t => t.ItemType == (int)KinaUnaTypes.TimeLineType.Contact).ToList();
                List<TimeLineItem> allTimeLineContactItemsInTimeLineItems = allTimeLineContactItems.Where(t => allContactItems.Any(c => c.ContactId == int.Parse(t.ItemId))).ToList();
                filteredTimeLineItems.AddRange(allTimeLineContactItemsInTimeLineItems);
            }
            
            return filteredTimeLineItems;
        }

        /// <summary>
        /// Filters TimeLineItems based on keywords.
        /// </summary>
        /// <param name="timeLineItems">The list of items to filter.</param>
        /// <param name="keywords">Comma separated list of keywords.</param>
        /// <param name="accessLevel">The required access level to view the items.</param>
        /// <returns>List of TimeLineItems that contain any of the keywords.</returns>
        public async Task<List<TimeLineItem>> GetTimeLineItemsWithKeyword(List<TimeLineItem> timeLineItems, string keywords, int accessLevel)
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
                    Picture picture = await picturesService.GetPicture(pictureId);
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
                    Video video = await videosService.GetVideo(videoId);
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
                    CalendarItem calendarItem = await calendarService.GetCalendarItem(calendarId);
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
                    VocabularyItem vocabularyItem = await vocabularyService.GetVocabularyItem(wordId);
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
                    Skill skill = await skillService.GetSkill(skillId);
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
                    Friend friend = await friendService.GetFriend(friendId);
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
                    Note note = await noteService.GetNote(noteId);
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
                    Contact contact = await contactService.GetContact(contactId);
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
                    Vaccination vaccination = await vaccinationService.GetVaccination(vaccinationId);
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
                    Location location = await locationService.GetLocation(locationId);
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
