(function ($) {
	
	$(document).ready(function () {
		let pageNo = $('input[name="pageNo"]').val();

		if (pageNo == undefined) pageNo = 1;
		renderOrder(pageNo);
	});

	function initializeScirpt() {
		//pagenation
		$('button.btn-prev').click(function () {
			let pageNo = $('input[name="pageNo"]').val() * 1 - 1;
			renderOrder(pageNo);
		});

		$('button.btn-first').click(function () {
			renderOrder(1);
		});

		$('button.btn-next').click(function () {
			let pageNo = $('input[name="pageNo"]').val() * 1 + 1;
			renderOrder(pageNo);
		});

		$('button.btn-last').on('click', function () {
			let pageNo = $('input[name="pageAllCnt"]').val();
			renderOrder(pageNo);
		});
		//
	}
	function renderOrder(pageNo) {
		let store = $('input[name="store"]').val();

		$.post('/shopify/order', { nPageNo: pageNo, store: store}, function (data) {
			$('.order-table-wrapper').html(data);

			initializeScirpt();
			
		}, 'html');
	}

	$('.refresh-order').on('click', function () {
		let store = $(this).data('store')
		$('#spinner').removeClass('d-none')
		$.post('/shopify/refreshOrder', { strStore: store }, function (data) {
			$('.order-table-wrapper').html(data);

			initializeScirpt();
			$('#spinner').addClass('d-none')
		}, 'html')
	})
})(window.jQuery);