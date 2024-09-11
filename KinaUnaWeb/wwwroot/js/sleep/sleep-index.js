import { setMomentLocale } from '../data-tools-v7.js';
let sleepChart;
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
/**
 * Configures the sleep chart: Assigns data, labels, axis types and scales, sets time/date formats.
 */
function setupSleepChart() {
    let chartContainer = document.querySelector("#chart-container");
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
function setupSleepSlider() {
    let chartContainer = document.querySelector("#chart-container");
    let sliderElement = null;
    const sleepSlider = document.querySelector('#sleep-slider');
    if (chartContainer === null || sleepSlider === null) {
        return;
    }
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
    let slpChart;
    if (chartContainer !== null) {
        slpChart = chartContainer.getContext("2d");
    }
    // Replace JQuery with vanilla JS
    // const pos = $(document).scrollTop();
    const pos = document.documentElement.scrollTop;
    sliderElement.noUiSlider.on('end', function () {
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
document.addEventListener('DOMContentLoaded', function () {
    setupDataTable();
    setupSleepChart();
    setupSleepSlider();
});
//# sourceMappingURL=sleep-index.js.map