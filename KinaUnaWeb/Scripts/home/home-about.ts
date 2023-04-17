async function loadEditAboutModal(): Promise<void> {
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

function showEditAboutModal(textContent: string) {
    return new Promise<void>(function (resolve, reject) {
        $('#editAboutTextModalDiv .modal-body').html(textContent);
        resolve();
    });
}

function getAboutReturnUrl(): string {
    let aboutPageReturnUrl: string = '';
    const aboutPageReturnUrlDiv = document.querySelector<HTMLDivElement>('#aboutPageReturnUrlDiv');
    if (aboutPageReturnUrlDiv !== null) {
        const aboutPageReturnUrlData = aboutPageReturnUrlDiv.dataset.aboutPageReturnUrl;
        if (aboutPageReturnUrlData) {
            aboutPageReturnUrl = aboutPageReturnUrlData;
        }
    }
    return aboutPageReturnUrl;
}

function getAboutPageTextId(): string {
    let aboutTextId = '';
    const aboutPageTextIdDiv = document.querySelector<HTMLDivElement>('#aboutPageTextIdDiv');
    if (aboutPageTextIdDiv !== null) {
        const aboutTextIdData = aboutPageTextIdDiv.dataset.aboutPageTextId;
        if (aboutTextIdData) {
            aboutTextId = aboutTextIdData;
        }
    }
    return aboutTextId;
}


$(function (): void {
    const editAboutTextButton = document.querySelector<HTMLButtonElement>('#editAboutTextButton');
    editAboutTextButton?.addEventListener('click', loadEditAboutModal);
});