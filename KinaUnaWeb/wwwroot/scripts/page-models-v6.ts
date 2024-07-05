interface BaseItemParameters {
    languageId: number;
}
interface BasePageParameters {
    progenyId: number;
    languageId: number;
    currentPageNumber: number;
    itemsPerPage: number;
    totalPages: number;
    totalItems: number;
    sort: number;
    tagFilter: string;
    showSettings: boolean;
}

interface BaseItemsPageResponseModel {
    pageNumber: number;
    totalPages: number;
    totalItems: number;
}

export class ContactItemParameters implements BaseItemParameters {
    contactId: number = 0;
    languageId: number = 0;
}

export class ContactsPageParameters implements BasePageParameters {
    progenyId: number = 0;
    languageId: number = 0;
    currentPageNumber: number = 0;
    itemsPerPage: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    sort: number = 0;
    sortBy: number = 0;
    tagFilter: string = '';
    showSettings: boolean = false;
    sortTags: number = 0;
}

export class ContactsPageResponse implements BaseItemsPageResponseModel {
    pageNumber: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    contactsList: number[] = [];
    tagsList: string[] = [];
}

export class FriendItemParameters implements BaseItemParameters {
    friendId: number = 0;
    languageId: number = 0;
}

export class FriendsPageParameters implements BasePageParameters {
    progenyId: number = 0;
    languageId: number = 0;
    currentPageNumber: number = 0;
    itemsPerPage: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    sort: number = 0;
    sortBy: number = 0;
    tagFilter: string = '';
    showSettings: boolean = false;
    sortTags: number = 0;
}

export class FriendsPageResponse implements BaseItemsPageResponseModel {
    pageNumber: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    friendsList: number[] = [];
    tagsList: string[] = [];
}

export class LocationItemParameters implements BaseItemParameters {
    locationId: number = 0;
    languageId: number = 0;
}

export class LocationsPageParameters implements BasePageParameters {
    progenyId: number = 0;
    languageId: number = 0;
    currentPageNumber: number = 0;
    itemsPerPage: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    sort: number = 0;
    sortBy: number = 0;
    tagFilter: string = '';
    showSettings: boolean = false;
    sortTags: number = 0;
}

export class LocationsPageResponse implements BaseItemsPageResponseModel {
    pageNumber: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    locationsList: number[] = [];
    tagsList: string[] = [];
}

export class NoteItemParameters implements BaseItemParameters {
    noteId: number = 0;
    languageId: number = 0;
}

export class NotesPageParameters implements BasePageParameters {
    progenyId: number = 0;
    languageId: number = 0;
    currentPageNumber: number = 0;
    itemsPerPage: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    sort: number = 1;
    tagFilter: string = '';
    showSettings: boolean = false;
}

export class NotesPageResponse implements BaseItemsPageResponseModel {
    pageNumber: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    notesList: number[] = [];
}

export class Picture {
    pictureId: number = 0;
    progenyId: number = 0;
    pictureNumber: number = 0;
}

export class PictureViewModel {
    pictureId: number = 0;
    progenyId: number = 0;
    pictureNumber: number = 0;
    sortBy: number = 1;
    tagFilter: string = '';
}
export class PicturesPageParameters implements BasePageParameters {
    progenyId: number = 0;
    languageId: number = 0;
    currentPageNumber: number = 0;
    itemsPerPage: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    sort: number = 1;
    tagFilter: string = '';
    showSettings: boolean = false;
    year: number = 0;
    month: number = 0;
    day: number = 0;
    firstItemYear: number = 1900;
    sortTags: number = 0;
}

export class PicturesList {
    pictureItems: Picture[] = [];
    allItemsCount: number = 0;
    remainingItemsCount: number = 0;
    firstItemYear: number = 0;
    totalPages: number = 0;
    currentPageNumber: number = 0;
    tagsList: string[] = [];
}

export class Video {
    videoId: number = 0;
    progenyId: number = 0;
    videoNumber: number = 0;
}

export class VideoViewModel {
    videoId: number = 0;
    progenyId: number = 0;
    videoNumber: number = 0;
    sortBy: number = 1;
    tagFilter: string = '';
}

export class VideosPageParameters implements BasePageParameters {
    progenyId: number = 0;
    languageId: number = 0;
    currentPageNumber: number = 0;
    itemsPerPage: number = 0;
    totalPages: number = 0;
    totalItems: number = 0;
    sort: number = 1;
    tagFilter: string = '';
    showSettings: boolean = false;
    year: number = 0;
    month: number = 0;
    day: number = 0;
    firstItemYear: number = 1900;
    sortTags: number = 0;
}

export class VideosList {
    videoItems: Video[] = [];
    allItemsCount: number = 0;
    remainingItemsCount: number = 0;
    firstItemYear: number = 0;
    totalPages: number = 0;
    currentPageNumber: number = 0;
    tagsList: string[] = [];
}

export class KinaUnaTextParameters {
    id: number = 0;
    textId: number = 0;
    languageId: number = 0;
    page: string = '';
    title: string = '';
    text: string = '';
}

export class KinaUnaTextResponse {
    id: number = 0;
    textId: number = 0;
    languageId: number = 0;
    page: string = '';
    title: string = '';
    text: string = '';
    created: Date = new Date();
    updated: Date = new Date();
}

export class TextTranslation {
    id: number = 0;
    page: string = '';
    word: string = '';
    translation: string = '';
    languageId: number = 0;
}

export class KinaUnaLanguage {
    id: number = 0;
    name: string = '';
    icon: string = '';
}

export class TextTranslationPageListModel {
    page: string = '';
    translations: TextTranslation[] = [];
    languagesList: KinaUnaLanguage[] = [];
}

export class AutoSuggestList {
    progenyId: number;
    suggestions: string[];
    constructor(_progenyId: number) {
        this.progenyId = _progenyId;
        this.suggestions = [];
    }
}

export class TimelineItem {
    timeLineId: number = 0;
    progenyId: number = 0;
    itemType: number = 0;
    itemId: string = '0';
}

export class TimelineParameters {
    progenyId: number = 0;
    skip: number = 0;
    count: number = 5;
    sortBy: number = 1;
    year: number = 0;
    month: number = 0;
    day: number = 0;
    firstItemYear: number = 1900;
}

export class TimeLineItemViewModel {
    typeId: number = 0;
    itemId: number = 0;
    tagFilter: string = '';
}

export class TimelineList {
    timelineItems: TimelineItem[] = [];
    allItemsCount: number = 0;
    remainingItemsCount: number = 0;
    firstItemYear: number = 0;
}

export class WebNotification {
    id: number = 0;
    to: string = "";
}

export class WebNotificationViewModel {
    id: number = 0;
}

export class WebNotficationsParameters {
    skip: number = 0;
    count: number = 10;
    unreadOnly: boolean = false;
}

export class WebNotificationsList {
    notificationsList: WebNotification[] = [];
    allNotificationsCount: number = 0;
    remainingItemsCount: number = 0;
}