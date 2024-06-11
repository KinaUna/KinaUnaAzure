import { setMomentLocale } from '../data-tools-v2.js';
function timestamp(str) {
    return new Date(str).getTime();
}
let sleepChart;
$(async function () {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY HH:mm');
    $('#sleepList').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
    let chartContainer = document.querySelector("#chartContainer");
    let sliderElement = null;
    if (chartContainer != null) {
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
        const sleepSlider = document.querySelector('#sliderSleep');
        if (chartContainer !== null && sleepSlider !== null) {
            sliderElement = sleepSlider;
        }
        if (sliderElement !== null) {
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
        }
        if (sleepSlider !== null) {
            let slpChart;
            if (chartContainer !== null) {
                slpChart = chartContainer.getContext("2d");
            }
            const pos = $(document).scrollTop();
            if (sliderElement !== null) {
                sliderElement.noUiSlider.on('end', function () {
                    if (sliderElement?.noUiSlider) {
                        const sliderValues = sliderElement.noUiSlider.get();
                        let sliderStartValue = 0;
                        let sliderEndValue = 1;
                        if (sliderValues.length === 2) {
                            sliderStartValue = parseInt(sliderValues[0]);
                            sliderEndValue = parseInt(sliderValues[1]);
                            $('#sliderStartVal').text(sliderStartString + moment(sliderStartValue).format("dddd, DD-MMMM-YYYY"));
                            $('#sliderEndVal').text(sliderEndString + moment(sliderEndValue).format("dddd, DD-MMMM-YYYY"));
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
                                $(document).scrollTop(pos);
                            }
                        }
                    }
                });
            }
        }
    }
});
//# sourceMappingURL=sleep-index.js.map