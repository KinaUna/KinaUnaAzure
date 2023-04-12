import * as Chart from 'chart.js';
import { setMomentLocale } from '../data-tools.js';
import * as moment from '../../node_modules/moment/ts3.1-typings/moment';
import * as noUiSlider from '../../node_modules/nouislider/dist/nouislider.js';
declare var sleepData: any;
declare var sleepLabel: string;
declare var durationInHoursString: string;
declare var sliderStartString: string;
declare var sliderEndString: string;
declare var sliderRangeMin: string;
declare var sliderRangeMax: string;

function timestamp(str: string) {
    return new Date(str).getTime();
}

$(async function (): Promise<void> {
    setMomentLocale();
        
    (<any>$.fn.dataTable).moment('DD-MMMM-YYYY HH:mm');
    $('#sleepList').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
        
    let chartContainer = document.querySelector<HTMLCanvasElement>("#chartContainer");
    let sliderElement: noUiSlider.target | null = null;

    if (chartContainer != null) {
        let myChart = new Chart(chartContainer, {
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
                    xAxes: [
                        {
                            type: 'time',
                            time: {
                                tooltipFormat: 'dd DD MMMM YYYY',
                                displayFormats: {
                                    quarter: 'MMMM YYYY'
                                }
                            }
                        }
                    ],
                    yAxes: [
                        {
                            scaleLabel: {
                                display: true,
                                labelString: durationInHoursString
                            },
                            ticks: {
                                beginAtZero: true
                            }
                        }
                    ]
                }
            }
        });

        myChart.update();
        
        const sleepSlider = document.querySelector<HTMLDivElement>('#sliderSleep');
        
        if (sleepSlider !== null) {
            let slpChart: CanvasRenderingContext2D | null;
            if (chartContainer !== null) {
                slpChart = chartContainer.getContext("2d");
                sliderElement = sleepSlider as noUiSlider.target
            }
            const pos = $(document).scrollTop();

            if (sliderElement !== null && sliderElement.noUiSlider) {
                sliderElement.noUiSlider.on('update',
                    function () {
                        if (sliderElement?.noUiSlider) {
                            const sliderValues: string[] = sliderElement.noUiSlider.get() as string[];
                            let sliderStartValue: string = '';
                            let sliderEndValue: string = '';
                            if (sliderValues.length === 2) {
                                sliderStartValue = sliderValues[0];
                                sliderEndValue = sliderValues[1];

                                $('#sliderStartVal').text(sliderStartString + moment(sliderStartValue, "x").format("dddd, DD-MMMM-YYYY"));
                                $('#sliderEndVal').text(sliderEndString + moment(sliderEndValue, "x").format("dddd, DD-MMMM-YYYY"));
                                const chartDiv = document.querySelector<HTMLDivElement>('#chartDiv');
                                if (chartDiv !== null) {
                                    chartDiv.innerHTML = '&nbsp;';
                                    chartDiv.innerHTML = '<canvas id="chartContainer"></canvas>';
                                }

                                const cfg = {
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
                                            xAxes: [
                                                {
                                                    type: 'time',
                                                    time: {
                                                        min: moment(sliderStartValue, "x").toString(),
                                                        max: moment(sliderEndValue, "x").toString(),
                                                        tooltipFormat: 'dd DD MMMM YYYY',
                                                        displayFormats: {
                                                            quarter: 'MMMM YYYY'
                                                        }
                                                    }
                                                }
                                            ],
                                            yAxes: [
                                                {
                                                    scaleLabel: {
                                                        display: true,
                                                        labelString: durationInHoursString
                                                    },
                                                    ticks: {
                                                        beginAtZero: true
                                                    }
                                                }
                                            ]
                                        }
                                    }
                                };

                                if (slpChart !== null) {
                                    const newChart = new Chart(slpChart, cfg);
                                    newChart.update();
                                }
                                if (pos) {
                                    $(document).scrollTop(pos);
                                }
                            }
                        }
                    });
            }
        }        
    }    

    if (sliderElement !== null) {
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
    }
    
});