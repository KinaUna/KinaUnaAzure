import { setEditItemButtonEventListeners } from '../addItem/add-item-v12.js';
import { getCurrentLanguageId, getCurrentProgenyId, ProgenyChangedEvent, setMomentLocale } from '../data-tools-v12.js';
import { showPopupAtLoad } from '../item-details/items-display-v12.js';
import { getTranslation } from '../localization-v12.js';
import { startTopMenuSpinner, stopTopMenuSpinner } from '../navigation-tools-v12.js';
import { Sleep, SleepDataModel, TimeLineType } from '../page-models-v12.js';
import { getProgenySelector } from '../shared/progeny-selector-v12.js';
import { popupSleepItem } from './sleep-details-v12.js';

declare global {
    interface WindowEventMap {
        'progenyChanged': ProgenyChangedEvent;
    }
}

declare var Chart: any;
declare var moment: any;
let sleepChart: any;
declare var noUiSlider: any;

async function getSleepTable(progenyId: number): Promise<void> {
    const sleepListTable = document.querySelector<HTMLTableElement>('#sleep-list-container-div');
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
                setDetailsButtonEventListeners();
                await renderSleepChartData(progenyId);
            }
        });
    }

}

function setDetailsButtonEventListeners(): void {
    const detailsButtons = document.querySelectorAll<HTMLAnchorElement>('.sleep-details-button');
    detailsButtons.forEach((button) => {
        function detailsButtonClicked(): void {
            const itemId = button.getAttribute('data-sleep-item-id');
            if (itemId) {
                popupSleepItem(itemId);
            }
        }
        button.removeEventListener('click', detailsButtonClicked)
        button.addEventListener('click', detailsButtonClicked);
    });
}

/**
 * Formats a date string to a timestamp.
 * @param dateString The date as a string.
 * @returns The date as a timestamp.
 */
function timestamp(dateString: string): number {
    return new Date(dateString).getTime();
}

/**
 * Configures the sleep datatable.
 */
function setupDataTable(): void {
    setMomentLocale();
    (<any>$.fn.dataTable).moment('DD-MMMM-YYYY HH:mm');
    $('#sleep-list').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
}

class MeasurementData {
    x: Date = new Date();
    y: number = 0.0;
}

/**
 * Configures the sleep chart: Assigns data, labels, axis types and scales, sets time/date formats.
 */
async function renderSleepChartData(progenyId: number): Promise<void> {
    let chartContainer = document.getElementById("sleep-chart-container");
    
    if (chartContainer === null) {
        return;
    }

    let sleepData: MeasurementData[] = [];
    let sleepDataModel: SleepDataModel = new SleepDataModel;
    await fetch(`/Sleep/SleepChartData?progenyId=${progenyId}`).then(async (response) => {
        if (response.ok) {
            sleepDataModel = await response.json();
            if (sleepDataModel === null || sleepDataModel === undefined) {                
                return;
            }

            sleepDataModel.chartList.forEach((item: Sleep) => {
                if (item.sleepStart) {
                    let dataPoint: MeasurementData = {
                        x: item.sleepStart,
                            y: item.sleepDurationHours
                    };
                    sleepData.push(dataPoint);
                }
               
            });            
        }
    });
    console.log(sleepData);
    const sleepLabel: string = await getTranslation('Sleep list', "Sleep", getCurrentLanguageId());
    const durationInHoursString: string = await getTranslation('Duration in hours', "Sleep", getCurrentLanguageId());
    
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

async function setupSleepSlider(chart: any, sliderRangeMin: string, sliderRangeMax: string): Promise<void> {
    let chartContainer = document.querySelector<HTMLCanvasElement>("#sleep-chart-container");
    let sliderElement: any = null;
    const sleepSlider = document.querySelector<HTMLDivElement>('#sleep-slider');

    if (chartContainer === null || sleepSlider === null) {
        return;
    }

    var durationInHoursString = await getTranslation('Duration in hours', "Sleep", getCurrentLanguageId());
    var sliderStartString = await getTranslation('Start:', "Sleep", getCurrentLanguageId());
    var sliderEndString = await getTranslation('End:', "Sleep", getCurrentLanguageId());
    
    sliderElement = sleepSlider as any;
    if (sliderElement === null) {
        return;
    }

    noUiSlider.create(sliderElement,
        {
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
        const sliderValues: string[] = sliderElement.noUiSlider.get() as string[];
        let sliderStartValue: number = 0;
        let sliderEndValue: number = 1;
        if (sliderValues.length === 2) {
            sliderStartValue = parseInt(sliderValues[0]);
            sliderEndValue = parseInt(sliderValues[1]);
            const sliderStartValueDiv = document.querySelector<HTMLDivElement>('#slider-start-value');
            const sliderEndValueDiv = document.querySelector<HTMLDivElement>('#slider-end-value');
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
    window.addEventListener('progenyChanged', async (event: ProgenyChangedEvent) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getSleepTable(changedItem.id);
        }
    });
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    startTopMenuSpinner();
    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');

    await showPopupAtLoad(TimeLineType.Sleep);

    await getSleepTable(currentProgenyId);

    addProgenyChangedEventListener();

    stopTopMenuSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});