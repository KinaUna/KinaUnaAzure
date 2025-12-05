import { setTagsAutoSuggestList, setContextAutoSuggestList, setLocationAutoSuggestList, setCategoriesAutoSuggestList, setVocabularyLanguagesAutoSuggestList } from "../data-tools-v12.js";
import { renderItemPermissionsEditor } from "../item-permissions-v12.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v12.js";
import { TimelineItem, TimeLineType } from "../page-models-v12.js";

let currentProgenyId: number;
let currentFamilyId: number;
let permissionsEditorTimelineItem = new TimelineItem();

export async function setupForIndividualOrFamilyButtons(itemId: string, itemType: TimeLineType, progenyId: number, familyId: number): Promise<void> {
    currentProgenyId = progenyId;
    currentFamilyId = familyId;
    permissionsEditorTimelineItem.itemId = itemId;
    permissionsEditorTimelineItem.itemType = itemType;
    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;

    let individualButton = document.querySelector<HTMLButtonElement>('#add-item-for-individual-button');

    if (currentFamilyId > 0) {
        await onFamilyButtonClicked();
    }
    else {
        await onIndividualButtonClicked();
    }

    if (individualButton !== null) {
        individualButton.removeEventListener('click', onIndividualButtonClicked);
        individualButton.addEventListener('click', onIndividualButtonClicked);
    }

    let familyButton = document.querySelector<HTMLButtonElement>('#add-item-for-family-button');
    if (familyButton !== null) {
        familyButton.removeEventListener('click', onFamilyButtonClicked);
        familyButton.addEventListener('click', onFamilyButtonClicked);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function onIndividualButtonClicked(): Promise<void> {
    let individualButton = document.querySelector<HTMLButtonElement>('#add-item-for-individual-button');
    if (individualButton !== null) {
        individualButton.classList.add('btn-primary');
        individualButton.classList.remove('btn-outline-primary');

    }
    const individualFormGroup = document.querySelector<HTMLDivElement>('#individual-select-from-group');
    if (individualFormGroup !== null) {
        individualFormGroup.classList.remove('d-none');
    }
    let familyButton = document.querySelector<HTMLButtonElement>('#add-item-for-family-button');
    if (familyButton !== null) {
        familyButton.classList.remove('btn-primary');
        familyButton.classList.add('btn-outline-primary');
    }
    const familyFormGroup = document.querySelector<HTMLDivElement>('#family-select-from-group');
    if (familyFormGroup !== null) {
        familyFormGroup.classList.add('d-none');
    }
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect) {
        if (progenyIdSelect.selectedIndex < 0) {
            progenyIdSelect.selectedIndex = 0;
        }
    }

    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect) {
        if (familyIdSelect.selectedIndex >= 0) {
            familyIdSelect.selectedIndex = -1;
        }
    }

    await setupProgenySelectList();
}

async function onFamilyButtonClicked(): Promise<void> {
    let familyButton = document.querySelector<HTMLButtonElement>('#add-item-for-family-button');
    if (familyButton !== null) {
        familyButton.classList.add('btn-primary');
        familyButton.classList.remove('btn-outline-primary');
    }
    const familyFormGroup = document.querySelector<HTMLDivElement>('#family-select-from-group');
    if (familyFormGroup !== null) {
        familyFormGroup.classList.remove('d-none');
    }
    let individualButton = document.querySelector<HTMLButtonElement>('#add-item-for-individual-button');
    if (individualButton !== null) {
        individualButton.classList.remove('btn-primary');
        individualButton.classList.add('btn-outline-primary');
    }
    const individualFormGroup = document.querySelector<HTMLDivElement>('#individual-select-from-group');
    if (individualFormGroup !== null) {
        individualFormGroup.classList.add('d-none');
    }

    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect) {
        if (familyIdSelect.selectedIndex < 0) {
            familyIdSelect.selectedIndex = 0;
        }
    }

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect) {
        if (progenyIdSelect.selectedIndex >= 0) {
            progenyIdSelect.selectedIndex = -1;
        }
    }

    await setupFamilySelectList();
}

/**
 * Sets up the Progeny select list and adds an event listener to update the tags and categories auto suggest lists when the selected Progeny changes.
 */
async function setupProgenySelectList(): Promise<void> {
    startFullPageSpinner();
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.removeEventListener('change', onProgenySelectListChanged);
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
        currentProgenyId = parseInt(progenyIdSelect.value);
        if (currentProgenyId > 0) {
            await setTagsAutoSuggestList([currentProgenyId], []);
            await setContextAutoSuggestList([currentProgenyId], []);
            await setLocationAutoSuggestList([currentProgenyId], []);
            await setCategoriesAutoSuggestList([currentProgenyId], []);
            await setVocabularyLanguagesAutoSuggestList([currentProgenyId]);

            currentFamilyId = 0;
            permissionsEditorTimelineItem.progenyId = currentProgenyId;
            permissionsEditorTimelineItem.familyId = currentFamilyId;
            await renderItemPermissionsEditor(permissionsEditorTimelineItem);
        }
    }
    stopFullPageSpinner();
}

async function onProgenySelectListChanged(): Promise<void> {
    startFullPageSpinner();
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect === null) {
        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }
    currentProgenyId = parseInt(progenyIdSelect.value);
    await setTagsAutoSuggestList([currentProgenyId], []);
    await setContextAutoSuggestList([currentProgenyId], []);
    await setLocationAutoSuggestList([currentProgenyId], []);
    await setCategoriesAutoSuggestList([currentProgenyId], []);

    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect !== null) {
        currentFamilyId = 0;
        familyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        familyIdSelect.selectedIndex = -1;
    }

    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;
    await renderItemPermissionsEditor(permissionsEditorTimelineItem);

    stopFullPageSpinner();
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function setupFamilySelectList(): Promise<void> {
    startFullPageSpinner();
    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect !== null) {
        familyIdSelect.removeEventListener('change', onFamilySelectListChanged);
        familyIdSelect.addEventListener('change', onFamilySelectListChanged);
        currentFamilyId = parseInt(familyIdSelect.value);
        if (currentFamilyId > 0) {
            await setTagsAutoSuggestList([], [currentFamilyId]);
            await setContextAutoSuggestList([], [currentFamilyId]);
            await setLocationAutoSuggestList([], [currentFamilyId]);
            await setCategoriesAutoSuggestList([currentProgenyId], []);
            currentProgenyId = 0;
            permissionsEditorTimelineItem.progenyId = currentProgenyId;
            permissionsEditorTimelineItem.familyId = currentFamilyId;
            await renderItemPermissionsEditor(permissionsEditorTimelineItem);
        }
    }
    stopFullPageSpinner();
}

async function onFamilySelectListChanged(): Promise<void> {
    startFullPageSpinner();
    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect === null) {
        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }
    currentFamilyId = parseInt(familyIdSelect.value);
    await setTagsAutoSuggestList([], [currentFamilyId]);
    await setContextAutoSuggestList([], [currentFamilyId]);
    await setLocationAutoSuggestList([], [currentFamilyId]);
    await setCategoriesAutoSuggestList([currentProgenyId], []);

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = 0;
        progenyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        progenyIdSelect.selectedIndex = -1;
    }

    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;
    await renderItemPermissionsEditor(permissionsEditorTimelineItem);
    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}