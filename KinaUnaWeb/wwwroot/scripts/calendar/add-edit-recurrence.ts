function setupFrequencySelectList() {
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    if (frequencySelect !== null) {
        frequencySelect.addEventListener('change', onFrequencySelectListChanged);
        const frequencyValue = parseInt(frequencySelect.value);
        setRecurrenceElements(frequencyValue);
    }

    const eventEndOptionsSelect = document.querySelector<HTMLSelectElement>('#event-end-option-select');
    if (eventEndOptionsSelect !== null) {
        eventEndOptionsSelect.addEventListener('change', onEventEndOptionsSelectListChanged);
    }
}

function onEventEndOptionsSelectListChanged() {
    updateEventRepeatDetailsDiv();
}

function onFrequencySelectListChanged() {
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    if (frequencySelect !== null) {
        const frequencyValue = parseInt(frequencySelect.value);
        setRecurrenceElements(frequencyValue);

    }
}
function setRecurrenceElements(frequencyValue: number) {
    const eventIntervalInputDiv = document.querySelector<HTMLDivElement>('#event-interval-input-div');
    const eventRepeatDetailsDiv = document.querySelector<HTMLDivElement>('#event-repeat-details-div');
    const eventRepeatUntilDiv = document.querySelector<HTMLDivElement>('#event-repeat-until-div');

    const eventIntervalDaySuffixDiv = document.querySelector<HTMLDivElement>('#event-interval-day-suffix-div');
    const eventIntervalWeekSuffixDiv = document.querySelector<HTMLDivElement>('#event-interval-week-suffix-div');
    const eventIntervalMonthSuffixDiv = document.querySelector<HTMLDivElement>('#event-interval-month-suffix-div');
    const eventIntervalYearSuffixDiv = document.querySelector<HTMLDivElement>('#event-interval-year-suffix-div');

    eventIntervalDaySuffixDiv?.classList.add('d-none');
    eventIntervalWeekSuffixDiv?.classList.add('d-none');
    eventIntervalMonthSuffixDiv?.classList.add('d-none');
    eventIntervalYearSuffixDiv?.classList.add('d-none');

    if (frequencyValue === 0) {
        eventIntervalInputDiv?.classList.add('d-none');
        eventRepeatUntilDiv?.classList.add('d-none');
        eventRepeatDetailsDiv?.classList.add('d-none');

    } else {
        eventIntervalInputDiv?.classList.remove('d-none');
        eventRepeatUntilDiv?.classList.remove('d-none');
        eventRepeatDetailsDiv?.classList.remove('d-none');

        updateEventRepeatDetailsDiv();
    }

    const eventRepeatDailyDiv = document.querySelector<HTMLDivElement>('#event-repeat-daily-div');
    if (frequencyValue === 1) {
        eventRepeatDailyDiv?.classList.remove('d-none');
        eventIntervalDaySuffixDiv?.classList.remove('d-none');
    }
    else {
        eventRepeatDailyDiv?.classList.add('d-none');
    }

    const eventRepeatWeeklyDiv = document.querySelector<HTMLDivElement>('#event-repeat-weekly-div');
    if (frequencyValue === 2) {
        eventRepeatWeeklyDiv?.classList.remove('d-none');
        eventIntervalWeekSuffixDiv?.classList.remove('d-none');
    }
    else {
        eventRepeatWeeklyDiv?.classList.add('d-none');
    }
    const eventRepeatMonthlyDiv = document.querySelector<HTMLDivElement>('#event-repeat-monthly-div');
    if (frequencyValue === 3) {
        eventRepeatMonthlyDiv?.classList.remove('d-none');
        eventIntervalMonthSuffixDiv?.classList.remove('d-none');
    }
    else {
        eventRepeatMonthlyDiv?.classList.add('d-none');
    }

    const eventRepeatYearlyDiv = document.querySelector<HTMLDivElement>('#event-repeat-yearly-div');
    if (frequencyValue === 4) {
        eventRepeatYearlyDiv?.classList.remove('d-none');
        eventIntervalYearSuffixDiv?.classList.remove('d-none');
    }
    else {
        eventRepeatYearlyDiv?.classList.add('d-none');
    }
}



