/**
 * Fetches the about text content and sets it in the edit modal.
 * @returns
 */
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
            setEditAboutModalContent(aboutTextContent);
        }
    }).catch(function (error) {
        console.log('Error loading about text content. Error: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function setEditAboutModalContent(textContent: string): void {
    $('#edit-about-text-modal-div .modal-body').html(textContent);
}

/**
 * Obtains the return URL for the about page.
 * @returns The return URL for the about page.
 */
function getAboutReturnUrl(): string {
    let aboutPageReturnUrl: string = '';
    const aboutPageReturnUrlDiv = document.querySelector<HTMLDivElement>('#about-page-return-url-div');
    if (aboutPageReturnUrlDiv !== null) {
        const aboutPageReturnUrlData = aboutPageReturnUrlDiv.dataset.aboutPageReturnUrl;
        if (aboutPageReturnUrlData) {
            aboutPageReturnUrl = aboutPageReturnUrlData;
        }
    }
    return aboutPageReturnUrl;
}

/**
 * Obtains the text id for the About page.
 * @returns The id as a string.
 */
function getAboutPageTextId(): string {
    let aboutTextId = '';
    const aboutPageTextIdDiv = document.querySelector<HTMLDivElement>('#about-page-text-id-div');
    if (aboutPageTextIdDiv !== null) {
        const aboutTextIdData = aboutPageTextIdDiv.dataset.aboutPageTextId;
        if (aboutTextIdData) {
            aboutTextId = aboutTextIdData;
        }
    }
    return aboutTextId;
}

/**
 * Adds the event listener for the edit button, which will display the edit modal.
 */
function addEditButtonEventListener(): void {
    const editAboutTextButton = document.querySelector<HTMLButtonElement>('#edit-about-text-button');
    editAboutTextButton?.addEventListener('click', loadEditAboutModal);
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', function (): void {
    addEditButtonEventListener();
});