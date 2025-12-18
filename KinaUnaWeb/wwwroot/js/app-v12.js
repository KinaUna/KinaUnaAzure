import { setAddItemButtonEventListeners } from './addItem/add-item-v12.js';
import { getCurrentLanguageId, getCurrentProgenyId } from './data-tools-v12.js';
import { addFamilyItemEventListenersForAllFamilies } from './families/family-details-v12.js';
import { hideBodyScrollbars, showBodyScrollbars } from './item-details/items-display-v12.js';
import { startFullPageSpinner, startFullPageSpinner2, setFullPageSpinnerEventListeners } from './navigation-tools-v12.js';
import { SetProgenyRequest } from './page-models-v12.js';
import { addProgenyItemEventListenersForAllProgenies } from './progeny/progeny-details-v12.js';
import { addQuickSearchButtonEventListener } from './search/quick-search.js';
import { getSelectedFamilies, getSelectedProgenies } from './settings-tools-v12.js';
import { initSidebar } from './sidebar-v12.js';
const serviceWorkerVersion = 'v11';
const serviceWorkerVersion_key = 'service_worker_version';
/**
 * Removes all service workers from the browser.
 */
function removeServiceWorkers() {
    navigator.serviceWorker.getRegistrations().then(function (registrations) {
        for (let registration of registrations) {
            registration.unregister();
        }
    }).catch(function (error) {
        console.log('Error removing service workers. Error: ' + error);
    });
}
/**
 * Updates all service workers in the browser.
 */
function updateServiceWorkers() {
    navigator.serviceWorker.getRegistrations().then(function (registrations) {
        for (let registration of registrations) {
            registration.unregister();
        }
        // Register current service worker
        navigator.serviceWorker.register('/pwabuilder-sw-v12.js');
    }).catch(function (error) {
        console.log('Error updating service workers. Error: ' + error);
    });
}
/**
 * Initializes the common page settings when the website is first loaded.
 */
function initPageSettings() {
    const localStorageServiceWorkerVersion = localStorage.getItem(serviceWorkerVersion_key);
    if (localStorageServiceWorkerVersion != null) {
        if (localStorageServiceWorkerVersion !== serviceWorkerVersion) {
            updateServiceWorkers();
            localStorage.setItem(serviceWorkerVersion_key, serviceWorkerVersion);
        }
    }
    else {
        updateServiceWorkers();
        localStorage.setItem(serviceWorkerVersion_key, serviceWorkerVersion);
    }
    initSidebar();
}
/**
 * Shows a full page spinner when navigating to another page.
 * If the menu is open: Collapses the menu if clicking outside the menu, or leaving the page..
 */
function closeMenuIfClickedOutsideOrLeaving(clickover, leavingPage) {
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');
    if (navbarToggler !== null && navbarCollapse !== null) {
        const menuOpened = navbarCollapse.classList.contains('show');
        const parentNav = clickover.closest('.navbar');
        if (menuOpened === true && (parentNav === null || leavingPage)) {
            navbarToggler.click();
        }
    }
}
/**
 * Shows a full page spinner when navigating to another page.
 * If the menu is open: Collapses the menu if clicking outside the menu, or leaving the page..
 */
function checkLeavePage(clickover) {
    let leavingPage = false;
    let parentLeavePage = clickover.closest('.leave-page');
    let parentLeavePage2 = clickover.closest('.leave-page2');
    if (clickover.classList.contains('leave-page') || parentLeavePage !== null) {
        startFullPageSpinner();
        leavingPage = true;
    }
    if (clickover.classList.contains('leave-page2') || parentLeavePage2 !== null) {
        startFullPageSpinner2();
        leavingPage = true;
    }
    return leavingPage;
}
/**
 * Hide all popups and modals if clicking outside the pop-up or modal.
 * @param {HTMLElement} clickover The element that was clicked.
 */
function collapsePopupsAndModals(clickover) {
    const itemDetailsPopup = clickover.closest('.item-details-content');
    const zebraDatePicker = clickover.closest('.zebra-datepicker'); // Date pickers are outside the item-details-popup, but can be part of it.
    const isFullScreenBackground = clickover.classList.contains('full-screen-bg');
    if (isFullScreenBackground || (itemDetailsPopup === null && zebraDatePicker !== null)) {
        const itemDetailsPopups = document.querySelectorAll('.item-details-popup');
        itemDetailsPopups.forEach(function (popup) {
            popup.innerHTML = '';
            popup.classList.add('d-none');
        });
        let bodyElement = document.querySelector('body');
        if (bodyElement) {
            bodyElement.style.removeProperty('overflow');
        }
    }
}
/**
 * Sets event listeners for clicking anywhere in the document.
 * For collapsing the menu if clicking outside the menu, or leaving the page.
 * Also collapses pop-ups and modals if clicking outside the pop-up or modal.
 */
