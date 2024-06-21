export function startFullPageSpinner() {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);
}
export function stopFullPageSpinner() {
    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
}
//# sourceMappingURL=navigation-tools.js.map