import * as LocaleHelper from '../localization-v2.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v2.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
$(async function () {
    languageId = getCurrentLanguageId();
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    if (document.getElementById('datetimepicker1') !== null) {
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
    }
});
//# sourceMappingURL=add-edit-progeny.js.map