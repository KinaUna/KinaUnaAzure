import { setMomentLocale } from '../data-tools-v1.js';

$(async function (): Promise<void> {
    setMomentLocale();
    (<any>$.fn.dataTable).moment('DD-MMMM-YYYY');
    $('#skillzList').DataTable({ 'scrollX': false, 'order': [[3, 'desc']] });
});