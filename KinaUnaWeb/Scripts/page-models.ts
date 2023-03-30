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