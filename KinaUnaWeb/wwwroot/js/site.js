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

window.addEventListener('message',
    function (event) {
        isAuthPage = false;
        if (~event.origin.indexOf('https://web.kinauna.com')) {
            isAuthPage = true;
            if (event.data === "closeKinaUnaFrame") {
                if (window === window.top) {
                    window.location.href = window.location.href;
                }
            }
        }
        if (~event.origin.indexOf('https://localhost:44324')) {
            isAuthPage = true;
            if (event.data === "closeKinaUnaFrame") {
                if (window === window.top) {
                    window.location.href = window.location.href;
                }
            }
        }
        if (~event.origin.indexOf('https://auth.kinauna.com')) {
            isAuthPage = true;
            if (event.data === "closeModal") {
                runWaitMeLeave();
                loginModalClosed();
                $('#loginModal').modal('hide');
                $(document.body).removeClass('modal-open');
                $('.modal-backdrop').remove();
            }
            if (event.data === "openModal") {
                $('#loginModal').modal('show');
            }
            if (event.data === "logOutKinaUna") {
                if (window === window.top) {
                    document.getElementById('logOutForm').submit();
                }
            }
        } else if (~event.origin.indexOf('https://localhost:44397')) {
            isAuthPage = true;
            if (event.data === "closeModal") {
                runWaitMeLeave();
                loginModalClosed();
                $('#loginModal').modal('hide');
                $(document.body).removeClass('modal-open');
                $('.modal-backdrop').remove();
            }
            if (event.data === "openModal") {
                $('#loginModal').modal('show');
            }
            if (event.data === "logOutKinaUna") {
                if (window === window.top) {
                    document.getElementById('logOutForm').submit();
                }
            }
        } else {
            return;
        }
    });

function frameLoaded() {
    if ($('#loginModal').is(':visible')) {
        checkFrame = setInterval(function () {
            if ($('#loginModal').is(':visible')) {
                var iframe = document.getElementById('loginFrame');
                iframe.contentWindow.postMessage('web', '*');
                checkFrameCount++;
                if (!isAuthPage && checkFrameCount > 1) {
                    console.log("Submitting LoginForm from checkFrame.");
                    document.getElementById('loginForm').submit();
                } else {
                    return false;
                }
            } else {
                loginModalClosed();
            }
        },
            5000);
    }
}
function loginModalClosed() {
    clearInterval(checkFrame);
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