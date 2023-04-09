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
export class TagsList {
    constructor(_progenyId) {
        this.progenyId = _progenyId;
        this.tags = [];
    }
}
//# sourceMappingURL=page-models.js.map