import { popupPictureDetails } from '../item-details/picture-details.js';
function addRandomPictureEventListener() {
    let randomPictureElement = document.querySelector('#random-picture-link');
    if (randomPictureElement) {
        const pictureId = randomPictureElement.getAttribute('data-random-picture-id') || '';
        randomPictureElement.addEventListener('click', function () {
            popupPictureDetails(pictureId);
        });
    }
}
document.addEventListener('DOMContentLoaded', async function () {
    addRandomPictureEventListener();
});
//# sourceMappingURL=home-index.js.map