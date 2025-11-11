import { popupPictureDetails } from '../pictures/picture-details.js';
function addRandomPictureEventListener() {
    let randomPictureElement = document.querySelector('#random-picture-link');
    if (randomPictureElement) {
        const pictureId = randomPictureElement.getAttribute('data-random-picture-id') || '';
        const showRandomPicture = async function () {
            await popupPictureDetails(pictureId);
        };
        randomPictureElement.removeEventListener('click', showRandomPicture);
        randomPictureElement.addEventListener('click', showRandomPicture);
    }
}
document.addEventListener('DOMContentLoaded', async function () {
    addRandomPictureEventListener();
});
//# sourceMappingURL=home-index.js.map