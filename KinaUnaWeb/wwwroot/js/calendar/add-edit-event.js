import * as LocaleHelper from '../localization-v8.js';
import { setContextAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, checkStartBeforeEndTime, getZebraDateTimeFormat, getLongDateTimeFormatMoment } from '../data-tools-v8.js';
import { setupRemindersSection } from '../reminders/reminders.js';
let zebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment;
let zebraDateTimeFormat;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId;
let startDateTimePickerId = '#event-start-date-time-picker';
let endDateTimePickerId = '#event-end-date-time-picker';
/**
 * Validates that the start date is before the end date.
 * If the start date is after the end date, the submit button is disabled and a warning is shown.
 */
function validateDatePickerStartEnd() {
    const saveItemNotificationDiv = document.querySelector('#save-item-notification');
    const submitEventButton = document.querySelector('#submit-button');
    if (checkStartBeforeEndTime(startDateTimePickerId, endDateTimePickerId, longDateTimeFormatMoment, warningStartIsAfterEndString)) {
        if (saveItemNotificationDiv !== null) {
            saveItemNotificationDiv.textContent = '';
        }
        if (submitEventButton !== null) {
            submitEventButton.disabled = false;
        }
        //$('#submit-button').prop('disabled', false);
    }
    else {
        if (saveItemNotificationDiv !== null) {
            saveItemNotificationDiv.textContent = warningStartIsAfterEndString;
        }
        if (submitEventButton !== null) {
            submitEventButton.disabled = true;
        }
    }
}
/**
 * Sets up the date time picker for the Start and End input fields.
 * Also sets up event listeners for the input fields to validate that the start date is before the end date.
 * @returns
 */
async function setupDateTimePickers() {
    setMomentLocale();
    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-event-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);
    const startDateTimePicker = $(startDateTimePickerId);
    startDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const endDateTimePicker = $(endDateTimePickerId);
    endDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    validateDatePickerStartEnd();
    const startZebraPicker = document.querySelector(startDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.addEventListener('change', () => { validateDatePickerStartEnd(); });
        startZebraPicker.addEventListener('blur', () => { validateDatePickerStartEnd(); });
        startZebraPicker.addEventListener('focus', () => { validateDatePickerStartEnd(); });
    }
    const endZebraPicker = document.querySelector(endDateTimePickerId);
    if (endZebraPicker !== null) {
        endZebraPicker.addEventListener('change', () => { validateDatePickerStartEnd(); });
        endZebraPicker.addEventListener('blur', () => { validateDatePickerStartEnd(); });
        endZebraPicker.addEventListener('focus', () => { validateDatePickerStartEnd(); });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Progeny select list and adds an event listener to update the context and location auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}
