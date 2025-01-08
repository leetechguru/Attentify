using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Google.Cloud.PubSub.V1;
using ShopifyService = GoogleLogin.Services.ShopifyService;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Microsoft.AspNetCore.Connections;
using Google.Api;
using ShopifySharp;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class EmailController : Controller
    {
        private readonly ILogger<EmailController> _logger;
        private SignInManager<AppUser> signInManager;
        private Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager;
        private readonly EMailService _emailService;
        private readonly ShopifyService _shopifyService;
        private readonly ModelService _smsService;
        private readonly LLMService _llmService;
        private const int nCntPerPage = 20;
        private readonly string _phoneNumber;

        public EmailController(SignInManager<AppUser> signinMgr, Microsoft.AspNetCore.Identity.UserManager<AppUser> userMgr, EMailService service, ShopifyService shopifyService, ModelService smsService, ILogger<EmailController> logger, IConfiguration _configuration, LLMService llmService)
        {
            signInManager = signinMgr;
            userManager = userMgr;
            _emailService = service;
            _shopifyService = shopifyService;
            _logger = logger;
            _smsService = smsService;
            _phoneNumber = _configuration["Twilio:PhoneNumber"];
            _llmService = llmService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AppUser? user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
#if DEBUG
                user = new AppUser();
                user.Email = "sherman@zahavas.com";
#else
                return Redirect("/account/Login");
#endif
            }
            string access_token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(access_token))
                return Redirect("/account/Login");

            return View();
        }

        [HttpPost]
        public IActionResult GetMailList(string strEmail, int nPageIndex = 0, int nEmailState = 0)
        {
            int nMailCnt = _emailService.GetMailCnt(strEmail, nEmailState);
            var emailList = _emailService.GetMailList(strEmail, nPageIndex, nCntPerPage, nEmailState);

            List<TbEmailsExt> emailExtList = new List<TbEmailsExt>();
            foreach (var email in emailList)
            {
                emailExtList.Add(new TbEmailsExt(email));
            }

            ViewBag.Emails = emailExtList;
            ViewBag.nMailTotalCnt = nMailCnt;

            return PartialView("View_EmailList");
        }

        [HttpPost]
        public IActionResult GetMailCntInfo(string strEmail)
        {
            int nInboxCnt = _emailService.GetMailCnt(strEmail, 0);
            int nArchievedCnt = _emailService.GetMailCnt(strEmail, 3);
            return Json(new { status = 200, nInboxCnt = nInboxCnt, nArchievedCnt = nArchievedCnt, nCntPerPage = nCntPerPage });
        }

        [HttpPost]
        public IActionResult GetMailDetail(string strMailId)
        {
            EmailExt emailExt = new EmailExt();
            emailExt = _emailService.GetMailDetail(strMailId);
            ViewBag.emailExt = emailExt;
            return PartialView("View_EmailDetail");
        }

        [HttpPost]
        public async Task<IActionResult> GetOrderInfo(string strMailId)
        {
            try
            {
                var user = await userManager.GetUserAsync(User);

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
    }
}
