import * as LocaleHelper from '../localization.js';

declare let moment: any;

let currentMomentLocale: string = 'en';
let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
function setLanguageId() {
    const languageIdDiv = document.querySelector<HTMLDivElement>('#languageIdDiv');

    if (languageIdDiv !== null) {
        const languageIdData = languageIdDiv.dataset.languageId;
        if (languageIdData) {
            languageId = parseInt(languageIdData);
        }
    }
}
function setMomentLocale() {
    const currentMomentLocaleDiv = document.querySelector<HTMLDivElement>('#currentMomentLocaleDiv');

    if (currentMomentLocaleDiv !== null) {
        const currentLocaleData = currentMomentLocaleDiv.dataset.currentLocale;
        if (currentLocaleData) {
            currentMomentLocale = currentLocaleData;
        }
    }
    moment.locale(currentMomentLocale);
}

function setDateTimeFormats() {
    const longDateTimeFormatMomentDiv = document.querySelector<HTMLDivElement>('#longDateTimeFormatMomentDiv');
    if (longDateTimeFormatMomentDiv !== null) {
        const longDateTimeFormatMomentData = longDateTimeFormatMomentDiv.dataset.longDateTimeFormatMoment;
        if (longDateTimeFormatMomentData) {
            longDateTimeFormatMoment = longDateTimeFormatMomentData;
        }
    }
}

function checkTimes() {
    let sTime: any = moment($('#datetimepicker1').val(), longDateTimeFormatMoment);
    let eTime: any = moment($('#datetimepicker2').val(), longDateTimeFormatMoment);
    if (sTime < eTime && sTime.isValid() && eTime.isValid()) {
        $('#notification').text('');
        $('#submitBtn').prop('disabled', false);

    } else {
        $('#submitBtn').prop('disabled', true);
        $('#notification').text(warningStartIsAfterEndString);
    };
};

$(async function (): Promise<void> {
    setLanguageId();
    setMomentLocale();
    setDateTimeFormats();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);

    const dateTimePicker1: any = $('#datetimepicker1');
    dateTimePicker1.Zebra_DatePicker({
        format: '@zebraDateTimeFormat',
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { checkTimes(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const dateTimePicker2: any = $('#datetimepicker2');
    dateTimePicker2.Zebra_DatePicker({
        format: '@zebraDateTimeFormat',
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { checkTimes(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    checkTimes();

    const zebra1 = document.querySelector<HTMLInputElement>('#datetimepicker1');
    if (zebra1 !== null) {
        zebra1.addEventListener('change', () => { checkTimes(); });
        zebra1.addEventListener('blur', () => { checkTimes(); });
        zebra1.addEventListener('focus', () => { checkTimes(); });
    }

    const zebra2 = document.querySelector<HTMLInputElement>('#datetimepicker2');
    if (zebra2 !== null) {
        zebra2.addEventListener('change', () => { checkTimes(); });
        zebra2.addEventListener('blur', () => { checkTimes(); });
        zebra2.addEventListener('focus', () => { checkTimes(); });
    }
});