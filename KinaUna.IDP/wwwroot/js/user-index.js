(function ($) {
    function User() {
        var $this = this;

        function initilizeModel() {
            $("#modal-action-user").on('loaded.bs.modal', function () {

            }).on('hidden.bs.modal', function () {
                $(this).removeData('bs.modal');
            });
        }
        $this.init = function () {
            initilizeModel();
        };
    }
    $(function () {
        var self = new User();
        self.init();
    });
}(jQuery))