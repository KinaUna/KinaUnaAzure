class SideBarSetting {
    showSidebar: boolean = false;
    showSidebarText: boolean = false;
}

const sidebarSetting = new SideBarSetting();
const sidebarElement = document.getElementById('sidebar-menu-div');
const show_sidebar_setting_key = 'show_sidebar_setting';
const show_sidebar_text_setting_key = 'show_sidebar_text_setting';

/**
 * Waits for a specified number of milliseconds.
 * @param milliseconds
 */
async function sidebarMenuDelay(milliseconds: number): Promise<any> {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

/**
 * Toggles the visibility of the text next to each icon.
 */
async function toggleSidebarText(): Promise<void> {
    sidebarSetting.showSidebarText = !sidebarSetting.showSidebarText;
    setSidebarText();

    localStorage.setItem(show_sidebar_text_setting_key, JSON.stringify(sidebarSetting.showSidebarText));
    await setSideBarPosition();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates the expand icon and visibility of the sidebar texts.
 */
function setSidebarText(): void {
    const sidebarExapanderIcon = document.getElementById('sidebar-text-expander');

    const sidebarTexts = document.querySelectorAll('.sidebar-item-text');
    const sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');

    if (sidebarExapanderIcon == null || sidebarTexts == null || sidebarTextsButton == null) {
        return;
    }

    sidebarExapanderIcon.style.transition = 'rotate 500ms ease-in-out 0ms';
    sidebarTexts.forEach(textItem => {
        if (sidebarSetting.showSidebarText) {
            sidebarExapanderIcon.style.rotate = '180deg';
            textItem.classList.remove('sidebar-item-text-hide');
            sidebarTextsButton.style.top = "50px";
            sidebarTextsButton.style.right = "12%";
        }
        else {
            sidebarExapanderIcon.style.rotate = '0deg';
            textItem.classList.add('sidebar-item-text-hide');
            sidebarTextsButton.style.top = "50px";
            sidebarTextsButton.style.right = "";
        }
    });
}

/**
 * Collapses/expands the sidebar menu.
 * When collapsed, only the side-bar-toggle-btn is shown.
 */
async function toggleSideBar(): Promise<void> {
    if (sidebarSetting.showSidebar) {
        sidebarSetting.showSidebar = false;
    }
    else {
        sidebarSetting.showSidebar = true;
    }

    localStorage.setItem(show_sidebar_setting_key, JSON.stringify(sidebarSetting.showSidebar));
    await setSideBarPosition();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates the layout of the sidebar to fit the screen/window size.
 */
async function setSideBarPosition(): Promise<void> {
    const viewportHeight = window.innerHeight;
    const viewportWidth = window.innerWidth;
    const sidebarMenuListWrapperElement = document.getElementById('sidebar-menu-list-wrapper');
    const sidebarNavUlElement = document.getElementById('sidebar-nav-ul');
    const navMainElement = document.getElementById('nav-main');
    const topLanguageElement = document.getElementById('top-language-div');
    const sidebarTogglerElement = document.getElementById('sidebar-toggler-div');
    const kinaUnaMainElement = document.getElementById('kinauna-main-div');
    const sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');

    if (navMainElement == null || topLanguageElement == null || sidebarTogglerElement == null || sidebarNavUlElement == null ||
        sidebarElement == null || kinaUnaMainElement == null || sidebarMenuListWrapperElement == null || sidebarTextsButton == null) {
        return;
    }
    const menuOffset = kinaUnaMainElement.offsetTop + 15;

    const sidebarHeight = viewportHeight - (menuOffset + sidebarTogglerElement.offsetHeight);
    const maxSidebarHeight = sidebarNavUlElement.scrollHeight + menuOffset + sidebarTogglerElement.offsetHeight + 20;
    sidebarElement.style.left = "0px";

    if (sidebarSetting.showSidebar) {
        sidebarTogglerElement.style.transition = 'border-bottom-right-radius 500ms ease-in-out 0ms';
        if (sidebarSetting.showSidebarText && viewportWidth > 992) {
            kinaUnaMainElement.classList.add('kinauna-main-wide');
            kinaUnaMainElement.classList.remove('kinauna-main');
        }
        else {
            kinaUnaMainElement.classList.add('kinauna-main');
            kinaUnaMainElement.classList.remove('kinauna-main-wide');
        }
        sidebarElement.style.opacity = '1.0';
        if (viewportHeight > maxSidebarHeight) {
            sidebarMenuListWrapperElement.style.overflowY = "hidden";
            sidebarMenuListWrapperElement.style.height = (sidebarNavUlElement.scrollHeight + 50) + 'px';
        }
        else {
            sidebarMenuListWrapperElement.style.overflowY = "auto";
            sidebarMenuListWrapperElement.style.height = sidebarHeight + 'px';
        };

        sidebarElement.style.top = menuOffset + 'px';

        sidebarTogglerElement.style.borderTopRightRadius = '25px';
        sidebarTogglerElement.style.borderBottomRightRadius = '0px';
        sidebarTogglerElement.style.paddingBottom = '35px';
        sidebarTogglerElement.style.width = sidebarMenuListWrapperElement.offsetWidth + 'px';
        await sidebarMenuDelay(500);
        sidebarTogglerElement.style.width = sidebarMenuListWrapperElement.offsetWidth + 'px';
        await sidebarMenuDelay(500);
        sidebarTogglerElement.style.width = sidebarMenuListWrapperElement.offsetWidth + 'px';
        sidebarTextsButton.style.visibility = "visible";
    }
    else {
        sidebarMenuListWrapperElement.style.overflowY = "hidden";
        sidebarTogglerElement.style.transition = 'border-bottom-right-radius 500ms ease-in-out 1000ms';
        sidebarMenuListWrapperElement.style.height = '0px';
        kinaUnaMainElement.classList.remove('kinauna-main');
        kinaUnaMainElement.classList.remove('kinauna-main-wide');
        sidebarElement.style.opacity = '.5';
        sidebarElement.style.top = menuOffset + 'px';
        sidebarTogglerElement.style.borderTopRightRadius = '25px';
        sidebarTogglerElement.style.borderBottomRightRadius = '25px';

        setTimeout(function () {
            sidebarTogglerElement.style.width = '55px';
        }, 500);
        setTimeout(function () {
            sidebarTogglerElement.style.paddingBottom = '0';
        }, 1050);
        sidebarTextsButton.style.visibility = "collapse";
    };

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets the current page's icon to active, to indicate which section the user is in.
 */
function highlightActivePageIcon(): void {
    const currentUrl = document.location.pathname.replace('/', '');
    const sidebarMenuItems = document.querySelectorAll<HTMLLIElement>('.sidebar-item');
    sidebarMenuItems.forEach(function (sidebarMenuItem): void {
        if (currentUrl.toLowerCase().startsWith(sidebarMenuItem.dataset.sidebarId as string)) {
            sidebarMenuItem.classList.add('active');
        };
        if (currentUrl.length === 0 && sidebarMenuItem.dataset.sidebarId === 'home') {
            sidebarMenuItem.classList.add('active');
        }
    });
}

/**
 * Adds event listeners to collapse/expand the sidebar and show/hide the text next to each icon.
 * Adds event listener to update the layout if the window size changes.
 */
function addSidebarEventListeners(): void {
    const toggleSideBarTextButton = document.querySelector<HTMLButtonElement>('#side-bar-toggle-text-btn');
    if (toggleSideBarTextButton !== null) {
        toggleSideBarTextButton.addEventListener('click', () => { toggleSidebarText(); });
    }

    const toggleSideBarButton = document.querySelector<HTMLButtonElement>('#side-bar-toggle-btn');
    if (toggleSideBarButton !== null) {
        toggleSideBarButton.addEventListener('click', () => { toggleSideBar(); });
    }

    window.onresize = setSideBarPosition;
}

/**
 * Gets the settings for expanding/collapsing the sidebar and showing/hiding texts from local storage.
 */
function loadSidebarSettings(): void {
    const localStorageShowSidebarString = localStorage.getItem(show_sidebar_setting_key);
    if (localStorageShowSidebarString != null) {
        sidebarSetting.showSidebar = JSON.parse(localStorageShowSidebarString);
        if (sidebarSetting.showSidebar != null) {
            if (!sidebarSetting.showSidebar) {
                sidebarSetting.showSidebar = false;
            }
            else {
                sidebarSetting.showSidebar = true;
            }
        } else {
            sidebarSetting.showSidebar = true;
            localStorage.setItem(show_sidebar_setting_key, JSON.stringify(sidebarSetting.showSidebar));
        }
    } else {
        sidebarSetting.showSidebar = true;
        localStorage.setItem(show_sidebar_setting_key, JSON.stringify(sidebarSetting.showSidebar));
    }
}

/**
 * Saves the current sidebar settings to local storage.
 */
function setSidebarLayout(): void {
    let viewportWidth = window.innerWidth;
    const localStorageShowSidebarTextString = localStorage.getItem(show_sidebar_text_setting_key);
    if (localStorageShowSidebarTextString != null) {
        sidebarSetting.showSidebarText = JSON.parse(localStorageShowSidebarTextString);
        if (sidebarSetting.showSidebarText != null) {
            if (!sidebarSetting.showSidebarText || viewportWidth < 992) {
                sidebarSetting.showSidebarText = false;
            }
            else {
                sidebarSetting.showSidebarText = true;
            }
        } else {
            sidebarSetting.showSidebarText = false;
            localStorage.setItem(show_sidebar_text_setting_key, JSON.stringify(sidebarSetting.showSidebarText));
        }
    } else {
        sidebarSetting.showSidebarText = false;
        localStorage.setItem(show_sidebar_text_setting_key, JSON.stringify(sidebarSetting.showSidebarText));
    }
}

/**
 * Initializes the sidepar element.
 */
export async function initSidebar(): Promise<void> {
    if (sidebarElement == null) {
        return;
    }

    sidebarElement.style.left = '-100px';
    
    loadSidebarSettings();

    setSidebarLayout();

    highlightActivePageIcon();

    addSidebarEventListeners();

    await setSideBarPosition().catch(function (error) {
        console.log('Error setting sidebar position. Error: ' + error);
    });
    setSidebarText();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}