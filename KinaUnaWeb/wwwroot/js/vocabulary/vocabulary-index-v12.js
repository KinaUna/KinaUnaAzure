import { setEditItemButtonEventListeners } from '../addItem/add-item-v12.js';
import { getCurrentLanguageId, getCurrentProgenyId, setMomentLocale } from '../data-tools-v12.js';
import { showPopupAtLoad } from '../item-details/items-display-v12.js';
import { getTranslation } from '../localization-v12.js';
import { startTopMenuSpinner, stopTopMenuSpinner } from '../navigation-tools-v12.js';
import { TimeLineType } from '../page-models-v12.js';
import { getProgenySelector } from '../shared/progeny-selector-v12.js';
import { popupVocabularyItem } from './vocabulary-details-v12.js';
async function getVocabulary(progenyId) {
    const vocabularyListTable = document.querySelector('#vocabulary-container-div');
    if (vocabularyListTable) {
        await fetch('/Vocabulary/VocabularyTable?progenyId=' + progenyId).then(async function (vocabularyTableResponse) {
            if (vocabularyTableResponse != null) {
                const vocabularyTableContent = await vocabularyTableResponse.text();
                vocabularyListTable.innerHTML = vocabularyTableContent;
                setupVocabularyDataTable();
                setEditItemButtonEventListeners();
                setDetailsButtonEventListeners();
                await setupVocabularyChart(progenyId);
            }
        });
    }
}
function setDetailsButtonEventListeners() {
    const detailsButtons = document.querySelectorAll('.vocabulary-details-button');
    detailsButtons.forEach((button) => {
        function detailsButtonClicked() {
            const itemId = button.getAttribute('data-vocabulary-item-id');
            if (itemId) {
                popupVocabularyItem(itemId);
            }
        }
        button.removeEventListener('click', detailsButtonClicked);
        button.addEventListener('click', detailsButtonClicked);
    });
}
class VocabularyData {
    constructor() {
        this.t = new Date();
        this.y = 0;
    }
}
/**
 * Sets up the Vocabulary chart.
 */
async function setupVocabularyChart(progenyId) {
    let vocabularyChartContainer = document.querySelector("#vocabulary-chart-container");
    if (vocabularyChartContainer != null) {
        let vocabularyData = [];
        fetch(`/Vocabulary/GetVocabularyData?progenyId=${progenyId}`)
            .then(response => response.json())
            .then(async (data) => {
            vocabularyData = data;
            let vocabularyDataPoints = [];
            let count = 1;
            vocabularyData.forEach((item) => {
                if (item !== null && item.wordDate !== null) {
                    vocabularyDataPoints.push({ t: new Date(item.wordDate), y: item.wordCount });
                }
            });
            let vocabularyChart = new Chart(vocabularyChartContainer, {
                type: 'line',
                data: {
                    datasets: [
                        {
                            label: await getTranslation('Words', "Vocabulary", getCurrentLanguageId()),
                            data: vocabularyDataPoints,
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
                                    labelString: await getTranslation('Number of words', "Vocabulary", getCurrentLanguageId())
                                },
                                ticks: {
                                    beginAtZero: true
                                }
                            }
                        ]
                    }
                }
            });
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
function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getVocabulary(changedItem.id);
        }
    });
}
document.addEventListener('DOMContentLoaded', async function () {
    startTopMenuSpinner();
    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');
    await showPopupAtLoad(TimeLineType.Vocabulary);
    await getVocabulary(currentProgenyId);
    addProgenyChangedEventListener();
    stopTopMenuSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=vocabulary-index-v12.js.map