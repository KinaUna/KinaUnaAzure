import { setMomentLocale } from '../data-tools.js';
$(async function () {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY');
    $('#skillzList').DataTable({ 'scrollX': false, 'order': [[3, 'desc']] });
});
//# sourceMappingURL=skills-index.js.map