import { getCurrentLanguageId } from "../data-tools-v12.js";

async function displayHelpDetails(page: string, element: string, targetDiv: string) {
    let targetElement = document.getElementById(targetDiv) as HTMLDivElement;
    if (!targetElement) {
        targetElement = document.getElementById('help-modal-div') as HTMLDivElement;
    }       
    
    if (targetElement === null) { return };
    const laguageId = await getCurrentLanguageId();
    let url = '/Help/HelpDetails?page=' + page + '&element=' + element + '&languageId=' + laguageId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const helpContentHtml = await response.text();
            targetElement.innerHTML = helpContentHtml;
            targetElement.classList.remove('d-none');
        }
    }).catch(function (error) {
        console.error('Error fetching help details:', error);
    });        
}

export function addHelpEventListeners() {
    const helpButtonsList = document.querySelectorAll<HTMLButtonElement>('.help-button');
    helpButtonsList.forEach((button) => {
        async function openHelpModal(event: MouseEvent) {
            const page = button.dataset.page;
            const element = button.dataset.element;
            const targetDiv = button.dataset.targetDiv;
            if (page && element && targetDiv) {
                await displayHelpDetails(page, element, targetDiv);
            }
        }
        button.removeEventListener('click', openHelpModal);
        button.addEventListener('click', openHelpModal);
    });
}
