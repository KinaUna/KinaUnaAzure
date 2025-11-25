import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item-v10.js";
import { addProgenyItemEventListenersForAllProgenies } from "../progeny/progeny-details-v10.js";
document.addEventListener('DOMContentLoaded', function () {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
    addProgenyItemEventListenersForAllProgenies();
});
//# sourceMappingURL=family-index-v10.js.map