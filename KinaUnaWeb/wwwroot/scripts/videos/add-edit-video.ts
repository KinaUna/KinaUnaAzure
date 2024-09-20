import * as LocaleHelper from '../localization-v8.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;
let toggleEditButton;
let copyLocationButton;
declare var copyLocationList: any;

/**
 * Configures the date time picker for the video date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    if (document.getElementById('video-date-time-picker') !== null) {
        const dateTimePicker: any = $('#video-date-time-picker');
        dateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up the Progeny select list and adds an event listener to update the tags and locations auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList(): void {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList(currentProgenyId);
            await setLocationAutoSuggestList(currentProgenyId);
        });
    }
}

/**
 * Sets up the Copy Location button and adds an event listener to copy the selected location to the latitude and longitude fields.
 */
function setupCopyLocationButton(): void {
    copyLocationButton = document.querySelector<HTMLButtonElement>('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', function () {
            const latitudeInput = document.getElementById('latitude') as HTMLInputElement;
            const longitudeInput = document.getElementById('longitude') as HTMLInputElement;
            const locationSelect = document.getElementById('copy-location') as HTMLSelectElement;

            if (latitudeInput !== null && longitudeInput !== null && locationSelect !== null) {
                let locId = parseInt(locationSelect.value);
                let selectedLocation = copyLocationList.find((obj: { id: number; name: string; lat: number, lng: number }) => { return obj.id === locId });

                latitudeInput.setAttribute('value', selectedLocation.lat);
                longitudeInput.setAttribute('value', selectedLocation.lng);
            }
        });
    }
}

/**
 * Sets up the Edit button and adds an event listener to toggle show/hide edit section.
 */
function setupEditButton(): void {
    toggleEditButton = document.querySelector<HTMLButtonElement>('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });
    }
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();

    await setupDateTimePicker();
    setupProgenySelectList();
    await setTagsAutoSuggestList(currentProgenyId);
    await setLocationAutoSuggestList(currentProgenyId);
    setupCopyLocationButton();    
    setupEditButton();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});