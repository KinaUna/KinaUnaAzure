
async function getPageHelpContentList(page: string, languageId: number): Promise<string> {
    let url = '/Help/HelpContentList?page=' + page + '&languageId=' + languageId;
    return fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const helpContentListHtml = await response.text();
            return helpContentListHtml;
        } else {
            return '';
        }
    }).catch(function (error) {
        console.error('Error fetching help content list:', error);
        return '';
    });
}

async function getEditHelpContent(helpContentId: string): Promise<string> {
    let url = '/Help/EditHelpContent/' + helpContentId;
    return fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const helpContentHtml = await response.text();
            return helpContentHtml;
        } else {
            return '';
        }
    }).catch(function (error) {
        console.error('Error fetching help content:', error);
        return '';
    });
}

function addEditButtonListeners(): void {
    const editButtons = document.querySelectorAll('[data-page-content-text-id]');
    editButtons.forEach((button) => {
        const editButton = button as HTMLButtonElement;
        const editButtonClickHandler = async function (event: MouseEvent) {
            const helpContentId = editButton.getAttribute('data-page-content-text-id');
            if (helpContentId) {
                const helpContentDiv = document.getElementById('help-content-div') as HTMLDivElement;
                if (helpContentDiv) {
                    helpContentDiv.innerHTML = await getEditHelpContent(helpContentId);
                }
            }
        };
        editButton.removeEventListener('click', editButtonClickHandler);
        editButton.addEventListener('click', editButtonClickHandler);
    });
}

/**
 * Initialization of the page: Set event listeners.
 */
document.addEventListener('DOMContentLoaded', function (): void {
    const selectPageElements = document.querySelectorAll('[data-help-page]');
    selectPageElements.forEach((element) => {
        const pageLiElement = element as HTMLLIElement;
        const selectPageClickHandler = async function (event: MouseEvent) {
            const helpPage = pageLiElement.getAttribute('data-help-page');
            if (helpPage) {
                const helpPageContentDiv = document.getElementById('help-page-content-div') as HTMLDivElement;
                if (helpPageContentDiv) {
                    helpPageContentDiv.innerHTML = await getPageHelpContentList(helpPage, 1);
                    addEditButtonListeners();
                }
            }
        };
        pageLiElement.removeEventListener('click', selectPageClickHandler);
        pageLiElement.addEventListener('click', selectPageClickHandler);
    });
});