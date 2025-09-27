import { getCurrentLanguageId } from '../data-tools-v9.js';
import { getTranslation } from '../localization-v9.js';
import { displayFamilyMemberDetails } from './family-member-details.js';
let familiesList = new Array();
let languageId = 1; // Default to English
export async function GetFamiliesList() {
    const response = await fetch('/Families/FamiliesList', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const data = await response.json();
        for (const family of data) {
            familiesList.push(family);
        }
        return Promise.resolve();
    }
    else {
        console.error('Failed to fetch families list:', response.statusText);
        return Promise.reject('Failed to fetch families list: ' + response.statusText);
    }
}
async function RenderFamilyMembers(family) {
    const familyMembersDiv = document.querySelector('#family-members-div');
    if (familyMembersDiv) {
        // Create div element with id 'family-members-{familyId}' to contain family name and its members.
        const familyMembersElement = document.createElement('div');
        familyMembersElement.id = 'family-members-' + family.familyId;
        familyMembersElement.className = 'family-members-element mb-4 p-3 border rounded';
        familyMembersDiv.appendChild(familyMembersElement);
        familyMembersElement.innerHTML = `<h5 class="mb-3">${family.name}</h5>`;
        if (family.familyMembers.length > 0) {
            family.familyMembers.forEach(async (member) => {
                let familyMemberHTML = await getFamilyMemberElement(member);
                familyMembersElement.appendChild(familyMemberHTML);
                addFamilyMemberElementEventListeners(member.familyMemberId);
            });
        }
        else {
            let noMembersMessage = getTranslation('No members found for this family.', 'Family', languageId);
            familyMembersElement.innerHTML += `<p>${noMembersMessage}</p>`;
        }
    }
    return Promise.reject('Family members div not found in the document.');
}
async function getFamilyMemberElement(familyMember) {
    let memberDivResponse = await fetch('/Families/FamilyMemberElement?familyMemberId=' + familyMember.familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (memberDivResponse.ok) {
        const memberElement = await memberDivResponse.text();
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = memberElement;
        return tempDiv.firstElementChild;
    }
    else {
        console.error('Failed to fetch family member element:', memberDivResponse.statusText);
        return Promise.reject('Failed to fetch family member element: ' + memberDivResponse.statusText);
    }
}
function addFamilyMemberElementEventListeners(familyMemberId) {
    const familyMemberElementDiv = document.querySelector('#family-member-element-' + familyMemberId);
    if (familyMemberElementDiv) {
        const familyMemberElementClickedAction = async function (event) {
            event.preventDefault();
            // Show family member details modal
            await displayFamilyMemberDetails(familyMemberId);
            return Promise.resolve();
        };
        familyMemberElementDiv.removeEventListener('click', familyMemberElementClickedAction);
        familyMemberElementDiv.addEventListener('click', familyMemberElementClickedAction);
    }
}
async function RenderAllFamilies() {
    for (const family of familiesList) {
        await RenderFamilyMembers(family);
    }
    return Promise.resolve();
}
document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();
    await GetFamiliesList();
    await RenderAllFamilies();
    return Promise.resolve();
});
//# sourceMappingURL=family-members.js.map