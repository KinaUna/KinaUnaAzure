import * as LocaleHelper from '../localization-v2.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v2.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;
let toggleEditBtn;
let copyLocationButton;
declare var copyLocationList: any;

$(async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    currentProgenyId = getCurrentProgenyId();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    if (document.getElementById('datetimepicker1') !== null) {
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
    }

    await setTagsAutoSuggestList(currentProgenyId);
    await setLocationAutoSuggestList(currentProgenyId);

    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#progenyIdSelect');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList(currentProgenyId);
            await setLocationAutoSuggestList(currentProgenyId);
        });
    }

    toggleEditBtn = document.querySelector<HTMLButtonElement>('#toggleEditBtn');
    if (toggleEditBtn !== null) {
        $("#toggleEditBtn").on('click', function () {
            $("#editSection").toggle(500);
        });
    }

    copyLocationButton = document.querySelector<HTMLButtonElement>('#copyLocationButton');
    if (copyLocationButton !== null) {
        $('#copyLocationButton').on('click', function () {
            let locId = Number($('#copyLocation').val());
            let selectedLoc = copyLocationList.find((obj: { id: number; name: string; lat: number, lng: number }) => { return obj.id === locId });
            $('#latitude').val(selectedLoc.lat);
            $('#longitude').val(selectedLoc.lng);
        });
    }
});