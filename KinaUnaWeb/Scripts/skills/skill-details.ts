import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v9.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v9.js';

/**
 * Adds event listeners to all elements with the data-skill-id attribute.
 * When clicked, the DisplaySkillItem function is called.
 * @param {string} itemId The id of the Skill to add event listeners for.
 */
export function addSkillItemListeners(itemId: string): void {
    const elementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-skill-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', onSkillItemDivClicked);
        });
    }
}

async function onSkillItemDivClicked(event: MouseEvent): Promise<void> {
    const skillElement: HTMLDivElement = event.currentTarget as HTMLDivElement;
    if (skillElement !== null) {
        const skillId = skillElement.dataset.skillId;
        if (skillId) {
            await displaySkillItem(skillId);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Enable other scripts to call the DisplaySkillItem function.
 * @param {string} skillId The id of the skill item to display.
 */
export async function popupSkillItem(skillId: string): Promise<void> {
    await displaySkillItem(skillId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Displays a skill item in a popup.
 * @param {string} skillId The id of the skill item to display.
 */
async function displaySkillItem(skillId: string): Promise<void> {
    startFullPageSpinner();
    let url = '/Skills/ViewSkill?skillId=' + skillId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const skillElementHtml = await response.text();
            const skillDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (skillDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = skillElementHtml;
                skillDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                skillDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            skillDetailsPopupDiv.innerHTML = '';
                            skillDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }

                setEditItemButtonEventListeners();
            }
        } else {
            console.error('Error getting skill item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting skill item. Error: ' + error);
    });

    stopFullPageSpinner();
}