import * as LocaleHelper from '../localization.js';
import { getTagsList } from '../data-tools.js';
declare let moment: any;

let currentMomentLocale: string = 'en';
let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let zebraDateTimeFormat: string;
let currentProgenyId: number;

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

    const zebraDateTimeFormatDiv = document.querySelector<HTMLDivElement>('#zebraDateTimeFormatDiv');
    if (zebraDateTimeFormatDiv !== null) {
        const zebraDateTimeFormatData = zebraDateTimeFormatDiv.dataset.zebraDateTimeFormat;
        if (zebraDateTimeFormatData) {
            zebraDateTimeFormat = zebraDateTimeFormatData;
        }
    }
}

function setProgenyId() {
    const progenyIdDiv = document.querySelector<HTMLDivElement>('#progenyIdDiv');
    if (progenyIdDiv !== null) {
        const progenyIdDivData = progenyIdDiv.dataset.progenyId;
        if (progenyIdDivData) {
            currentProgenyId = parseInt(progenyIdDivData);
        }
    }
}

async function setTagsList(progenyId: number) {
    const tagsList = await getTagsList(progenyId);
    let tagListElement: any = $('input[id="tagList"');
    tagListElement.amsifySuggestags({
        suggestions: tagsList.tags
    });
}

$(async function (): Promise<void> {
    setLanguageId();
    setMomentLocale();
    setDateTimeFormats();
    setProgenyId();
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
        
    await setTagsList(currentProgenyId);

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsList(currentProgenyId);
        });
    }

});