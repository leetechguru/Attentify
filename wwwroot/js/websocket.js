const connection = new signalR.HubConnectionBuilder()
    .withUrl("/ws")  // The URL for the SignalR Hub
    .build();

connection.start().then(function () {
    //console.log("Connected to SignalR Hub!");
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("ReceiveMessage", function (user, message) {
    //console.log(message);

    let objCount = JSON.parse(message);
    if (!objCount) return
    if (objCount.type == "mail") {
        let p = objCount.MailInfo;
        if (!p) return

        if (p.nCntWhole != "0") {
            $('#sidebarMenu .message-badge').show();
            $('#sidebarMenu .message-badge').html(p.nCntWhole)
            $('.cnt-all').html(p.nCntWhole)
            $('.email-left-box .btn-all span').html(p.nCntWhole)
        } else {
            $('#sidebarMenu .message-badge').hide();
        }

        if (p.nCntToDo != "0") {
            $('.cnt-todo').html(p.nCntToDo)
            $('.email-left-box .btn-todo span').html(p.nCntToDo)
        } else {
            $('.cnt-todo').html("")
            $('.email-left-box .btn-todo span').html("")
        }

        if (p.nCntInProgress != "0") {
            $('.cnt-inprogress').html(p.nCntInProgress)
            $('.email-left-box .btn-inprogress span').html(p.nCntInProgress)
        } else {
            $('.cnt-inprogress').html("")
            $('.email-left-box .btn-inprogress span').html("")
        }

        if (p.nCntLowPriority != "0") {
            $('.cnt-lowpriority').html(p.nCntLowPriority)
            $('.email-left-box .btn-lowpriority span').html(p.nCntLowPriority)
        } else {
            $('.cnt-lowpriority').html("")
            $('.email-left-box .btn-lowpriority span').html("")
        }

        if (p.nCntUnread != "0") {
            $('.new-mail-cnt').html(p.nCntUnread)
        } else {
            $('.new-mail-cnt').html("")
        }
    } else if (objCount.type == "store") {
        let stores = objCount.orders;
        if (!stores) return;
        let nAllReq = 0;
        let header = '', left = '';
        for (let i = 0; i < stores.length; i++) {
            let store = stores[i];
            if (!store) continue;
            nAllReq += store.count * 1;
            header += `<li>
                            <a href="/shopify/orders?store=${store.store}">
                                <span class="mr-3 avatar-icon bg-success-lighten-2"><i class="icon-present"></i></span>
                                <div class="notification-content">
                                    <h6 class="notification-heading">New Orders ${store.store}</h6>
                                    <span class="notification-text">${store.count}</span>
                                </div>
                            </a>
                        </li>`
            left += `<li>
                        <a href="/shopify/orders?store=${store.store}" style="display: flex; align-items: center;">
                            <span class="label label-pill custom-badge">${store.count != '0' ? store.count : ''}</span>
                            <span class="caption">${store.store}</span>
                        </a>
                    </li>`
        }
        $('.notify-header .order-notify').html(header);
        $('#menu .order-menu').html(left);
        if (nAllReq != 0)
            $('.order-all-cnt').html(nAllReq);
    } else if (objCount.type == "sms") {
        let sms = objCount.sms;
        let smsCnt = sms.length
        if (smsCnt * 1 > 0) {
            $('.new-sms-cnt').html(smsCnt)
        }else{
            $('.new-sms-cnt').html("")
        }
        let html = "<ul>";
        for (let key in sms) {
            html += `<li class="notification-unread">
                        <a href="/sms/index?phone=${sms[key].From}">
                            <img class="float-left mr-3 avatar-img" src="/images/user/form-user.png" alt="">
                            <div class="notification-content">
                                <div class="notification-heading">${sms[key].From}</div>
                                <div class="notification-timestamp">${sms[key].DtString}</div>
                                <div class="notification-text">${truncateString(sms[key].Body, 20)}</div>
                            </div>
                        </a>
                    </li>`;
        }
        html += '</ul>'
        $('.sms-unread-content').html(html);
    }
});

function truncateString(str, length) {
    if (str.length > length) {
        return str.slice(0, length) + '...';
    }
    return str;
}
//ws.onmessage = (event) => {

//    console.log(event.data);
//    if (!event.data) return;
//    let objCount = JSON.parse(event.data);
//    if (!objCount) return
//    if (objCount.type == "mail") {
//        let p = objCount.MailInfo;
//        if (!p) return
//        $('.new-mail-cnt').html(p.nCntUnread)

//        if (p.nCntWhole != "0") {
//            $('#sidebarMenu .message-badge').show();
//            $('#sidebarMenu .message-badge').html(p.nCntWhole)
//            $('.cnt-all').html(p.nCntWhole)
//            $('.email-left-box .btn-all span').html(p.nCntWhole)
//        } else {
//            $('#sidebarMenu .message-badge').hide();
//        }

//        if (p.nCntToDo != "0") {
//            $('.cnt-todo').html(p.nCntToDo)
//            $('.email-left-box .btn-todo span').html(p.nCntToDo)
//        }

//        if (p.nCntInProgress != "0") {
//            $('.cnt-inprogress').html(p.nCntInProgress)
//            $('.email-left-box .btn-inprogress span').html(p.nCntInProgress)
//        }

//        if (p.nCntLowPriority != "0") {
//            $('.cnt-lowpriority').html(p.nCntLowPriority)            
//            $('.email-left-box .btn-lowpriority span').html(p.nCntLowPriority)
//        }
//    } else if (objCount.type == "") {

//    }
    
//};

//ws.onclose = () => {
//    console.log("Disconnected from WebSocket");
//};

////function sendMessage() {
////    const messageInput = document.getElementById("messageInput");
////    const message = messageInput.value;
////    ws.send(message);
////    messageInput.value = "";
////}