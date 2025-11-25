import { popupPictureDetails } from '../pictures/picture-details-v11.js';
import { getSelectedFamilies, getSelectedProgenies } from '../settings-tools-v11.js';
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
function addSelectTriviaProgenyEventListeners() {
    const progenyElements = document.querySelectorAll('.select-trivia-progeny-link');
    progenyElements.forEach(function (progenyElement) {
        const triviaProgenyClicked = function (event) {
            event.preventDefault();
            const progenyId = progenyElement.getAttribute('data-progeny-id');
            if (progenyId) {
                getProgenyTrivia(parseInt(progenyId), 0);
            }
        };
        progenyElement.removeEventListener('click', triviaProgenyClicked);
        progenyElement.addEventListener('click', triviaProgenyClicked);
    });
}
function setActiveProgenyTriviaButton(progenyId) {
    const progenyElements = document.querySelectorAll('.select-trivia-progeny-link');
    progenyElements.forEach(function (progenyElement) {
        const elementProgenyId = progenyElement.getAttribute('data-progeny-id');
        if (elementProgenyId) {
            progenyElement.classList.remove('active');
            if (parseInt(elementProgenyId) === progenyId) {
                progenyElement.classList.add('active');
            }
        }
    });
}
async function getProgenyTrivia(progenyId, familyId) {
    const triviaRequest = {
        progenyId: progenyId,
        progenies: await getSelectedProgenies(),
        familyId: familyId,
        families: await getSelectedFamilies(),
        skip: 0,
        count: 1,
        sortBy: 0,
        year: 0,
        month: 0,
        day: 0,
        firstItemYear: 0,
        tagFilter: ''
    };
    await fetch('/Home/ProgenyTrivia', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(triviaRequest)
    }).then(async function (getTriviaContent) {
        if (getTriviaContent != null) {
            const triviaHtml = await getTriviaContent.text();
            const triviaDiv = document.querySelector('#progeny-trivia-div');
            if (triviaDiv) {
                triviaDiv.innerHTML = triviaHtml;
            }
            addRandomPictureEventListener();
            addSelectTriviaProgenyEventListeners();
            setActiveProgenyTriviaButton(progenyId);
        }
    });
}
document.addEventListener('DOMContentLoaded', async function () {
    await getProgenyTrivia(0, 0);
});
//# sourceMappingURL=home-index-v11.js.map