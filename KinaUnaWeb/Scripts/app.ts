const serviceWorkerVersion = 'v5';
import { startFullPageSpinner, stopFullPageSpinner, startFullPageSpinner2, setFullPageSpinnerEventListeners } from './navigation-tools-v8.js';
import { initSidebar } from './sidebar-v8.js';

const serviceWorkerVersion_key = 'service_worker_version';

/**
 * Removes all service workers from the browser.
 */
function removeServiceWorkers(): void {
    navigator.serviceWorker.getRegistrations().then(
        function (registrations) {
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
function updateServiceWorkers(): void {
    navigator.serviceWorker.getRegistrations().then(
        function (registrations) {
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
function initPageSettings(): void {
    const localStorageServiceWorkerVersion = localStorage.getItem(serviceWorkerVersion_key);
    if (localStorageServiceWorkerVersion != null) {
        if (localStorageServiceWorkerVersion !== serviceWorkerVersion) {
            updateServiceWorkers();
            localStorage.setItem(serviceWorkerVersion_key, serviceWorkerVersion);
        }
    } else {
        updateServiceWorkers();
        localStorage.setItem(serviceWorkerVersion_key, serviceWorkerVersion);
    }

    initSidebar();
}

/**
 * Shows a full page spinner when navigating to another page.
 * If the menu is open: Collapses the menu if clicking outside the menu, or leaving the page..
 */

function closeMenuIfClickedOutsideOrLeaving(clickover: HTMLElement, leavingPage: boolean): void {
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');

    if (navbarToggler !== null && navbarCollapse !== null) {
        const menuOpened = navbarCollapse.classList.contains('show');
        const parentNav = clickover.closest('.navbar');

        if (menuOpened === true && (parentNav === null || leavingPage)) {
            (navbarToggler as HTMLElement).click();
        }
    }
}

/**
 * Shows a full page spinner when navigating to another page.
 * If the menu is open: Collapses the menu if clicking outside the menu, or leaving the page..
 */
function checkLeavePage(clickover: HTMLElement): boolean {
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
function collapsePopupsAndModals(clickover: HTMLElement): void {
    const itemDetailsPopup = clickover.closest('.item-details-content');
    const zebraDatePicker = clickover.closest('.zebra-datepicker'); // Date pickers are outside the item-details-popup, but can be part of it.

    if (itemDetailsPopup === null && zebraDatePicker !== null) {
        const itemDetailsPopups = document.querySelectorAll('.item-details-popup');
        
        itemDetailsPopups.forEach(function (popup) {
            (popup as HTMLElement).innerHTML = '';
            (popup as HTMLElement).classList.add('d-none');
        });

        let bodyElement = document.querySelector<HTMLBodyElement>('body');
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
function setDocumentClickEventListeners(): void {
    document.addEventListener('click', function (event) {
        const clickover = event.target as HTMLElement;

        if (clickover !== null) {
            let leavingPage = checkLeavePage(clickover);
            closeMenuIfClickedOutsideOrLeaving(clickover, leavingPage);
            collapsePopupsAndModals(clickover);
            }
    });

    setFullPageSpinnerEventListeners();
}

/**
 * Shows the select progeny dropdown when the picture of the current progeny is clicked.
 */
function showSelectProgenyDropdownWhenCurrentProgenyClicked(): void {
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

/**
 * Initializes the page settings when the website is first loaded.
 */

document.addEventListener('DOMContentLoaded', function (): void {
    initPageSettings();

    showSelectProgenyDropdownWhenCurrentProgenyClicked();

    setDocumentClickEventListeners();
});
