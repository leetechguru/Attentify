var attentify = {
    conversation: [],
    getConversation: function () {
        let strConversation = this.getCookies("conversation")
        if (strConversation)
            this.conversation = JSON.parse(strConversation)
        else
            this.conversation = [];
    },
    setCookie: function (name, value, days) {
        const date = new Date();
        if (!days) days = 30
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = `expires=${date.toUTCString()}`;
        let conversation = `converstaion=${JSON.stringify(this.conversation)}`
        document.cookie = `${name}=${value}; ${expires};${conversation}; Secure; SameSite=None;path=/`;
    },
    setConfig: function (dark) {
        this.setCookie("config", dark)
    },
    setConversation: function () {
        this.setCookie("conversation", JSON.stringify(this.conversation))
    },
    getCookies: function (key) {
        const cookies = document.cookie.split("; ");
        const cookieObject = {};
        cookies.forEach(cookie => {
            const [key, value] = cookie.split("=");
            cookieObject[key] = decodeURIComponent(value);
        });
        return cookieObject[key];
    },
    update: function () {
        let config = attentify.getCookies('config')
        if (config) {
            $('body').removeClass('dark light')
            $('body').addClass(config)
        }
        attentify.getConversation();
    },
    addConversation: function (id, type, email) {
        const objConv = {
            type: type,
            id: id,
            email: email
        };
        let nIdx = this.conversation.findIndex((obj) => obj.email == objConv.email);
        //let nIdx = this.conversation.findIndex((obj) => obj.id == objConv.id);
        if (nIdx == -1) {
            this.conversation.push(objConv)
        } else {
            this.conversation[nIdx].type = objConv.type;
            this.conversation[nIdx].email = objConv.email;
            this.conversation[nIdx].id = objConv.id;
        }

        this.setConversation()
    },
    removeConversation: function (id) {
        let nIdx = this.conversation.findIndex((obj) => obj.id == id);
        if (nIdx == -1) {
            return;
        } else {
            this.conversation.splice(nIdx, 1);
        }
        this.setConversation()
    },
    getConverstaionEmail: function (id) {
        let nIdx = this.conversation.findIndex((obj) => obj.id == id);
        if (nIdx == -1) {
            return "";
        } else {
            return this.conversation[nIdx].email;
        }
        return ""
    },
    initPopup: function () {
        const dark = this.getCookies('config')
        $('.dark_mode_toggle_menu_item input[type="checkbox"]').prop('checked', dark == 'dark')
    },
    initPerformanceData: function (p) {
        if (!p) return
        let emailCnt = $('#personalInboxOverviewController .total_conversations > span:first-child').html() * 1;
        let smsCnt = $('#smsOverview .total_conversations > span:first-child').html() * 1;

        if (p.nCntOnTime) {
            let onHappy = $('#performance_data .happy > span:first-child').html();
            onHappy = onHappy * 1 + p.nCntOnTime * 1;
            $('#performance_data .happy > span:first-child').html(onHappy);

            let ontimeTag = $('#performance_data .ontime > span:first-child');
            
            let timePer = onHappy / (emailCnt + smsCnt);
            if (timePer != Number.isNaN) {
                timePer = Math.round(timePer * 100)
                ontimeTag.html(timePer + "%")
                $('.service_performance .day_percentage > span:last-child').html(timePer + "%")
            } else {
                ontimeTag.html("")
            }
        }
        if (p.nCntDanger) {
            let dangerTag = $('#performance_data .neutral > span:first-child');
            let onDanger = dangerTag.html();
            onDanger = onDanger * 1 + p.nCntDanger * 1;
            dangerTag.html(onDanger);
        }
        if (p.nCntLate) {
            let sadTag = $('#performance_data .sad > span:first-child');
            let onSad = sadTag.html();
            onSad = onSad * 1 + p.nCntLate * 1;
            sadTag.html(onSad);
        }
        if (p.nCntArchived) {
            let archivedTag = $('#performance_data .archived > span:first-child');
            let onArchived = archivedTag.html();
            onArchived = onArchived * 1 + p.nCntArchived * 1;
            archivedTag.html(onArchived);

            let retentionPer = (emailCnt + smsCnt - onArchived) / (emailCnt + smsCnt);
            if (retentionPer != Number.isNaN) {
                retentionPer = Math.round(retentionPer * 100)
                $('#performance_data .retention > span.percentage').html(retentionPer + "%");
            }
        }
        if (p.nCntReply) {
            let replyTag = $('#performance_data .replied > span:first-child');
            let onReply = replyTag.html();
            onReply = onReply * 1 + p.nCntReply * 1;
            replyTag.html(onReply);

            let replyPer = onReply / (emailCnt + smsCnt);
            if (replyPer != Number.isNaN) {
                replyPer = Math.round(replyPer * 100)
                $('.service_performance .happy_customers > span:last-child').html(replyPer + "%")
            }
        }
    },
    init: function () {
        this.update();
        //this.initConversation();
        this.initHeadeeMenu();
        this.initPopup();

        $('.dashboard_menu_item > .mode_popup.menu_item').on('click', function () {
            $('#popup_menu > .dashboard_menu_popup').removeClass('d-none');
        })

        $('.dark_mode_toggle_menu_item').on('click', function () {
            const checkbox = $(this).find('input[type="checkbox"]');            
            checkbox.prop('checked', !checkbox.prop('checked'));
            
            if (checkbox.prop('checked')) {
                attentify.setConfig("dark")
            } else {
                attentify.setConfig("light")
            }
            attentify.update();
        })

        $(document).on('click', function (event) {
            if (!$(event.target).closest('#popup_menu, .dashboard_menu_item > .mode_popup.menu_item').length) {
                $('#popup_menu > .dashboard_menu_popup').addClass('d-none');
            }
        });

        $(window).on('resize', function () {
            const totalHeight = $(this).height() - $('.dashboard_header.user').height() - $('.ui-tabs-nav.ui-helper-reset.ui-helper-clearfix.ui-widget-header.ui-corner-all').height();
            $('.dashboard_content > .widgets').height(totalHeight)

            $('.inbox_scroll').height(totalHeight - 90);
        })

        $('#personalInboxOverviewController .expand-div').on('click', function () {
            const icon = $(this).children('span:first-child').attr('data-icon')
            if (icon == 'V') {
                $(this).children('span:first-child').attr('data-icon', '/')
                $(this).children('span:nth-child(2)').html('Collapse')
            } else {
                $(this).children('span:first-child').attr('data-icon', 'V')
                $(this).children('span:nth-child(2)').html('Expand')
            }
            $('#personalInboxOverviewController .webstores').toggleClass('d-none')
        })

        $('#smsOverview .expand-div').on('click', function () {
            const icon = $(this).children('span:first-child').attr('data-icon')
            if (icon == 'V') {
                $(this).children('span:first-child').attr('data-icon', '/')
                $(this).children('span:nth-child(2)').html('Collapse')
            } else {
                $(this).children('span:first-child').attr('data-icon', 'V')
                $(this).children('span:nth-child(2)').html('Expand')
            }
            $('#smsOverview .webstores').toggleClass('d-none')
        })
        ///////////////////////////////websocket init///////////////////////////////////
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/ws")
            .build();

        connection.start().then(function () {
        }).catch(function (err) {
            return console.error(err.toString());
        });

        connection.on("ReceiveMessage", function (user, message) {
            let objCount = JSON.parse(message);
            if (!objCount) return
            if (objCount.type == "mail") {
                let p = objCount.MailInfo;
                if (!p) return

                if (p.nCntWhole != "0") {
                    $('#personalInboxOverviewController .total_conversations > span:first-child').html(p.nCntWhole)
                } else {
                    $('#personalInboxOverviewController .total_conversations > span:first-child').html(0)
                }

                if (p.nCntUnread != "0") {
                    $('#personalInboxOverviewController .unread_badge').removeClass('d-none')
                    $('#personalInboxOverviewController .unread_badge').html(p.nCntUnread)
                } else {
                    $('#personalInboxOverviewController .unread_badge').addClass('d-none')
                }

                if (p.nCntRead != "0") {
                    $('#personalInboxOverviewController .my-webstores td:nth-child(5)').html(p.nCntRead)
                } else {
                    $('#personalInboxOverviewController .my-webstores td:nth-child(5)').html('-')
                }

                if (p.nCntLate != "0") {
                    $('#personalInboxOverviewController .too_late > span:first-child').html(p.nCntLate)
                    $('#personalInboxOverviewController .my-webstores td:nth-child(2)').html(p.nCntLate)
                } else {
                    $('#personalInboxOverviewController .too_late > span:first-child').html(0)
                    $('#personalInboxOverviewController .my-webstores td:nth-child(2)').html('-')
                }

                if (p.nCntDanger != "0") {
                    $('#personalInboxOverviewController .em_danger > span:first-child').html(p.nCntDanger)
                    $('#personalInboxOverviewController .my-webstores td:nth-child(3)').html(p.nCntDanger)
                } else {
                    $('#personalInboxOverviewController .em_danger > span:first-child').html(0)
                    $('#personalInboxOverviewController .my-webstores td:nth-child(3)').html('-')
                }

                if (p.nCntOnTime != "0") {
                    $('#personalInboxOverviewController .on_time > span:first-child').html(p.nCntOnTime)
                    $('#personalInboxOverviewController .my-webstores td:nth-child(4)').html(p.nCntOnTime)

                } else {
                    $('#personalInboxOverviewController .on_time > span:first-child').html(0)
                    $('#personalInboxOverviewController .my-webstores td:nth-child(4)').html(p.nCntOnTime)
                }

                if (p.nCntArchived != "0") {
                    $('#performance_data .archived > span.value').html(p.nCntArchived)
                } else {
                    $('#performance_data .archived > span.value').html(0)
                }

                attentify.initPerformanceData(p)
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
                let p = objCount.SMSInfo;
                if (!p) return

                if (p.nCntWhole != "0") {
                    $('#smsOverview .total_conversations > span:first-child').html(p.nCntWhole)
                } else {
                    $('#smsOverview .total_conversations > span:first-child').html(0)
                }

                if (p.nCntUnread != "0") {
                    $('#smsOverview .unread_badge').removeClass('d-none')
                    $('#smsOverview .unread_badge').html(p.nCntUnread)
                } else {
                    $('#smsOverview .unread_badge').addClass('d-none')
                }

                if (p.nCntRead != "0") {
                    $('#smsOverview .my-webstores td:nth-child(5)').html(p.nCntRead)
                } else {
                    $('#smsOverview .my-webstores td:nth-child(5)').html('-')
                }

                if (p.nCntLate != "0") {
                    $('#smsOverview .too_late > span:first-child').html(p.nCntLate)
                    $('#smsOverview .my-webstores td:nth-child(2)').html(p.nCntLate)
                } else {
                    $('#smsOverview .too_late > span:first-child').html(0)
                    $('#smsOverview .my-webstores td:nth-child(2)').html('-')
                }

                if (p.nCntDanger != "0") {
                    $('#smsOverview .em_danger > span:first-child').html(p.nCntDanger)
                    $('#smsOverview .my-webstores td:nth-child(3)').html(p.nCntDanger)
                } else {
                    $('#smsOverview .em_danger > span:first-child').html(0)
                    $('#smsOverview .my-webstores td:nth-child(3)').html('-')
                }

                if (p.nCntOnTime != "0") {
                    $('#smsOverview .on_time > span:first-child').html(p.nCntOnTime)
                    $('#smsOverview .my-webstores td:nth-child(4)').html(p.nCntOnTime)
                } else {
                    $('#smsOverview .on_time > span:first-child').html(0)
                    $('#smsOverview .my-webstores td:nth-child(4)').html(p.nCntOnTime)
                }
                attentify.initPerformanceData(p)
            } else if (objCount.type == "new_sms") {
                if (window.location.href.includes('home/dashboard')) return;
                if ($('input[name="Type"]').val() * 1 != 3) return;
                let strPhone = $('input[name="GMail"]').val();
                if (!strPhone) return;

                let p = objCount.SMSInfo;
                if (!p || !p.sm_from) return

                if (strPhone != p.sm_from) return;

                const id = $('input[name="id"]').val();
                const Type = $('input[name="Type"]').val();
                const GMail = $('input[name="GMail"]').val();


                $('#conversationList').append(`<li class="loading">
                                       <span>Loading...</span>
                                    </li>`)
                $.post('/home/GetMessageList', { id, Type, GMail }, function (data) {
                    try {
                        $('#conversationList').html(data);
                    } catch (e) {
                        console.log(e)
                    } finally {
                        $('#conversationList').find('.loading').remove();
                    }
                }, 'html');
            }
        });
    },
    initHeadeeMenu: function () {
        let html = `<li class="non-wizard dashboard_tab ui-state-default ui-corner-top ui-tabs-active ui-state-active" role="tab" tabindex="0" aria-controls="tabs-1" aria-labelledby="ui-id-1" aria-selected="true">
                        <a href="/home/dashboard">
                            <span class="dashboard_tab_icon" data-icon="&"></span>
                            <span class="tabtask">Dashboard</span>
                            <span id="dashboard_tab_unreaditems" style="display: none;" class="dashboard_tab_unreaditems"></span>
                        </a>
                    </li>`;
        for (var i in this.conversation) {
            let _o = this.conversation[i];
            html += `<li class="conv_tab ui-state-default ui-corner-top" aria-selected="false">
                        <a href="javascript:;" class="ui-tabs-anchor" data-id="${_o.id}" data-type="${_o.type}">
                            <span class="tabtask">${_o.email}</span>
                            <span class="conversation_tab_unreaditems" style="display: none;"></span>
                        </a>
                        <span class="close-tab close-container voice-connected">
                            <span class="ui-icon voice-tab-status-indication ui-icon-voice-connected" data-icon=""></span>
                            <span class="ui-icon ui-icon-close" data-icon="g"></span>
                        </span>
                    </li>`;
        }
        $('ul.ui-corner-all').html(html);
        $('ul.ui-corner-all > li').on('click', function () {
            const id = $(this).children('a').data('id');
            const type = $(this).children('a').data('type');
            location.href = `/home/conversation?id=${id}&Type=${type}`;
        })

        $('.close-tab').on('click', function (e) {
            const id = $(this).siblings('a').data('id')
            attentify.removeConversation(id)
            if ($(this).parent("li").hasClass('ui-tabs-active')) {
                location.href = "/home/dashboard";
            } else {
                attentify.initHeadeeMenu()
            }
            e.stopPropagation()
        });
    },
    loadEmailList: function () {
        let PageNo = $('input[name="PageNo"]').val();
        if (!PageNo) PageNo = 1;
        let Type = $('input[name="Type"]').val();
        if (!Type) Type = 1;

        if ($('#preview_emails').find('.loading').length > 0)return
        $('#preview_emails').append(`<li class="loading">
                                       <span>Loading...</span>
                                    </li>`)
        $.post('/home/MessagesByUserPerPage', { PageNo, Type }, function (data) {
            try {
                $('#preview_emails').append(data);
                $('.inbox_menu_column > .menu span').each(function () {
                    $(this).removeClass('active')
                    if ($(this).data('mode') == Type)
                        $(this).addClass('active')
                })
                let caption = 'inbox';
                if (Type == 2) {
                    caption = 'archive'
                } else if (Type == 3) {
                    caption = 'SMS'
                }
                $('#inbox span.caption').html(caption)                           
            } catch (e) {
                console.log(e);
            } finally {
                $('#preview_emails').find('.loading').remove();
                attentify._addConversation();
            }
        }, 'html');
    },

    _addConversation: function () {
        $('#preview_emails > .inbox_list_item > .inboxitem').on('click', function () {
            const em_id = $(this).data('id');
            let Type = $('input[name="Type"]').val();
            if (!Type) Type = 1;
            let email = $(this).data('email')
            attentify.addConversation(em_id, Type, email)
            location.href = `/home/conversation?id=${em_id}&Type=${Type}`;
        })

        $('#preview_emails > .inbox_list_item > .inboxitem input[type="checkbox"]').on('click', function (e) {
            e.stopPropagation()
        })
    },
    /////////////////////////////////dashboard init///////////////////////////////////
    initDashboard: function () {
        this.getConversation();
        $('.refresh_static_view').on('click', function () {
            $('input[name="PageNo"]').val(1);
            $('#preview_emails').html('')
            attentify.loadEmailList()
        })
        $('.inbox_content').on('scroll', function () {
            if ($(this).scrollTop() + $(this).innerHeight() >= $(this)[0].scrollHeight) {
                let PageNo = $('input[name="PageNo"]').val();
                let Type = $('input[name="Type"]').val();
                if (!Type) Type = 1
                if (!PageNo) PageNo = 1

                let _pageCnt = $('input[name="PageCnt"]').val();
                let arrPageCnt = JSON.parse(_pageCnt);
                let Cnt = arrPageCnt.find(a => a[0] == Type);
                if (PageNo * 1 >= Cnt[1] * 1) return;

                PageNo = PageNo * 1 + 1

                $('input[name="PageNo"]').val(PageNo);
                attentify.loadEmailList()
            }
        })
        $('.inbox_menu_column .menu.mode_inbox .items .menu_item').on('click', function () {
            $('.inbox_menu_column > .menu span').each(function () {
                $(this).removeClass('active')                
            })
            $(this).addClass('active')
            let Type = $(this).data('mode');
            let _orgType = $('input[name="Type"]').val();
            if (Type != _orgType) {
                $('#preview_emails').html('');
            }
            $('input[name="Type"]').val(Type);
            $('input[name="PageNo"]').val(0);
            attentify.loadEmailList();
        })

        $('#checkbox').on('click', function () {
            let checked = $(this).prop('checked')
            $('#preview_emails > li input[type="checkbox"]').prop('checked', checked)
        });

        $('ul.ui-corner-all > li:first-child').addClass('ui-tabs-active ')

        $('.action.archive').on('click', function () {
            let arrChecked = [];
            $('#preview_emails > .inbox_list_item > .inboxitem input[type="checkbox"]').each(function () {
                if ($(this).prop('checked'))
                arrChecked.push($(this).attr("id"))
            })
            let Type = $(this).data('mode');
            if (!Type || Type == 0) Type = 1;
            $.post('/home/MakeStateByGMails', { arrGmail: arrChecked, nType: Type }, function (resp) {
                if (resp.status == 1) {
                    $('#preview_emails').html("")
                    attentify.loadEmailList();
                }
            }, 'json')
        })
    },
    ///////////////////////////////////init conversation////////////////////////////
    initConversation: function () {
        this.getConversation();
        $('.panels span.panel_close').on('click', function () {
            $('.conversation_container').removeClass('conversation_container_with_panel')
            $('.panels .panels_inner.panel_width').css('display', 'none')
            $('.conversation.history.conversation_with_panel').css('right', 0);
            $('.panel_icons.panel_open_icon').css('display', 'block')
        })
        $('.panel_icons.panel_open_icon').on('click', function () {
            $('.conversation_container').addClass('conversation_container_with_panel')
            $('.panels .panels_inner.panel_width').css('display', 'block', 'important')
            $('.conversation.history.conversation_with_panel').css('right', 320);
            $('.panel_icons.panel_open_icon').css('display', 'none')
        })

        const id = $('input[name="id"]').val();
        const Type = $('input[name="Type"]').val();
        const GMail = $('input[name="GMail"]').val();

        //$('ul.ui-corner-all > li').each(function () {
        //    $(this).removeClass('aa')
        //    const _id = $(this).children('a').data('id');
        //    const _type = $(this).children('a').data('type');
        //    if (id == _id && _type == Type) {
        //        $(this).addClass('aa')
        //    }
        //})
        
        $('#conversationList').append(`<li class="loading">
                                       <span>Loading...</span>
                                    </li>`)
        $.post('/home/GetMessageList', { id, Type, GMail }, function (data) {
            try {
                $('#conversationList').html(data);

                $('#processAI').trigger('click');
            } catch (e) {
                console.log(e)
            } finally {
                $('#conversationList').find('.loading').remove();
            }
        }, 'html');

        $('ul.ui-corner-all > li').each(function () {
            $(this).removeClass('ui-tabs-active')
            if ($(this).children().attr('data-id') == id)
                $(this).addClass('ui-tabs-active')
        })

        $('.archive_button').on('click', function () {
            $.post('/home/makestatebygmail', { strGmail: GMail, em_state: 3 }, function (resp) {
                if (resp.status == 1) {
                    attentify.removeConversation(id);
                    location.href = "/home/dashboard";
                }
            }, 'json')
        });

        
        $('#SendExternalMessageButton').off('click');

        $('#SendExternalMessageButton').on('click', function (e) {
            e.preventDefault();
            $(this).prop('disabled', true);
            const msg = $('textarea[name="externalMessage"]').val();
            if (!msg) return;
            const strTo = attentify.getConverstaionEmail(id)

            $.post('/home/sendRequestEmail_', { strTo: strTo, strBody: msg, Type: Type }, function (resp) {
                if (resp.status == 1) {
                    location.href = `/home/conversation?id=${id}&Type=${Type}`;
                }
                $(this).prop('disabled', false);
            }, 'json')
        })

        $('#processAI').on('click', function (e) {
            let _id = $('input[name="id"]').val();
            let _Type = $('input[name="Type"]').val();
            e.preventDefault();
            let strTo = attentify.getConverstaionEmail(_id)
            if (!strTo) return;
            $('.order_info_widget .loading').removeClass('d-none')
            if (_Type == 3)
                strTo = _id

            $.post('/home/process', { strGmail: strTo, Type: _Type }, function (resp) {
                try {
                    if (resp.status == 4) {
                        const orderDetail = resp.data.orderDetail;
                        let _orderDetail = JSON.parse(orderDetail)
                        attentify.setOrderInfo(_orderDetail)

                    } else if (resp.status == -1) {
                        $('textarea[name="externalMessage"]').val(resp.data.rephase.msg);                        
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
                            icon: "error",
                            title: resp.data.rephase.msg
                        });
                    }
                } catch (e){
                    console.log(e)
                } finally {
                    $('.order_info_widget .loading').addClass('d-none')
                    $(this).prop('disabled', false);
                }
            }, 'json')
        })

        $('.bnt-prev').on('click', function () {
            window.history.back();
        })
    },

    setOrderInfo: function (orderInfoDetail) {
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
        let _html = `<div class="card-box"><h4 class="header-title">${_orderInfo.name}
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
            if (_orderInfo.discount_allocations && _orderInfo.discount_allocations.length > 0) {
                if (_orderInfo.discount_applications[0].type == "discount_code") {
                    code = _orderInfo.discount_applications[0].code;
                } else if (_orderInfo.discount_applications[0].type == "automatic") {
                    code = _orderInfo.discount_applications[0].title;
                }
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
				<div class="row button-wrapper">
					<div class="col mr-10">
						<button onclick="javascript:attentify.cancel(${_orderInfo.id});" class="bnt-cancel button" ${cancelDisable ? "disabled" : ""}>Cancel</button>
					</div>
					<div class="col">
						<button onclick="javascript:attentify.refund(${_orderInfo.id});" class="bnt-refund button" ${refundDisable ? "disabled" : ""}>Refund</button>
					</div>
				</div>
			</div></div>`
        $('#order_info').html(_html)
        //$('.mobile-card-body').removeClass('d-none')
    },
    cancel: function (orderId) {
        const swalWithBootstrapButtons = Swal.mixin({
            customClass: {
                confirmButton: "btn btn-success",
                cancelButton: "btn btn-danger"
            },
            buttonsStyling: false
        })
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
    },
    refund: function (orderId) {
        const swalWithBootstrapButtons = Swal.mixin({
            customClass: {
                confirmButton: "btn btn-success",
                cancelButton: "btn btn-danger"
            },
            buttonsStyling: false
        })
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
    },
    
};

(function ($) {
    window.cancel = attentify.cancel;
    window.refund = attentify.refund;
})(window.jQuery);