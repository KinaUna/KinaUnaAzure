import * as LocaleHelper from '../localization.js';
import { getTagsList, getContextsList, getCurrentProgenyId } from '../data-tools.js';
declare let moment: any;

let currentMomentLocale: string = 'en';
let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
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
    const zebraDateTimeFormatDiv = document.querySelector<HTMLDivElement>('#zebraDateTimeFormatDiv');
    if (zebraDateTimeFormatDiv !== null) {
        const zebraDateTimeFormatData = zebraDateTimeFormatDiv.dataset.zebraDateTimeFormat;
        if (zebraDateTimeFormatData) {
            zebraDateTimeFormat = zebraDateTimeFormatData;
        }
    }
}



async function setTagsAutoSuggestList(progenyId: number) {
    let tagListElement = document.getElementById('tagList');
    if (tagListElement !== null) {
        const tagsList = await getTagsList(progenyId);

        ($('#tagList') as any).amsifySuggestags({
            suggestions: tagsList.suggestions
        });
    }    

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function setContextAutoSuggestList(progenyId: number) {
    let contextInputElement = document.getElementById('contextInput');
    if (contextInputElement !== null) {
        const contextsList = await getContextsList(progenyId);

        ($('#contextInput') as any).amsifySuggestags({
            suggestions: contextsList.suggestions
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

$(async function (): Promise<void> {
    setLanguageId();
    setMomentLocale();
    setDateTimeFormats();
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
    await setContextAutoSuggestList(currentProgenyId);

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList(currentProgenyId);
            await setContextAutoSuggestList(currentProgenyId);
        });
    }

});