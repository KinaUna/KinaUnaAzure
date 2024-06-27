import { popupPictureDetails } from '../item-details/picture-details.js';

function addRandomPictureEventListener(): void {
    let randomPictureElement = document.querySelector<HTMLDivElement>('#random-picture-link');
    if (randomPictureElement) {
        const pictureId = randomPictureElement.getAttribute('data-random-picture-id') || '';
        randomPictureElement.addEventListener('click', function () {
            popupPictureDetails(pictureId);
        });
    }
}
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    addRandomPictureEventListener();
});