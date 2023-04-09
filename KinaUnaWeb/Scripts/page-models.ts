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

export class TagsList {
    progenyId: number;
    tags: string[];
    constructor(_progenyId: number) {
        this.progenyId = _progenyId;
        this.tags = [];
    }
}