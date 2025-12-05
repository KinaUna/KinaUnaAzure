import { setEditItemButtonEventListeners } from '../addItem/add-item-v11.js';
import { getCurrentLanguageId, getCurrentProgenyId, setMomentLocale } from '../data-tools-v11.js';
import { showPopupAtLoad } from '../item-details/items-display-v11.js';
import { getTranslation } from '../localization-v11.js';
import { startTopMenuSpinner, stopTopMenuSpinner } from '../navigation-tools-v11.js';
import { SleepDataModel, TimeLineType } from '../page-models-v11.js';
import { getProgenySelector } from '../shared/progeny-selector-v11.js';
let sleepChart;
async function getSleepTable(progenyId) {
    const sleepListTable = document.querySelector('#sleep-list-container-div');
    if (sleepListTable) {
        await fetch('/Sleep/SleepTable?progenyId=' + progenyId, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async function (sleepListResponse) {
            if (sleepListResponse != null) {
                const sleepListHtml = await sleepListResponse.text();
                sleepListTable.innerHTML = sleepListHtml;
                setupDataTable();
                setEditItemButtonEventListeners();
                await renderSleepChartData(progenyId);
            }
        });
    }
}
/**
 * Formats a date string to a timestamp.
 * @param dateString The date as a string.
 * @returns The date as a timestamp.
 */
function timestamp(dateString) {
    return new Date(dateString).getTime();
}
/**
 * Configures the sleep datatable.
 */
function setupDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY HH:mm');
    $('#sleep-list').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
}
class MeasurementData {
    constructor() {
        this.x = new Date();
        this.y = 0.0;
    }
}
/**
 * Configures the sleep chart: Assigns data, labels, axis types and scales, sets time/date formats.
 */
async function renderSleepChartData(progenyId) {
    let chartContainer = document.getElementById("sleep-chart-container");
    if (chartContainer === null) {
        return;
    }
    let sleepData = [];
    let sleepDataModel = new SleepDataModel;
    await fetch(`/Sleep/SleepChartData?progenyId=${progenyId}`).then(async (response) => {
        if (response.ok) {
            sleepDataModel = await response.json();
            if (sleepDataModel === null || sleepDataModel === undefined) {
                return;
            }
            sleepDataModel.chartList.forEach((item) => {
                if (item.sleepStart) {
                    let dataPoint = {
                        x: item.sleepStart,
                        y: item.sleepDurationHours
                    };
                    sleepData.push(dataPoint);
                }
            });
        }
    });
    console.log(sleepData);
    const sleepLabel = await getTranslation('Sleep list', "Sleep", getCurrentLanguageId());
    const durationInHoursString = await getTranslation('Duration in hours', "Sleep", getCurrentLanguageId());
    sleepChart = new Chart(chartContainer, {
        type: 'line',
        data: {
            datasets: [
                {
                    label: durationInHoursString,
                    data: sleepData,
                    borderColor: 'rgb(224, 168, 224)',
                    backgroundColor: 'rgb(100, 60, 100)',
                    borderWidth: 1,
                    borderDash: [5, 5],
                    fill: 'start'
                }
            ]
        },
        options: {
            scales: {
                x: {
                    type: 'time',
                    time: {
                        tooltipFormat: 'dd DD MMMM YYYY',
                        displayFormats: {
                            quarter: 'MMMM YYYY'
                        }
                    }
                },
                y: {
                    scaleLabel: {
                        display: true,
                        labelString: durationInHoursString
                    },
                    ticks: {
                        beginAtZero: true
                    }
                }
            }
        }
    });
    // sleepChart.update();
    await setupSleepSlider(sleepChart, sleepDataModel.sliderStart, sleepDataModel.sliderEnd);
}
async function setupSleepSlider(chart, sliderRangeMin, sliderRangeMax) {
    let chartContainer = document.querySelector("#sleep-chart-container");
    let sliderElement = null;
    const sleepSlider = document.querySelector('#sleep-slider');
    if (chartContainer === null || sleepSlider === null) {
        return;
    }
    var durationInHoursString = await getTranslation('Duration in hours', "Sleep", getCurrentLanguageId());
    var sliderStartString = await getTranslation('Start:', "Sleep", getCurrentLanguageId());
    var sliderEndString = await getTranslation('End:', "Sleep", getCurrentLanguageId());
    sliderElement = sleepSlider;
    if (sliderElement === null) {
        return;
    }
    noUiSlider.create(sliderElement, {
        connect: true,
        range: {
            min: timestamp(sliderRangeMin),
            max: timestamp(sliderRangeMax)
        },
        step: 1000 * 60 * 60 * 24,
        start: [
            timestamp(sliderRangeMin),
            timestamp(sliderRangeMax)
        ]
    });
    // Replace JQuery with vanilla JS
    // const pos = $(document).scrollTop();
    const pos = document.documentElement.scrollTop;
    const onSliderEnd = function () {
        const sliderValues = sliderElement.noUiSlider.get();
        let sliderStartValue = 0;
        let sliderEndValue = 1;
        if (sliderValues.length === 2) {
            sliderStartValue = parseInt(sliderValues[0]);
            sliderEndValue = parseInt(sliderValues[1]);
            const sliderStartValueDiv = document.querySelector('#slider-start-value');
            const sliderEndValueDiv = document.querySelector('#slider-end-value');
            if (sliderStartValueDiv !== null && sliderEndValueDiv !== null) {
                sliderStartValueDiv.textContent = sliderStartString + moment(sliderStartValue).format("dddd, DD-MMMM-YYYY");
                sliderEndValueDiv.textContent = sliderEndString + moment(sliderEndValue).format("dddd, DD-MMMM-YYYY");
            }
            console.log(chart);
            chart.options.scales.x = {
                type: 'time',
                time: {
                    tooltipFormat: 'dd DD MMMM YYYY',
                    displayFormats: {
                        quarter: 'MMMM YYYY'
                    }
                },
                min: new Date(sliderStartValue),
                max: new Date(sliderEndValue)
            };
            chart.update();
            //if (pos) {
            //    document.documentElement.scrollTop = pos;
            //}
        }
    };
    sliderElement.noUiSlider.on('end', onSliderEnd);
}
function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getSleepTable(changedItem.id);
        }
    });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    startTopMenuSpinner();
    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');
    await showPopupAtLoad(TimeLineType.Sleep);
    await getSleepTable(currentProgenyId);
    addProgenyChangedEventListener();
    stopTopMenuSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=sleep-index-v11.js.map