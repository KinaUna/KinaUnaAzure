import { showPopupAtLoad } from "../item-details/items-display-v8.js";
import { TimeLineType } from "../page-models-v8.js";
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    await showPopupAtLoad(TimeLineType.Vaccination);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=vaccinations-index.js.map