function setDocumentClickEventListeners() {
    document.removeEventListener('click', onDocumentClicked);
    document.addEventListener('click', onDocumentClicked);
    setFullPageSpinnerEventListeners();
}
function onDocumentClicked(event) {
    const clickover = event.target;
    if (clickover !== null) {
        let leavingPage = checkLeavePage(clickover);
        closeMenuIfClickedOutsideOrLeaving(clickover, leavingPage);
        collapsePopupsAndModals(clickover);
    }
}
/**
 * Shows the select progeny dropdown when the picture of the current progeny is clicked.
 */
function showSelectProgenyDropdownWhenCurrentProgenyClicked() {
    $(".selected-child-profile-picture").on('click', function () {
        if ($('.navbar-toggler').css('display') !== 'none') {
            $('.navbar-toggler').trigger('click');
            $('#select-child-dropdown-menu').toggleClass('show');
        }
        else {
            $('#select-child-menu-button').trigger('click');
        }
    });
}
function setSelectProgenyAndFamilyButtonsEventListeners() {
    let selectProgenyButtons = document.querySelectorAll('.select-progeny-button');
    selectProgenyButtons.forEach(function (button) {
        button.removeEventListener('click', onSelectProgenyButtonClicked);
        button.addEventListener('click', onSelectProgenyButtonClicked);
    });
    let selectFamilyButtons = document.querySelectorAll('.select-family-button');
    selectFamilyButtons.forEach(function (button) {
        button.removeEventListener('click', onSelectFamilyButtonClicked);
        button.addEventListener('click', onSelectFamilyButtonClicked);
    });
}
function onSelectProgenyButtonClicked(event) {
    event.preventDefault();
    let selectedButton = event.currentTarget;
    selectedButton.classList.toggle('selected');
    setSelectedProgenies();
    getSelectedProgenies();
}
function onSelectFamilyButtonClicked(event) {
    event.preventDefault();
    let selectedButton = event.currentTarget;
    selectedButton.classList.toggle('selected');
    setSelectedFamilies();
    getSelectedFamilies();
}
function setSelectedProgenies() {
    let selectedProgenyButtons = document.querySelectorAll('.select-progeny-button.selected');
    let selectedProgenyIds = [];
    selectedProgenyButtons.forEach(function (button) {
        let selectedProgenyData = button.getAttribute('data-select-progeny-id');
        if (selectedProgenyData) {
            selectedProgenyIds.push(selectedProgenyData);
        }
    });
    let currentProgenyId = getCurrentProgenyId();
    if (!selectedProgenyIds.includes(currentProgenyId.toString())) {
        selectedProgenyIds.push(currentProgenyId.toString());
    }
    if (selectedProgenyIds.length === 0) {
        let allProgenyButtons = document.querySelectorAll('.select-progeny-button');
        allProgenyButtons.forEach(function (button) {
            let selectedProgenyData = button.getAttribute('data-select-progeny-id');
            if (selectedProgenyData) {
                selectedProgenyIds.push(selectedProgenyData);
            }
        });
    }
    localStorage.setItem('selectedProgenies', JSON.stringify(selectedProgenyIds));
    const selectedProgeniesChangedEvent = new Event('progeniesChanged');
    window.dispatchEvent(selectedProgeniesChangedEvent);
}
function setSelectedFamilies() {
    let selectedFamilyButtons = document.querySelectorAll('.select-family-button.selected');
    let selectedFamilyIds = [];
    selectedFamilyButtons.forEach(function (button) {
        let selectedFamilyData = button.getAttribute('data-select-family-id');
        if (selectedFamilyData) {
            selectedFamilyIds.push(selectedFamilyData);
        }
    });
    if (selectedFamilyIds.length === 0) {
        let allFamilyButtons = document.querySelectorAll('.select-family-button');
        allFamilyButtons.forEach(function (button) {
            let selectedFamilyData = button.getAttribute('data-select-family-id');
            if (selectedFamilyData) {
                selectedFamilyIds.push(selectedFamilyData);
            }
        });
    }
    localStorage.setItem('selectedFamilies', JSON.stringify(selectedFamilyIds));
    const selectedFamiliesChangedEvent = new Event('familiesChanged');
    window.dispatchEvent(selectedFamiliesChangedEvent);
}
function setSetDefaultProgenyEventListeners() {
    let setDefaultProgenyButtons = document.querySelectorAll('.set-default-progeny-button');
    setDefaultProgenyButtons.forEach(function (button) {
        button.removeEventListener('click', onSetDefaultProgenyButtonClicked);
        button.addEventListener('click', onSetDefaultProgenyButtonClicked);
    });
}
async function onSetDefaultProgenyButtonClicked(event) {
    let selectedButton = event.currentTarget;
    let selectedProgenyId = selectedButton.getAttribute('data-default-progeny-id');
    if (selectedProgenyId !== null) {
        await setDefaultProgeny(parseInt(selectedProgenyId));
    }
}
async function setDefaultProgeny(progenyId) {
    let request = new SetProgenyRequest();
    request.progenyId = progenyId;
    request.languageId = getCurrentLanguageId();
    await fetch('/Home/SetDefaultProgeny', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(request)
    }).then(async function (setDefaultProgenyResult) {
        if (setDefaultProgenyResult.ok) {
            // Reload page.
            location.reload();
        }
    });
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Closes the item details modal if it is open.
 * @return True if the modal was open and is now closed, false if it was already closed.
 */