function updateEventRepeatDetailsDiv() {
    const eventEndOptionsSelect = document.querySelector<HTMLSelectElement>('#event-end-option-select');
    if (eventEndOptionsSelect !== null) {
        const eventEndOptionsValue = parseInt(eventEndOptionsSelect.value);
        const eventRepeatUntilDateDiv = document.querySelector<HTMLDivElement>('#event-repeat-until-date-div');
        const eventRepeatUntilCountDiv = document.querySelector<HTMLDivElement>('#event-repeat-until-count-div');

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
    const weekDayButtons = document.querySelectorAll<HTMLButtonElement>('.weekday-icon');
    weekDayButtons.forEach((weekDayButton) => {
        weekDayButton.addEventListener('click', onWeekDayButtonClicked);
    });
}

function onWeekDayButtonClicked(evt: MouseEvent) {
    evt.preventDefault();
    const weekDayButtonElement = evt.currentTarget as HTMLButtonElement;

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
    const monthDayButtons = document.querySelectorAll<HTMLButtonElement>('.monthday-icon');
    monthDayButtons.forEach((monthDayButton) => {
        monthDayButton.addEventListener('click', onMonthDayButtonClicked);
    });

    const yearlyMonthDayButtons = document.querySelectorAll<HTMLButtonElement>('.yearly-monthday-icon');
    yearlyMonthDayButtons.forEach((monthDayButton) => {
        monthDayButton.addEventListener('click', onMonthDayButtonClicked);
    });
}

function onMonthDayButtonClicked(evt: MouseEvent) {
    evt.preventDefault();
    const monthDayButtonElement = evt.currentTarget as HTMLButtonElement;

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
    const dayNumberTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-monthly-day-number-type-radio');
    const dayPatternTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-monthly-day-pattern-type-radio');

    if (dayNumberTypeRadioButton !== null) {
        dayNumberTypeRadioButton.addEventListener('change', onMonthlyTypeRadioButtonChanged);
    }

    if (dayPatternTypeRadioButton !== null) {
        dayPatternTypeRadioButton.addEventListener('change', onMonthlyTypeRadioButtonChanged);
    }
}

function onMonthlyTypeRadioButtonChanged() {
    const dayNumberTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-monthly-day-number-type-radio');
    const dayPatternTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-monthly-day-pattern-type-radio');
    const repeatByMonthSelectedType = document.querySelector<HTMLInputElement>('#event-repeat-monthly-type-selected-input');
    if (dayNumberTypeRadioButton !== null && dayPatternTypeRadioButton !== null) {
        const repeatByMonthDayNumberDiv = document.querySelector<HTMLDivElement>('#event-repeat-monthly-on-day-number-div');
        const repeatByMonthDaysPatternDiv = document.querySelector<HTMLDivElement>('#event-repeat-monthly-on-days-pattern-div');
        if (dayNumberTypeRadioButton.checked) {
            repeatByMonthDayNumberDiv?.classList.remove('d-none');
            repeatByMonthDaysPatternDiv?.classList.add('d-none');
            repeatByMonthSelectedType?.setAttribute('value', '0');
        }
        else {
            repeatByMonthDayNumberDiv?.classList.add('d-none');
            repeatByMonthDaysPatternDiv?.classList.remove('d-none');
            repeatByMonthSelectedType?.setAttribute('value', '1');
        }
    }
}

function addMonthDayNumberIconButtonEventListeners() {
    const monthDayNumberButtons = document.querySelectorAll<HTMLButtonElement>('.month-day-number-icon');
    monthDayNumberButtons.forEach((monthDayNumberButton) => {
        monthDayNumberButton.addEventListener('click', onMonthDayNumberButtonClicked);
    });

    const yearlyDayNumberButtons = document.querySelectorAll<HTMLButtonElement>('.yearly-month-day-number-icon');
    yearlyDayNumberButtons.forEach((monthDayNumberButton) => {
        monthDayNumberButton.addEventListener('click', onMonthDayNumberButtonClicked);
    });
}

function onMonthDayNumberButtonClicked(evt: MouseEvent) {
    evt.preventDefault();
    const monthDayNumberButtonElement = evt.currentTarget as HTMLButtonElement;

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
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    const monthlyDayNumberInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-by-date-input-div');
    if (frequencySelect === null || monthlyDayNumberInput === null) return;

    const frequencyValue = parseInt(frequencySelect.value);

    const monthDayNumbers: string[] = [];
    let monthDayNumberButtons: NodeListOf<HTMLButtonElement> | null = null;
    if (frequencyValue === 3) {
        
        monthDayNumberButtons = document.querySelectorAll<HTMLButtonElement>('.month-day-number-icon');
               
    }
    if (frequencyValue === 4) {

        monthDayNumberButtons = document.querySelectorAll<HTMLButtonElement>('.yearly-month-day-number-icon');

    }

    if (monthDayNumberButtons === null) return;

    monthDayNumberButtons.forEach((monthDayNumberButton) => {
        if (monthDayNumberButton.classList.contains('selected')) {
            if (monthDayNumberButton.dataset.monthday) {
                monthDayNumbers.push(monthDayNumberButton.dataset.monthday);
            }
        }
    }); 
    
    monthlyDayNumberInput.value = monthDayNumbers.join(',');
    
}

function updateSelectedWeekDaysInput() {
    // Get all selected days
    const selectedWeekDaysButtons = document.querySelectorAll<HTMLButtonElement>('.weekday-icon');
    const selectedDaysList: string[] = [];
    selectedWeekDaysButtons.forEach((weekDayButtonElement) => {
        if (weekDayButtonElement.classList.contains('selected')) {

            if (weekDayButtonElement.dataset.weekday) {
                selectedDaysList.push(weekDayButtonElement.dataset.weekday);
            }
        }
    });

    const eventRepeatWeeklyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-weekly-days-input');
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
    const byDayInput = document.querySelector<HTMLInputElement>('#event-recurrence-byday-input');
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    if (byDayInput !== null && frequencySelect !== null) {
        const frequencyValue = parseInt(frequencySelect.value);
        byDayInput.value = '';
        if (frequencyValue < 2) {
            return;
        }

        if (frequencyValue === 2) {
            const eventRepeatWeeklyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-weekly-days-input');
            if (eventRepeatWeeklyDaysInput !== null) {
                byDayInput.value = eventRepeatWeeklyDaysInput.value;
            }
            return;
        }

        if (frequencyValue === 3) {
            const eventRepeatMonthlyDayInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-days-input');
            if (eventRepeatMonthlyDayInput !== null) {
                byDayInput.value = eventRepeatMonthlyDayInput.value;
            }
            return;
        }

        if (frequencyValue === 4) {
            const eventRepeatYearlyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-yearly-days-input');
            if (eventRepeatYearlyDaysInput !== null) {
                byDayInput.value = eventRepeatYearlyDaysInput.value;
            }
            return;
        }
    }
}

function initializeRepeatWeekDaysInput() {
    const selectedWeekDaysButtons = document.querySelectorAll<HTMLButtonElement>('.weekday-icon');
    selectedWeekDaysButtons.forEach((buttonElement) => {
        buttonElement.classList.remove('selected');
    });
    const eventRepeatWeeklyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-weekly-days-input');
    if (eventRepeatWeeklyDaysInput !== null) {
        const selectedDays = eventRepeatWeeklyDaysInput.value.split(',');
        selectedDays.forEach((day) => {
            // Get the button with the data-weekday attribute that matches the day
            const dayButton = document.querySelector<HTMLButtonElement>('.weekday-icon[data-weekday="' + day + '"]');
            if (dayButton !== null) {
                dayButton.classList.add('selected');
            }
        });
    }
}

function initializeRepeatMonthlyTypeRadioButtons() {
    const byMonthDayInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-by-date-input-div');
    if (byMonthDayInput !== null) {
        const dayNumberTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-monthly-day-number-type-radio');
        const dayPatternTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-monthly-day-pattern-type-radio');
        const repeatByMonthDayNumberDiv = document.querySelector<HTMLDivElement>('#event-repeat-monthly-on-day-number-div');
        const repeatByMonthDaysPatternDiv = document.querySelector<HTMLDivElement>('#event-repeat-monthly-on-days-pattern-div');
        const repeatByMonthSelectedType = document.querySelector<HTMLInputElement>('#event-repeat-monthly-type-selected-input');

        if (dayNumberTypeRadioButton !== null && dayPatternTypeRadioButton !== null) {
            let byMonthDayStringArray: string[] = byMonthDayInput.value.split(',');
            let byMonthDayIntArray: number[] = [];
            byMonthDayStringArray.forEach((dayString) => {
                let parsedDayNumber = parseInt(dayString);
                if (!isNaN(parsedDayNumber) && parsedDayNumber !== 0) {
                    byMonthDayIntArray.push(parsedDayNumber);
                }
            });
            if (byMonthDayIntArray.length > 0) {
                dayNumberTypeRadioButton.checked = true;
                dayPatternTypeRadioButton.checked = false;
                repeatByMonthDayNumberDiv?.classList.remove('d-none');
                repeatByMonthDaysPatternDiv?.classList.add('d-none');
                repeatByMonthSelectedType?.setAttribute('value', '0');
            }
            else {
                dayNumberTypeRadioButton.checked = false;
                dayPatternTypeRadioButton.checked = true;
                repeatByMonthDayNumberDiv?.classList.add('d-none');
                repeatByMonthDaysPatternDiv?.classList.remove('d-none');
                repeatByMonthSelectedType?.setAttribute('value', '1');
            }
        }

    }
}

function initializeRepeatMonthDayNumberInput() {
    const monthlyDayNumberInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-by-date-input-div');
    let monthlyDayNumbers: string[] = [];
    if (monthlyDayNumberInput !== null) {
        monthlyDayNumbers = monthlyDayNumberInput.value.split(',');
        monthlyDayNumbers.forEach((dayNumber) => {
            const monthlyDayButton = document.querySelector<HTMLButtonElement>('.month-day-number-icon[data-monthday="' + dayNumber + '"]');
            if (monthlyDayButton !== null) {
                monthlyDayButton.classList.add('selected');
            }

            const yearlyDayButton = document.querySelector<HTMLButtonElement>('.yearly-month-day-number-icon[data-monthday="' + dayNumber + '"]');
            if (yearlyDayButton !== null) {
                yearlyDayButton.classList.add('selected');
            }
        });
    }
}
function initializeRepeatMonthDaysInput() {
    const monthlyWeekDaysButtons = document.querySelectorAll<HTMLButtonElement>('.monthday-icon');
    monthlyWeekDaysButtons.forEach((buttonElement) => {
        buttonElement.classList.remove('selected');
    });

    const selectedMonthlyDaysPrefixes = document.querySelectorAll<HTMLInputElement>('.event-repeat-monthly-by-day-prefix-checkbox');
    selectedMonthlyDaysPrefixes.forEach((prefixElement) => {
        prefixElement.checked = false;
    });

    const eventRepeatMonthlyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-days-input');
    if (eventRepeatMonthlyDaysInput !== null && selectedMonthlyDaysPrefixes !== null) {
        const selectedDays = eventRepeatMonthlyDaysInput.value.split(',');
        selectedDays.forEach((day) => {

            // Get the input with the dayStartString value
            let dayStartString: string = day.slice(0, -2);
            const dayPrefixCheckbox = document.querySelector<HTMLInputElement>('.event-repeat-monthly-by-day-prefix-checkbox[value="' + dayStartString + '"]');
            if (dayPrefixCheckbox !== null) {
                dayPrefixCheckbox.checked = true;
            }

            // Get the button with the data-weekday attribute that matches the day
            let dayEndString: string = day.slice(-2);
            const dayButton = document.querySelector<HTMLButtonElement>('.monthday-icon[data-weekday="' + dayEndString + '"]');
            if (dayButton !== null) {
                dayButton.classList.add('selected');
            }
        });
    }
}

function addMonthlyByDayPrefixEventListeners() {
    const monthlyByDayPrefixCheckboxes = document.querySelectorAll<HTMLInputElement>('.event-repeat-monthly-by-day-prefix-checkbox');
    monthlyByDayPrefixCheckboxes.forEach((monthlyByDayPrefixCheckbox) => {
        monthlyByDayPrefixCheckbox.addEventListener('change', onMonthlyByDayPrefixCheckboxChanged);
    });

    const yearlyByDayPrefixCheckboxes = document.querySelectorAll<HTMLInputElement>('.event-repeat-yearly-by-day-prefix-checkbox');
    yearlyByDayPrefixCheckboxes.forEach((yearlyByDayPrefixCheckbox) => {
        yearlyByDayPrefixCheckbox.addEventListener('change', onMonthlyByDayPrefixCheckboxChanged);
    });
}

function onMonthlyByDayPrefixCheckboxChanged() {
    updateRepeatMonthlyDaysInput();
}

function updateRepeatMonthlyDaysInput() {
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    if (frequencySelect === null) return;

    const frequencyValue = parseInt(frequencySelect.value);
    const selectedPrefixes: string[] = [];
    const selectedMonthDays: string[] = [];

    if (frequencyValue === 2) {
        const monthlyByDayPrefixCheckboxes = document.querySelectorAll<HTMLInputElement>('.event-repeat-monthly-by-day-prefix-checkbox');
        
        monthlyByDayPrefixCheckboxes.forEach((monthlyByDayPrefixCheckbox) => {
            if (monthlyByDayPrefixCheckbox.checked) {
                selectedPrefixes.push(monthlyByDayPrefixCheckbox.value);
            }
        });
        
        const monthDayButtons = document.querySelectorAll<HTMLButtonElement>('.monthday-icon');

        monthDayButtons.forEach((monthDayButton) => {
            if (monthDayButton.classList.contains('selected')) {
                if (monthDayButton.dataset.weekday) {
                    selectedMonthDays.push(monthDayButton.dataset.weekday);
                }
            }
        });

        const eventRepeatMonthlyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-days-input');
        if (eventRepeatMonthlyDaysInput !== null) {
            eventRepeatMonthlyDaysInput.value = '';
            const byDayArray: string[] = [];
            if (selectedPrefixes.length > 0 && selectedMonthDays.length > 0) {
                selectedPrefixes.forEach((prefix) => {
                    selectedMonthDays.forEach((day) => {
                        byDayArray.push(prefix + day);
                    });
                });

                eventRepeatMonthlyDaysInput.value = byDayArray.join(',');
            }
        }
    }

    if (frequencyValue === 3) {
        const monthlyByDayPrefixCheckboxes = document.querySelectorAll<HTMLInputElement>('.event-repeat-yearly-by-day-prefix-checkbox');

        monthlyByDayPrefixCheckboxes.forEach((monthlyByDayPrefixCheckbox) => {
            if (monthlyByDayPrefixCheckbox.checked) {
                selectedPrefixes.push(monthlyByDayPrefixCheckbox.value);
            }
        });

        const monthDayButtons = document.querySelectorAll<HTMLButtonElement>('.yearly-monthday-icon');

        monthDayButtons.forEach((monthDayButton) => {
            if (monthDayButton.classList.contains('selected')) {
                if (monthDayButton.dataset.weekday) {
                    selectedMonthDays.push(monthDayButton.dataset.weekday);
                }
            }
        });

        const eventRepeatMonthlyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-days-input');
        if (eventRepeatMonthlyDaysInput !== null) {
            eventRepeatMonthlyDaysInput.value = '';
            const byDayArray: string[] = [];
            if (selectedPrefixes.length > 0 && selectedMonthDays.length > 0) {
                selectedPrefixes.forEach((prefix) => {
                    selectedMonthDays.forEach((day) => {
                        byDayArray.push(prefix + day);
                    });
                });

                eventRepeatMonthlyDaysInput.value = byDayArray.join(',');
            }
        }
    }
    

    updateRecurrenceByDayInput();
}

function initializeRepeatYearDaysInput() {
    const yearlyWeekDaysButtons = document.querySelectorAll<HTMLButtonElement>('.yearly-monthday-icon');
    yearlyWeekDaysButtons.forEach((buttonElement) => {
        buttonElement.classList.remove('selected');
    });

    const selectedYearlyDaysPrefixes = document.querySelectorAll<HTMLInputElement>('.event-repeat-yearly-by-day-prefix-checkbox');
    selectedYearlyDaysPrefixes.forEach((prefixElement) => {
        prefixElement.checked = false;
    });

    const eventRepeatYearlyDaysInput = document.querySelector<HTMLInputElement>('#event-repeat-yearly-days-input');
    if (eventRepeatYearlyDaysInput !== null && selectedYearlyDaysPrefixes !== null) {
        const selectedDays = eventRepeatYearlyDaysInput.value.split(',');
        selectedDays.forEach((day) => {

            // Get the input with the dayStartString value
            let dayStartString: string = day.slice(0, -2);
            const dayPrefixCheckbox = document.querySelector<HTMLInputElement>('.event-repeat-yearly-by-day-prefix-checkbox[value="' + dayStartString + '"]');
            if (dayPrefixCheckbox !== null) {
                dayPrefixCheckbox.checked = true;
            }

            // Get the button with the data-weekday attribute that matches the day
            let dayEndString: string = day.slice(-2);
            const dayButton = document.querySelector<HTMLButtonElement>('.yearly-monthday-icon[data-weekday="' + dayEndString + '"]');
            if (dayButton !== null) {
                dayButton.classList.add('selected');
            }
        });
    }
}

function addYearlyMonthDayIconButtonEventListeners() {
    const monthDayButtons = document.querySelectorAll<HTMLButtonElement>('.yearly-monthday-icon');
    monthDayButtons.forEach((monthDayButton) => {
        monthDayButton.addEventListener('click', onYearlyMonthDayButtonClicked);
    });
}

function onYearlyMonthDayButtonClicked(evt: MouseEvent) {
    evt.preventDefault();
    const monthDayButtonElement = evt.currentTarget as HTMLButtonElement;

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

function initializeRepeatYearlyTypeRadioButtons() {
    const byMonthDayInput = document.querySelector<HTMLInputElement>('#event-repeat-monthly-by-date-input-div');
    if (byMonthDayInput !== null) {
        const dayNumberTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-yearly-day-number-type-radio');
        const dayPatternTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-yearly-day-pattern-type-radio');
        const repeatByMonthDayNumberDiv = document.querySelector<HTMLDivElement>('#event-repeat-yearly-on-day-number-div');
        const repeatByMonthDaysPatternDiv = document.querySelector<HTMLDivElement>('#event-repeat-yearly-on-pattern-div');
        const repeatByYearSelectedType = document.querySelector<HTMLInputElement>('#event-repeat-yearly-type-selected-input');

        if (dayNumberTypeRadioButton !== null && dayPatternTypeRadioButton !== null) {
            let byMonthDayStringArray: string[] = byMonthDayInput.value.split(',');
            let byMonthDayIntArray: number[] = [];
            byMonthDayStringArray.forEach((dayString) => {
                let parsedDayNumber = parseInt(dayString);
                if (!isNaN(parsedDayNumber) && parsedDayNumber !== 0) {
                    byMonthDayIntArray.push(parsedDayNumber);
                }
            });
            if (byMonthDayIntArray.length > 0) {
                dayNumberTypeRadioButton.checked = true;
                dayPatternTypeRadioButton.checked = false;
                repeatByMonthDayNumberDiv?.classList.remove('d-none');
                repeatByMonthDaysPatternDiv?.classList.add('d-none');
                repeatByYearSelectedType?.setAttribute('value', '0');
            }
            else {
                dayNumberTypeRadioButton.checked = false;
                dayPatternTypeRadioButton.checked = true;
                repeatByMonthDayNumberDiv?.classList.add('d-none');
                repeatByMonthDaysPatternDiv?.classList.remove('d-none');
                repeatByYearSelectedType?.setAttribute('value', '1');
            }
        }
    }
}

function addEventYearlyTypeRadioButtonsEventListeners() {
    const dayNumberTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-yearly-day-number-type-radio');
    const dayPatternTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-yearly-day-pattern-type-radio');

    if (dayNumberTypeRadioButton !== null) {
        dayNumberTypeRadioButton.addEventListener('change', onYearlyTypeRadioButtonChanged);
    }

    if (dayPatternTypeRadioButton !== null) {
        dayPatternTypeRadioButton.addEventListener('change', onYearlyTypeRadioButtonChanged);
    }
}

function onYearlyTypeRadioButtonChanged() {
    const dayNumberTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-yearly-day-number-type-radio');
    const dayPatternTypeRadioButton = document.querySelector<HTMLInputElement>('#event-repeat-yearly-day-pattern-type-radio');
    const repeatByYearSelectedType = document.querySelector<HTMLInputElement>('#event-repeat-yearly-type-selected-input');
    if (dayNumberTypeRadioButton !== null && dayPatternTypeRadioButton !== null) {
        const repeatByMonthDayNumberDiv = document.querySelector<HTMLDivElement>('#event-repeat-yearly-on-day-number-div');
        const repeatByMonthDaysPatternDiv = document.querySelector<HTMLDivElement>('#event-repeat-yearly-on-pattern-div');
        if (dayNumberTypeRadioButton.checked) {
            repeatByMonthDayNumberDiv?.classList.remove('d-none');
            repeatByMonthDaysPatternDiv?.classList.add('d-none');
            repeatByYearSelectedType?.setAttribute('value', '0');
        }
        else {
            repeatByMonthDayNumberDiv?.classList.add('d-none');
            repeatByMonthDaysPatternDiv?.classList.remove('d-none');
            repeatByYearSelectedType?.setAttribute('value', '1');
        }
    }
}

export function setupRecurrenceSection() {
    setupFrequencySelectList();

    addWeekDayIconButtonEventListeners();
    initializeRepeatWeekDaysInput();

    addMonthDayIconButtonEventListeners();
    initializeRepeatMonthDaysInput();
    initializeRepeatMonthDayNumberInput();
    addMonthlyByDayPrefixEventListeners();
    addMonthDayNumberIconButtonEventListeners();
    updateRepeatMonthlyDaysInput();
    initializeRepeatMonthlyTypeRadioButtons();
    addEventMonthlyTypeRadioButtonsEventListeners();

    initializeRepeatYearDaysInput();
    initializeRepeatYearlyTypeRadioButtons();
    addEventYearlyTypeRadioButtonsEventListeners();
}