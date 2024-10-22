import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { addProgenyItemEventListenersForAllProgenies } from "../progeny/progeny-details.js";

document.addEventListener('DOMContentLoaded', function (): void {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
    addProgenyItemEventListenersForAllProgenies();
});