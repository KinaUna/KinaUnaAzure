import * as LocaleHelper from '../localization-v9.js';
import { setTagsAutoSuggestList, setContextAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemProgenyId } from '../data-tools-v9.js';
import { TimelineItem, TimeLineType } from '../page-models-v9.js';
import { renderItemPermissionsEditor } from '../item-permissions.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;
let currentFamilyId: number;
let permissionsEditorTimelineItem = new TimelineItem();
/**
 * Configures the date time picker for the contact date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-friend-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    const dateTimePicker: any = $('#friend-date-time-picker');
    dateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up the Progeny select list and adds an event listener to update the context and tags auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList(): void {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}

async function onProgenySelectListChanged(): Promise<void> {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setTagsAutoSuggestList([currentProgenyId], []);
        await setContextAutoSuggestList([currentProgenyId], []);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export async function initializeAddEditFriend(itemId: string): Promise<void> {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentItemProgenyId();

    await setupDateTimePicker();

    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Friend, currentProgenyId, 0);

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}