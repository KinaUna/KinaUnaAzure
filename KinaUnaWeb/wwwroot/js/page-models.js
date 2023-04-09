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
    }
}
export class TimeLineItemViewModel {
    constructor() {
        this.typeId = 0;
        this.itemId = 0;
        this.tagFilter = '';
    }
}
//# sourceMappingURL=page-models.js.map