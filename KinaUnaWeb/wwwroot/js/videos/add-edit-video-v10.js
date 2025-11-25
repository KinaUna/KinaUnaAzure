import * as LocaleHelper from '../localization-v10.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemProgenyId } from '../data-tools-v10.js';
import { setAddItemButtonEventListeners } from '../addItem/add-item-v10.js';
import { addCopyLocationButtonEventListener } from '../locations/location-tools-v10.js';
import { TimeLineType } from '../page-models-v11.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v10.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let toggleEditButton;
let copyLocationButton;
/**
 * Configures the date time picker for the video date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-video-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    if (document.getElementById('video-date-time-picker') !== null) {
        const dateTimePicker = $('#video-date-time-picker');
        dateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function initializeAddEditVideo(itemId) {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentItemProgenyId();
    await setupDateTimePicker();
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Video, currentProgenyId, 0);
    addCopyLocationButtonEventListener();
    setAddItemButtonEventListeners();
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-video-v10.js.map