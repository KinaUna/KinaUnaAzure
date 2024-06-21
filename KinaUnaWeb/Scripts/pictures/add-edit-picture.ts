import * as LocaleHelper from '../localization-v6.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v6.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let currentProgenyId: number;
let toggleEditButton;
let copyLocationButton;
declare var copyLocationList: any;

/**
 * Configures the date time picker for the picture date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    if (document.getElementById('picture-date-time-picker') !== null) {
        const dateTimePicker: any = $('#picture-date-time-picker');
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
 * Sets up the Progeny select list and adds an event listener to update the tags and location auto suggest lists when the selected Progeny changes.
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
 * Adds an event listener to the edit button to toggle the edit section.
 * Shows/hides the edit section when the edit button is clicked.
 */
function addEditButtonEventListener(): void {
    toggleEditButton = document.querySelector<HTMLButtonElement>('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });
    }
}

/**
 * Adds an event listener to the copy location button to copy the selected location to the latitude and longitude fields.
 */
function addCopyLocationButtonEventListener(): void {
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
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();

    await setupDateTimePicker();
    setupProgenySelectList();
    await setTagsAutoSuggestList(currentProgenyId);
    await setLocationAutoSuggestList(currentProgenyId);
    
    addEditButtonEventListener();
    addCopyLocationButtonEventListener();
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});