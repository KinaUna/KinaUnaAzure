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
                val = await cldrResponse.text();
            }).catch(function (error) {
                console.log('Error loading cldr-data. Error: ' + error);
            });
        }
        loader(JSON.parse(val));
    }
}
//# sourceMappingURL=localization.js.map