import * as LocaleHelper from '../localization-v10.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemProgenyId } from '../data-tools-v10.js';
import { TimelineItem, TimeLineType } from '../page-models-v10.js';
import { renderItemPermissionsEditor } from '../item-permissions-v10.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v10.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;

/**
 * Configures the date time picker for the vaccination date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-measurement-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    const dateTimePicker: any = $('#measurement-date-time-picker');
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

export async function initializeAddEditMeasurement(itemId: string): Promise<void> {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentItemProgenyId();

    await setupDateTimePicker();

    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Measurement, currentProgenyId, 0);

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}