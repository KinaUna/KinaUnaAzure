export async function displayFamilyMemberDetails(familyMemberId: number): Promise<void> {
    const response = await fetch('/Families/FamilyMemberDetails?familyMemberId=' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyMemberDetails = await response.text();
        const modalDiv = document.querySelector<HTMLDivElement>('#modal-div');
        if (modalDiv) {
            modalDiv.innerHTML = familyMemberDetails;
            modalDiv.classList.remove('d-none');
            // Todo: Add event listeners
            return Promise.resolve();
        } else {
            return Promise.reject('Modal div not found in the document.');
        }
    } else {
        console.error('Failed to fetch family member details:', response.statusText);
        return Promise.reject('Failed to fetch family member details: ' + response.statusText);
    }
}

