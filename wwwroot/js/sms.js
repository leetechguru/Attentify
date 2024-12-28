!function ($) {
    "use strict";

    var Components = function () { };
    //initializing Slimscroll
    Components.prototype.initSlimScrollPlugin = function () {
        $.fn.slimScroll && $(".slimscroll").slimScroll({
            height: 'auto',
            position: 'right',
            size: "8px",
            touchScrollStep: 20,
            color: '#9ea5ab'
        });
    },
    Components.prototype.init = function () {
        this.initSlimScrollPlugin()
    },

    $.Components = new Components, $.Components.Constructor = Components

}(window.jQuery),

function($) {
    "use strict";
    $.Components.init();

    $(function () {
        if ($('.phone-users > .inbox-item').length == 0) return;
        let phoneNumber = $('.phone-users > .inbox-item.active').data('phone');
        selectPhone(phoneNumber);
    });

    var selectPhone = function (phone) {
        if (!phone) {
            phone = $('.phone-users > .inbox-item:first-child').data('phone');
        }

        $.post('/sms/initializeSms', { phoneNumber: phone }, function (data) {
            $('.conversation-list').data('from', phone)
            $('.conversation-list').html(data)
        }, 'html');
    }    

    $('.phone-users > .inbox-item').click(function () {
        $('.phone-users > .inbox-item').each(function () {
            $(this).removeClass('active')
        })
        $(this).addClass('active')
        let phone = $(this).data('phone')

        selectPhone(phone)
    });

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

    var clickAI = function () {
        let phone = $('.phone-users > .inbox-item.active').data('phone')
        if (!phone) return;

		$('.order-info').addClass('d-none');
		$('#spinner').removeClass('d-none')
        $.post('/sms/parseAI', { phone: phone }, function (resp) {
            if (resp.status == 4) { //once correct sms then render order information
                let orderInfo = resp.data.order;
                let orderNo = resp.data.orderId;
                let orderDetail = resp.data.orderDetail
                if (orderInfo) {
                    let _orderDetail = JSON.parse(orderDetail)
                    $('.order-info').removeClass('d-none');
                    renderOrderInfo(orderInfo, _orderDetail)
                }
            } else if (resp.status == -1) { //once inccorect sms then show alert
                Toast.fire({
                    icon: "error",
                    title: resp.data.rephase.msg
                });
			} 
			$('#spinner').addClass('d-none')
        }, 'json');

    }

    $('.btn-response').on('click', function () {
        let sms = $('.text-sms').val();
        let phone = $('.phone-users > .inbox-item.active').data('phone')
        if (!phone) return;

        $('#spinner').removeClass('d-none')
        if (sms) {  //send sms to customer
            $.post('/sms/sendSms', { sms: sms, phone: phone}, function (resp) {
                //rerender
                $('.conversation-list').data('from', phone)
                $('.conversation-list').html(resp)

				$('.text-sms').val('')
                $('#spinner').addClass('d-none')
            }, 'html')            
        } else {    //parse mail and make response.
            $.post('/sms/response', { phone: phone}, function (resp) {
                if (resp.status == 1) {
                    try {
                        let objRephase = resp.data.rephase;

                        let msg = "";
                        if (typeof objRephase == 'object' && objRephase != null) {
                            msg = objRephase.msg;
                        } else if (typeof objRephase == 'string') {
                            let _objRephase = JSON.parse(objRephase);
                            msg = _objRephase.msg;
                        }
                        $('.text-sms').val(msg);
                    } catch (error) {
                        console.error("error: " + error.message)
                    } finally {
                        $('#spinner').addClass('d-none')
                    }
                }
                //$('#spinner').addClass('d-none')
            }, 'json');
        }
    });
    $('.btn-process').on('click', function () {
        clickAI()
    })

	var renderOrderInfo = function(orderInfo, orderInfoDetail) {
		
		$('.order-info .card-title').html(`<h4>${orderInfo.or_name}</h4>`)
		
		if (orderInfo.or_status == 2) {
			$('.order-info .btn-order').addClass('d-none')
		} else {
			$('.order-info .btn-order').removeClass('d-none')
			if (orderInfo.or_fulfill_status != 0) {
				$('.order-info .btn-order .btn-cancel').addClass('d-none')
			} else {
				$('.order-info .btn-order').data('idx', orderInfo.or_id)
			}
		}

		//for mobile
		let _orderInfo = orderInfoDetail.order;
		let dateObject = new Date(_orderInfo.created_at);
		let formattedDate = dateObject.toLocaleString('en-US', {
			year: 'numeric', // e.g., '2024'
			month: 'long',   // e.g., 'December'
			day: 'numeric',  // e.g., '2'
			hour: 'numeric', // e.g., '12'
			minute: '2-digit', // e.g., '17'
			second: '2-digit', // e.g., '00'
			hour12: true      // Use 12-hour format with AM/PM
		});
		let orderStatus = "", fulfillStatus = "";
		let _html = `<h4 class="header-title">${_orderInfo.name}
						<span class="badge badge-pill badge-secondary ml-2">${_orderInfo.financial_status}</span>
						<span class="badge badge-pill ml-2 ${_orderInfo.fulfillment_status == null ? "badge-warning" : "badge-success"} ">${_orderInfo.fulfillment_status == null ? "Unfulfilled" : _orderInfo.fulfillment_status}</span>
					</h4>
					<p class="sub-header">
						${formattedDate}
					</p>
					<section class="order-detail">
						<div class="row1_col1">`
		if (_orderInfo.cancel_reason != null && _orderInfo.cancelled_at != null) {
			orderStatus = "cancelled"
			_html = `<h4 class="header-title">${_orderInfo.name}
						<span class="badge badge-pill badge-warning ml-2">Cancelled</span>
						<span class="badge badge-pill badge-secondary ml-2">${_orderInfo.financial_status}</span>
						<span class="badge badge-pill ml-2 ${_orderInfo.fulfillment_status == null ? "badge-warning" : "badge-success"} ">${_orderInfo.fulfillment_status == null ? "Unfulfilled" : _orderInfo.fulfillment_status}</span>
					</h4>
					<p class="sub-header">
						${formattedDate}
					</p>
					<section class="order-detail">
						<div class="row1_col1">`
		}
		if (_orderInfo.fulfillment_status == null)
			_html += `<ul class="sortable-list taskList list-unstyled ui-sortable" id="upcoming">
						<li class="ui-sortable-handle">
							<span class="float-right">${_orderInfo.shipping_lines[0].code}</span>
							<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Delivery method</a></h5>                                                
						</li>`
		else {
			fulfillStatus = "fulfilled"
			let fulfillDate = new Date(_orderInfo.fulfillments[0].updated_at);
			let formattedFulfill = fulfillDate.toLocaleString('en-US', {
				year: 'numeric', // e.g., '2024'
				month: 'long',   // e.g., 'December'
				day: 'numeric',  // e.g., '2'				
			});
			_html += `<ul class="sortable-list taskList list-unstyled ui-sortable" id="upcoming">
						<li class="ui-sortable-handle">
							<span class="float-right">${formattedFulfill}</span>
							<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Fulfilled</a></h5>       
							<div class="clearfix">
							<div class="row">
								<div class="col">
									<p class="mt-0"><a href="javascript: void(0);" class="text-dark">${_orderInfo.fulfillments[0].tracking_company}</a></p>    									
								</div>
								<div class="col-auto">
									<h6 class="float-right">${_orderInfo.fulfillments[0].tracking_number}</h6>                                            
								</div>
							</div>
						</li>`
		}
		let cancelDisable = false, refundDisable = false;
		if (fulfillStatus = "fulfilled") {
			cancelDisable = true;
		}
		if (orderStatus == "cancelled") {
			cancelDisable = true;
			refundDisable = true;
		}

		let itemCnt = 0, itemTaxPrice = 0, itemPrice = 0, _discount = 0;
		for (let i = 0; i < _orderInfo.line_items.length; i++) {
			let line_item = _orderInfo.line_items[i];
			itemCnt += line_item.quantity * 1
			let _price = line_item.price;
			itemPrice += line_item.price * line_item.quantity;
			if (line_item.pre_tax_price != null && line_item.pre_tax_price != undefined) {
				itemTaxPrice += line_item.pre_tax_price * line_item.quantity
				_price = line_item.pre_tax_price
			} else {
				itemTaxPrice = itemPrice
			}
			let _discount_alloc = line_item.discount_allocations
			if (_discount_alloc != undefined && _discount_alloc.length != 0)
				_discount += _discount_alloc[0].amount * 1;

			_html += `<li class="ui-sortable-handle">
						<div class="row">
							<div class="col">
								<h6 class="font-13 mt-2 mb-0">${line_item.name}</h6>
							</div>
							<div class="col-auto">
								<div class="text-right">
									<p class="font-13 mt-2 mb-0">$${_price * line_item.quantity}</p>
								</div>
							</div>
						</div>
						<p>SKU: ${line_item.sku}</p>
						<div class="clearfix"></div>
						<p>$${_price} × ${line_item.quantity}</p>                        
					</li>`
		}
		_html += `<li class="ui-sortable-handle paid">                        
						<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Subtotal</a></h5>                                                
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${itemCnt > 1 ? itemCnt + " items" : "1 item"}</p>
							</div>
							<div class="col-auto">
								<div class="text-right">
									<p class="font-13 mt-2 mb-0">$${itemPrice}</p>
								</div>
							</div>
						</div>`
		if (_orderInfo.fulfillment_status != null) {
			let code = "";
			if (_orderInfo.discount_applications[0].type == "discount_code") {
				code = _orderInfo.discount_applications[0].code;
			} else if (_orderInfo.discount_applications[0].type == "automatic") {
				code = _orderInfo.discount_applications[0].title;
			}
			_html += `<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Discound</a></h5>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${code}</p>
							</div>
							<div class="col-auto">
								<div class="text-right">
									<p class="font-13 mt-2 mb-0">-$${_discount}</p>
								</div>
							</div>
						</div>`
		}

		_html += `<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Shipping</a></h5>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${_orderInfo.shipping_lines[0].title}</p>
							</div>
							<div class="col-auto">
								<div class="text-right">
									<p class="font-13 mt-2 mb-0">$${_orderInfo.shipping_lines[0].price}</p>
								</div>
							</div>
						</div>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<h5 class="font-13 mt-2 mb-0">Total</h5>
							</div>
							<div class="col-auto">
								<div class="text-right">
									<p class="font-13 mt-2 mb-0">$${_orderInfo.total_price}</p>
								</div>
							</div>
						</div>
						<div class="row paid-row">
							<div class="col">
								<h5 class="font-13 mt-2 mb-0">Paid</h5>
							</div>
							<div class="col-auto">
								<div class="text-right">
									<p class="font-13 mt-2 mb-0">$${_orderInfo.current_total_price}</p>
								</div>
							</div>
						</div>
					</li>
				</ul>
			</div>
			<div class="row2_col2">
				<ul class="sortable-list taskList list-unstyled ui-sortable">
					<li class="ui-sortable-handle customer">
						<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Customer</a></h5>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${_orderInfo.customer.first_name}${_orderInfo.customer.last_name} </p>
								<p class="font-13 mt-2 mb-0 d-none">2 orders</p>
							</div>                            
						</div>
						<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Contact information</a></h5>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${_orderInfo.customer.email != null ? _orderInfo.customer.email : "no email address"}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.customer.phone != null ? _orderInfo.customer.phone : "no phone number"}</p>
							</div>
						</div>                        
						<div class="clearfix"></div>
						<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Shipping address</a></h5>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${_orderInfo.shipping_address.name}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.shipping_address.address1}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.shipping_address.city} ${_orderInfo.shipping_address.province_code} ${_orderInfo.shipping_address.zip}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.shipping_address.country}</p>															
							</div>
						</div>
						<h5 class="mt-0"><a href="javascript: void(0);" class="text-dark">Billing address</a></h5>
						<div class="clearfix"></div>
						<div class="row">
							<div class="col">
								<p class="font-13 mt-2 mb-0">${_orderInfo.billing_address.name}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.billing_address.address1}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.billing_address.city} ${_orderInfo.shipping_address.province_code} ${_orderInfo.shipping_address.zip}</p>
								<p class="font-13 mt-2 mb-0">${_orderInfo.billing_address.country}</p>															
							</div>
						</div>
					</li>
				</ul>  
				<div class="row">
					<div class="col">
						<button onclick="javascript:window.cancel(${_orderInfo.id});" class="btn btn-primary btn-block mt-3 waves-effect waves-light" ${cancelDisable ? "disabled" : ""}>Cancel</button>
					</div>
					<div class="col">
						<button onclick="javascript:window.refund(${_orderInfo.id});" class="btn btn-secondary btn-block mt-3 waves-effect waves-light" ${refundDisable ? "disabled" : ""}>Refund</button>
					</div>
				</div>
			</div>`
		$('#order_info').html(_html)
		$('.mobile-card-body').removeClass('d-none')
	}

	window.cancel = cancel
	window.refund = refund

	function cancel(orderId) {
		swalWithBootstrapButtons.fire({
			title: "Question",
			text: "Should you process the cancellation request?",
			icon: "question",
			showCancelButton: true,
			confirmButtonText: "Yes, I'll do it!",
			cancelButtonText: "No, I won't.!",
			reverseButtons: true
		}).then((result) => {
			if (result.isConfirmed) {
				const Toast = Swal.mixin({
					toast: true,
					position: "top-end",
					showConfirmButton: false,
					timer: 3000,
					timerProgressBar: true,
					didOpen: (toast) => {
						toast.onmouseenter = Swal.stopTimer;
						toast.onmouseleave = Swal.resumeTimer;
					},
					didClose: () => {
						let pageNo = $('input[name="pageNo"]').val();
						if (pageNo == undefined) pageNo = 0;
						renderMessage(pageNo);
					}
				});
				$.post('/home/requestShopify', { orderId: orderId, type: 2, em_idx: 0 }, function (resp) {
					if (resp.status == 1) {
						Toast.fire({
							icon: "success",
							title: "The cacellation request was successful."
						});
					} else {
						Toast.fire({
							icon: "error",
							title: "The cacellation request failed."
						});
					}
				}, 'json');
			}
		});
	}

	function refund(orderId) {
		swalWithBootstrapButtons.fire({
			title: "Question",
			text: "Should you process the refund request?",
			icon: "question",
			showCancelButton: true,
			confirmButtonText: "Yes, I'll do it!",
			cancelButtonText: "No, I won't.!",
			reverseButtons: true
		}).then((result) => {
			if (result.isConfirmed) {
				const Toast = Swal.mixin({
					toast: true,
					position: "top-end",
					showConfirmButton: false,
					timer: 3000,
					timerProgressBar: true,
					didOpen: (toast) => {
						toast.onmouseenter = Swal.stopTimer;
						toast.onmouseleave = Swal.resumeTimer;
					},
					didClose: () => {
						let pageNo = $('input[name="pageNo"]').val();
						if (pageNo == undefined) pageNo = 0;
						renderMessage(pageNo);
					}
				});
				$.post('/home/requestShopify', { orderId: orderId, type: 3, em_idx: 0 }, function (resp) {
					if (resp.status == 1) {
						Toast.fire({
							icon: "success",
							title: "The refund request was successful."
						});
					} else {
						Toast.fire({
							icon: "error",
							title: "The refund request failed."
						});
					}
				}, 'json');
			}
		});
	}
} (window.jQuery);