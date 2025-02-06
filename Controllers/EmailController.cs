using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Web;
using ShopifyService = GoogleLogin.Services.ShopifyService;
using GoogleLogin.Models;
using GoogleLogin.Services;
using WebSocketSharp;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Text.Json;
using System.Linq.Expressions;
using MailKit;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class EmailController : Controller
    {
        private readonly ILogger<EmailController>   _logger;
        private SignInManager<AppUser>              _signInManager;
        private UserManager<AppUser>                _userManager;
        private readonly EMailService               _emailService;
        private readonly EMailTokenService          _emailTokenService;
        private readonly ShopifyService             _shopifyService;
        private readonly SmsService                 _smsService;
        private readonly LLMService                 _llmService;
        private readonly IConfiguration             _configuration;
        private readonly IServiceScopeFactory       _serviceScopeFactory;
        private readonly string                     _phoneNumber;
        public static readonly string[]             Scopes = { "email", "profile", "https://www.googleapis.com/auth/gmail.modify" };
        private const int           nCntPerPage = 20;

        public EmailController(
            SignInManager<AppUser>  signinMgr, 
            IServiceScopeFactory    serviceScopeFactory,
            UserManager<AppUser>    userMgr, 
            EMailService            service, 
            EMailTokenService       emailTokenService,
            ShopifyService          shopifyService,
            SmsService              smsService, 
            ILogger<EmailController> logger, 
            IConfiguration          configuration, 
            LLMService              llmService)
        {
            _signInManager          = signinMgr;
            _serviceScopeFactory    = serviceScopeFactory;
            _userManager            = userMgr;
            _emailService           = service;
            _emailTokenService      = emailTokenService;
            _shopifyService         = shopifyService;
            _logger                 = logger;
            _smsService             = smsService;
            _configuration          = configuration;
            _phoneNumber            = _configuration["Twilio:PhoneNumber"] ?? "";
            _llmService             = llmService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
#if DEBUG
                user = new AppUser();
                user.Email = "sherman@zahavas.com";
#else
                return Redirect("/account/Login");
#endif
            }

            ViewBag.mailAccountList = _emailTokenService.GetMailAccountList(_userManager.GetUserId(HttpContext.User) ?? "");
            return View();
        }

        [HttpGet("email/detail")]
        public async Task<IActionResult> EmailDetail(string id)
        {
            if (id == string.Empty) return BadRequest("Mail Id must be required!");
            AppUser? user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
#if DEBUG
                user = new AppUser();
                user.Email = "sherman@zahavas.com";
#else
                return Redirect("/account/Login");
#endif
            }

            string access_token = HttpContext.Session.GetString("AccessToken") ?? string.Empty;
            if (string.IsNullOrEmpty(access_token))
                return Redirect("/account/Login");

            EmailExt emailExt       = _emailService.GetMailDetail(id);
            List<EmailExt> listResult = new List<EmailExt>();
            listResult = _emailService.GetMailDetailList(emailExt.em_from, emailExt.em_to);

            ViewBag.customerInfo    = _emailService.GetCustomerInfo(id);
            ViewBag.emailExt        = emailExt;
            ViewBag.emailList       = listResult;

            int status = 0;
            string strRespond = string.Empty;
            string strMailEncodeBody = _emailService.GetMailEncodeBody(id);

            foreach (var item in listResult)
            {
                strMailEncodeBody += _emailService.GetMailEncodeBody(item.em_id);
            }
            //strMailEncodeBody = "";
            if (strMailEncodeBody != "")
            {
                strRespond = await _llmService.GetResponseLLM(strMailEncodeBody);

                if (strRespond != string.Empty)
                {
                    JObject jsonObj = JObject.Parse(strRespond);
                    status = Convert.ToInt32((jsonObj["status"] ?? '0').ToString());
                    
                    if (status == 1)
                    {
                        string strOrderName = (jsonObj["order_id"] ?? "").ToString();

                        TbOrder tbOrder = _shopifyService.GetOrderInfo(strOrderName);

                        if (tbOrder != null)
                        {
                            try
                            {
                                string orderDetail = await _shopifyService.GetOrderInfoRequest(tbOrder.or_id);

                                var jsonOrder = JsonDocument.Parse(orderDetail).RootElement.GetProperty("order");
                                var jsonCustomer = jsonOrder.GetProperty("customer");
                                var jsonAddress = jsonCustomer.GetProperty("default_address");

                                ViewBag.orderId = jsonOrder.GetProperty("id");
                                ViewBag.orderName = jsonOrder.GetProperty("name");
                                ViewBag.financial_status = jsonOrder.GetProperty("financial_status");
                                ViewBag.fulfillment_status = jsonOrder.GetProperty("fulfillment_status");
                                ViewBag.closed_at = jsonOrder.GetProperty("closed_at");
                                ViewBag.lineName = jsonOrder.GetProperty("line_items")[0].GetProperty("name");
                                ViewBag.deliveryMethod = jsonOrder.GetProperty("shipping_lines")[0].GetProperty("code");
                                ViewBag.sku = jsonOrder.GetProperty("fulfillments")[0].GetProperty("line_items")[0].GetProperty("sku");
                                ViewBag.total_line_items_price = jsonOrder.GetProperty("total_line_items_price");
                                ViewBag.total_shipping_price_amount =
                                                jsonOrder.GetProperty("total_shipping_price_set")
                                                        .GetProperty("shop_money")
                                                        .GetProperty("amount");
                                ViewBag.current_total_price = jsonOrder.GetProperty("current_total_price");
                                ViewBag.current_total_discounts = jsonOrder.GetProperty("current_total_discounts");

                                Customer orderCustomerInfo = new Customer
                                {
                                    FirstName = jsonCustomer.GetProperty("first_name").ToString(),
                                    LastName = jsonCustomer.GetProperty("last_name").ToString(),
                                    Email = jsonCustomer.GetProperty("email").ToString(),
                                    Phone = jsonCustomer.GetProperty("phone").ToString(),
                                    shippingAddress = jsonAddress.GetProperty("name").ToString()
                                                    + jsonAddress.GetProperty("address1").ToString()
                                                    + jsonAddress.GetProperty("city").ToString()
                                                    + jsonAddress.GetProperty("zip").ToString()
                                };

                                ViewBag.orderCustomerInfo = orderCustomerInfo;
                            } catch
                            {
                                Console.WriteLine("*****************PKH: failed getting order data from server***************");
                            }
                            
                        }
                    }
                }
            }
           
            return View("View_EmailDetail");
        }

        [HttpPost]
        public IActionResult GetMailList(string strEmail, int nPageIndex = 0, int nEmailState = 0)
        {
            if (strEmail != "All")
            {
                string accessToken = _emailTokenService.GetAccessTokenFromMailName(strEmail);
                _emailService.UpdateMailDatabaseAsync(accessToken, strEmail, 10);
            }

            int nMailCnt = _emailService.GetMailCnt(strEmail, nEmailState);
            var emailList = _emailService.GetMailList(strEmail, nPageIndex, nCntPerPage, nEmailState);

            List<TbEmailsExt> emailExtList = new List<TbEmailsExt>();
            foreach (var email in emailList)
            {
                emailExtList.Add(new TbEmailsExt(email));
            }

            ViewBag.Emails          = emailExtList;
            ViewBag.nMailTotalCnt   = nMailCnt;

            return PartialView("View_EmailList");
        }

        [HttpPost]
        public IActionResult GetMailCntInfo(string strEmail)
        {
            int nInboxCnt     = _emailService.GetMailCnt(strEmail, 0);
            int nArchievedCnt = _emailService.GetMailCnt(strEmail, 3);
            return Json(new { status = 200, nInboxCnt = nInboxCnt, nArchievedCnt = nArchievedCnt, nCntPerPage = nCntPerPage });
        }

        [HttpPost]
        public IActionResult GetCustomerInfo(string strMailId)
        {

            CustomerInfo customerInfo = _emailService.GetCustomerInfo(strMailId);
            return Json(new { status = 201, customerInfo = customerInfo });
        }

        [HttpPost]
        public async Task<IActionResult> GetOrderInfo(string strMailId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                int status = 0;
                string strRespond = string.Empty;

                string strMailEncodeBody = _emailService.GetMailEncodeBody(strMailId);
                if (strMailEncodeBody != "")
                {
                    strRespond = await _llmService.GetResponseLLM(strMailEncodeBody);
                    JObject jsonObj = JObject.Parse(strRespond);
                    status = Convert.ToInt32((jsonObj["status"] ?? '0').ToString());
                }
                Console.WriteLine(strRespond);
                if (status == 1)
                {
                    JObject jsonObj = JObject.Parse(strRespond);
                    string strType = (jsonObj["type"] ?? "").ToString();
                    string strOrderName = (jsonObj["order_id"] ?? "").ToString();

                    TbOrder tbOrder = _shopifyService.GetOrderInfo(strOrderName);
                    Console.WriteLine(tbOrder);
                    if ( tbOrder != null)
                    {
                        string orderDetail = await _shopifyService.GetOrderInfoRequest(tbOrder.or_id);
                        ViewBag.status = 201;
                        ViewBag.orderName = strOrderName;
                        ViewBag.order = tbOrder;
                        ViewBag.orderDetail = orderDetail;
                        return PartialView("View_OrderDetail");
                    }
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            ViewBag.status = -201;
            return PartialView("View_OrderDetail");
        }

        [HttpPost]
        public async Task<IActionResult> requestShopify(long orderId, int type, long em_idx)
        {
            var user = await _userManager.GetUserAsync(User);
            try
            {
                if (type ==2)
                {
                    bool isResult = true;//await _shopifyService.CancelOrder(orderId);

                    TbOrder p = await _shopifyService.OrderRequest(orderId);

                    if (isResult)
                    {
                        if (em_idx != 0)
                        {
                            await _emailService.ChangeState(em_idx, 3);
                        }

                        
                    }

                    return Json(new { status = isResult? 201: -201, order = p });
                } else if (type == 3)
                {
                    bool isResult = true;// await _shopifyService.RefundOrder(orderId);
                    if (isResult)
                    {
                        if(em_idx !=0)
                        {
                            await _emailService.ChangeState(em_idx, 2);
                        }
                    }

                    TbOrder p = _shopifyService.GetOrderInfo(orderId);
                    string orderDetail = await _shopifyService.GetOrderInfoRequest(p.or_id);
                    return Json(new { status = isResult ? 201 : -201, order = p, orderDetail = orderDetail });
                }
            } catch ( Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Json(new { status = 0 });
        }
    }

    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string shippingAddress { get; set; }
        public string BillingAddress { get; set; }
        public Customer()
        {
            FirstName = "";
            LastName = "";
            Email = "";
            Phone = "";
            shippingAddress = "";
            BillingAddress = "";
        }
    }
}
