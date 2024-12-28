(function ($) {
    const mapPaymentStatus = new Map([[0, "paid"], [1, "partially_refunded"]]);
    const mapFulfiilmentStatus = new Map([[0, "Unfulfilled"], [1, "Fullfilled"]]);

    $('.btn-todo').on('click', function () {
        $('#messages_form [name="em_state"]').val(0);
        $('#messages_form').submit();
    });
    $('.btn-inprogress').on('click', function () {
        $('#messages_form [name="em_state"]').val(1);
        $('#messages_form').submit();
    });
    $('.btn-lowpriority').on('click', function () {
        $('#messages_form [name="em_state"]').val(2);
        $('#messages_form').submit();
    });
    $('.btn-all').on('click', function () {
        $('#messages_form [name="em_state"]').val(-1);
        $('#messages_form').submit();
    });
    $('.btn-trash').on('click', function () {
        $('#messages_form [name="em_state"]').val(4);
        $('#messages_form').submit();
    });
    $('.btn-spam').on('click', function () {
        $('#messages_form [name="em_state"]').val(5);
        $('#messages_form').submit();
    });
    $('.mailcontent-header').on('click', function () {
        let bodyObj = $(this).siblings('.mailcontent-body');
        if (bodyObj.hasClass('d-none')) {
            bodyObj.removeClass('d-none');
        } else {
            bodyObj.addClass('d-none');
        }
    });

    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
            confirmButton: "btn btn-success",
            cancelButton: "btn btn-danger"
        },
        buttonsStyling: false
    });

    $('.btn-sendmail').on('click', function () {
        let toUser = $('#inputEmail').val();
        let body = objQuill.getText();
        let em_idx = $(this).data('idx');
        swalWithBootstrapButtons.fire({
            title: "Should you send the email?",
            text: "",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Yes, I'll do it!",
            cancelButtonText: "No, I won't.!",
            reverseButtons: true
        }).then((result) => {
            const Toast = Swal.mixin({
                toast: true,
                position: "top-end",
                showConfirmButton: false,
                timer: 3000,
                timerProgressBar: true,
                didOpen: (toast) => {
                    toast.onmouseenter = Swal.stopTimer;
                    toast.onmouseleave = Swal.resumeTimer;
                }
            });
            if (result.isConfirmed) {
                $.post('/home/sendRequestEmail', { strTo: toUser, strBody: body, em_idx: em_idx }, function (resp) {
                    if (resp.status == 1) {
                        Toast.fire({
                            icon: "success",
                            title: "sending email success!"
                        });
                    } else {
                        Toast.fire({
                            icon: "error",
                            title: "sending email fail!"
                        });
                    }
                }, 'json');

            }
        });

    });
    $('.btn-respond-trash').on('click', function () {
        $('.respond-section').addClass('d-none');
    });
    

    const _Toast = Swal.mixin({
        toast: true,
        position: "top-end",
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.onmouseenter = Swal.stopTimer;
            toast.onmouseleave = Swal.resumeTimer;
        },
        didClose: () => {
            window.location.reload();
        } 
    });

    $('.btn-order .btn-cancel').on('click', function () {
        let orderId = $(this).parent().data('idx');
        let emIdx = $(this).parent().data('emidx');
        if (!orderId || !emIdx) return;
        $.post('/home/requestShopify', { orderId: orderId, type: 2, em_idx: emIdx }, function (resp) {
            if (resp.status == 1) {
                _Toast.fire({
                    icon: "success",
                    title: "Refund successflly!"
                });
            }
        }, 'json');
    });
    
    $('.btn-order .btn-refund').on('click', function () {
        let orderId = $(this).parent().data('idx');
        let emIdx = $(this).parent().data('emidx');
        if (!orderId || !emIdx) return;
        $.post('/home/requestShopify', { orderId: orderId, type: 3, em_idx: emIdx }, function (resp) {
            if (resp.status == 1) {                
                _Toast.fire({
                    icon: "success",
                    title: "Refund successflly!"
                });
            }
        }, 'json');
    });
    

    $('.btn-rephase').on('click', function () {
        let em_idx = $(this).data('idx');
        $(this).prop('disabled', true);
        $(this).html('<span class="spinner-border spinner-border-sm" role = "status" aria-hidden="true" > </span> Loading...');

        const _swalWithBootstrapButtons = Swal.mixin({
            customClass: {
                confirmButton: "btn btn-success",
                cancelButton: "btn btn-danger"
            },
            buttonsStyling: false
        });


        $.post('/home/rephase', { em_idx: em_idx }, function (resp) {
            if (resp.status == 1) {
                let strRephase = resp.data.rephase;
                objQuill.setText(strRephase);
            } else if (resp.status == 2) {
                _swalWithBootstrapButtons.fire({
                    title: "Process the cancellation request?",
                    text: "The customer's request to cancel the order with ID '" + resp.data.orderId + "'—would you process this request?",
                    icon: "question",
                    showCancelButton: true,
                    confirmButtonText: "Yes, I'll do it!",
                    cancelButtonText: "No, I'll not!",
                    reverseButtons: true
                }).then((result) => {
                    if (result.isConfirmed) {
                        $.post('/home/requestShopify', { orderId: resp.data.orderId, type: 2 }, function (resp) {
                            if (resp.status == 1) {
                                const Toast = Swal.mixin({
                                    toast: true,
                                    position: "top-end",
                                    showConfirmButton: false,
                                    timer: 3000,
                                    timerProgressBar: true,
                                    didOpen: (toast) => {
                                        toast.onmouseenter = Swal.stopTimer;
                                        toast.onmouseleave = Swal.resumeTimer;
                                    }
                                });
                                Toast.fire({
                                    icon: "success",
                                    title: "Cancellation request will be accepted"
                                });
                            }
                        }, 'json');
                    }
                });
            } else if (resp.status == 3) {
                _swalWithBootstrapButtons.fire({
                    title: "Process the refund request?",
                    text: "The customer's request to refund the order with ID " + resp.data.orderId + "—would you process this request?",
                    icon: "question",
                    showCancelButton: true,
                    confirmButtonText: "Yes, I'll do it!",
                    cancelButtonText: "No, I'll not!",
                    reverseButtons: true
                }).then((result) => {
                    if (result.isConfirmed) {
                        $.post('/home/requestShopify', { orderId: resp.data.orderId, type: 3 }, function (resp) {
                            if (resp.status == 1) {
                                _swalWithBootstrapButtons.fire({
                                    title: "Success!",
                                    text: "Refund request will be accepted.",
                                    icon: "success"
                                });
                            }
                        }, 'json');
                    }
                });
            }
            $('.btn-rephase').prop('disabled', false);
            $('.btn-rephase').html('Rephase');
        }, 'json');
    });

    $('.more-actions .act-todo').on('click', function () {
        let idx = $(this).parent().data('idx');
        makeState(idx, 0);
    });

    $('.more-actions .act-inprogress').on('click', function () {
        let idx = $(this).parent().data('idx');
        makeState(idx, 1);
    });

    $('.more-actions .act-onhold').on('click', function () {
        let idx = $(this).parent().data('idx');
        makeState(idx, 2);
    });
    $('.more-actions .act-archived').on('click', function () {
        let idx = $(this).parent().data('idx');
        makeState(idx, 3);
    });
    $('.more-actions .act-trash').on('click', function () {
        let idx = $(this).parent().data('idx');
        makeState(idx, 4);
    });
    $('.more-actions .act-spam').on('click', function () {
        let idx = $(this).parent().data('idx');
        makeState(idx, 5);
    });
    var makeState = function(idx, em_state){
        if (!idx) return;
        const _Toast = Swal.mixin({
            toast: true,
            position: "top-end",
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.onmouseenter = Swal.stopTimer;
                toast.onmouseleave = Swal.resumeTimer;
            },           
        });
        $.post('/home/makestate', { idx: [idx], em_state: em_state }, function (resp) {
            if (resp.status == 1) {
                _Toast.fire({
                    icon: "success",
                    title: "Success!"
                });
            }
        }, 'json');
    }
})(window.jQuery);