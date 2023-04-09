import { TagsList } from './page-models.js';

export async function getTagsList(progenyId: number): Promise<TagsList> {
    let tagsList: TagsList = new TagsList(progenyId);

    const getTagsListParameters: TagsList = new TagsList(progenyId);

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

    return new Promise<TagsList>(function (resolve, reject) {
        resolve(tagsList);
    });
}