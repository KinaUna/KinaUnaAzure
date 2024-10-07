import { setMomentLocale } from '../data-tools-v8.js';
import { showPopupAtLoad } from '../item-details/items-display-v8.js';
import { TimeLineType } from '../page-models-v8.js';

declare var vocabularyData: any;
declare var vocabularyChartLabel: string;
declare var vocabularyChartYaxisLabel: string;
declare var Chart: any;

/**
 * Sets up the Vocabulary chart.
 */
function setupVocabularyChart(): void {
    let vocabularyChartContainer = document.querySelector<HTMLCanvasElement>("#chart-container");
    if (vocabularyChartContainer != null) {
        let vocabularyChart = new Chart(vocabularyChartContainer, {
            type: 'line',
            data: {
                datasets: [
                    {
                        label: vocabularyChartLabel,
                        data: vocabularyData,
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
                                tooltipFormat: 'dd DD MMM YYYY',
                                displayFormats: {
                                    quarter: 'MMM YYYY'
                                }
                            }
                        }
                    ],
                    yAxes: [
                        {
                            scaleLabel: {
                                display: true,
                                labelString: vocabularyChartYaxisLabel
                            },
                            ticks: {
                                beginAtZero: true
                            }
                        }
                    ]
                }
            }

        });
    }
}

/**
 * Sets up the DataTable for the Vocabulary list.
 */
function setupVocabularyDataTable() {
    setMomentLocale();
    ($.fn.dataTable as any).moment('DD-MMMM-YYYY');
    $('#word-list-table').DataTable({
        'scrollX': false,
        'order': [[4, 'desc']]
    });
}

document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    setupVocabularyChart();
    setupVocabularyDataTable();    

    await showPopupAtLoad(TimeLineType.Vocabulary);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});