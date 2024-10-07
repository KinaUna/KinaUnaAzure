import { showPopupAtLoad } from "../item-details/items-display-v8";
import { TimeLineType } from "../page-models-v8";

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {

    await showPopupAtLoad(TimeLineType.Measurement);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});
