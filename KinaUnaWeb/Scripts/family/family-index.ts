import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";

document.addEventListener('DOMContentLoaded', function (): void {
    setEditItemButtonEventListeners();
    setDeleteItemButtonEventListeners();
});