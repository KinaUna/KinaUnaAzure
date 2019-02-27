(function ($) {
    function ApplicationRole() {
        var $this = this;

        function initilizeModel() {
            $("#modal-action-application-role").on('loaded.bs.modal', function () {

            }).on('hidden.bs.modal', function () {
                $(this).removeData('bs.modal');
            });
        }
        $this.init = function () {
            initilizeModel();
        };
    }
    $(function () {
        var self = new ApplicationRole();
        self.init();
    });
}(jQuery))