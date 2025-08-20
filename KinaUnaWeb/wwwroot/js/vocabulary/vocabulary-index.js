import { setMomentLocale } from '../data-tools-v9.js';
import { showPopupAtLoad } from '../item-details/items-display-v9.js';
import { TimeLineType } from '../page-models-v9.js';
/**
 * Sets up the Vocabulary chart.
 */
function setupVocabularyChart() {
    let vocabularyChartContainer = document.querySelector("#chart-container");
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
    $.fn.dataTable.moment('DD-MMMM-YYYY');
    $('#word-list-table').DataTable({
        'scrollX': false,
        'order': [[4, 'desc']]
    });
}
document.addEventListener('DOMContentLoaded', async function () {
    setupVocabularyChart();
    setupVocabularyDataTable();
    await showPopupAtLoad(TimeLineType.Vocabulary);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=vocabulary-index.js.map