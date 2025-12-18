using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Search;
using KinaUnaProgenyApi.Services.AccessManagementService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.Search
{
    /// <summary>
    /// Service for searching entities across progenies and families with access control.
    /// </summary>
    public class SearchService(
        ProgenyDbContext progenyDbContext,
        MediaDbContext mediaDbContext,
        IAccessManagementService accessManagementService,
        ITimelineService timelineService) : ISearchService
    {
        public async Task<SearchResponse<TimeLineItem>> QuickSearch(SearchRequest request, UserInfo currentUserInfo)
        {
            SearchResponse<TimeLineItem> response = new SearchResponse<TimeLineItem> { SearchRequest = request };
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return response;
            }

            string query = request.Query.Trim().ToLower();
            List<TimeLineItem> accessibleItems = [];
            
            // Search in progenies
            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;
                List<TimeLineItem> progenyTimelineList = await timelineService.GetTimeLineList(progenyId, 0, currentUserInfo);
                accessibleItems.AddRange(progenyTimelineList);
            }

            // Search in families
            foreach (int familyId in request.FamilyIds)
            {
                if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<TimeLineItem> familyTimelineList = await timelineService.GetTimeLineList(0, familyId, currentUserInfo);
                accessibleItems.AddRange(familyTimelineList);
            }

            
            if (request.Sort == 0)
            {
                accessibleItems = accessibleItems.OrderByDescending(i => i.ProgenyTime).ToList();
            }
            else
            {
                accessibleItems = accessibleItems.OrderBy(i => i.ProgenyTime).ToList();
            }

            List<TimeLineItem> finalItems = [];
            int itemsFoundCount = 0;
            for (int i = 0; itemsFoundCount <= request.NumberOfItems + request.Skip && i < accessibleItems.Count; i++)
            {
                TimeLineItem item = accessibleItems[i];
                
                switch (item.ItemType)
                {
                    case (int)KinaUnaTypes.TimeLineType.Calendar:
                        CalendarItem calendarItem = await progenyDbContext.CalendarDb.FindAsync(int.Parse(item.ItemId));
                        if (calendarItem != null &&
                            ((calendarItem.Title ?? string.Empty).ToLower().Contains(query)
                            || (calendarItem.Notes ?? string.Empty).ToLower().Contains(query)
                            || (calendarItem.Location ?? string.Empty).ToLower().Contains(query)
                            || (calendarItem.Context ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Contact:
                                                Contact contactItem = await progenyDbContext.ContactsDb.FindAsync(int.Parse(item.ItemId));
                        if (contactItem != null &&
                            ((contactItem.FirstName ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.MiddleName ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.LastName ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.DisplayName ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.Email1 ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.Email2 ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.Context ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.Notes ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.Website ?? string.Empty).ToLower().Contains(query)
                            || (contactItem.Tags ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Friend:
                        Friend friendItem = await progenyDbContext.FriendsDb.FindAsync(int.Parse(item.ItemId));
                        if (friendItem != null &&
                            ((friendItem.Name ?? string.Empty).ToLower().Contains(query)
                            || (friendItem.Description ?? string.Empty).ToLower().Contains(query)
                            || (friendItem.Context ?? string.Empty).ToLower().Contains(query)
                            || (friendItem.Notes ?? string.Empty).ToLower().Contains(query)
                            || (friendItem.Tags ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.KanbanBoard:
                        KanbanBoard kanbanBoardItem = await progenyDbContext.KanbanBoardsDb.FindAsync(int.Parse(item.ItemId));
                        if (kanbanBoardItem != null &&
                            ((kanbanBoardItem.Title ?? string.Empty).ToLower().Contains(query)
                            || (kanbanBoardItem.DescriptionAsPlainText() ?? string.Empty).ToLower().Contains(query)
                            || (kanbanBoardItem.Tags ?? string.Empty).ToLower().Contains(query)
                            || (kanbanBoardItem.Context ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Location:
                        Location locationItem = await progenyDbContext.LocationsDb.FindAsync(int.Parse(item.ItemId));
                        if (locationItem != null &&
                            ((locationItem.Name ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.StreetName ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.City ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.District ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.County ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.State ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.Country ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.PostalCode ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.Notes ?? string.Empty).ToLower().Contains(query)
                            || (locationItem.Tags ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Measurement:
                        // Measurement items are not included in quick search.
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Note:
                        Note noteItem = await progenyDbContext.NotesDb.FindAsync(int.Parse(item.ItemId));
                        if (noteItem != null &&
                            ((noteItem.Title ?? string.Empty).ToLower().Contains(query)
                            || (noteItem.ContentAsPlainText() ?? string.Empty).ToLower().Contains(query)
                            || (noteItem.Category ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Photo:
                        Picture pictureItem = await mediaDbContext.PicturesDb.FindAsync(int.Parse(item.ItemId));
                        if (pictureItem != null &&
                            ((pictureItem.Tags ?? string.Empty).ToLower().Contains(query)
                            || (pictureItem.Location ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Skill:
                        Skill skillItem = await progenyDbContext.SkillsDb.FindAsync(int.Parse(item.ItemId));
                        if (skillItem != null &&
                            ((skillItem.Name ?? string.Empty).ToLower().Contains(query)
                            || (skillItem.Description ?? string.Empty).ToLower().Contains(query)
                            || (skillItem.Category ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Sleep:
                        // Sleep items are not included in quick search.
                        break;
                    case (int)KinaUnaTypes.TimeLineType.TodoItem:
                        TodoItem todoItem = await progenyDbContext.TodoItemsDb.FindAsync(int.Parse(item.ItemId));
                        if (todoItem != null &&
                            ((todoItem.Title ?? string.Empty).ToLower().Contains(query)
                            || (todoItem.DescriptionAsPlainText() ?? string.Empty).ToLower().Contains(query)
                            || (todoItem.Context ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Vaccination:
                        Vaccination vaccinationItem = await progenyDbContext.VaccinationsDb.FindAsync(int.Parse(item.ItemId));
                        if (vaccinationItem != null &&
                            ((vaccinationItem.VaccinationName ?? string.Empty).ToLower().Contains(query)
                            || (vaccinationItem.VaccinationDescription ?? string.Empty).ToLower().Contains(query)
                            || (vaccinationItem.Notes ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Video:
                        Video videoItem = await mediaDbContext.VideoDb.FindAsync(int.Parse(item.ItemId));
                        if (videoItem != null &&
                            ((videoItem.Tags ?? string.Empty).ToLower().Contains(query)
                            || (videoItem.Location ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                    case (int)KinaUnaTypes.TimeLineType.Vocabulary:
                        VocabularyItem vocabularyItem = await progenyDbContext.VocabularyDb.FindAsync(int.Parse(item.ItemId));
                        if (vocabularyItem != null &&
                            ((vocabularyItem.Word ?? string.Empty).ToLower().Contains(query)
                            || (vocabularyItem.Word ?? string.Empty).ToLower().Contains(query)
                            || (vocabularyItem.Description ?? string.Empty).ToLower().Contains(query)
                            || (vocabularyItem.SoundsLike ?? string.Empty).ToLower().Contains(query)))
                        {
                            finalItems.Add(item);
                            itemsFoundCount++;
                        }
                        break;
                }
            }


            if (finalItems.Count > request.Skip + request.NumberOfItems)
            {
                response.RemainingItems = 1;
            }
            finalItems = finalItems.Skip(request.Skip).Take(request.NumberOfItems).ToList();
            response.Results = finalItems;

            return response ;
        }

        #region Calendar Items

        public async Task<SearchResponse<CalendarItem>> SearchCalendarItems(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<CalendarItem> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<CalendarItem> accessibleItems = [];

            // Search in progenies
            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<CalendarItem> items = await progenyDbContext.CalendarDb
                    .AsNoTracking()
                    .Where(c => c.ProgenyId == progenyId && c.FamilyId == 0
                        && (c.Title.ToLower().Contains(query)
                            || c.Notes.ToLower().Contains(query)
                            || c.Location.ToLower().Contains(query)
                            || c.Context.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (CalendarItem item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, item.EventId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Calendar, item.EventId, item.ProgenyId, item.FamilyId, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            // Search in families
            foreach (int familyId in request.FamilyIds)
            {
                if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<CalendarItem> items = await progenyDbContext.CalendarDb
                    .AsNoTracking()
                    .Where(c => c.FamilyId == familyId && c.ProgenyId == 0
                        && (c.Title.ToLower().Contains(query)
                            || c.Notes.ToLower().Contains(query)
                            || c.Location.ToLower().Contains(query)
                            || c.Context.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (CalendarItem item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, item.EventId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Calendar, item.EventId, item.ProgenyId, item.FamilyId, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.StartTime ?? i.CreatedTime);
        }

        #endregion

        #region Contacts

        public async Task<SearchResponse<Contact>> SearchContacts(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Contact> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Contact> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Contact> items = await progenyDbContext.ContactsDb
                    .AsNoTracking()
                    .Where(c => c.ProgenyId == progenyId && c.FamilyId == 0
                        && (c.FirstName.ToLower().Contains(query)
                            || c.MiddleName.ToLower().Contains(query)
                            || c.LastName.ToLower().Contains(query)
                            || c.DisplayName.ToLower().Contains(query)
                            || c.Email1.ToLower().Contains(query)
                            || c.Email2.ToLower().Contains(query)
                            || c.Context.ToLower().Contains(query)
                            || c.Notes.ToLower().Contains(query)
                            || c.Website.ToLower().Contains(query)
                            || c.Tags.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Contact item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, item.ContactId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Contact, item.ContactId, item.ProgenyId, item.FamilyId, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            foreach (int familyId in request.FamilyIds)
            {
                if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Contact> items = await progenyDbContext.ContactsDb
                    .AsNoTracking()
                    .Where(c => c.FamilyId == familyId && c.ProgenyId == 0
                        && (c.FirstName.ToLower().Contains(query)
                            || c.MiddleName.ToLower().Contains(query)
                            || c.LastName.ToLower().Contains(query)
                            || c.DisplayName.ToLower().Contains(query)
                            || c.Email1.ToLower().Contains(query)
                            || c.Email2.ToLower().Contains(query)
                            || c.Context.ToLower().Contains(query)
                            || c.Notes.ToLower().Contains(query)
                            || c.Website.ToLower().Contains(query)
                            || c.Tags.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Contact item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, item.ContactId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Contact, item.ContactId, item.ProgenyId, item.FamilyId, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.DateAdded ?? i.CreatedTime);
        }

        #endregion

        #region Friends

        public async Task<SearchResponse<Friend>> SearchFriends(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Friend> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Friend> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Friend> items = await progenyDbContext.FriendsDb
                    .AsNoTracking()
                    .Where(f => f.ProgenyId == progenyId
                        && ((f.Name ?? string.Empty).ToLower().Contains(query)
                            || (f.Description ?? string.Empty).ToLower().Contains(query)
                            || (f.Context ?? string.Empty).ToLower().Contains(query)
                            || (f.Notes ?? string.Empty).ToLower().Contains(query)
                            || (f.Tags ?? string.Empty).ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Friend item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, item.FriendId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Friend, item.FriendId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.FriendAddedDate);
        }

        #endregion

        #region Kanban Boards

        public async Task<SearchResponse<KanbanBoard>> SearchKanbanBoards(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<KanbanBoard> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<KanbanBoard> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<KanbanBoard> items = await progenyDbContext.KanbanBoardsDb
                    .AsNoTracking()
                    .Where(k => k.ProgenyId == progenyId && k.FamilyId == 0 && !k.IsDeleted
                        && (k.Title.ToLower().Contains(query)
                            || k.Description.ToLower().Contains(query)
                            || k.Tags.ToLower().Contains(query)
                            || k.Context.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (KanbanBoard item in items)
                {
                    
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, item.KanbanBoardId, currentUserInfo, PermissionLevel.View))
                    {
                        // Check also description as plain text, as some HTML tags may match the query.
                        if (item.Title.ToLower().Contains(query)
                            || item.DescriptionAsPlainText().ToLower().Contains(query)
                            || item.Tags.ToLower().Contains(query)
                            || item.Context.ToLower().Contains(query))
                        {
                            item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                                KinaUnaTypes.TimeLineType.KanbanBoard, item.KanbanBoardId, item.ProgenyId, item.FamilyId, currentUserInfo);
                            accessibleItems.Add(item);
                        }
                    }
                }
            }

            foreach (int familyId in request.FamilyIds)
            {
                if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<KanbanBoard> items = await progenyDbContext.KanbanBoardsDb
                    .AsNoTracking()
                    .Where(k => k.FamilyId == familyId && k.ProgenyId == 0 && !k.IsDeleted
                        && (k.Title.ToLower().Contains(query)
                            || k.Description.ToLower().Contains(query)
                            || k.Tags.ToLower().Contains(query)
                            || k.Context.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (KanbanBoard item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, item.KanbanBoardId, currentUserInfo, PermissionLevel.View))
                    {
                        // Check also description as plain text, as some HTML tags may match the query.
                        if (item.Title.ToLower().Contains(query)
                            || item.DescriptionAsPlainText().ToLower().Contains(query)
                            || item.Tags.ToLower().Contains(query)
                            || item.Context.ToLower().Contains(query))
                        {
                            item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                                KinaUnaTypes.TimeLineType.KanbanBoard, item.KanbanBoardId, item.ProgenyId, item.FamilyId, currentUserInfo);
                            accessibleItems.Add(item);
                        }
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.CreatedTime);
        }

        #endregion

        #region Locations

        public async Task<SearchResponse<Location>> SearchLocations(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Location> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Location> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Location> items = await progenyDbContext.LocationsDb
                    .AsNoTracking()
                    .Where(l => l.ProgenyId == progenyId && l.FamilyId == 0
                        && (l.Name.ToLower().Contains(query)
                            || l.StreetName.ToLower().Contains(query)
                            || l.City.ToLower().Contains(query)
                            || l.District.ToLower().Contains(query)
                            || l.County.ToLower().Contains(query)
                            || l.State.ToLower().Contains(query)
                            || l.Country.ToLower().Contains(query)
                            || l.PostalCode.ToLower().Contains(query)
                            || l.Notes.ToLower().Contains(query)
                            || l.Tags.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Location item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Location, item.LocationId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Location, item.LocationId, item.ProgenyId, item.FamilyId, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            foreach (int familyId in request.FamilyIds)
            {
                if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Location> items = await progenyDbContext.LocationsDb
                    .AsNoTracking()
                    .Where(l => l.FamilyId == familyId && l.ProgenyId == 0
                        && (l.Name.ToLower().Contains(query)
                            || l.StreetName.ToLower().Contains(query)
                            || l.City.ToLower().Contains(query)
                            || l.District.ToLower().Contains(query)
                            || l.County.ToLower().Contains(query)
                            || l.State.ToLower().Contains(query)
                            || l.Country.ToLower().Contains(query)
                            || l.PostalCode.ToLower().Contains(query)
                            || l.Notes.ToLower().Contains(query)
                            || l.Tags.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Location item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Location, item.LocationId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Location, item.LocationId, item.ProgenyId, item.FamilyId, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.Date ?? i.CreatedTime);
        }

        #endregion

        #region Measurements

        public async Task<SearchResponse<Measurement>> SearchMeasurements(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Measurement> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Measurement> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Measurement> items = await progenyDbContext.MeasurementsDb
                    .AsNoTracking()
                    .Where(m => m.ProgenyId == progenyId
                        && ((m.EyeColor ?? string.Empty).ToLower().Contains(query)
                            || (m.HairColor ?? string.Empty).ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Measurement item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Measurement, item.MeasurementId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Measurement, item.MeasurementId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.Date);
        }

        #endregion

        #region Notes

        public async Task<SearchResponse<Note>> SearchNotes(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Note> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Note> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Note> items = await progenyDbContext.NotesDb
                    .AsNoTracking()
                    .Where(n => n.ProgenyId == progenyId
                        && (n.Title.ToLower().Contains(query)
                            || n.Content.ToLower().Contains(query)
                            || n.Category.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Note item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Note, item.NoteId, currentUserInfo, PermissionLevel.View))
                    {
                        // Check also content as plain text, as some HTML tags may match the query.
                        if (item.Title.ToLower().Contains(query)
                            || item.ContentAsPlainText().ToLower().Contains(query)
                            || item.Category.ToLower().Contains(query))
                        {
                            item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                                KinaUnaTypes.TimeLineType.Note, item.NoteId, item.ProgenyId, 0, currentUserInfo);
                            accessibleItems.Add(item);
                        }
                        
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.CreatedDate);
        }

        #endregion

        #region Pictures

        public async Task<SearchResponse<Picture>> SearchPictures(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Picture> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Picture> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Picture> items = await mediaDbContext.PicturesDb
                    .AsNoTracking()
                    .Where(p => p.ProgenyId == progenyId
                        && (p.Tags.ToLower().Contains(query)
                            || p.Location.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Picture item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, item.PictureId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Photo, item.PictureId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.PictureTime ?? i.CreatedTime);
        }

        #endregion

        #region Skills

        public async Task<SearchResponse<Skill>> SearchSkills(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Skill> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Skill> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Skill> items = await progenyDbContext.SkillsDb
                    .AsNoTracking()
                    .Where(s => s.ProgenyId == progenyId
                        && (s.Name.ToLower().Contains(query)
                            || s.Description.ToLower().Contains(query)
                            || s.Category.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Skill item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Skill, item.SkillId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Skill, item.SkillId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.SkillFirstObservation ?? i.SkillAddedDate);
        }

        #endregion

        #region Sleep Records

        public async Task<SearchResponse<Sleep>> SearchSleepRecords(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Sleep> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Sleep> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Sleep> items = await progenyDbContext.SleepDb
                    .AsNoTracking()
                    .Where(s => s.ProgenyId == progenyId
                        && s.SleepNotes.ToLower().Contains(query))
                    .ToListAsync();

                foreach (Sleep item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, item.SleepId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Sleep, item.SleepId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.SleepStart);
        }

        #endregion

        #region Todo Items

        public async Task<SearchResponse<TodoItem>> SearchTodoItems(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<TodoItem> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<TodoItem> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<TodoItem> items = await progenyDbContext.TodoItemsDb
                    .AsNoTracking()
                    .Where(t => t.ProgenyId == progenyId && t.FamilyId == 0 && !t.IsDeleted
                        && (t.Title.ToLower().Contains(query)
                            || t.Description.ToLower().Contains(query)
                            || t.Notes.ToLower().Contains(query)
                            || t.Tags.ToLower().Contains(query)
                            || t.Context.ToLower().Contains(query)
                            || t.Location.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (TodoItem item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, item.TodoItemId, currentUserInfo, PermissionLevel.View))
                    {
                        // Check also description as plain text, as some HTML tags may match the query.
                        if (item.Title.ToLower().Contains(query)
                            || item.DescriptionAsPlainText().ToLower().Contains(query)
                            || item.Notes.ToLower().Contains(query)
                            || item.Tags.ToLower().Contains(query)
                            || item.Context.ToLower().Contains(query)
                            || item.Location.ToLower().Contains(query))
                        {
                            item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                                KinaUnaTypes.TimeLineType.TodoItem, item.TodoItemId, item.ProgenyId, item.FamilyId, currentUserInfo);
                            accessibleItems.Add(item);
                        }
                    }
                }
            }

            foreach (int familyId in request.FamilyIds)
            {
                if (!await accessManagementService.HasFamilyPermission(familyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<TodoItem> items = await progenyDbContext.TodoItemsDb
                    .AsNoTracking()
                    .Where(t => t.FamilyId == familyId && t.ProgenyId == 0 && !t.IsDeleted
                        && (t.Title.ToLower().Contains(query)
                            || t.Description.ToLower().Contains(query)
                            || t.Notes.ToLower().Contains(query)
                            || t.Tags.ToLower().Contains(query)
                            || t.Context.ToLower().Contains(query)
                            || t.Location.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (TodoItem item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, item.TodoItemId, currentUserInfo, PermissionLevel.View))
                    {
                        // Check also description as plain text, as some HTML tags may match the query.
                        if (item.Title.ToLower().Contains(query)
                            || item.DescriptionAsPlainText().ToLower().Contains(query)
                            || item.Notes.ToLower().Contains(query)
                            || item.Tags.ToLower().Contains(query)
                            || item.Context.ToLower().Contains(query)
                            || item.Location.ToLower().Contains(query))
                        {
                            item.ItemPerMission = await accessManagementService.GetItemPermissionForUser(
                                KinaUnaTypes.TimeLineType.TodoItem, item.TodoItemId, item.ProgenyId, item.FamilyId, currentUserInfo);
                            accessibleItems.Add(item);
                        }
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.CreatedTime);
        }

        #endregion

        #region Vaccinations

        public async Task<SearchResponse<Vaccination>> SearchVaccinations(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Vaccination> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Vaccination> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Vaccination> items = await progenyDbContext.VaccinationsDb
                    .AsNoTracking()
                    .Where(v => v.ProgenyId == progenyId
                        && (v.VaccinationName.ToLower().Contains(query)
                            || v.VaccinationDescription.ToLower().Contains(query)
                            || v.Notes.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Vaccination item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, item.VaccinationId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPermission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Vaccination, item.VaccinationId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.VaccinationDate);
        }

        #endregion

        #region Videos

        public async Task<SearchResponse<Video>> SearchVideos(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<Video> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<Video> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<Video> items = await mediaDbContext.VideoDb
                    .AsNoTracking()
                    .Where(v => v.ProgenyId == progenyId
                        && ((v.Tags ?? string.Empty).ToLower().Contains(query)
                            || (v.Location ?? string.Empty).ToLower().Contains(query)))
                    .ToListAsync();

                foreach (Video item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Video, item.VideoId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPermission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Video, item.VideoId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.VideoTime ?? i.CreatedTime);
        }

        #endregion

        #region Vocabulary Items

        public async Task<SearchResponse<VocabularyItem>> SearchVocabularyItems(SearchRequest request, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchResponse<VocabularyItem> { SearchRequest = request };
            }

            string query = request.Query.Trim().ToLower();
            List<VocabularyItem> accessibleItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                if (!await accessManagementService.HasProgenyPermission(progenyId, currentUserInfo, PermissionLevel.View))
                    continue;

                List<VocabularyItem> items = await progenyDbContext.VocabularyDb
                    .AsNoTracking()
                    .Where(v => v.ProgenyId == progenyId
                        && (v.Word.ToLower().Contains(query)
                            || v.Description.ToLower().Contains(query)
                            || v.Language.ToLower().Contains(query)
                            || v.SoundsLike.ToLower().Contains(query)))
                    .ToListAsync();

                foreach (VocabularyItem item in items)
                {
                    if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, item.WordId, currentUserInfo, PermissionLevel.View))
                    {
                        item.ItemPermission = await accessManagementService.GetItemPermissionForUser(
                            KinaUnaTypes.TimeLineType.Vocabulary, item.WordId, item.ProgenyId, 0, currentUserInfo);
                        accessibleItems.Add(item);
                    }
                }
            }

            return CreateResponse(accessibleItems, request, i => i.Date ?? i.DateAdded);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a paginated search response from a list of items.
        /// </summary>
        private static SearchResponse<T> CreateResponse<T>(List<T> items, SearchRequest request, Func<T, DateTime> dateSelector)
        {
            IOrderedEnumerable<T> sorted = request.Sort == 0
                ? items.OrderByDescending(dateSelector)
                : items.OrderBy(dateSelector);

            List<T> paginated = request.NumberOfItems > 0
                ? sorted.Skip(request.Skip).Take(request.NumberOfItems).ToList()
                : sorted.Skip(request.Skip).ToList();

            return new SearchResponse<T>
            {
                Results = paginated,
                TotalCount = items.Count,
                PageNumber = request.NumberOfItems > 0 && request.Skip > 0
                    ? request.Skip / request.NumberOfItems + 1
                    : 1,
                RemainingItems = items.Count - (request.Skip + paginated.Count),
                SearchRequest = request
            };
        }

        #endregion
    }
}
