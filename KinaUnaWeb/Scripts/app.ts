const serviceWorkerVersion = 'v2';
const bodyContentDiv: any = $('.body-content')
function runWaitMeLeave(): void {
    
    bodyContentDiv.waitMe({
        effect: 'roundBounce',
        text: '',
        bg: 'rgba(25,24,21,0.5)',
        color: [
            '#6a5081', '#7a6095', '#8a70aa', '#9a80bb', '#aa90cc', '#bbaadd', '#ccbbee', '#ddccff',
            '#ccbbee', '#bbaadd', '#aa90cc', '#9a80bb', '#6a5081', '#7a6095', '#8a70aa', '#9a80bb',
            '#aa90cc', '#bbaadd', '#ccbbee', '#ddccff', '#ccbbee', '#bbaadd', '#aa90cc', '#9a80bb'
        ],
        maxSize: '',
        waitTime: -1,
        source: '',
        textPos: 'vertical',
        fontSize: '',
        onClose: function () { }
    });
}

function stopWaitMeLeave(): void {
    bodyContentDiv.waitMe("hide");
}

function runWaitMeLeave2(): void {
    bodyContentDiv.waitMe({
        effect: 'roundBounce',
        text: '',
        bg: 'rgba(40,20,60,0.25)',
        color: ['#6a5081', '#7a6095', '#8a70aa', '#9a80bb', '#aa90cc', '#bbaadd', '#ccbbee', '#ddccff', '#ccbbee', '#bbaadd', '#aa90cc', '#9a80bb', '#6a5081', '#7a6095', '#8a70aa', '#9a80bb', '#aa90cc', '#bbaadd', '#ccbbee', '#ddccff', '#ccbbee', '#bbaadd', '#aa90cc', '#9a80bb'],
        maxSize: '',
        waitTime: -1,
        source: '',
        textPos: 'vertical',
        fontSize: '',
        onClose: function () { }
    });
}

function removeServiceWorkers(): void {
    navigator.serviceWorker.getRegistrations().then(
        function (registrations) {
            for (let registration of registrations) {
                registration.unregister();
            }
        });
}

function updateServiceWorkers(): void {
    navigator.serviceWorker.getRegistrations().then(
        function (registrations) {
            for (let registration of registrations) {
                registration.update();
            }
        });
}

class SideBarSetting {
    showSidebar: boolean = false;
    showSidebarText: boolean = false;
}

const sidebarSetting = new SideBarSetting();
const sidebarElement = document.getElementById('sidebar-menu-div');

const show_sidebar_setting_key = 'show_sidebar_setting';
const show_sidebar_text_setting_key = 'show_sidebar_text_setting';
const serviceWorkerVersion_key = 'service_worker_version';

function sidebarMenuDelay(milliseconds: number): Promise<any> {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

function toggleSidebarText(): void {
    sidebarSetting.showSidebarText = !sidebarSetting.showSidebarText;
    setSidebarText();

    localStorage.setItem(show_sidebar_text_setting_key, JSON.stringify(sidebarSetting.showSidebarText));
    setSideBarPosition();
}

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

function toggleSideBar(): void {
    if (sidebarSetting.showSidebar) {
        sidebarSetting.showSidebar = false;
    }
    else {
        sidebarSetting.showSidebar = true;
    }
        
    localStorage.setItem(show_sidebar_setting_key, JSON.stringify(sidebarSetting.showSidebar));
    setSideBarPosition();
}

async function setSideBarPosition(): Promise<void> {
    const viewportHeight = window.innerHeight;
    const viewportWidth = window.innerWidth;
    const sidebarMenuListWrapperElement = document.getElementById('sidebar-menu-list-wrapper');
    const sidebarNavUlElement = document.getElementById('sidebar-nav-ul');
    const navMainElement = document.getElementById('navMain');
    const topLanguageElement = document.getElementById('topLanguageDiv');
    const sidebarTogglerElement = document.getElementById('sidebarTogglerDiv');
    const kinaUnaMainElement = document.getElementById('kinaunaMainDiv');
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

function setActivePageClass(): void {
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

function initPageSettings(): void {
    if (sidebarElement == null) {
        return;
    }

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

    sidebarElement.style.left = '-100px';
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

    setActivePageClass();
}

$(function () {
    $(document).on('click', function (event) {
        let clickover = $(event.target);
        let _opened = $(".navbar-collapse").hasClass("show");
        if (_opened === true && !clickover.hasClass('navbar-toggler')) {
            $(".navbar-toggler").trigger('click');
        }
    });

    $('.leavePage').on('click', function () {
        let dropDownMenuElement: any = $(this).closest('.dropdown-menu').prev();
        dropDownMenuElement.dropdown('toggle');
        if ($('.navbar-toggler').css('display') !== 'none' && document.getElementById('bodyClick')) {
            $('.navbar-toggler').trigger('click');
        }
        
        runWaitMeLeave();
    });

    $(".leavePage2").on('click', function () {
        runWaitMeLeave2();
    });

    $(".selectedChildProfilePic").on('click', function () {
        if ($('.navbar-toggler').css('display') !== 'none') {
            $('.navbar-toggler').trigger('click');
            $('#selectChildDropDownMenu').toggleClass('show');
        }
        else {
            $('#selectChildMenuButton').trigger('click');
        }
    });

    const toggleSideBarTextButton = document.querySelector<HTMLButtonElement>('#side-bar-toggle-text-btn');
    if (toggleSideBarTextButton !== null) {
        toggleSideBarTextButton.addEventListener('click', () => { toggleSidebarText(); });
    }

    const toggleSideBarButton = document.querySelector<HTMLButtonElement>('#side-bar-toggle-btn');
    if (toggleSideBarButton !== null) {
        toggleSideBarButton.addEventListener('click', () => { toggleSideBar(); });
    }

    initPageSettings();
    setSideBarPosition();
    setSidebarText();
    window.onresize = setSideBarPosition;
        
    window.addEventListener('waitMeStart', () => {
        runWaitMeLeave();
    });
    window.addEventListener('waitMeStop', () => {
        stopWaitMeLeave();
    });
});