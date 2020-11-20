// Creare's 'Implied Consent' EU Cookie Law Banner v:2.4
// Conceived by Robert Kent, James Bavington & Tom Foyster
var dropCookie = true; // false disables the Cookie, allowing you to style the banner
var cookieDuration = 180; // Number of days before the cookie expires, and the banner reappears
var cookieName = 'kinaUnaCompliancev100'; // Name of our cookie
var cookieValue = 'on'; // Value of cookie

function createDiv() {
    var bodytag = document.getElementsByTagName('body')[0];
    var div = document.createElement('nav');
    div.setAttribute('id', 'cookie-law');
    div.setAttribute('class', 'navbar navbar-dark fixed-bottom bg-warning');
    div.setAttribute('style', 'padding-top: 10px; padding-bottom: 25px; margin-bottom: -15px;');
    div.innerHTML =
        '<div class="container">KinaUna.com uses cookies to store user state and login information.<br/>By continuing we assume your permission to deploy cookies.<br/>For more information see the privacy policy.<a class="leavePage" href="/Home/Privacy/" rel="nofollow" title="Privacy Policy">Privacy Policy</a> <a class="close-cookie-banner btn btn-success" href="javascript:void(0);" onclick="removeMe();"><span>OK</span></a></div>';
    // Be advised the Close Banner 'X' link requires jQuery
    // bodytag.appendChild(div); // Adds the Cookie Law Banner just before the closing </body> tag
    // or
    bodytag.insertBefore(div, bodytag.firstChild); // Adds the Cookie Law Banner just after the opening <body> tag
    document.getElementsByTagName('body')[0].className += ' cookiebanner'; //Adds a class tothe <body> tag when the banner is visible
}

function createCookie(name, value, days) {
    var expires;
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toGMTString();
    }
    else {
        expires = "";
    }
    if (window.dropCookie) {
        document.cookie = name + "=" + value + expires + ";domain=.kinauna.io;path=/";
    }
}

function checkCookie(name) {
    var nameEq = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) === ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEq) === 0) return c.substring(nameEq.length, c.length);
    }
    return null;
}

function eraseCookie(name) {
    createCookie(name, "", -1);
}

window.onload = function () {
    if (checkCookie(window.cookieName) !== window.cookieValue) {
        createDiv();
    }
}

function removeMe() {
    // Create the cookie only if the user click on "Close"
    createCookie(window.cookieName, window.cookieValue, window.cookieDuration); // Create the cookie
    // then close the window/
    var element = document.getElementById('cookie-law');
    element.parentNode.removeChild(element);
}