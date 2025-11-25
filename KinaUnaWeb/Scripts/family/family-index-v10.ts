import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item-v10.js";
import { addProgenyItemEventListenersForAllProgenies } from "../progeny/progeny-details-v10.js";

document.addEventListener('DOMContentLoaded', function (): void {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
    addProgenyItemEventListenersForAllProgenies();
});