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
}

export class WebNotificationsList {
    notificationsList: WebNotification[] = [];
    allNotificationsCount: number = 0;
    remainingItemsCount: number = 0;
}