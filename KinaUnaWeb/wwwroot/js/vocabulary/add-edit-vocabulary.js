import * as LocaleHelper from '../localization-v2.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v2.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
$(async function () {
    languageId = getCurrentLanguageId();
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    currentProgenyId = getCurrentProgenyId();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dateTimePicker1 = $('#datetimepicker1');
    dateTimePicker1.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const progenyIdSelect = document.querySelector('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
        });
    }
});
//# sourceMappingURL=add-edit-vocabulary.js.map