const bodyContentDiv = $('.body-content');
/**
 * Displays the default cirle loading spinner in the middle of the page, and fades the rest of the page.
 */
function startFullPageLoadingSpinner() {
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
/**
 * Hides the page loading spinner.
 */
function stopFullPageLoadingSpinner() {
    bodyContentDiv.waitMe("hide");
}
/**
 * Displays an alternative loading spinner in the middle of the page, and fades the rest of the page.
 */
function startFullPageLoadingSpinner2() {
    bodyContentDiv.waitMe({
        effect: 'roundBounce',
        text: '',
        bg: 'rgba(40,20,60,0.25)',
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
/**
 * Shows a spinner with 3 bouncing dots in the given element.
 * @param spinnerElementId The id of the element where the spinner should be shown.
 * @param bgOpacity The opacity of the background.
 * @param bgRed The red value of the background color.
 * @param bgGreen The green value of the background color.
 * @param bgBlue The blue value of the background color.
 */
export function startLoadingItemsSpinner(spinnerElementId, bgOpacity = 0.0, bgRed = 177, bgGreen = 77, bgBlue = 227) {
    const loadingItemsDiv = $('#' + spinnerElementId);
    loadingItemsDiv.waitMe({
        effect: 'bounce',
        text: '',
        bg: 'rgba(' + bgRed + ', ' + bgGreen + ', ' + bgBlue + ', ' + bgOpacity + ')',
        color: '#a022b1',
        maxSize: '',
        waitTime: -1,
        source: '',
        textPos: 'vertical',
        fontSize: '',
        onClose: function () { }
    });
}
/**
 * Hides the spinner in the given element.
 * @param spinnerElementId The id of the element where the spinner should be hidden.
 */
export function stopLoadingItemsSpinner(spinnerElementId) {
    const loadingItemsDiv = $('#' + spinnerElementId);
    loadingItemsDiv.waitMe("hide");
}
/**
 * Adds event listeners for the full page loading spinner.
 */
export function setFullPageSpinnerEventListeners() {
    window.addEventListener('waitMeStart', () => {
        startFullPageLoadingSpinner();
    });
    window.addEventListener('waitMeStop', () => {
        stopFullPageLoadingSpinner();
    });
    window.addEventListener('waitMeStart2', () => {
        startFullPageLoadingSpinner2();
    });
}
/**
 * Triggers an event for showing the full page spinner.
 */
export function startFullPageSpinner() {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);
}
/**
 * Triggers an event for hiding the full page spinner.
 */
export function stopFullPageSpinner() {
    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
}
/**
 * Triggers an event for showing the alternative full page spinner.
 */
export function startFullPageSpinner2() {
    const waitMeStartEvent = new Event('waitMeStart2');
    window.dispatchEvent(waitMeStartEvent);
}
/**
 * Triggers an event for hiding the alternative full page spinner.
 */
export function stopFullPageSpinner2() {
    const waitMeStopEvent = new Event('waitMeStop2');
    window.dispatchEvent(waitMeStopEvent);
}
//# sourceMappingURL=navigation-tools-v8.js.map