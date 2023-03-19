const bodyContentDiv = $('.body-content');
function runWaitMeLeave() {
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
function runWaitMeLeave2() {
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
function removeServiceWorkers() {
    navigator.serviceWorker.getRegistrations().then(function (registrations) {
        for (let registration of registrations) {
            registration.unregister();
        }
    });
}
class SideBarSetting {
}
const sidebarSetting = new SideBarSetting();
function sidebarMenuDelay(milliseconds) {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}
function toggleSidebarText() {
    sidebarSetting.showSidebarText = !sidebarSetting.showSidebarText;
    setSidebarText();
    localStorage.setItem('show_sidebar_text_setting', JSON.stringify(sidebarSetting.showSidebarText));
    setSideBarPosition();
}
function setSidebarText() {
    const sidebarExapanderIcon = document.getElementById('sidebar-text-expander');
    sidebarExapanderIcon.style.transition = 'rotate 500ms ease-in-out 0ms';
    const sidebarTexts = document.querySelectorAll('.sidebar-item-text');
    const sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');
    sidebarTexts.forEach(textItem => {
        if (sidebarSetting.showSidebarText) {
            sidebarExapanderIcon.style.rotate = '180deg';
            textItem.classList.remove('sidebar-item-text-hide');
            sidebarTextsButton.style.top = "12px";
            sidebarTextsButton.style.right = "5%";
        }
        else {
            sidebarExapanderIcon.style.rotate = '0deg';
            textItem.classList.add('sidebar-item-text-hide');
            sidebarTextsButton.style.top = "58px";
            sidebarTextsButton.style.right = "";
        }
    });
}
function toggleSideBar() {
    if (sidebarSetting.showSidebar) {
        sidebarSetting.showSidebar = false;
    }
    else {
        sidebarSetting.showSidebar = true;
    }
    localStorage.setItem('show_sidebar_setting', JSON.stringify(sidebarSetting.showSidebar));
    setSideBarPosition();
}
async function setSideBarPosition() {
    let viewportHeight = window.innerHeight;
    const sidebarElement = document.getElementById('sidebar-menu-div');
    const sidebarMenuListWrapperElement = document.getElementById('sidebar-menu-list-wrapper');
    const sidebarNavUlElement = document.getElementById('sidebar-nav-ul');
    const navMainElement = document.getElementById('navMain');
    const topLanguageElement = document.getElementById('topLanguageDiv');
    const sidebarTogglerElement = document.getElementById('sidebarTogglerDiv');
    const kinaUnaMainElement = document.getElementById('kinaunaMainDiv');
    const sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');
    const menuOffset = navMainElement.scrollHeight + topLanguageElement.scrollHeight + 25;
    let sidebarHeight = viewportHeight - (menuOffset + sidebarTogglerElement.offsetHeight);
    let maxSidebarHeight = sidebarNavUlElement.scrollHeight + menuOffset + sidebarTogglerElement.offsetHeight + 10;
    sidebarElement.style.left = "0px";
    if (sidebarSetting.showSidebar) {
        sidebarTogglerElement.style.transition = 'border-bottom-right-radius 500ms ease-in-out 0ms';
        kinaUnaMainElement.classList.add('kinauna-main');
        sidebarElement.style.opacity = '1.0';
        if (viewportHeight > maxSidebarHeight) {
            sidebarMenuListWrapperElement.style.overflowY = "hidden";
            sidebarMenuListWrapperElement.style.height = (sidebarNavUlElement.scrollHeight + 50) + 'px';
        }
        else {
            sidebarMenuListWrapperElement.style.overflowY = "auto";
            sidebarMenuListWrapperElement.style.height = sidebarHeight + 'px';
        }
        ;
        sidebarElement.style.top = menuOffset + 'px';
        sidebarTogglerElement.style.borderTopRightRadius = '25px';
        sidebarTogglerElement.style.borderBottomRightRadius = '0px';
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
        sidebarElement.style.opacity = '.5';
        sidebarElement.style.top = menuOffset + 'px';
        sidebarTogglerElement.style.borderTopRightRadius = '25px';
        sidebarTogglerElement.style.borderBottomRightRadius = '25px';
        setTimeout(function () {
            sidebarTogglerElement.style.width = '55px';
        }, 500);
        sidebarTextsButton.style.visibility = "collapse";
    }
    ;
}
function initPageSettings() {
    const sidebarElement = document.getElementById('sidebar-menu-div');
    sidebarElement.style.left = '-100px';
    sidebarSetting.showSidebar = JSON.parse(localStorage.getItem('show_sidebar_setting'));
    if (sidebarSetting.showSidebar != null) {
        if (!sidebarSetting.showSidebar) {
            sidebarSetting.showSidebar = false;
        }
        else {
            sidebarSetting.showSidebar = true;
        }
    }
    else {
        sidebarSetting.showSidebar = true;
        localStorage.setItem('show_sidebar_setting', JSON.stringify(sidebarSetting.showSidebar));
    }
    sidebarSetting.showSidebarText = JSON.parse(localStorage.getItem('show_sidebar_text_setting'));
    if (sidebarSetting.showSidebarText != null) {
        if (!sidebarSetting.showSidebarText) {
            sidebarSetting.showSidebarText = false;
        }
        else {
            sidebarSetting.showSidebarText = true;
        }
    }
    else {
        sidebarSetting.showSidebarText = false;
        localStorage.setItem('show_sidebar_text_setting', JSON.stringify(sidebarSetting.showSidebarText));
    }
}
$(function () {
    $(document).on('click', function (event) {
        let clickover = $(event.target);
        let _opened = $(".navbar-collapse").hasClass("show");
        if (_opened === true && !clickover.hasClass("navbar-toggler")) {
            $(".navbar-toggler").trigger('click');
        }
    });
    $('.leavePage').on('click', function () {
        let dropDownMenuElement = $(this).closest('.dropdown-menu').prev();
        dropDownMenuElement.dropdown('toggle');
        if ($('.navbar-toggler').css('display') !== 'none' && document.getElementById('bodyClick')) {
            $('.navbar-toggler').trigger('click');
        }
        runWaitMeLeave();
    });
    $(".leavePage2").on('click', function () {
        runWaitMeLeave2();
    });
    initPageSettings();
    setSideBarPosition();
    setSidebarText();
    window.onresize = setSideBarPosition;
});
//# sourceMappingURL=app.js.map