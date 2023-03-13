function runWaitMeLeave() {
    $('.body-content').waitMe({
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
    $('body-content').waitMe({
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

let showSidebar = true;
let firstRun = true;

function toggleSideBar() {
    let sidebarElement = document.getElementById('sidebar-menu-div');
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

function setSideBarPosition() {
    let viewportHeight = window.innerHeight;
    let sidebarElement = document.getElementById('sidebar-menu-div');
    let sidebarMenuListElement = document.getElementById('sidebar-menu-list-div');
    let sidebarMenuListWrapperElement = document.getElementById('sidebar-menu-list-wrapper');
    let sidebarNavUlElement = document.getElementById('sidebar-nav-ul');
    let navMainElement = document.getElementById('navMain');
    let topLanguageElement = document.getElementById('topLanguageDiv');
    let sidebarTogglerElement = document.getElementById('sidebarTogglerDiv');
    let kinaUnaMainElement = document.getElementById('kinaunaMainDiv');
    let menuOffset = navMainElement.scrollHeight + topLanguageElement.scrollHeight + 25;
    let sidebarHeight = viewportHeight - (menuOffset + sidebarTogglerElement.offsetHeight);
    let maxSidebarHeight = sidebarNavUlElement.scrollHeight + menuOffset + sidebarTogglerElement.offsetHeight + 10;
    sidebarElement.style.left = "0px";

    if (showSidebar) {
        sidebarTogglerElement.style.transition = 'border-bottom-right-radius 500ms ease-in-out 0ms';
        kinaUnaMainElement.classList.add('kinauna-main');
        sidebarElement.style.opacity = '1.0';
        if (viewportHeight > maxSidebarHeight) {
            sidebarMenuListWrapperElement.style.height = (sidebarNavUlElement.scrollHeight + 20) + 'px';
            sidebarElement.style.width = '55px';
            sidebarTogglerElement.style.width = '55px';
        }
        else {
            sidebarMenuListWrapperElement.style.height = sidebarHeight + 'px';
            sidebarElement.style.width = '72px';
            sidebarTogglerElement.style.width = '72px';
        };

        
        sidebarElement.style.top = menuOffset + 'px';
        sidebarElement.style.overflowY = "";
        sidebarTogglerElement.style.borderTopRightRadius = '25px';
        sidebarTogglerElement.style.borderBottomRightRadius = '0px';
    }
    else {
        sidebarTogglerElement.style.transition = 'border-bottom-right-radius 500ms ease-in-out 1000ms';
        sidebarMenuListWrapperElement.style.height = '0px';
        kinaUnaMainElement.classList.remove('kinauna-main');
        sidebarElement.style.opacity = '.5';
        sidebarElement.style.overflowY = "hidden";
        let sidebarTogglerBottom = viewportHeight - menuOffset - sidebarTogglerElement.scrollHeight;
        // sidebarElement.style.bottom = sidebarTogglerBottom + 'px';
        sidebarElement.style.top = menuOffset + 'px';
        //sidebarElement.style.width = '55px';
        //sidebarTogglerElement.style.width = '55px';
        sidebarTogglerElement.style.borderTopRightRadius = '25px';
        sidebarTogglerElement.style.borderBottomRightRadius = '25px';
    };
}

let showSidebarSetting = {};

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
        showSidebarSetting = { showSidebar: true };
        localStorage.setItem('show_sidebar_setting', JSON.stringify(showSidebarSetting));
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
        $(this).closest('.dropdown-menu').prev().dropdown('toggle');
        if ($('.navbar-toggler').css('display') !== 'none' && document.getElementById('bodyClick')) {
            $('.navbar-toggler').trigger('click');
        }
        document.getElementById('navMain').style.opacity = 0.8;
        runWaitMeLeave();
    });

    $(".leavePage2").click(function() {
        runWaitMeLeave2();
    });

    initPageSettings();
    setSideBarPosition();
    window.onresize = setSideBarPosition;
});