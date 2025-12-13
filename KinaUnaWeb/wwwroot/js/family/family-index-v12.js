import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item-v12.js";
import { addProgenyItemEventListenersForAllProgenies } from "../progeny/progeny-details-v12.js";
document.addEventListener('DOMContentLoaded', function () {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
    addProgenyItemEventListenersForAllProgenies();
});
//# sourceMappingURL=family-index-v12.js.map