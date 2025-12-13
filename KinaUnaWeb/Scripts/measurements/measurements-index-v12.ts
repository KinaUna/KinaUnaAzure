import { setEditItemButtonEventListeners } from "../addItem/add-item-v12.js";
import { getCurrentLanguageId, getCurrentProgenyId, ProgenyChangedEvent, setMomentLocale } from "../data-tools-v12.js";
import { showPopupAtLoad } from "../item-details/items-display-v12.js";
import { startTopMenuSpinner, stopTopMenuSpinner } from "../navigation-tools-v12.js";
import { Measurement, TimeLineType } from "../page-models-v12.js";
import { getProgenySelector } from "../shared/progeny-selector-v12.js";
import { getTranslation } from "../localization-v12.js";
import { popupMeasurementItem } from "./measurement-details-v12.js";

declare global {
    interface WindowEventMap {
        'progenyChanged': ProgenyChangedEvent;
    }
}
declare var Chart: any;

async function getMeasurements(progenyId: number): Promise<void> {
    const measurementsListTable = document.querySelector<HTMLTableElement>('#measurements-list-container-div');
    if (measurementsListTable) {
        await fetch('/Measurements/MeasurementsTable?progenyId=' + progenyId, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async function (measurementsListResponse) {
            if (measurementsListResponse != null) {
                const measurementsListHtml = await measurementsListResponse.text();
                measurementsListTable.innerHTML = measurementsListHtml;
                setupDataTable();
                setEditItemButtonEventListeners();
                setDetailsButtonEventListeners();
                await renderMeasurementsChartData(progenyId);
            }
        });
    }   
}

function setDetailsButtonEventListeners(): void {
    const detailsButtons = document.querySelectorAll<HTMLAnchorElement>('.measurement-details-button');
    detailsButtons.forEach((button) => {
        function detailsButtonClicked(): void {
            const itemId = button.getAttribute('data-measurement-item-id');
            if (itemId) {
                popupMeasurementItem(itemId);
            }
        }
        button.removeEventListener('click', detailsButtonClicked)
        button.addEventListener('click', detailsButtonClicked);
    });
}

function setupDataTable(): void {
    setMomentLocale();
    (<any>$.fn.dataTable).moment('DD-MMM-YYYY');
    $('#measurements-list').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
}

function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event: ProgenyChangedEvent) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getMeasurements(changedItem.id);
        }
    });
}

class MeasurementData {
    t: Date = new Date();
    y: number = 0;
}
async function renderMeasurementsChartData(progenyId: number): Promise<void> {
    const heightChartContainer = document.getElementById('height-chart-container');
    const weightChartContainer = document.getElementById('weight-chart-container');
    let measurementsData: Measurement[] = [];
    await fetch(`/Measurements/GetMeasurementsData?progenyId=${progenyId}`)
        .then(response => response.json())
        .then(async data => {
            measurementsData = data;
            let heightData: MeasurementData[] = [];
            measurementsData.forEach((item: Measurement) => {
                if (item.height !== null && item.height !== 0) {
                    heightData.push({ t: new Date(item.date), y: item.height });
                }
            });
            // Create the height and weight charts using the data
            if (heightChartContainer) {
                const heightString = await getTranslation('Height', "Measurements", getCurrentLanguageId());
                const heightChart = new Chart(heightChartContainer, {
                    type: 'line',
                    data: {
                        datasets: [{
                            label: heightString,
                            data: heightData,
                            borderColor: [
                                'rgba(255, 99, 132, 1)',
                                'rgba(54, 162, 235, 1)',
                                'rgba(255, 206, 86, 1)',
                                'rgba(75, 192, 192, 1)',
                                'rgba(153, 102, 255, 1)',
                                'rgba(255, 159, 64, 1)'
                            ],
                            borderWidth: 1
                        }]
                    },
                    options: {
                        scales: {
                            xAxes: [{
                                type: 'time',
                                time: {
                                    displayFormats: {
                                        quarter: 'MMM YYYY'
                                    }
                                }
                            }],
                            yAxes: [{
                                ticks: {
                                    beginAtZero: false
                                }
                            }]
                        }
                    }
                });
            }

            let weightData: MeasurementData[] = [];
            measurementsData.forEach((item: Measurement) => {
                if (item.weight !== null && item.weight !== 0) {
                    weightData.push({ t: new Date(item.date), y: item.weight });
                }
            });
            if (weightChartContainer) {
                const weightString = await getTranslation('Weight', "Measurements", getCurrentLanguageId());
                const weightChart = new Chart(weightChartContainer, {
                    type: 'line',
                    data: {

                        datasets: [{
                            label: weightString,
                            data: weightData,
                            borderColor: 'rgb(95, 192, 192)',
                            borderWidth: 1
                        }]
                    },
                    options: {
                        scales: {
                            xAxes: [{
                                type: 'time',
                                time: {
                                    displayFormats: {
                                        quarter: 'MMM YYYY'
                                    }
                                }
                            }],
                            yAxes: [{
                                ticks: {
                                    beginAtZero: false
                                }
                            }]
                        }
                    }
                });
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
    await showPopupAtLoad(TimeLineType.Measurement);

    await getMeasurements(currentProgenyId);
    addProgenyChangedEventListener();
    
    stopTopMenuSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});
