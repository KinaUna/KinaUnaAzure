import * as LocaleHelper from '../localization-v2.js';
import { setTagsAutoSuggestList, setCategoriesAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v2.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;

$(async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    currentProgenyId = getCurrentProgenyId();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    const dateTimePicker1: any = $('#datetimepicker1');
    dateTimePicker1.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    await setTagsAutoSuggestList(currentProgenyId);
    await setCategoriesAutoSuggestList(currentProgenyId);

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList(currentProgenyId);
            await setCategoriesAutoSuggestList(currentProgenyId);
        });
    }
});