import * as LocaleHelper from '../localization-v11.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, setVocabularyLanguagesAutoSuggestList, getCurrentItemProgenyId } from '../data-tools-v11.js';
import { TimelineItem, TimeLineType } from '../page-models-v11.js';
import { renderItemPermissionsEditor } from '../item-permissions-v11.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v11.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;

/**
 * Configures the date time picker for the word date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-vocabulary-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    const dateTimePicker: any = $('#vocabulary-date-time-picker');
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

export async function initializeAddEditVocabulary(itemId: string): Promise<void> {
    currentProgenyId = getCurrentItemProgenyId();
    languageId = getCurrentLanguageId();

    await setupDateTimePicker();

    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Vocabulary, currentProgenyId, 0);

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}