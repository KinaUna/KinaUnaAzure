import { setTagsAutoSuggestList, setContextAutoSuggestList, setLocationAutoSuggestList, setCategoriesAutoSuggestList, setVocabularyLanguagesAutoSuggestList } from "../data-tools-v9.js";
import { renderItemPermissionsEditor } from "../item-permissions.js";
import { TimelineItem } from "../page-models-v9.js";
let currentProgenyId;
let currentFamilyId;
let permissionsEditorTimelineItem = new TimelineItem();
export async function setupForIndividualOrFamilyButtons(itemId, itemType, progenyId, familyId) {
    currentProgenyId = progenyId;
    currentFamilyId = familyId;
    permissionsEditorTimelineItem.itemId = itemId;
    permissionsEditorTimelineItem.itemType = itemType;
    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;
    let individualButton = document.querySelector('#add-item-for-individual-button');
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
    let familyButton = document.querySelector('#add-item-for-family-button');
    if (familyButton !== null) {
        familyButton.removeEventListener('click', onFamilyButtonClicked);
        familyButton.addEventListener('click', onFamilyButtonClicked);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function onIndividualButtonClicked() {
    let individualButton = document.querySelector('#add-item-for-individual-button');
    if (individualButton !== null) {
        individualButton.classList.add('btn-primary');
        individualButton.classList.remove('btn-outline-primary');
    }
    const individualFormGroup = document.querySelector('#individual-select-from-group');
    if (individualFormGroup !== null) {
        individualFormGroup.classList.remove('d-none');
    }
    let familyButton = document.querySelector('#add-item-for-family-button');
    if (familyButton !== null) {
        familyButton.classList.remove('btn-primary');
        familyButton.classList.add('btn-outline-primary');
    }
    const familyFormGroup = document.querySelector('#family-select-from-group');
    if (familyFormGroup !== null) {
        familyFormGroup.classList.add('d-none');
    }
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect) {
        if (progenyIdSelect.selectedIndex < 0) {
            progenyIdSelect.selectedIndex = 0;
        }
    }
    await setupProgenySelectList();
}
async function onFamilyButtonClicked() {
    let familyButton = document.querySelector('#add-item-for-family-button');
    if (familyButton !== null) {
        familyButton.classList.add('btn-primary');
        familyButton.classList.remove('btn-outline-primary');
    }
    const familyFormGroup = document.querySelector('#family-select-from-group');
    if (familyFormGroup !== null) {
        familyFormGroup.classList.remove('d-none');
    }
    let individualButton = document.querySelector('#add-item-for-individual-button');
    if (individualButton !== null) {
        individualButton.classList.remove('btn-primary');
        individualButton.classList.add('btn-outline-primary');
    }
    const individualFormGroup = document.querySelector('#individual-select-from-group');
    if (individualFormGroup !== null) {
        individualFormGroup.classList.add('d-none');
    }
    const familyIdSelect = document.querySelector('#item-family-id-select');
    if (familyIdSelect) {
        if (familyIdSelect.selectedIndex < 0) {
            familyIdSelect.selectedIndex = 0;
        }
    }
    await setupFamilySelectList();
}
/**
 * Sets up the Progeny select list and adds an event listener to update the tags and categories auto suggest lists when the selected Progeny changes.
 */
async function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
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
}
async function onProgenySelectListChanged() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect === null) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    currentProgenyId = parseInt(progenyIdSelect.value);
    await setTagsAutoSuggestList([currentProgenyId], []);
    await setContextAutoSuggestList([currentProgenyId], []);
    await setLocationAutoSuggestList([currentProgenyId], []);
    await setCategoriesAutoSuggestList([currentProgenyId], []);
    const familyIdSelect = document.querySelector('#item-family-id-select');
    if (familyIdSelect !== null) {
        currentFamilyId = 0;
        familyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        familyIdSelect.selectedIndex = -1;
    }
    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;
    await renderItemPermissionsEditor(permissionsEditorTimelineItem);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function setupFamilySelectList() {
    const familyIdSelect = document.querySelector('#item-family-id-select');
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
}
async function onFamilySelectListChanged() {
    const familyIdSelect = document.querySelector('#item-family-id-select');
    if (familyIdSelect === null) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    currentFamilyId = parseInt(familyIdSelect.value);
    await setTagsAutoSuggestList([], [currentFamilyId]);
    await setContextAutoSuggestList([], [currentFamilyId]);
    await setLocationAutoSuggestList([], [currentFamilyId]);
    await setCategoriesAutoSuggestList([currentProgenyId], []);
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = 0;
        progenyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        progenyIdSelect.selectedIndex = -1;
    }
    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;
    await renderItemPermissionsEditor(permissionsEditorTimelineItem);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=setup-for-selection.js.map