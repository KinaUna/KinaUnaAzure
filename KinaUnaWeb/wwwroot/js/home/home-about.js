"use strict";
async function loadEditAboutModal() {
    await fetch('/Admin/EditText?id=' + getAboutPageTextId() + "&returnUrl=" + getAboutReturnUrl(), {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (aboutTextResponse) {
        if (aboutTextResponse.text != null) {
            const aboutTextContent = await aboutTextResponse.text();
            await showEditAboutModal(aboutTextContent);
        }
    }).catch(function (error) {
        console.log('Error loading about text content. Error: ' + error);
    });
}
function showEditAboutModal(textContent) {
    return new Promise(function (resolve, reject) {
        $('#editAboutTextModalDiv .modal-body').html(textContent);
        resolve();
    });
}
function getAboutReturnUrl() {
    let aboutPageReturnUrl = '';
    const aboutPageReturnUrlDiv = document.querySelector('#aboutPageReturnUrlDiv');
    if (aboutPageReturnUrlDiv !== null) {
        const aboutPageReturnUrlData = aboutPageReturnUrlDiv.dataset.aboutPageReturnUrl;
        if (aboutPageReturnUrlData) {
            aboutPageReturnUrl = aboutPageReturnUrlData;
        }
    }
    return aboutPageReturnUrl;
}
function getAboutPageTextId() {
    let aboutTextId = '';
    const aboutPageTextIdDiv = document.querySelector('#aboutPageTextIdDiv');
    if (aboutPageTextIdDiv !== null) {
        const aboutTextIdData = aboutPageTextIdDiv.dataset.aboutPageTextId;
        if (aboutTextIdData) {
            aboutTextId = aboutTextIdData;
        }
    }
    return aboutTextId;
}
$(function () {
    const editAboutTextButton = document.querySelector('#editAboutTextButton');
    editAboutTextButton?.addEventListener('click', loadEditAboutModal);
});
//# sourceMappingURL=home-about.js.map