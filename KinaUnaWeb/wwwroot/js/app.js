const serviceWorkerVersion = 'v6';
import { setAddItemButtonEventListeners } from './addItem/add-item.js';
import { getCurrentLanguageId, getCurrentProgenyId } from './data-tools-v8.js';
import { startFullPageSpinner, startFullPageSpinner2, setFullPageSpinnerEventListeners } from './navigation-tools-v8.js';
import { SetProgenyRequest } from './page-models-v8.js';
import { addProgenyItemEventListenersForAllProgenies } from './progeny/progeny-details.js';
import { getSelectedProgenies } from './settings-tools-v8.js';
import { initSidebar } from './sidebar-v8.js';
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
            registration.update();
        }
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
function setSelectProgenyButtonsEventListeners() {
    let selectProgenyButtons = document.querySelectorAll('.select-progeny-button');
    selectProgenyButtons.forEach(function (button) {
        button.addEventListener('click', onSelectProgenyButtonClicked);
    });
}
function onSelectProgenyButtonClicked(event) {
    event.preventDefault();
    let selectedButton = event.currentTarget;
    selectedButton.classList.toggle('selected');
    setSelectedProgenies();
    getSelectedProgenies();
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
function setSetDefaultProgenyEventListeners() {
    let setDefaultProgenyButtons = document.querySelectorAll('.set-default-progeny-button');
    setDefaultProgenyButtons.forEach(function (button) {
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
 * Initializes the page settings when the website is first loaded.
 */
document.addEventListener('DOMContentLoaded', function () {
    initPageSettings();
    showSelectProgenyDropdownWhenCurrentProgenyClicked();
    setDocumentClickEventListeners();
    setSelectProgenyButtonsEventListeners();
    getSelectedProgenies();
    setSetDefaultProgenyEventListeners();
    addProgenyItemEventListenersForAllProgenies();
    setAddItemButtonEventListeners();
});
//# sourceMappingURL=app.js.map