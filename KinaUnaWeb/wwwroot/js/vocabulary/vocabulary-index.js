import { setMomentLocale } from '../data-tools.js';
let vocabularyChartContainer = document.querySelector("#chartContainer");
if (vocabularyChartContainer != null) {
    let myChart = new Chart(vocabularyChartContainer, {
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
$(async function () {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY');
    $('#wordList').DataTable({
        'scrollX': false,
        'order': [[4, 'desc']]
    });
});
//# sourceMappingURL=vocabulary-index.js.map