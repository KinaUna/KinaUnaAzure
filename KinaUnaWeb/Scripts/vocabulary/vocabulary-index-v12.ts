import { setEditItemButtonEventListeners } from '../addItem/add-item-v12.js';
import { getCurrentLanguageId, getCurrentProgenyId, ProgenyChangedEvent, setMomentLocale } from '../data-tools-v12.js';
import { showPopupAtLoad } from '../item-details/items-display-v12.js';
import { getTranslation } from '../localization-v12.js';
import { startTopMenuSpinner, stopTopMenuSpinner } from '../navigation-tools-v12.js';
import { TimeLineType, VocabularyItem, WordDateCount } from '../page-models-v12.js';
import { getProgenySelector } from '../shared/progeny-selector-v12.js';
import { popupVocabularyItem } from './vocabulary-details-v12.js';

declare global {
    interface WindowEventMap {
        'progenyChanged': ProgenyChangedEvent;
    }
}
declare var Chart: any;

async function getVocabulary(progenyId: number): Promise<void> {
    const vocabularyListTable = document.querySelector<HTMLTableElement>('#vocabulary-container-div');
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

function setDetailsButtonEventListeners(): void {
    const detailsButtons = document.querySelectorAll<HTMLAnchorElement>('.vocabulary-details-button');
    detailsButtons.forEach((button) => {
        function detailsButtonClicked(): void {
            const itemId = button.getAttribute('data-vocabulary-item-id');
            if (itemId) {
                popupVocabularyItem(itemId);
            }
        }
        button.removeEventListener('click', detailsButtonClicked)
        button.addEventListener('click', detailsButtonClicked);
    });
}

class VocabularyData {
    t: Date = new Date();
    y: number = 0;
}

/**
 * Sets up the Vocabulary chart.
 */
async function setupVocabularyChart(progenyId: number): Promise<void> {
    let vocabularyChartContainer = document.querySelector<HTMLCanvasElement>("#vocabulary-chart-container");
    if (vocabularyChartContainer != null) {
        let vocabularyData: WordDateCount[] = [];
        fetch(`/Vocabulary/GetVocabularyData?progenyId=${progenyId}`)
            .then(response => response.json())
            .then(async data => {
                vocabularyData = data;
                let vocabularyDataPoints: VocabularyData[] = [];
                let count = 1;
                vocabularyData.forEach((item: WordDateCount) => {
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
    ($.fn.dataTable as any).moment('DD-MMMM-YYYY');
    $('#word-list-table').DataTable({
        'scrollX': false,
        'order': [[4, 'desc']]
    });
}

function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event: ProgenyChangedEvent) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getVocabulary(changedItem.id);
        }
    });
}

document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    startTopMenuSpinner();

    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');
    
    await showPopupAtLoad(TimeLineType.Vocabulary);
    await getVocabulary(currentProgenyId);
    addProgenyChangedEventListener();

    stopTopMenuSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});