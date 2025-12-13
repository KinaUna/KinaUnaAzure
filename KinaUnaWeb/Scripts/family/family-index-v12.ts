import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item-v12.js";
import { addProgenyItemEventListenersForAllProgenies } from "../progeny/progeny-details-v12.js";

document.addEventListener('DOMContentLoaded', function (): void {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
    addProgenyItemEventListenersForAllProgenies();
});