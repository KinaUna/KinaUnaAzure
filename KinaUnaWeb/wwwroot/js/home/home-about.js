"use strict";
const aboutPageTextIdDiv = document.querySelector('#aboutPageTextIdDiv');
const editAboutTextButton = document.querySelector('#editAboutTextButton');
const editAboutTextModalDiv = document.querySelector('#editAboutTextModalDiv');
const aboutPageReturnUrlDiv = document.querySelector('#aboutPageReturnUrlDiv');
let aboutTextId = '';
let aboutPageReturnUrl = '';
async function showEditAboutModal() {
    await fetch('/Admin/EditText?id=' + aboutTextId + "&returnUrl=" + aboutPageReturnUrl, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (aboutTextResponse) {
        if (aboutTextResponse.text != null) {
            const aboutTextContent = await aboutTextResponse.text();
            $('#editAboutTextModalDiv .modal-body').html(aboutTextContent);
        }
    }).catch(function (error) {
        console.log('Error loading about text content. Error: ' + error);
    });
}
$(function () {
    if (aboutPageTextIdDiv !== null) {
        const aboutTextIdData = aboutPageTextIdDiv.dataset.aboutPageTextId;
        if (aboutTextIdData) {
            aboutTextId = aboutTextIdData;
        }
    }
    if (aboutPageReturnUrlDiv !== null) {
        const aboutPageReturnUrlData = aboutPageReturnUrlDiv.dataset.aboutPageReturnUrl;
        if (aboutPageReturnUrlData) {
            aboutPageReturnUrl = aboutPageReturnUrlData;
        }
    }
    editAboutTextButton?.addEventListener('click', showEditAboutModal);
});
//# sourceMappingURL=home-about.js.map