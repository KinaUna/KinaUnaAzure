import { TagsList } from './page-models.js';
export async function getTagsList(progenyId) {
    let tagsList = new TagsList(progenyId);
    const getTagsListParameters = new TagsList(progenyId);
    await fetch('/Progeny/GetTags/', {
        method: 'POST',
        body: JSON.stringify(getTagsListParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getTagsResponse) {
        tagsList = await getTagsResponse.json();
    }).catch(function (error) {
        console.log('Error loading tags. Error: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(tagsList);
    });
}
//# sourceMappingURL=data-tools.js.map