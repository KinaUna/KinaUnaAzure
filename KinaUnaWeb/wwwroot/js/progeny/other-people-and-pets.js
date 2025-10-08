import { getCurrentLanguageId } from "../data-tools-v9.js";
let progeniesList = new Array();
let languageId = 1; // Default to English
export async function getOtherPeopleList() {
    const otherPeopleListDiv = document.querySelector('#people-and-pets-list-div');
    if (otherPeopleListDiv) {
        otherPeopleListDiv.innerHTML = '';
        const response = await fetch('/Progeny/OtherPeopleAndPetsList', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (response.ok) {
            const data = await response.json();
            for (const progeny of data) {
                await renderOtherPeopleElement(progeny.id);
            }
            return Promise.resolve();
        }
        else {
            console.error('Failed to fetch other people and pets list:', response.statusText);
            return Promise.reject('Failed to fetch other people and pets list: ' + response.statusText);
        }
    }
    return Promise.reject('Families list div not found in the document.');
}
async function renderOtherPeopleElement(progenyId) {
    const response = await fetch('/Progeny/OtherPeopleElement?progenyId=' + progenyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const otherPeopleElement = await response.text();
        const otherPeopleListDiv = document.querySelector('#people-and-pets-list-div');
        if (otherPeopleListDiv) {
            otherPeopleListDiv.insertAdjacentHTML('beforeend', otherPeopleElement);
            addOtherPeopleElementEventListeners(progenyId);
            return Promise.resolve();
        }
        return Promise.reject('Other people and pets list div not found in the document.');
    }
    else {
        console.error('Failed to fetch other people or pets element:', response.statusText);
        return Promise.reject('Failed to fetch family element: ' + response.statusText);
    }
}
function addOtherPeopleElementEventListeners(progenyId) {
}
document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();
    await getOtherPeopleList();
    return Promise.resolve();
});
//# sourceMappingURL=other-people-and-pets.js.map