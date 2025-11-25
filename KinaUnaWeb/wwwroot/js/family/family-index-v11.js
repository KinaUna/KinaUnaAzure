import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item-v11.js";
import { addProgenyItemEventListenersForAllProgenies } from "../progeny/progeny-details-v11.js";
document.addEventListener('DOMContentLoaded', function () {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
    addProgenyItemEventListenersForAllProgenies();
});
//# sourceMappingURL=family-index-v11.js.map