async function onProgenySelectListChanged() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setContextAutoSuggestList([currentProgenyId]);
        await setLocationAutoSuggestList([currentProgenyId]);
    }
}
function setupFrequencySelectList() {
    const frequencySelect = document.querySelector('#event-repeat-frequency-select');
    if (frequencySelect !== null) {
        frequencySelect.addEventListener('change', onFrequencySelectListChanged);
    }
    const eventEndOptionsSelect = document.querySelector('#event-end-option-select');
    if (eventEndOptionsSelect !== null) {
        eventEndOptionsSelect.addEventListener('change', onEventEndOptionsSelectListChanged);
    }
}
function onEventEndOptionsSelectListChanged() {
    updateEventRepeatDetailsDiv();
}
function onFrequencySelectListChanged() {
    const frequencySelect = document.querySelector('#event-repeat-frequency-select');
    if (frequencySelect !== null) {
        const frequencyValue = parseInt(frequencySelect.value);
        const eventIntervalInputDiv = document.querySelector('#event-interval-input-div');
        const eventRepeatDetailsDiv = document.querySelector('#event-repeat-details-div');
        const eventRepeatUntilDiv = document.querySelector('#event-repeat-until-div');
        const eventIntervalDaySuffixDiv = document.querySelector('#event-interval-day-suffix-div');
        const eventIntervalWeekSuffixDiv = document.querySelector('#event-interval-week-suffix-div');
        const eventIntervalMonthSuffixDiv = document.querySelector('#event-interval-month-suffix-div');
        const eventIntervalYearSuffixDiv = document.querySelector('#event-interval-year-suffix-div');
        eventIntervalDaySuffixDiv?.classList.add('d-none');
        eventIntervalWeekSuffixDiv?.classList.add('d-none');
        eventIntervalMonthSuffixDiv?.classList.add('d-none');
        eventIntervalYearSuffixDiv?.classList.add('d-none');
        if (frequencyValue === 0) {
            eventIntervalInputDiv?.classList.add('d-none');
            eventRepeatUntilDiv?.classList.add('d-none');
            eventRepeatDetailsDiv?.classList.add('d-none');
        }
        else {
            eventIntervalInputDiv?.classList.remove('d-none');
            eventRepeatUntilDiv?.classList.remove('d-none');
            eventRepeatDetailsDiv?.classList.remove('d-none');
            updateEventRepeatDetailsDiv();
        }
        const eventRepeatDailyDiv = document.querySelector('#event-repeat-daily-div');
        if (frequencyValue === 1) {
            eventRepeatDailyDiv?.classList.remove('d-none');
            eventIntervalDaySuffixDiv?.classList.remove('d-none');
        }
        else {
            eventRepeatDailyDiv?.classList.add('d-none');
        }
        const eventRepeatWeeklyDiv = document.querySelector('#event-repeat-weekly-div');
        if (frequencyValue === 2) {
            eventRepeatWeeklyDiv?.classList.remove('d-none');
            eventIntervalWeekSuffixDiv?.classList.remove('d-none');
        }
        else {
            eventRepeatWeeklyDiv?.classList.add('d-none');
        }
        const eventRepeatMonthlyDiv = document.querySelector('#event-repeat-monthly-div');
        if (frequencyValue === 3) {
            eventRepeatMonthlyDiv?.classList.remove('d-none');
            eventIntervalMonthSuffixDiv?.classList.remove('d-none');
        }
        else {
            eventRepeatMonthlyDiv?.classList.add('d-none');
        }
        const eventRepeatYearlyDiv = document.querySelector('#event-repeat-yearly-div');
        if (frequencyValue === 4) {
            eventRepeatYearlyDiv?.classList.remove('d-none');
            eventIntervalYearSuffixDiv?.classList.remove('d-none');
        }
        else {
            eventRepeatYearlyDiv?.classList.add('d-none');
        }
    }
}
function updateEventRepeatDetailsDiv() {
    const eventEndOptionsSelect = document.querySelector('#event-end-option-select');
    if (eventEndOptionsSelect !== null) {
        const eventEndOptionsValue = parseInt(eventEndOptionsSelect.value);
        const eventRepeatUntilDateDiv = document.querySelector('#event-repeat-until-date-div');
        const eventRepeatUntilCountDiv = document.querySelector('#event-repeat-until-count-div');
        if (eventEndOptionsValue === 0) {
            eventRepeatUntilDateDiv?.classList.add('d-none');
            eventRepeatUntilCountDiv?.classList.add('d-none');
        }
        else {
            if (eventEndOptionsValue === 1) {
                eventRepeatUntilDateDiv?.classList.remove('d-none');
                eventRepeatUntilCountDiv?.classList.add('d-none');
            }
            else {
                eventRepeatUntilDateDiv?.classList.add('d-none');
                eventRepeatUntilCountDiv?.classList.remove('d-none');
            }
        }
    }
}
function addWeekDayIconButtonEventListeners() {
    const weekDayButtons = document.querySelectorAll('.weekday-icon');
    weekDayButtons.forEach((weekDayButton) => {
        weekDayButton.addEventListener('click', onWeekDayButtonClicked);
    });
}
function onWeekDayButtonClicked(evt) {
    evt.preventDefault();
    const weekDayButtonElement = evt.currentTarget;
    if (weekDayButtonElement !== null) {
        if (weekDayButtonElement.classList.contains('selected')) {
            weekDayButtonElement.classList.remove('selected');
        }
        else {
            weekDayButtonElement.classList.add('selected');
        }
        updateSelectedWeekDaysInput();
    }
}
function addMonthDayIconButtonEventListeners() {
    const monthDayButtons = document.querySelectorAll('.monthday-icon');
    monthDayButtons.forEach((monthDayButton) => {
        monthDayButton.addEventListener('click', onMonthDayButtonClicked);
    });
}
function onMonthDayButtonClicked(evt) {
    evt.preventDefault();
    const monthDayButtonElement = evt.currentTarget;
    if (monthDayButtonElement !== null) {
        if (monthDayButtonElement.classList.contains('selected')) {
            monthDayButtonElement.classList.remove('selected');
        }
        else {
            monthDayButtonElement.classList.add('selected');
        }
        updateSelectedMonthDaysInput();
    }
}
function addEventMonthlyTypeRadioButtonsEventListeners() {
    const dayNumberTypeRadioButton = document.querySelector('#event-repeat-monthly-day-number-type-radio');
    const dayPatternTypeRadioButton = document.querySelector('#event-repeat-monthly-day-pattern-type-radio');
    if (dayNumberTypeRadioButton !== null) {
        dayNumberTypeRadioButton.addEventListener('change', onMonthlyTypeRadioButtonChanged);
    }
    if (dayPatternTypeRadioButton !== null) {
        dayPatternTypeRadioButton.addEventListener('change', onMonthlyTypeRadioButtonChanged);
    }
}
function onMonthlyTypeRadioButtonChanged() {
    const dayNumberTypeRadioButton = document.querySelector('#event-repeat-monthly-day-number-type-radio');
    const dayPatternTypeRadioButton = document.querySelector('#event-repeat-monthly-day-pattern-type-radio');
    if (dayNumberTypeRadioButton !== null && dayPatternTypeRadioButton !== null) {
        const repeatByMonthDayNumberDiv = document.querySelector('#event-repeat-monthly-on-day-number-div');
        const repeatByMonthDaysPatternDiv = document.querySelector('#event-repeat-monthly-on-days-pattern-div');
        if (dayNumberTypeRadioButton.checked) {
            repeatByMonthDayNumberDiv?.classList.remove('d-none');
            repeatByMonthDaysPatternDiv?.classList.add('d-none');
        }
        else {
            repeatByMonthDayNumberDiv?.classList.add('d-none');
            repeatByMonthDaysPatternDiv?.classList.remove('d-none');
        }
    }
}
function addMonthDayNumberIconButtonEventListeners() {
    const monthDayNumberButtons = document.querySelectorAll('.month-day-number-icon');
    monthDayNumberButtons.forEach((monthDayNumberButton) => {
        monthDayNumberButton.addEventListener('click', onMonthDayNumberButtonClicked);
    });
}
function onMonthDayNumberButtonClicked(evt) {
    evt.preventDefault();
    const monthDayNumberButtonElement = evt.currentTarget;
    if (monthDayNumberButtonElement !== null) {
        if (monthDayNumberButtonElement.classList.contains('selected')) {
            monthDayNumberButtonElement.classList.remove('selected');
        }
        else {
            monthDayNumberButtonElement.classList.add('selected');
        }
        updateSelectedMonthDayNumbersInput();
    }
}
function updateSelectedMonthDayNumbersInput() {
    const monthlyDayNumberInput = document.querySelector('#event-repeat-monthly-by-date-input-div');
    const monthDayNumberButtons = document.querySelectorAll('.month-day-number-icon');
    const monthDayNumbers = [];
    if (monthlyDayNumberInput !== null) {
        monthDayNumberButtons.forEach((monthDayNumberButton) => {
            if (monthDayNumberButton.classList.contains('selected')) {
                if (monthDayNumberButton.dataset.monthday) {
                    monthDayNumbers.push(monthDayNumberButton.dataset.monthday);
                }
            }
        });
        monthlyDayNumberInput.value = monthDayNumbers.join(',');
    }
}
function updateSelectedWeekDaysInput() {
    // Get all selected days
    const selectedWeekDaysButtons = document.querySelectorAll('.weekday-icon');
    const selectedDaysList = [];
    selectedWeekDaysButtons.forEach((weekDayButtonElement) => {
        if (weekDayButtonElement.classList.contains('selected')) {
            if (weekDayButtonElement.dataset.weekday) {
                selectedDaysList.push(weekDayButtonElement.dataset.weekday);
            }
        }
    });
    const eventRepeatWeeklyDaysInput = document.querySelector('#event-repeat-weekly-days-input');
    // Add selected days to input field
    if (eventRepeatWeeklyDaysInput !== null) {
        eventRepeatWeeklyDaysInput.value = selectedDaysList.join(',');
    }
    updateRecurrenceByDayInput();
}
function updateSelectedMonthDaysInput() {
    updateRepeatMonthlyDaysInput();
}
function updateRecurrenceByDayInput() {
    const byDayInput = document.querySelector('#event-recurrence-byday-input');
    const frequencySelect = document.querySelector('#event-repeat-frequency-select');
    if (byDayInput !== null && frequencySelect !== null) {
        const frequencyValue = parseInt(frequencySelect.value);
        byDayInput.value = '';
        if (frequencyValue < 2) {
            return;
        }
        if (frequencyValue === 2) {
            const eventRepeatWeeklyDaysInput = document.querySelector('#event-repeat-weekly-days-input');
            if (eventRepeatWeeklyDaysInput !== null) {
                byDayInput.value = eventRepeatWeeklyDaysInput.value;
            }
            return;
        }
        if (frequencyValue === 3) {
            const eventRepeatMonthlyDayInput = document.querySelector('#event-repeat-monthly-days-input');
            if (eventRepeatMonthlyDayInput !== null) {
                byDayInput.value = eventRepeatMonthlyDayInput.value;
            }
            return;
        }
        if (frequencyValue === 4) {
            const eventRepeatYearlyDaysInput = document.querySelector('#event-repeat-yearly-days-input');
            if (eventRepeatYearlyDaysInput !== null) {
                byDayInput.value = eventRepeatYearlyDaysInput.value;
            }
            return;
        }
    }
}
function initializeRepeatWeekDaysInput() {
    const selectedWeekDaysButtons = document.querySelectorAll('.weekday-icon');
    selectedWeekDaysButtons.forEach((buttonElement) => {
        buttonElement.classList.remove('selected');
    });
    const eventRepeatWeeklyDaysInput = document.querySelector('#event-repeat-weekly-days-input');
    if (eventRepeatWeeklyDaysInput !== null) {
        const selectedDays = eventRepeatWeeklyDaysInput.value.split(',');
        selectedDays.forEach((day) => {
            // Get the button with the data-weekday attribute that matches the day
            const dayButton = document.querySelector('.weekday-icon[data-weekday="' + day + '"]');
            if (dayButton !== null) {
                dayButton.classList.add('selected');
            }
        });
    }
}
function initializeRepeatMonthlyTypeRadioButtons() {
    const byMonthDayInput = document.querySelector('#event-repeat-monthly-by-date-input-div');
    if (byMonthDayInput !== null) {
        const dayNumberTypeRadioButton = document.querySelector('#event-repeat-monthly-day-number-type-radio');
        const dayPatternTypeRadioButton = document.querySelector('#event-repeat-monthly-day-pattern-type-radio');
        const repeatByMonthDayNumberDiv = document.querySelector('#event-repeat-monthly-on-day-number-div');
        const repeatByMonthDaysPatternDiv = document.querySelector('#event-repeat-monthly-on-days-pattern-div');
        if (dayNumberTypeRadioButton !== null && dayPatternTypeRadioButton !== null) {
            let byMonthDayStringArray = byMonthDayInput.value.split(',');
            let byMonthDayIntArray = [];
            byMonthDayStringArray.forEach((dayString) => {
                let parsedDayNumber = parseInt(dayString);
                if (!isNaN(parsedDayNumber) && parsedDayNumber !== 0) {
                    byMonthDayIntArray.push(parsedDayNumber);
                }
            });
            if (byMonthDayIntArray.length > 1) {
                dayNumberTypeRadioButton.checked = true;
                dayPatternTypeRadioButton.checked = false;
                repeatByMonthDayNumberDiv?.classList.remove('d-none');
                repeatByMonthDaysPatternDiv?.classList.add('d-none');
            }
            else {
                dayNumberTypeRadioButton.checked = false;
                dayPatternTypeRadioButton.checked = true;
                repeatByMonthDayNumberDiv?.classList.add('d-none');
                repeatByMonthDaysPatternDiv?.classList.remove('d-none');
            }
        }
    }
}
function initializeRepeatMonthDayNumberInput() {
    const monthlyDayNumberInput = document.querySelector('#event-repeat-monthly-by-date-input-div');
    let monthlyDayNumbers = [];
    if (monthlyDayNumberInput !== null) {
        monthlyDayNumbers = monthlyDayNumberInput.value.split(',');
        monthlyDayNumbers.forEach((dayNumber) => {
            const dayButton = document.querySelector('.month-day-number-icon[data-monthday="' + dayNumber + '"]');
            if (dayButton !== null) {
                dayButton.classList.add('selected');
            }
        });
    }
}
function initializeRepeatMonthDaysInput() {
    const monthlyWeekDaysButtons = document.querySelectorAll('.monthday-icon');
    monthlyWeekDaysButtons.forEach((buttonElement) => {
        buttonElement.classList.remove('selected');
    });
    const selectedMonthlyDaysPrefixes = document.querySelectorAll('.event-repeat-mothly-by-day-prefix-checkbox');
    selectedMonthlyDaysPrefixes.forEach((prefixElement) => {
        prefixElement.checked = false;
    });
    const eventRepeatMonthlyDaysInput = document.querySelector('#event-repeat-monthly-days-input');
    if (eventRepeatMonthlyDaysInput !== null && selectedMonthlyDaysPrefixes !== null) {
        const selectedDays = eventRepeatMonthlyDaysInput.value.split(',');
        console.log(selectedDays);
        selectedDays.forEach((day) => {
            // Get the input with the dayStartString value
            let dayStartString = day.slice(0, -2);
            const dayPrefixCheckbox = document.querySelector('.event-repeat-mothly-by-day-prefix-checkbox[value="' + dayStartString + '"]');
            if (dayPrefixCheckbox !== null) {
                dayPrefixCheckbox.checked = true;
            }
            // Get the button with the data-weekday attribute that matches the day
            let dayEndString = day.slice(-2);
            const dayButton = document.querySelector('.monthday-icon[data-weekday="' + dayEndString + '"]');
            if (dayButton !== null) {
                dayButton.classList.add('selected');
            }
        });
    }
}
function addMonthlyByDayPrefixEventListeners() {
    const monthlyByDayPrefixCheckboxes = document.querySelectorAll('.event-repeat-mothly-by-day-prefix-checkbox');
    monthlyByDayPrefixCheckboxes.forEach((monthlyByDayPrefixCheckbox) => {
        monthlyByDayPrefixCheckbox.addEventListener('change', onMonthlyByDayPrefixCheckboxChanged);
    });
}
function onMonthlyByDayPrefixCheckboxChanged() {
    updateRepeatMonthlyDaysInput();
}
function updateRepeatMonthlyDaysInput() {
    const monthlyByDayPrefixCheckboxes = document.querySelectorAll('.event-repeat-mothly-by-day-prefix-checkbox');
    const selectedPrefixes = [];
    monthlyByDayPrefixCheckboxes.forEach((monthlyByDayPrefixCheckbox) => {
        if (monthlyByDayPrefixCheckbox.checked) {
            selectedPrefixes.push(monthlyByDayPrefixCheckbox.value);
        }
    });
    const selectedMonthDays = [];
    const monthDayButtons = document.querySelectorAll('.monthday-icon');
    monthDayButtons.forEach((monthDayButton) => {
        if (monthDayButton.classList.contains('selected')) {
            if (monthDayButton.dataset.weekday) {
                selectedMonthDays.push(monthDayButton.dataset.weekday);
            }
        }
    });
    const eventRepeatMonthlyDaysInput = document.querySelector('#event-repeat-monthly-days-input');
    if (eventRepeatMonthlyDaysInput !== null) {
        eventRepeatMonthlyDaysInput.value = '';
        const byDayArray = [];
        if (selectedPrefixes.length > 0 && selectedMonthDays.length > 0) {
            selectedPrefixes.forEach((prefix) => {
                selectedMonthDays.forEach((day) => {
                    byDayArray.push(prefix + day);
                });
            });
            eventRepeatMonthlyDaysInput.value = byDayArray.join(',');
        }
    }
    updateRecurrenceByDayInput();
}
export async function initializeAddEditEvent() {
    currentProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();
    await setContextAutoSuggestList([currentProgenyId]);
    await setLocationAutoSuggestList([currentProgenyId]);
    setupProgenySelectList();
    setupDateTimePickers();
    setupRemindersSection();
    setupFrequencySelectList();
    addWeekDayIconButtonEventListeners();
    addMonthDayIconButtonEventListeners();
    initializeRepeatWeekDaysInput();
    initializeRepeatMonthDaysInput();
    initializeRepeatMonthDayNumberInput();
    addMonthlyByDayPrefixEventListeners();
    addMonthDayNumberIconButtonEventListeners();
    updateRepeatMonthlyDaysInput();
    initializeRepeatMonthlyTypeRadioButtons();
    addEventMonthlyTypeRadioButtonsEventListeners();
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-event.js.map