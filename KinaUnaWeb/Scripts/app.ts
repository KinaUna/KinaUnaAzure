let bodyContentDiv: any = $('.body-content')
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
    navigator.serviceWorker.getRegistrations().then(
        function (registrations) {
            for (let registration of registrations) {
                registration.unregister();
            }
        });
}

function sidebarMenuDelay(milliseconds) {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

let showSidebar = true;
let showSidebarText = false;

function toggleSidebarText() {
    showSidebarText = !showSidebarText;
    setSidebarText();

    let updatedShowSidebarTextSetting = { showSidebarText: showSidebarText };
    localStorage.setItem('show_sidebar_text_setting', JSON.stringify(updatedShowSidebarTextSetting));
    setSideBarPosition();
}

function setSidebarText() {
    let sidebarExapanderIcon = document.getElementById('sidebar-text-expander');
    sidebarExapanderIcon.style.transition = 'rotate 500ms ease-in-out 0ms';
    let sidebarTexts = document.querySelectorAll('.sidebar-item-text');
    let sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');

    sidebarTexts.forEach(textItem => {
        if (showSidebarText) {
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
    if (showSidebar) {
        showSidebar = false;
    }
    else {
        showSidebar = true;
    }

    let updatedShowSidebarSetting = { showSidebar: showSidebar };
    localStorage.setItem('show_sidebar_setting', JSON.stringify(updatedShowSidebarSetting));
    setSideBarPosition();
}

async function setSideBarPosition() {
    let viewportHeight = window.innerHeight;
    let sidebarElement = document.getElementById('sidebar-menu-div');
    let sidebarMenuListWrapperElement = document.getElementById('sidebar-menu-list-wrapper');
    let sidebarNavUlElement = document.getElementById('sidebar-nav-ul');
    let navMainElement = document.getElementById('navMain');
    let topLanguageElement = document.getElementById('topLanguageDiv');
    let sidebarTogglerElement = document.getElementById('sidebarTogglerDiv');
    let kinaUnaMainElement = document.getElementById('kinaunaMainDiv');
    let sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');
    let menuOffset = navMainElement.scrollHeight + topLanguageElement.scrollHeight + 25;
    let sidebarHeight = viewportHeight - (menuOffset + sidebarTogglerElement.offsetHeight);
    let maxSidebarHeight = sidebarNavUlElement.scrollHeight + menuOffset + sidebarTogglerElement.offsetHeight + 10;
    sidebarElement.style.left = "0px";

    if (showSidebar) {
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
        };

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
    };
}

class ShowSideBarSetting {
    showSidebar: boolean;
}

class ShowSideBarTextSetting {
    showSidebarText: boolean;
}

let showSidebarSetting = new ShowSideBarSetting();
let showSidebarTextSetting = new ShowSideBarTextSetting();

function initPageSettings() {
    let sidebarElement = document.getElementById('sidebar-menu-div');
    sidebarElement.style.left = '-100px';
    showSidebarSetting = JSON.parse(localStorage.getItem('show_sidebar_setting'));
    if (showSidebarSetting != null) {
        if (!showSidebarSetting.showSidebar) {
            showSidebar = false;
        }
        else {
            showSidebar = true;
        }
    } else {
        showSidebarSetting.showSidebar = true;
        localStorage.setItem('show_sidebar_setting', JSON.stringify(showSidebarSetting));
    }

    showSidebarTextSetting = JSON.parse(localStorage.getItem('show_sidebar_text_setting'));
    if (showSidebarTextSetting != null) {
        if (!showSidebarTextSetting.showSidebarText) {
            showSidebarText = false;
        }
        else {
            showSidebarText = true;
        }
    } else {
        showSidebarTextSetting.showSidebarText = false;
        localStorage.setItem('show_sidebar_text_setting', JSON.stringify(showSidebarTextSetting));
    }
}
$(document).ready(function () {
    $(document).click(function (event) {
        var clickover = $(event.target);
        var _opened = $(".navbar-collapse").hasClass("show");
        if (_opened === true && !clickover.hasClass("navbar-toggler")) {
            $(".navbar-toggler").click();
        }
    });

    $('.leavePage').click(function () {
        let dropDownMenuElement: any = $(this).closest('.dropdown-menu').prev();
        dropDownMenuElement.dropdown('toggle');
        if ($('.navbar-toggler').css('display') !== 'none' && document.getElementById('bodyClick')) {
            $('.navbar-toggler').trigger('click');
        }
        
        runWaitMeLeave();
    });

    $(".leavePage2").click(function () {
        runWaitMeLeave2();
    });

    initPageSettings();
    setSideBarPosition();
    setSidebarText();
    window.onresize = setSideBarPosition;
});