import { setMomentLocale } from '../data-tools-v6.js';

declare var Chart: any;
declare var moment: any;
declare var noUiSlider: any;
declare var sleepData: any;
declare var sleepLabel: string;
declare var durationInHoursString: string;
declare var sliderStartString: string;
declare var sliderEndString: string;
declare var sliderRangeMin: string;
declare var sliderRangeMax: string;

let sleepChart: any;

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

/**
 * Configures the sleep chart: Assigns data, labels, axis types and scales, sets time/date formats.
 */
function setupSleepChart(): void {
    let chartContainer = document.querySelector<HTMLCanvasElement>("#chart-container");
    
    if (chartContainer === null) {
        return;
    }

    sleepChart = new Chart(chartContainer, {
        type: 'bar',
        data: {
            datasets: [
                {
                    label: sleepLabel,
                    data: sleepData,
                    borderColor: 'rgb(75, 192, 192)',
                    borderWidth: 1
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

    sleepChart.update();
}

function setupSleepSlider(): void {
    let chartContainer = document.querySelector<HTMLCanvasElement>("#chart-container");
    let sliderElement: any = null;
    const sleepSlider = document.querySelector<HTMLDivElement>('#sleep-slider');

    if (chartContainer === null || sleepSlider === null) {
        return;
    }

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

    let slpChart: CanvasRenderingContext2D | null;
    if (chartContainer !== null) {
        slpChart = chartContainer.getContext("2d");

    }
    // Replace JQuery with vanilla JS
    // const pos = $(document).scrollTop();
    const pos = document.documentElement.scrollTop;

    sliderElement.noUiSlider.on('end',
        function () {
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

                sleepChart.options.scales.x = {
                    type: 'time',
                    time: {
                        tooltipFormat: 'dd DD MMMM YYYY',
                        displayFormats: {
                            quarter: 'MMMM YYYY'
                        }
                    },
                    min: sliderStartValue,
                    max: sliderEndValue
                };
                sleepChart.update();
                if (pos) {
                    // Replace Jquery with vanilla JS
                    // $(document).scrollTop(pos);
                    document.documentElement.scrollTop = pos;
                }
            }
        });
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', function (): void {
    setupDataTable();
    setupSleepChart();
    setupSleepSlider();
});