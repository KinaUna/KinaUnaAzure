import { TextTranslation } from './page-models-v8.js';

/**
 * Fetches and loads the cldr-data files for the specified culture for Syncfusion components, used for translations and formatting of data related to dates and numbers.
 * @param currentCulture The locale code for the culture to get.
 * @param syncfusion the base object provided by Syncfusion (The ej object injected by the Syncfusion script).
 */
export async function loadCldrCultureFiles(currentCulture: string, syncfusion: any): Promise<void> {
    let files =['ca-gregorian.json', 'numberingSystems.json', 'numbers.json', 'timeZoneNames.json', 'ca-islamic.json'];
    let loader = syncfusion.base.loadCldr;
    
    for(let prop = 0; prop <files.length; prop++) {
        let val: any;
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
        } else {
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Fetches the translations used by the Zebra date-time-picker.
 * @param languageId The id for the language to get translations for.
 * @returns The ZebraDatePickerTranslation object containing the translated values.
 */
export async function getZebraDatePickerTranslations(languageId: number): Promise<ZebraDatePickerTranslations> {
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

    return new Promise<ZebraDatePickerTranslations>(function (resolve, reject) {
        resolve(translations);
    });
}

/**
 * Default translations for the Zebra date-time-picker.
 */
export class ZebraDatePickerTranslations {
    daysArray: string[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    monthsArray: string[] = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    todayString: string = 'Today';
    clearString: string = 'Clear';
}

/**
 * Fecthes a translation for a specific word (or sentence) on a specific page in a specific language.
 * @param word The original word to translate.
 * @param page The page on which the word is.
 * @param languageId The id for the language to translate to.
 * @returns The translated word (or sentence).
 */
export async function getTranslation(word: string, page: string, languageId: number): Promise<string> {
    let translationItem: TextTranslation = new TextTranslation();
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
        const textTranslation: TextTranslation = await translationsResponse.json();
        translationString = textTranslation.translation;

    }).catch(function (error) {
        console.log('Error loading Zebra Date Picker translations. Error: ' + error);
    });

    return new Promise<string>(function (resolve, reject) {
        resolve(translationString);
    });
}