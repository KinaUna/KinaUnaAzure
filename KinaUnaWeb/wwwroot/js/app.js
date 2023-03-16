var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
var bodyContentDiv = $('.body-content');
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
        for (var _i = 0, registrations_1 = registrations; _i < registrations_1.length; _i++) {
            var registration = registrations_1[_i];
            registration.unregister();
        }
    });
}
function sidebarMenuDelay(milliseconds) {
    return new Promise(function (resolve) {
        setTimeout(resolve, milliseconds);
    });
}
var showSidebar = true;
var showSidebarText = false;
function toggleSidebarText() {
    showSidebarText = !showSidebarText;
    setSidebarText();
    var updatedShowSidebarTextSetting = { showSidebarText: showSidebarText };
    localStorage.setItem('show_sidebar_text_setting', JSON.stringify(updatedShowSidebarTextSetting));
    setSideBarPosition();
}
function setSidebarText() {
    var sidebarExapanderIcon = document.getElementById('sidebar-text-expander');
    sidebarExapanderIcon.style.transition = 'rotate 500ms ease-in-out 0ms';
    var sidebarTexts = document.querySelectorAll('.sidebar-item-text');
    var sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');
    sidebarTexts.forEach(function (textItem) {
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
    var updatedShowSidebarSetting = { showSidebar: showSidebar };
    localStorage.setItem('show_sidebar_setting', JSON.stringify(updatedShowSidebarSetting));
    setSideBarPosition();
}
function setSideBarPosition() {
    return __awaiter(this, void 0, void 0, function () {
        var viewportHeight, sidebarElement, sidebarMenuListWrapperElement, sidebarNavUlElement, navMainElement, topLanguageElement, sidebarTogglerElement, kinaUnaMainElement, sidebarTextsButton, menuOffset, sidebarHeight, maxSidebarHeight;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    viewportHeight = window.innerHeight;
                    sidebarElement = document.getElementById('sidebar-menu-div');
                    sidebarMenuListWrapperElement = document.getElementById('sidebar-menu-list-wrapper');
                    sidebarNavUlElement = document.getElementById('sidebar-nav-ul');
                    navMainElement = document.getElementById('navMain');
                    topLanguageElement = document.getElementById('topLanguageDiv');
                    sidebarTogglerElement = document.getElementById('sidebarTogglerDiv');
                    kinaUnaMainElement = document.getElementById('kinaunaMainDiv');
                    sidebarTextsButton = document.getElementById('side-bar-toggle-text-btn');
                    menuOffset = navMainElement.scrollHeight + topLanguageElement.scrollHeight + 25;
                    sidebarHeight = viewportHeight - (menuOffset + sidebarTogglerElement.offsetHeight);
                    maxSidebarHeight = sidebarNavUlElement.scrollHeight + menuOffset + sidebarTogglerElement.offsetHeight + 10;
                    sidebarElement.style.left = "0px";
                    if (!showSidebar) return [3 /*break*/, 3];
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
                    return [4 /*yield*/, sidebarMenuDelay(500)];
                case 1:
                    _a.sent();
                    sidebarTogglerElement.style.width = sidebarMenuListWrapperElement.offsetWidth + 'px';
                    return [4 /*yield*/, sidebarMenuDelay(500)];
                case 2:
                    _a.sent();
                    sidebarTogglerElement.style.width = sidebarMenuListWrapperElement.offsetWidth + 'px';
                    sidebarTextsButton.style.visibility = "visible";
                    return [3 /*break*/, 4];
                case 3:
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
                    _a.label = 4;
                case 4:
                    ;
                    return [2 /*return*/];
            }
        });
    });
}
var ShowSideBarSetting = /** @class */ (function () {
    function ShowSideBarSetting() {
    }
    return ShowSideBarSetting;
}());
var ShowSideBarTextSetting = /** @class */ (function () {
    function ShowSideBarTextSetting() {
    }
    return ShowSideBarTextSetting;
}());
var showSidebarSetting = new ShowSideBarSetting();
var showSidebarTextSetting = new ShowSideBarTextSetting();
function initPageSettings() {
    var sidebarElement = document.getElementById('sidebar-menu-div');
    sidebarElement.style.left = '-100px';
    showSidebarSetting = JSON.parse(localStorage.getItem('show_sidebar_setting'));
    if (showSidebarSetting != null) {
        if (!showSidebarSetting.showSidebar) {
            showSidebar = false;
        }
        else {
            showSidebar = true;
        }
    }
    else {
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
    }
    else {
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
        var dropDownMenuElement = $(this).closest('.dropdown-menu').prev();
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
//# sourceMappingURL=app.js.map