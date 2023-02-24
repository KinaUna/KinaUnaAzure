var checkFrame;
var isAuthPage = false;
var checkFrameCount = 0;
function runWaitMeLeave() {
    $('.body-content').waitMe({
        effect: 'roundBounce',
        text: '',
        bg: 'rgba(225,240,215,0.3)',
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
    
});