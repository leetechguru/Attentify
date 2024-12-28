(function ($) {
	const mapPaymentStatus = new Map([[0, "paid"], [1, "partially_refunded"], [2, "refunded"]]);
	const mapFulfiilmentStatus = new Map([[0, "Unfulfilled"], [1, "Fullfilled"]]);

	const mapDisp = new Map([[0, "To Do"], [1, "In Progress"], [2, "Low Priority"], [-1, "All"], [4, "Trash"], [5, "Spam"]])
	$('.btn-todo').on('click', function () {
		$('#messages_form [name="em_state"]').val(0);
		renderMessage(0);
	});
	$('.btn-inprogress').on('click', function () {
		$('#messages_form [name="em_state"]').val(1);
		renderMessage(0);
	});
	$('.btn-lowpriority').on('click', function () {
		$('#messages_form [name="em_state"]').val(2);
		renderMessage(0);
	});
	$('.btn-all').on('click', function () {
		$('#messages_form [name="em_state"]').val(-1);
		renderMessage(0);
	});
	$('.btn-trash').on('click', function () {
		$('#messages_form [name="em_state"]').val(4);
		renderMessage(0);
	});
	$('.btn-spam').on('click', function () {
		$('#messages_form [name="em_state"]').val(5);
		renderMessage(0);
	});
	$('.btn-asread').on('click', function () {
		let checkIds = getCheckedIds();
		let em_idx = $(this).data('idx');
		makeReadState(em_idx, 1);
	});
	$('.btn-asunread').on('click', function () {
		let em_idx = $(this).data('idx');
		makeReadState(em_idx, 0);
	});
	
	$('.btn-make-allread').on('click', function () {
		$.post('/home/makeallread', {}, function (resp) {
			if (resp.status == 1) {
				let pageNo = $('input[name="pageNo"]').val();

				if (pageNo == undefined) pageNo = 0;
				renderMessage(pageNo);
			}
		}, 'json');
	});

	$('.act-todo').on('click', function () {
		makeState(0);
	});

	$('.act-inprogress').on('click', function () {
		makeState(1);
	});

	$('.act-lowpriority').on('click', function () {
		makeState(2);
	});

	$('.act-archived').on('click', function () {
		makeState(3);
	});

	$('.act-trash').on('click', function () {
		makeState(4);
	});

	$('.act-spam').on('click', function () {
		makeState(5);
	});

	
	$(document).ready(function () {
		let pageNo = $('input[name="pageNo"]').val();
		
		if (pageNo == undefined) pageNo = 0;
		renderMessage(pageNo);
	});

	const swalWithBootstrapButtons = Swal.mixin({
		customClass: {
			confirmButton: "btn btn-success",
			cancelButton: "btn btn-danger"
		},
		buttonsStyling: false
	});

	var initializeScript = function () {
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
		//pagenation
		$('button.btn-prev').click(function () {
			let pageNo = $('input[name="pageNo"]').val() * 1 - 1;
			renderMessage(pageNo);
		});

		$('button.btn-first').click(function () {
			renderMessage(1);
		});

		$('button.btn-next').click(function () {
			let pageNo = $('input[name="pageNo"]').val() * 1 + 1;
			renderMessage(pageNo);
		});

		$('button.btn-last').on('click', function () {
			let pageNo = $('input[name="pageAllCnt"]').val();
			renderMessage(pageNo);
		});
		//////////
		//detail
		$("div.message-details").on('click', function () {
			let idx = $(this).data('idx');
			if (idx == 0) return;
			detailMessage(idx)
			//let em_state = $('#messages_form [name="em_state"]').val();
			//document.location.href = "/home/details?em_idx=" + idx + "&em_state=" + em_state;

		});
		$('.btn-prev-go').on('click', function () {
			let pageNo = $('input[name="pageNo"]').val();

			if (pageNo == undefined) pageNo = 0;
			renderMessage(pageNo);
		})
		let objQuill = null;
		if ($('.quill-editor-default').length) {
			objQuill = new Quill('.quill-editor-default', {
				theme: 'snow'
			});
		}
		

		$('.btn-respond').on('click', function () {	//When click "Process" button
			let em_idx = $(this).data('idx');
			$(this).prop('disabled', true);
			$(this).html('<span class="spinner-border spinner-border-sm" role = "status" aria-hidden="true" > </span> Parsing...');
			$('#spinner').removeClass('d-none')
			$.post('/home/rephase', { em_idx: em_idx }, function (resp) {
				if (resp.status == 1) {
					$('.respond-section').removeClass('d-none');
					//$('.btn-respond').addClass('d-none');
					let strRephase = resp.data.rephase;
					let objRephase = JSON.parse(strRephase);
					objQuill.setText(objRephase.msg);
					//} else if (resp.status == 2) {
					//	swalWithBootstrapButtons.fire({
					//		title: "Should you process the cancellation request?",
					//		text: "The customer's request to cancel the order with ID '" + resp.data.orderId + "'—would you process this request?",
					//		icon: "question",
					//		showCancelButton: true,
					//		confirmButtonText: "Yes, I'll do it!",
					//		cancelButtonText: "No, I won't.!",
					//		reverseButtons: true
					//	}).then((result) => {
					//		if (result.isConfirmed) {
					//			const Toast = Swal.mixin({
					//				toast: true,
					//				position: "top-end",
					//				showConfirmButton: false,
					//				timer: 3000,
					//				timerProgressBar: true,
					//				didOpen: (toast) => {
					//					toast.onmouseenter = Swal.stopTimer;
					//					toast.onmouseleave = Swal.resumeTimer;
					//				}
					//			});
					//			$.post('/home/requestShopify', { orderId: resp.data.orderId, type: 2, em_idx: resp.data.em_idx }, function (resp) {
					//				if (resp.status == 1) {
					//					Toast.fire({
					//						icon: "success",
					//						title: "Cancellation request will be accepted"
					//					});
					//				} else {
					//					Toast.fire({
					//						icon: "error",
					//						title: "Cancellation request will be failed"
					//					});
					//				}
					//			}, 'json');
					//		}
					//	});
					//} else if (resp.status == 3) {
					//	swalWithBootstrapButtons.fire({
					//		title: "Should you process the refund request?",
					//		text: "The customer's request to refund the order with ID " + resp.data.orderId + "—would you process this request?",
					//		icon: "question",
					//		showCancelButton: true,
					//		confirmButtonText: "Yes, I'll do it!",
					//		cancelButtonText: "No, I'll not!",
					//		reverseButtons: true
					//	}).then((result) => {
					//		if (result.isConfirmed) {
					//			$.post('/home/requestShopify', { orderId: resp.data.orderId, type: 3, em_idx: resp.data.em_idx }, function (resp) {
					//				if (resp.status == 1) {
					//					swalWithBootstrapButtons.fire({
					//						title: "Success!",
					//						text: "Refund request will be accepted.",
					//						icon: "success"
					//					});
					//				}
					//			}, 'json');
					//		}
					//	});
				} else if (resp.status == 4) {
					let orderInfo = resp.data.order;
					let orderNo = resp.data.orderId;
					let em_idx = resp.data.em_idx;
					let orderDetail = resp.data.orderDetail
					if (orderInfo) {
						let _orderDetail = JSON.parse(orderDetail)
						$('.order-info').removeClass('d-none');
						renderOrderInfo(orderInfo, _orderDetail, em_idx)
					}
				} else if (resp.status == -1) {
					Toast.fire({
						icon: "error",
						title: resp.data.rephase.msg
					});
				}
				$('.btn-respond').html('Process');
				$('#spinner').addClass('d-none')
			}, 'json');
		});

		$('.btn-response').on('click', function () {	//When click "Response" button
			$('#spinner').removeClass('d-none')
			let em_idx = $(this).data('idx');
			$.post('/home/response', { em_idx: em_idx }, function (resp) {
				if (resp.status == 1) {
					$('.respond-section').removeClass('d-none');					
					try {
						let objRephase = resp.data.rephase;
						
						let msg = "";
						if (typeof objRephase == 'object' && objRephase != null) {
							msg = objRephase.msg;
						} else if (typeof objRephase == 'string') {
							let _objRephase = JSON.parse(objRephase);
							msg = _objRephase.msg;
						}
						objQuill.setText(msg);
					} catch(error) {
						console.error("error: " + error.message)
					} finally {
						$('#spinner').addClass('d-none')
					}
				}
				$('#spinner').addClass('d-none')
			}, 'json');
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
		
		$('.btn-order .btn-cancel').on('click', function () {
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

			let orderId = $(this).parent().data('idx');
			let emIdx = $(this).parent().data('emidx');
			if (!orderId || !emIdx) return;
			$('.btn-order .btn-cancel').prop('disabled', true);
			$('#spinner').removeClass('d-none')
			$.post('/home/requestShopify', { orderId: orderId, type: 2, em_idx: emIdx }, function (resp) {
				if (resp.status == 1) {
					_Toast.fire({
						icon: "success",
						title: "Refund successflly!"
					});
					if (resp.order) {
						let orderInfo = resp.order;
						let orderInfoDetail = JSON.parse(resp.orderDetail)
						renderOrderInfo(orderInfo, orderInfoDetail, em_idx)
					}
				}
				$('.btn-order .btn-cancel').prop('disabled', false);
				$('#spinner').addClass('d-none')
			}, 'json');
		});
		//////////
	}
	var detailMessage = function (em_idx) {
		$('#spinner').removeClass('d-none')
		$.post('/home/MessageByIdx', { em_idx: em_idx }, function (data) {
			$('.email-list').html(data);

			initializeScript();
			let em_state = $('#messages_form [name="em_state"]').val()*1;
			mapDisp.forEach((value, key) => {
				if (key == em_state) {
					$(`.email-left-box [data-state="${key}"] > p`).html(`<b class='text-primary'>${value}</b>`);
				} else {
					$(`.email-left-box [data-state="${key}"] > p`).html(`<b>${value}</b>`)
				}
			});
			$('#spinner').addClass('d-none')
		}, 'html')
	}
	function renderMessage(pageNo) {
		let em_state = $('#messages_form [name="em_state"]').val();
		let pagePerCnt = $('input[name="pagePerCnt"]').val();
		if (pagePerCnt == undefined || pagePerCnt == 0) pagePerCnt = 20;
		$('#spinner').removeClass('d-none')
		$.post('/home/MessagesByPage', { pageNo: pageNo, pagePerCnt: pagePerCnt, em_state: em_state }, function (data) {
			$('.email-list').html(data);

			initializeScript();
			em_state = em_state * 1;
			mapDisp.forEach((value, key) => {
				if (key == em_state) {
					$(`.email-left-box [data-state="${key}"] > p`).html(`<b class='text-primary'>${value}</b>`);
				} else {
					$(`.email-left-box [data-state="${key}"] > p`).html(`<b>${value}</b>`)
				}
			});
			$('#spinner').addClass('d-none')
		}, 'html');
	}

	function makeReadState(idx, make_read) {
		$.post('/home/makeread', { idx: idx, make_read: make_read }, function (resp) {
			if (resp.status == 1) {
				let pageNo = $('input[name="pageNo"]').val();
				renderMessage(pageNo);
			}
		}, 'json');
	}

	function makeState(em_state) {
		let checkedIds = getCheckedIds();
		if (checkedIds == [] || checkedIds.length == 0) {
			let _i = $('#em_idx').val();
			if (_i) {
				checkedIds.push(_i);
			}
			if(checkedIds == [] || checkedIds.length == 0)
				return;
		}

		$.post('/home/makestate', { idx: checkedIds, em_state: em_state }, function (resp) {
			if (resp.status == 1) {
				let pageNo = $('input[name="pageNo"]').val();
				renderMessage(pageNo);
			}
		}, 'json');
	}

	function getCheckedIds() {
		let checkedIds = [];
		$('.email-list .message input.chk2').each(function () {
			if ($(this).prop("checked")) {
				let id = $(this).data('idx')
				checkedIds.push(id)
			}
		})
		return checkedIds;
	}	

	function renderOrderInfo(orderInfo, orderInfoDetail, em_idx) {
		//$('.order-info').removeClass('d-none');
		$('.order-info .card-title').html(`<h4>${orderInfo.or_name}</h4>`)
		let html = `<tr class=" ${orderInfo.or_fulfill_status == 1 ? "fulfilled" : ""} ${orderInfo.or_status == 2 ? "cancelled" : ""}">
                                    <th>${orderInfo.or_name}</th>
                                    <td>${orderInfo.or_date}</td>
                                    <td>${orderInfo.or_customer}</td>
                                    <td>online store</td>
                                    <td>$${orderInfo.or_total.toFixed(2)}</td>
                                    <td class="color-primary">${mapPaymentStatus.get(orderInfo.or_payment_status)}</td>
                                    <td class="color-primary"><span class="badge ${orderInfo.or_fulfill_status == 0 ? "badge-warning" : "badge-light"} px-2">${mapFulfiilmentStatus.get(orderInfo.or_fulfill_status)}</span></td>
                                    <td>${orderInfo.or_itemCnt} ${orderInfo.or_itemCnt > 1 ? "items" : "item"}</td>
                                    <td class="color-primary"></td>
                                </tr>`
		//$('.order-info .order-table tbody').html(html)
		if (orderInfo.or_status == 2) {
			$('.order-info .btn-order').addClass('d-none')
		} else {
			$('.order-info .btn-order').removeClass('d-none')
			if (orderInfo.or_fulfill_status != 0) {
				$('.order-info .btn-order .btn-cancel').addClass('d-none')
			} else {
				$('.order-info .btn-order').data('idx', orderInfo.or_id)
				$('.order-info .btn-order').data('emidx', em_idx)
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
				_discount += _discount_alloc[0].amount*1;

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
		_html +=	`<li class="ui-sortable-handle paid">                        
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
						<button onclick="javascript:window.cancel(${_orderInfo.id}, ${em_idx});" class="btn btn-primary btn-block mt-3 waves-effect waves-light" ${cancelDisable ? "disabled": ""}>Cancel</button>
					</div>
					<div class="col">
						<button onclick="javascript:window.refund(${_orderInfo.id}, ${em_idx});" class="btn btn-secondary btn-block mt-3 waves-effect waves-light" ${refundDisable ? "disabled": ""}>Refund</button>
					</div>
				</div>
			</div>`
		$('#order_info').html(_html)
		$('.mobile-card-body').removeClass('d-none')
	}
	window.cancel = cancel
	window.refund = refund

	function cancel(orderId, em_idx) {
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
				$.post('/home/requestShopify', { orderId: orderId, type: 2, em_idx: em_idx }, function (resp) {
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

	function refund(orderId, em_idx) {
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
				$.post('/home/requestShopify', { orderId: orderId, type: 3, em_idx: em_idx }, function (resp) {
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
})(window.jQuery);

