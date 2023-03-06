document.domain = document.domain;
$(document).ready(function () {
    $(".closeModal").click(function () {
        window.parent.postMessage("closeModal", '*');
    });

    $(".leavePage").click(function () {
        runWaitMeLeave();
        function runWaitMeLeave() {
            $('.body-content').waitMe({
                //none, rotateplane, stretch, orbit, roundBounce, win8,
                //win8_linear, ios, facebook, rotation, timer, pulse,
                //progressBar, bouncePulse or img
                effect: 'roundBounce',
                //place text under the effect (string).
                text: '',
                //background for container (string).
                bg: 'rgba(225,240,215,0.3)',
                //color for background animation and text (string).
                color: ['#6a5081', '#7a6095', '#8a70aa', '#9a80bb', '#aa90cc', '#bbaadd', '#ccbbee', '#ddccff', '#ccbbee', '#bbaadd', '#aa90cc', '#9a80bb', '#6a5081', '#7a6095', '#8a70aa', '#9a80bb', '#aa90cc', '#bbaadd', '#ccbbee', '#ddccff', '#ccbbee', '#bbaadd', '#aa90cc', '#9a80bb'],
                //max size
                maxSize: '',
                //wait time im ms to close
                waitTime: -1,
                //url to image
                source: '',
                //or 'horizontal'
                textPos: 'vertical',
                //font size
                fontSize: '',
                // callback
                onClose: function () { }
            });
        }
    });
});