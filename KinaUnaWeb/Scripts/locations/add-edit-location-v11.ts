import * as LocaleHelper from '../localization-v11.js';
import { setTagsAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemFamilyId, getCurrentItemProgenyId } from '../data-tools-v11.js';
import { TimelineItem, TimeLineType } from '../page-models-v11.js';
import { renderItemPermissionsEditor } from '../item-permissions-v11.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v11.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;
let currentFamilyId: number;

/**
 * Configures the date time picker for the location date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-location-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    const dateTimePicker1: any = $('#location-date-time-picker');
    dateTimePicker1.Zebra_DatePicker({
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

function setupHereMaps() {
    const fullScreenOverlay = document.getElementById('full-screen-overlay-div');
    if (fullScreenOverlay !== null) {
        if (fullScreenOverlay.querySelector('script') !== null) {
            eval((fullScreenOverlay.querySelector('script') as HTMLElement).innerHTML);
        }
    }
}

export async function initializeAddEditLocation(itemId: string): Promise<void> {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentItemProgenyId();
    currentFamilyId = getCurrentItemFamilyId();

    await setupDateTimePicker();

    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Location, currentProgenyId, currentFamilyId);

    ($(".selectpicker") as any).selectpicker('refresh');

    setupHereMaps();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}