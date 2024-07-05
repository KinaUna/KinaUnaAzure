export class ContactItemParameters {
    constructor() {
        this.contactId = 0;
        this.languageId = 0;
    }
}
export class ContactsPageParameters {
    constructor() {
        this.progenyId = 0;
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
export class LocationItemParameters {
    constructor() {
        this.locationId = 0;
        this.languageId = 0;
    }
}
export class LocationsPageParameters {
    constructor() {
        this.progenyId = 0;
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
export class NoteItemParameters {
    constructor() {
        this.noteId = 0;
        this.languageId = 0;
    }
}
export class NotesPageParameters {
    constructor() {
        this.progenyId = 0;
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
export class PicturesPageParameters {
    constructor() {
        this.progenyId = 0;
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
    constructor(_progenyId) {
        this.progenyId = _progenyId;
        this.suggestions = [];
    }
}
export class TimelineItem {
    constructor() {
        this.timeLineId = 0;
        this.progenyId = 0;
        this.itemType = 0;
        this.itemId = '0';
    }
}
export class TimelineParameters {
    constructor() {
        this.progenyId = 0;
        this.skip = 0;
        this.count = 5;
        this.sortBy = 1;
        this.year = 0;
        this.month = 0;
        this.day = 0;
        this.firstItemYear = 1900;
    }
}
export class TimeLineItemViewModel {
    constructor() {
        this.typeId = 0;
        this.itemId = 0;
        this.tagFilter = '';
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
//# sourceMappingURL=page-models-v6.js.map