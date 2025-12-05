import { setEditItemButtonEventListeners } from '../addItem/add-item-v11.js';
import { getCurrentLanguageId, getCurrentProgenyId, ProgenyChangedEvent, setMomentLocale } from '../data-tools-v11.js';
import { showPopupAtLoad } from '../item-details/items-display-v11.js';
import { getTranslation } from '../localization-v11.js';
import { startTopMenuSpinner, stopTopMenuSpinner } from '../navigation-tools-v11.js';
import { TimeLineType, VocabularyItem, WordDateCount } from '../page-models-v11.js';
import { getProgenySelector } from '../shared/progeny-selector-v11.js';

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
                await setupVocabularyChart(progenyId);
            }
        });
    }
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