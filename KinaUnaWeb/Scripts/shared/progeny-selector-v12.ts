import { ProgenyChangedEvent } from "../data-tools-v12.js";
import { Progeny, TimelineParameters } from "../page-models-v12.js";
import { getSelectedFamilies, getSelectedProgenies } from "../settings-tools-v12.js";

export async function getProgenySelector(progenyId: number, familyId: number, parentDivId: string) {
    const selectorContainerDiv = document.querySelector<HTMLDivElement>('#' + parentDivId);
    if (selectorContainerDiv) {
        
        const progenySelectorRequest: TimelineParameters = {
            progenyId: progenyId,
            progenies: await getSelectedProgenies(),
            familyId: familyId,
            families: await getSelectedFamilies(),
            skip: 0,
            count: 1,
            sortBy: 0,
            year: 0,
            month: 0,
            day: 0,
            firstItemYear: 0,
            tagFilter: ''
        }
        await fetch('/Progeny/ProgenySelectorElement', {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(progenySelectorRequest)
        }).then(async function (getProgenySelectorContent) {
            if (getProgenySelectorContent != null) {
                const progenySelectorHtml = await getProgenySelectorContent.text();

                if (selectorContainerDiv) {
                    selectorContainerDiv.innerHTML = progenySelectorHtml;
                }
                addSelectProgenySelectorEventListeners();
                setActiveProgenySelectorButton(progenyId);
            }
        });
    }
}

function setActiveProgenySelectorButton(progenyId: number) {
    if (progenyId === 0) {
        return;
    }

    const progenyElements = document.querySelectorAll<HTMLAnchorElement>('.progeny-selector-link');
    progenyElements.forEach(function (progenyElement) {
        const elementProgenyId = progenyElement.getAttribute('data-progeny-id');
        if (elementProgenyId) {
            progenyElement.classList.remove('active');
            if (parseInt(elementProgenyId) === progenyId) {
                progenyElement.classList.add('active');
            }
        }
    });
}

function addSelectProgenySelectorEventListeners() {
    const progenyElements = document.querySelectorAll<HTMLAnchorElement>('.progeny-selector-link');
    progenyElements.forEach(function (progenyElement) {
        const progenySelectorProgenyClicked = function (event: MouseEvent) {
            event.preventDefault();
            const progenyId = progenyElement.getAttribute('data-progeny-id');
            if (progenyId) {
                const progeny = new Progeny();
                progeny.id = parseInt(progenyId);
                const progenyChangedEvent = new ProgenyChangedEvent(progeny);
                window.dispatchEvent(progenyChangedEvent);
                setActiveProgenySelectorButton(progeny.id);
            }
        };
        progenyElement.removeEventListener('click', progenySelectorProgenyClicked);
        progenyElement.addEventListener('click', progenySelectorProgenyClicked);
    });
}