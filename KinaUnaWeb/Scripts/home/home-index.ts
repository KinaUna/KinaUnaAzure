import { popupPictureDetails } from '../pictures/picture-details.js';

function addRandomPictureEventListener(): void {
    let randomPictureElement = document.querySelector<HTMLDivElement>('#random-picture-link');
    if (randomPictureElement) {
        const pictureId = randomPictureElement.getAttribute('data-random-picture-id') || '';
        const showRandomPicture = async function () {
            await popupPictureDetails(pictureId);
        }
        randomPictureElement.removeEventListener('click', showRandomPicture);
        randomPictureElement.addEventListener('click', showRandomPicture);
    }
}

document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    addRandomPictureEventListener();
});