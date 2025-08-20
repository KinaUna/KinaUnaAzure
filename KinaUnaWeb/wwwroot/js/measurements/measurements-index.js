import { showPopupAtLoad } from "../item-details/items-display-v9.js";
import { TimeLineType } from "../page-models-v9.js";
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    await showPopupAtLoad(TimeLineType.Measurement);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=measurements-index.js.map