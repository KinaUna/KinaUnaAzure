import { showPopupAtLoad } from "../item-details/items-display-v8.js";
import { TimeLineType } from "../page-models-v8.js";

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {

    await showPopupAtLoad(TimeLineType.Vaccination);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});