function closeItemDetailsModal() {
    const itemDetailsPopupDiv = document.querySelector('#item-details-div');
    if (itemDetailsPopupDiv !== null) {
        if (itemDetailsPopupDiv.classList.contains('d-none')) {
            return false;
        }
        itemDetailsPopupDiv.innerHTML = '';
        itemDetailsPopupDiv.classList.add('d-none');
        showBodyScrollbars();
        return true;
    }
    return false;
}
/**
 * Closes the quick search modal if it is open.
 * @return True if the modal was open and is now closed, false if it was already closed.
 */
function closeQuickSearchModal() {
    const quickSearchModalDiv = document.querySelector('#quick-search-modal-div');
    if (quickSearchModalDiv !== null) {
        if (quickSearchModalDiv.classList.contains('d-none')) {
            return false;
        }
        quickSearchModalDiv.innerHTML = '';
        quickSearchModalDiv.classList.add('d-none');
        showBodyScrollbars();
        return true;
    }
    return false;
}
function closeKanbanItemDetailsModal() {
    const kanbanItemDetailsPopupDiv = document.querySelector('#kanban-item-details-div');
    if (kanbanItemDetailsPopupDiv !== null) {
        if (kanbanItemDetailsPopupDiv.classList.contains('d-none')) {
            return false;
        }
        kanbanItemDetailsPopupDiv.innerHTML = '';
        kanbanItemDetailsPopupDiv.classList.add('d-none');
        showBodyScrollbars();
        return true;
    }
    return false;
}
function closeSettingsModals() {
    const settingsModals = document.querySelectorAll('.settings-modal');
    let anyClosed = false;
    settingsModals.forEach(function (modal) {
        if (!modal.classList.contains('d-none')) {
            modal.classList.add('d-none');
            anyClosed = true;
            showBodyScrollbars();
        }
    });
    return anyClosed;
}
function closeAddItemModal() {
    const addItemModalDiv = document.querySelector('#main-modal');
    if (addItemModalDiv !== null) {
        if (addItemModalDiv.classList.contains('d-none')) {
            return false;
        }
        addItemModalDiv.classList.add('d-none');
        return true;
    }
    return false;
}
/**
 * Handles the popstate event to close modals/pop-ups instead of navigating back.
 * @param event The PopStateEvent object.
 */
function onPopState(event) {
    closeItemDetailsModal();
    closeQuickSearchModal();
    closeKanbanItemDetailsModal();
    closeSettingsModals();
    closeAddItemModal();
}
/**
 * Overrides the back button behavior to close modals/pop-ups instead of navigating back.
 */
function addOverrideBackButtonEventListener() {
    window.removeEventListener('popstate', onPopState);
    window.addEventListener('popstate', onPopState);
}
/**
 * Handles the click event on the "Add Item" button to open the "Add Item" modal.
 * @param event The MouseEvent object.
 */
function onAddItemButtonClicked(event) {
    event.preventDefault();
    const addItemModalDiv = document.querySelector('#main-modal');
    if (addItemModalDiv !== null) {
        addItemModalDiv.classList.remove('d-none');
        history.pushState(null, document.title, window.location.href);
        hideBodyScrollbars();
        const addItemModalCloseButton = document.querySelector('#add-item-modal-close-button');
        if (addItemModalCloseButton !== null) {
            const closeAddItemModalAction = function () {
                addItemModalDiv.classList.add('d-none');
                showBodyScrollbars();
                history.back();
            };
            addItemModalCloseButton.removeEventListener('click', closeAddItemModalAction);
            addItemModalCloseButton.addEventListener('click', closeAddItemModalAction);
        }
    }
}
/**
 * Adds event listeners to the "Add Item" button on the main page.
 * When clicked, it opens the "Add Item" modal.
 */
function addAddItemButtonEventListeners() {
    let addItemButton = document.querySelector('#main-page-add-item-button');
    if (addItemButton !== null) {
        addItemButton.removeEventListener('click', onAddItemButtonClicked);
        addItemButton.addEventListener('click', onAddItemButtonClicked);
    }
}
/**
 * Initializes the page settings when the website is first loaded.
 */
document.addEventListener('DOMContentLoaded', function () {
    initPageSettings();
    showSelectProgenyDropdownWhenCurrentProgenyClicked();
    setDocumentClickEventListeners();
    setSelectProgenyAndFamilyButtonsEventListeners();
    getSelectedProgenies();
    getSelectedFamilies();
    setSetDefaultProgenyEventListeners();
    addProgenyItemEventListenersForAllProgenies();
    addFamilyItemEventListenersForAllFamilies();
    setAddItemButtonEventListeners();
    addQuickSearchButtonEventListener();
    addOverrideBackButtonEventListener();
    addAddItemButtonEventListeners();
});
//# sourceMappingURL=app-v12.js.map