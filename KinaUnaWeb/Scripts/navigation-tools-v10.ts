const bodyContentDiv: any = $('body');
/**
 * Displays the default cirle loading spinner in the middle of the page, and fades the rest of the page.
 */
function startFullPageLoadingSpinner(): void {
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
function stopFullPageLoadingSpinner(): void {
    bodyContentDiv.waitMe("hide");
}

/**
 * Displays an alternative loading spinner in the middle of the page, and fades the rest of the page.
 */
function startFullPageLoadingSpinner2(): void {
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
export function startLoadingItemsSpinner(spinnerElementId: string, bgOpacity: number = 0.0, bgRed: number = 177, bgGreen: number = 77, bgBlue: number = 227): void {
    const loadingItemsDiv: any = $('#' + spinnerElementId);
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
export function stopLoadingItemsSpinner(spinnerElementId: string): void {
    const loadingItemsDiv: any = $('#' + spinnerElementId);
    loadingItemsDiv.waitMe("hide");
}

/**
 * Adds event listeners for the full page loading spinner.
 */
export function setFullPageSpinnerEventListeners(): void {
    window.removeEventListener('waitMeStart', startFullPageLoadingSpinner);
    window.addEventListener('waitMeStart', startFullPageLoadingSpinner);

    window.removeEventListener('waitMeStop', stopFullPageLoadingSpinner);
    window.addEventListener('waitMeStop', stopFullPageLoadingSpinner);

    window.removeEventListener('waitMeStart2', startFullPageLoadingSpinner2);
    window.addEventListener('waitMeStart2', startFullPageLoadingSpinner2);

    window.removeEventListener('waitMeStop2', stopFullPageSpinner2);
    window.addEventListener('waitMeStop2', stopFullPageSpinner2);
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
