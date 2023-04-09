import * as LocaleHelper from '../localization.js';
import { setContextAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, checkTimes, getZebraDateTimeFormat, getLongDateTimeFormatMoment } from '../data-tools.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let zebraDateTimeFormat: string;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId: number;

$(async function (): Promise<void> {
    currentProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();
    setMomentLocale();
    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);

    setContextAutoSuggestList(currentProgenyId);

    const dateTimePicker1: any = $('#datetimepicker1');
    dateTimePicker1.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const dateTimePicker2: any = $('#datetimepicker2');
    dateTimePicker2.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString);

    const zebra1 = document.querySelector<HTMLInputElement>('#datetimepicker1');
    if (zebra1 !== null) {
        zebra1.addEventListener('change', () => { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); });
        zebra1.addEventListener('blur', () => { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); });
        zebra1.addEventListener('focus', () => { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); });
    }

    const zebra2 = document.querySelector<HTMLInputElement>('#datetimepicker2');
    if (zebra2 !== null) {
        zebra2.addEventListener('change', () => { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); });
        zebra2.addEventListener('blur', () => { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); });
        zebra2.addEventListener('focus', () => { checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString); });
    }

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setContextAutoSuggestList(currentProgenyId);
        });
    }
});