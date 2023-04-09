import * as LocaleHelper from '../localization.js';
import { getTagsList, getContextsList, getCurrentProgenyId } from '../data-tools.js';
let currentMomentLocale = 'en';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
function setLanguageId() {
    const languageIdDiv = document.querySelector('#languageIdDiv');
    if (languageIdDiv !== null) {
        const languageIdData = languageIdDiv.dataset.languageId;
        if (languageIdData) {
            languageId = parseInt(languageIdData);
        }
    }
}
function setMomentLocale() {
    const currentMomentLocaleDiv = document.querySelector('#currentMomentLocaleDiv');
    if (currentMomentLocaleDiv !== null) {
        const currentLocaleData = currentMomentLocaleDiv.dataset.currentLocale;
        if (currentLocaleData) {
            currentMomentLocale = currentLocaleData;
        }
    }
    moment.locale(currentMomentLocale);
}
function setDateTimeFormats() {
    const zebraDateTimeFormatDiv = document.querySelector('#zebraDateTimeFormatDiv');
    if (zebraDateTimeFormatDiv !== null) {
        const zebraDateTimeFormatData = zebraDateTimeFormatDiv.dataset.zebraDateTimeFormat;
        if (zebraDateTimeFormatData) {
            zebraDateTimeFormat = zebraDateTimeFormatData;
        }
    }
}
async function setTagsAutoSuggestList(progenyId) {
    let tagListElement = document.getElementById('tagList');
    if (tagListElement !== null) {
        const tagsList = await getTagsList(progenyId);
        $('#tagList').amsifySuggestags({
            suggestions: tagsList.suggestions
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function setContextAutoSuggestList(progenyId) {
    let contextInputElement = document.getElementById('contextInput');
    if (contextInputElement !== null) {
        const contextsList = await getContextsList(progenyId);
        $('#contextInput').amsifySuggestags({
            suggestions: contextsList.suggestions
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
$(async function () {
    setLanguageId();
    setMomentLocale();
    setDateTimeFormats();
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
    await setTagsAutoSuggestList(currentProgenyId);
    await setContextAutoSuggestList(currentProgenyId);
    const progenyIdSelect = document.querySelector('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList(currentProgenyId);
            await setContextAutoSuggestList(currentProgenyId);
        });
    }
});
//# sourceMappingURL=add-edit-contact.js.map