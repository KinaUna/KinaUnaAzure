export class ContactItemParameters {
    constructor() {
        this.contactId = 0;
        this.languageId = 0;
    }
}
export class ContactsPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 0;
        this.sortBy = 0;
        this.tagFilter = '';
        this.showSettings = false;
        this.sortTags = 0;
    }
}
export class ContactsPageResponse {
    constructor() {
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.contactsList = [];
        this.tagsList = [];
    }
}
export class FriendItemParameters {
    constructor() {
        this.friendId = 0;
        this.languageId = 0;
    }
}
export class FriendsPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 0;
        this.sortBy = 0;
        this.tagFilter = '';
        this.showSettings = false;
        this.sortTags = 0;
    }
}
export class FriendsPageResponse {
    constructor() {
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.friendsList = [];
        this.tagsList = [];
    }
}
export class LocationItem {
    constructor() {
        this.locationId = 0;
        this.progenyId = 0;
        this.name = '';
        this.latitude = 0;
        this.longitude = 0;
    }
}
export class LocationItemParameters {
    constructor() {
        this.locationId = 0;
        this.languageId = 0;
    }
}
export class LocationsPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 0;
        this.sortBy = 0;
        this.tagFilter = '';
        this.showSettings = false;
        this.sortTags = 0;
    }
}
export class LocationsPageResponse {
    constructor() {
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.locationsList = [];
        this.tagsList = [];
    }
}
export class NearByPhotosRequest {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.locationItem = new LocationItem();
        this.distance = 0.25;
        this.sortOrder = 1;
        this.numberOfPictures = 10;
    }
}
export class NearByPhotosResponse {
    constructor() {
        this.progenyId = 0;
        this.locationItem = new LocationItem();
        this.picturesList = [];
        this.numberOfPictures = 0;
    }
}
export class PicturesLocationsRequest {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.distance = 0.1;
    }
}
export class PicturesLocationsResponse {
    constructor() {
        this.progenyId = 0;
        this.locationsList = [];
        this.numberOfLocations = 0;
    }
}
export class NoteItemParameters {
    constructor() {
        this.noteId = 0;
        this.languageId = 0;
    }
}
export class NotesPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 1;
        this.tagFilter = '';
        this.showSettings = false;
    }
}
export class NotesPageResponse {
    constructor() {
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.notesList = [];
    }
}
export class Picture {
    constructor() {
        this.pictureId = 0;
        this.progenyId = 0;
        this.pictureNumber = 0;
    }
}
export class PictureViewModel {
    constructor() {
        this.pictureId = 0;
        this.progenyId = 0;
        this.pictureNumber = 0;
        this.sortBy = 1;
        this.tagFilter = '';
    }
}
export class PictureViewModelRequest {
    constructor() {
        this.progenies = [];
        this.pictureId = 0;
        this.sortOrder = 1;
        this.tagFilter = '';
        this.timeZone = '';
    }
}
export class PicturesPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 1;
        this.tagFilter = '';
        this.showSettings = false;
        this.year = 0;
        this.month = 0;
        this.day = 0;
        this.firstItemYear = 1900;
        this.sortTags = 0;
    }
}
export class PicturesList {
    constructor() {
        this.pictureItems = [];
        this.allItemsCount = 0;
        this.remainingItemsCount = 0;
        this.firstItemYear = 0;
        this.totalPages = 0;
        this.currentPageNumber = 0;
        this.tagsList = [];
    }
}
export class Video {
    constructor() {
        this.videoId = 0;
        this.progenyId = 0;
        this.videoNumber = 0;
    }
}
export class VideoViewModel {
    constructor() {
        this.videoId = 0;
        this.progenyId = 0;
        this.videoNumber = 0;
        this.sortBy = 1;
        this.tagFilter = '';
    }
}
export class VideosPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 1;
        this.tagFilter = '';
        this.showSettings = false;
        this.year = 0;
        this.month = 0;
        this.day = 0;
        this.firstItemYear = 1900;
        this.sortTags = 0;
    }
}
export class VideosList {
    constructor() {
        this.videoItems = [];
        this.allItemsCount = 0;
        this.remainingItemsCount = 0;
        this.firstItemYear = 0;
        this.totalPages = 0;
        this.currentPageNumber = 0;
        this.tagsList = [];
    }
}
export class VideoViewModelRequest {
    constructor() {
        this.progenies = [];
        this.videoId = 0;
        this.sortOrder = 1;
        this.tagFilter = '';
        this.timeZone = '';
    }
}
export class KinaUnaTextParameters {
    constructor() {
        this.id = 0;
        this.textId = 0;
        this.languageId = 0;
        this.page = '';
        this.title = '';
        this.text = '';
    }
}
export class KinaUnaTextResponse {
    constructor() {
        this.id = 0;
        this.textId = 0;
        this.languageId = 0;
        this.page = '';
        this.title = '';
        this.text = '';
        this.created = new Date();
        this.updated = new Date();
    }
}
export class TextTranslation {
    constructor() {
        this.id = 0;
        this.page = '';
        this.word = '';
        this.translation = '';
        this.languageId = 0;
    }
}
export class KinaUnaLanguage {
    constructor() {
        this.id = 0;
        this.name = '';
        this.icon = '';
    }
}
export class TextTranslationPageListModel {
    constructor() {
        this.page = '';
        this.translations = [];
        this.languagesList = [];
    }
}
export class AutoSuggestList {
    constructor(_progenies) {
        this.progenies = _progenies;
        this.suggestions = [];
    }
}
export class TimelineItem {
    constructor() {
        this.timeLineId = 0;
        this.progenyId = 0;
        this.itemType = 0;
        this.itemId = '0';
        this.itemYear = 0;
        this.itemMonth = 0;
        this.itemDay = 0;
    }
}
export class TimelineParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.skip = 0;
        this.count = 5;
        this.sortBy = 1;
        this.year = 0;
        this.month = 0;
        this.day = 0;
        this.firstItemYear = 1900;
        this.tagFilter = '';
    }
}
export class TimelineRequest {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.accessLevel = 5;
        this.skip = 0;
        this.numberOfItems = 5;
        this.year = 0;
        this.month = 0;
        this.day = 0;
        this.tagFilter = '';
        this.categoryFilter = '';
        this.contextFilter = '';
        this.timeLineTypeFilter = [];
        this.sortOrder = 1;
        this.firstItemYear = 1900;
    }
}
export class TimelineResponse {
    constructor() {
        this.timeLineItems = [];
        this.remainingItemsCount = 0;
        this.request = new TimelineRequest();
    }
}
export class TimeLineItemViewModel {
    constructor() {
        this.typeId = 0;
        this.itemId = 0;
        this.tagFilter = '';
        this.itemYear = 0;
        this.itemMonth = 0;
        this.itemDay = 0;
    }
}
export class TimelineList {
    constructor() {
        this.timelineItems = [];
        this.allItemsCount = 0;
        this.remainingItemsCount = 0;
        this.firstItemYear = 0;
    }
}
export class OnThisDayRequest {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.accessLevel = 5;
        this.skip = 0;
        this.numberOfItems = 5;
        this.year = 0;
        this.month = 0;
        this.day = 0;
        this.tagFilter = '';
        this.categoryFilter = '';
        this.contextFilter = '';
        this.timeLineTypeFilter = [];
        this.onThisDayPeriod = OnThisDayPeriod.Year;
        this.sortOrder = 1;
        this.firstItemYear = 1900;
    }
}
export class OnThisDayResponse {
    constructor() {
        this.timeLineItems = [];
        this.remainingItemsCount = 0;
        this.request = new OnThisDayRequest();
    }
}
export class WebNotification {
    constructor() {
        this.id = 0;
        this.to = "";
    }
}
export class WebNotificationViewModel {
    constructor() {
        this.id = 0;
    }
}
export class WebNotficationsParameters {
    constructor() {
        this.skip = 0;
        this.count = 10;
        this.unreadOnly = false;
    }
}
export class WebNotificationsList {
    constructor() {
        this.notificationsList = [];
        this.allNotificationsCount = 0;
        this.remainingItemsCount = 0;
    }
}
export var TimeLineType;
(function (TimeLineType) {
    TimeLineType[TimeLineType["Photo"] = 1] = "Photo";
    TimeLineType[TimeLineType["Video"] = 2] = "Video";
    TimeLineType[TimeLineType["Calendar"] = 3] = "Calendar";
    TimeLineType[TimeLineType["Vocabulary"] = 4] = "Vocabulary";
    TimeLineType[TimeLineType["Skill"] = 5] = "Skill";
    TimeLineType[TimeLineType["Friend"] = 6] = "Friend";
    TimeLineType[TimeLineType["Measurement"] = 7] = "Measurement";
    TimeLineType[TimeLineType["Sleep"] = 8] = "Sleep";
    TimeLineType[TimeLineType["Note"] = 9] = "Note";
    TimeLineType[TimeLineType["Contact"] = 10] = "Contact";
    TimeLineType[TimeLineType["Vaccination"] = 11] = "Vaccination";
    TimeLineType[TimeLineType["Location"] = 12] = "Location";
    TimeLineType[TimeLineType["User"] = 13] = "User";
    TimeLineType[TimeLineType["UserAccess"] = 14] = "UserAccess";
    TimeLineType[TimeLineType["TodoItem"] = 15] = "TodoItem";
    TimeLineType[TimeLineType["KanbanBoard"] = 16] = "KanbanBoard";
    TimeLineType[TimeLineType["KanbanItem"] = 17] = "KanbanItem";
    TimeLineType[TimeLineType["Child"] = 100] = "Child";
})(TimeLineType || (TimeLineType = {}));
export var TodoStatusType;
(function (TodoStatusType) {
    TodoStatusType[TodoStatusType["NotStarted"] = 0] = "NotStarted";
    TodoStatusType[TodoStatusType["InProgress"] = 1] = "InProgress";
    TodoStatusType[TodoStatusType["Completed"] = 2] = "Completed";
    TodoStatusType[TodoStatusType["Cancelled"] = 3] = "Cancelled";
    TodoStatusType[TodoStatusType["Overdue"] = 4] = "Overdue";
})(TodoStatusType || (TodoStatusType = {}));
export var OnThisDayPeriod;
(function (OnThisDayPeriod) {
    OnThisDayPeriod[OnThisDayPeriod["Week"] = 1] = "Week";
    OnThisDayPeriod[OnThisDayPeriod["Month"] = 2] = "Month";
    OnThisDayPeriod[OnThisDayPeriod["Quarter"] = 3] = "Quarter";
    OnThisDayPeriod[OnThisDayPeriod["Year"] = 4] = "Year";
})(OnThisDayPeriod || (OnThisDayPeriod = {}));
export class CalendarReminderRequest {
    constructor() {
        this.calendarReminderId = 0;
        this.eventId = 0;
        this.notifyTimeString = "";
        this.notifyTimeOffsetType = 1;
        this.userId = '';
        this.notified = false;
    }
}
export class CalendarItem {
    constructor() {
        this.eventId = 0;
        this.progenyId = 0;
        this.title = '';
        this.notes = '';
        this.location = '';
        this.context = '';
        this.allDay = false;
        this.accessLevel = 5;
        this.startString = '';
        this.endString = '';
        this.author = '';
    }
}
export class CalendarItemsRequest {
    constructor() {
        this.progenyIds = [];
        this.startYear = 0;
        this.startMonth = 0;
        this.startDay = 0;
        this.endYear = 0;
        this.endMonth = 0;
        this.endDay = 0;
    }
}
export class SetProgenyRequest {
    constructor() {
        this.progenyId = 0;
        this.languageId = 0;
    }
}
export class TodosPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 0; // 0 Ascending, 1 Descending
        this.sortBy = 0; // 0 for DueDate, 1 for CreatedTime, 2 for StartDate, 3 for CompletedDate
        this.groupBy = 0; // 0 for no grouping, 1 for Status, 2 for Progeny/AssignedTo, 3 for Location
        this.tagFilter = '';
        this.contextFilter = '';
        this.locationFilter = '';
        this.statusFilter = [];
        this.startYear = 0;
        this.startMonth = 0;
        this.startDay = 0;
        this.endYear = 0;
        this.endMonth = 0;
        this.endDay = 0;
        this.showSettings = false;
    }
}
export class SubtasksPageParameters {
    constructor() {
        this.parentTodoItemId = 0;
        this.progenyId = 0;
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 0; // 0 Ascending, 1 Descending
        this.sortBy = 0; // 0 for DueDate, 1 for CreatedTime, 2 for StartDate, 3 for CompletedDate
        this.groupBy = 0; // 0 for no grouping, 1 for Status, 2 for Progeny/AssignedTo, 3 for Location
        this.tagFilter = '';
        this.contextFilter = '';
        this.locationFilter = '';
        this.statusFilter = [];
        this.startYear = 0;
        this.startMonth = 0;
        this.startDay = 0;
        this.endYear = 0;
        this.endMonth = 0;
        this.endDay = 0;
        this.showSettings = false;
    }
}
export class TodosPageResponse {
    constructor() {
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.todosList = [];
        this.tagsList = [];
        this.contextsList = [];
    }
}
export class SubtasksPageResponse {
    constructor() {
        this.parentTodoItemId = 0;
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.subtasksList = [];
    }
}
export class TodoItemParameters {
    constructor() {
        this.todoItemId = 0;
        this.languageId = 0;
    }
}
export class TodoItem {
    constructor() {
        this.todoItemId = 0;
        this.uId = '';
        this.parentTodoItemId = 0;
        this.progenyId = 0;
        this.title = '';
        this.description = '';
        this.status = 0;
        this.startDate = new Date();
        this.dueDate = new Date();
        this.completedDate = new Date();
        this.notes = '';
        this.accessLevel = 5;
        this.tags = [];
        this.context = '';
        this.createdBy = '';
        this.modifiedBy = '';
        this.createdTime = new Date();
        this.modifiedTime = new Date();
        this.isDeleted = false;
    }
}
export class KanbanBoardsPageParameters {
    constructor() {
        this.progenyId = 0;
        this.progenies = [];
        this.languageId = 0;
        this.currentPageNumber = 0;
        this.itemsPerPage = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.sort = 0; // 0 Ascending, 1 Descending
        this.tagFilter = '';
        this.contextFilter = '';
        this.includeDeleted = false;
        this.showSettings = false;
    }
}
export class KanbanBoardsPageResponse {
    constructor() {
        this.pageNumber = 0;
        this.totalPages = 0;
        this.totalItems = 0;
        this.KanbanBoardsList = [];
        this.tagsList = [];
        this.contextsList = [];
    }
}
export class KanbanBoard {
    constructor() {
        this.kanbanBoardId = 0;
        this.uId = '';
        this.progenyId = 0;
        this.title = '';
        this.description = '';
        this.columns = '';
        this.accessLevel = 5;
        this.tags = [];
        this.context = '';
        this.createdBy = '';
        this.modifiedBy = '';
        this.createdTime = new Date();
        this.modifiedTime = new Date();
        this.isDeleted = false;
        this.columnsList = [];
    }
}
export class KanbanBoardElementParameters {
    constructor() {
        this.kanbanBoardId = 0;
        this.languageId = 0;
    }
}
export class KanbanBoardColumn {
    constructor() {
        this.id = 0;
        this.columnIndex = 0;
        this.title = '';
        this.wipLimit = 0;
    }
}
export class KanbanItem {
    constructor() {
        this.kanbanItemId = 0;
        this.uId = '';
        this.kanbanBoardId = 0;
        this.todoItemId = 0;
        this.columnIndex = 0;
        this.rowIndex = 0;
        this.createdBy = '';
        this.modifiedBy = '';
        this.createdTime = new Date();
        this.modifiedTime = new Date();
        this.isDeleted = false;
    }
}
//# sourceMappingURL=page-models-v9.js.map