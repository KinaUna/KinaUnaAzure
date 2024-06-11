import { TextTranslation } from './page-models-v2.js';
export async function loadCldrCultureFiles(currentCulture, syncfusion) {
    let files = ['ca-gregorian.json', 'numberingSystems.json', 'numbers.json', 'timeZoneNames.json', 'ca-islamic.json'];
    let loader = syncfusion.base.loadCldr;
    for (let prop = 0; prop < files.length; prop++) {
        let val;
        if (files[prop] === 'numberingSystems.json') {
            await fetch('/cldr-data/supplemental/' + files[prop], {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (cldrResponse) {
                val = await cldrResponse.text();
            }).catch(function (error) {
                console.log('Error loading cldr-data. Error: ' + error);
            });
        }
        else {
            await fetch('/cldr-data/main/' + currentCulture + '/' + files[prop], {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (cldrResponse) {
                val = await cldrResponse.json();
            }).catch(function (error) {
                console.log('Error loading cldr-data. Error: ' + error);
            });
        }
        loader(val);
    }
}
export async function getZebraDatePickerTranslations(languageId) {
    let translations = new ZebraDatePickerTranslations();
    if (languageId > 1) {
        await fetch('/Translations/ZebraDatePicker/' + languageId, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        }).then(async function (translationsResponse) {
            translations = await translationsResponse.json();
        }).catch(function (error) {
            console.log('Error loading Zebra Date Picker translations. Error: ' + error);
        });
    }
    return new Promise(function (resolve, reject) {
        resolve(translations);
    });
}
export class ZebraDatePickerTranslations {
    constructor() {
        this.daysArray = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        this.monthsArray = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        this.todayString = 'Today';
        this.clearString = 'Clear';
    }
}
export async function getTranslation(word, page, languageId) {
    let translationItem = new TextTranslation();
    translationItem.word = word;
    translationItem.page = page;
    translationItem.languageId = languageId;
    let translationString = word;
    await fetch('/Translations/GetTranslation/', {
        method: 'POST',
        body: JSON.stringify(translationItem),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (translationsResponse) {
        const textTranslation = await translationsResponse.json();
        translationString = textTranslation.translation;
    }).catch(function (error) {
        console.log('Error loading Zebra Date Picker translations. Error: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(translationString);
    });
}
//# sourceMappingURL=localization-v2.js.map