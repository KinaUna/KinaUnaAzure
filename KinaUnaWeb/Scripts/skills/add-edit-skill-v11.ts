import * as LocaleHelper from '../localization-v11.js';
import { setCategoriesAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getCurrentItemProgenyId } from '../data-tools-v11.js';
import { TimelineItem, TimeLineType } from '../page-models-v11.js';
import { renderItemPermissionsEditor } from '../item-permissions-v11.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v11.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let zebraDateTimeFormat: string;
let currentProgenyId: number;

/**
 * Configures the date time picker for the skill date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-skill-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    const dateTimePicker: any = $('#skill-date-time-picker');
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

export async function initializeAddEditSkill(itemId: string): Promise<void> {
    currentProgenyId = getCurrentItemProgenyId();
    languageId = getCurrentLanguageId();

    await setupDateTimePicker();

    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Skill, currentProgenyId, 0);

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}