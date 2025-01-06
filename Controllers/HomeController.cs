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

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private SignInManager<AppUser> signInManager;
        private Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager;
        private readonly EMailService _emailService;
        private readonly ShopifyService _shopifyService;
        private readonly ModelService _smsService;
        private readonly LLMService _llmService;
		private const int PerPageCnt = 20;
        private readonly string _phoneNumber;
        public HomeController(SignInManager<AppUser> signinMgr, Microsoft.AspNetCore.Identity.UserManager<AppUser> userMgr, EMailService service, ShopifyService shopifyService, ModelService smsService, ILogger<HomeController> logger, IConfiguration _configuration, LLMService llmService)
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
            if(string.IsNullOrEmpty(access_token))
				return Redirect("/account/Login");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetMailList(string strEmail, int PageNo = 1, int Type = 1)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return PartialView("EmailList");
            }

            int nPageCnt = 0;
            nPageCnt = _emailService.GetMailListPerUserCount(strEmail, PerPageCnt, Type);
            var emails = _emailService.GetMailListPerUser(strEmail, PageNo > 0 ? PageNo - 1 : PageNo, PerPageCnt, Type);

            List<TbEmailsExt> lstEmails = new List<TbEmailsExt>();
            foreach (var email in emails)
            {
                lstEmails.Add(new TbEmailsExt(email));
            }

            ViewBag.Emails = lstEmails;
            if (lstEmails.Count == 0)
                PageNo -= 1;

            ViewBag.PageCnt = nPageCnt;
            ViewBag.PageNo = PageNo;
            ViewBag.Type = Type;

            return PartialView("EmailList");
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(string id, int Type)
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
            if (Type == 0) {
                Type = 1;
            }
            CustomerInfo obj = null;
            if(Type == 1)
            {
                obj = await _emailService.GetCustomerInfo(id);
            }else if(Type == 3) {
                obj = _smsService.GetCustomerInfo(id);
			}

            ViewBag.Customer = obj;
            ViewBag.User = user;
            ViewBag.scripts = new List<string> { "/js/sweetalert2.all.js", "/assets/scripts/conversation.js" };
            ViewBag.styles = new List<string> { "/css/sweetalert2.css", "/assets/bundles/css/customize.css" };
            ViewBag.id = id;
            ViewBag.Type = Type;
            ViewBag.GMail = obj != null ? obj.strEmail : "";
            return View();
        }

        private async Task<List<int[]>> GetCountPerType()
        {
            AppUser? user = await userManager.GetUserAsync(HttpContext.User);
            if(user == null)
            {
                return new List<int[]> ();
            }

            var lstReturn = new List<(int, int)>();
            foreach(var nType in new List<int> { 1, 2, 3 })
            {
                int nPageCnt = 0;
                if (nType == 1 || nType == 2)
                {
                    nPageCnt = _emailService.GetMailListPerUserCount(user.Email, PerPageCnt, nType);
                }else if(nType == 3)
                {
                    string myPhone = _phoneNumber;
                    if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                        myPhone = user.PhoneNumber;

                    nPageCnt = _smsService.GetSMSListPerUserCount(myPhone, PerPageCnt);
                }
                lstReturn.Add((nType, nPageCnt));
            }
            return lstReturn.Select(e => new [] {e.Item1, e.Item2}).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> GetMessageList(string id, int Type, string GMail)
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
            List<EmailExt> lstResult = new List<EmailExt>();

            if(Type == 1 || Type == 2) 
            {
                lstResult = await _emailService.GetGmailList(GMail, access_token, user.Email); //await _emailService.GetGmailList(GMail, user.Email);
            }
            else if(Type == 3)
            {
                string myPhone = _phoneNumber;
                if (user.PhoneNumber != null)
                    myPhone = user.PhoneNumber;
                lstResult = await _smsService.GetMessageList(GMail, myPhone);                
            }

			ViewBag.MessageList = lstResult;
            ViewBag.id = id;
            ViewBag.Type = Type;

            return PartialView("MessageDetail");
		}
        
        /// <param name="Type"></param>
        /// Type == 1 : my inbox
        /// Type == 2 : archived
        /// Type == 3 : my SMS
        
        
		[HttpPost]
		public async Task<IActionResult> MakeStateByGMail(string strGmail, int em_state)
		{
			var user = await userManager.GetUserAsync(User);

			if (user == null || string.IsNullOrEmpty(user.Email))
				return Json(new { status = false });

			if (string.IsNullOrEmpty(strGmail))
                return Json(new { status = false });
			
            bool isSuccess = await _emailService.ChangeStates(strGmail, em_state, user.Email);
			
			return Json(new { status = isSuccess });
		}

        [HttpPost]
        public async Task<IActionResult> MakeStateByGMails(List<string> arrGmail, int nType)
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null || string.IsNullOrEmpty(user.Email))
                return Json(new { status = false });

            if (arrGmail.Count == 0)
                return Json(new { status = false });

            if(nType == 1 || nType == 2)
            {
                await _emailService.ChangeState(arrGmail, 3);
                return Json(new { status = true });
            }else if(nType == 3)
            {
                await _smsService.ChangeState(arrGmail, 3);
                return Json(new { status = true });
            }
            return Json(new { status = true });

        }

        [HttpPost]
        public async Task<IActionResult> requestShopify(long orderId, int type, long em_idx)
        {
			var user = await userManager.GetUserAsync(User);
			try
            {
                if (type == 2)
                {
                    bool isResult = await _shopifyService.CancelOrder(orderId);
                    
                    TbOrder p = await _shopifyService.OrderRequest(orderId);
                    if (isResult)
                    {
                        if(em_idx != 0)
                        {
                            await _emailService.ChangeState(em_idx, 3); 
                        }
                    }
                    
                    return Json(new { status = isResult ? 1 : 0, order = p });
                } else if (type == 3)
                {
                    bool isResult = await _shopifyService.RefundOrder(orderId);
                    if (isResult)
                    {
                        if(em_idx != 0)
                        {
                            await _emailService.ChangeState(em_idx, 2); 
                        }
					}
					TbOrder p = await _shopifyService.GetOrderInfo(orderId);
                    string orderDetail = await _shopifyService.GetOrderInfoRequest(p.or_id);
                    return Json(new { status = isResult ? 1 : 0, order = p, orderDetail = orderDetail });
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Json(new { status = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> createOrderForTest()
        {
            string objOrder = "{\"order\":{\"currency\":\"EUR\",\"lineItems\":[{\"title\":\"Big Brown Bear Boots\",\"priceSet\":{\"shopMoney\":{\"amount\":74.99,\"currencyCode\":\"EUR\"}},\"quantity\":3,\"taxLines\":[{\"priceSet\":{\"shopMoney\":{\"amount\":10.2,\"currencyCode\":\"EUR\"}},\"rate\":0.06,\"title\":\"State tax\"}]}],\"transactions\":[{\"kind\":\"SALE\",\"status\":\"SUCCESS\",\"amountSet\":{\"shopMoney\":{\"amount\":238.47,\"currencyCode\":\"EUR\"}}}]}}";
            return Json(new { objOrder = objOrder });
        }
        
		[HttpPost]
		public async Task<IActionResult> sendRequestEmail_(string strTo, string strBody, int Type = 1)
		{
			try
			{
				var user = await userManager.GetUserAsync(User);
				if (user == null)
				{
					return Json(new { status = 0 });
				
                }

                bool isResult = false;
                if(Type == 1 || Type == 2)
                {
				    string access_token = HttpContext.Session.GetString("AccessToken");
				    isResult = await _emailService.SendEmailAsync(strTo, user.Email, access_token, "request", strBody);
                }else if (Type == 3)
                {
                    await _smsService.SendSms(strBody, strTo, _phoneNumber);
                    isResult = true;
                }

				if (isResult)
				{
					//await SendMailInfo(user.Email);

					return Json(new { status = 1 });
				}
				return Json(new { status = 0 });
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			return Json(new { status = 0 });
		}

        private TbOrder GetOrderInfoByType(string strId, int Type)
        {
            //type == 1, 2 - strId =Gmail
            //type == 3 - strId = phoneNumber
            try
            {
                TbOrder p = null;
				if (Type == 1 || Type == 2)
				{
					p = _shopifyService.GetOrderInfoByEmail(strId);
				}
				else if (Type == 3)
				{
					p = _shopifyService.GetOrderInfoByPhone(strId);
				}
			}
			catch(Exception ex)
            {
                Console.WriteLine("home/GetOrderInfoByType " + ex.Message);
            }
            return null;
        }

		[HttpPost]
		public async Task<IActionResult> Process(string strGmail, int Type = 1)
		{
            try
            {
                var user = await userManager.GetUserAsync(User);

                int status = 0;
                string strRespond = string.Empty;

                if(Type == 1 || Type == 2)
                {
                    TbEmail? pEmail = _emailService.GetMailInfo_(strGmail, user.Email);
                    if (pEmail != null)
                    {
				        strRespond = await _llmService.GetResponseLLM(pEmail.em_body);
					    JObject jsonObj = JObject.Parse(strRespond);
					    status = Convert.ToInt32(jsonObj["status"].ToString());
				    }
                } else if(Type == 3)
                {
                    TbSms pSms = await _smsService.GetSmsById(strGmail);
					if (pSms != null)
					{
						strRespond = await LLMService.GetResponseAsync(pSms.sm_body);
						JObject jsonObj = JObject.Parse(strRespond);
						status = Convert.ToInt32(jsonObj["status"].ToString());
                        strGmail = pSms.sm_from;
					}
				}
				if (status == 0)
				{
                    TbOrder p = GetOrderInfoByType(strGmail, Type);
                    
					if (p == null)
					{
						return Json(new { status = -1, data = new { rephase = new { msg = "There is no order information available." } } });
					}
					else
					{
						string orderDetail = await _shopifyService.GetOrderInfoRequest(p.or_id);
						return Json(new { status = 4, data = new { orderId = p.or_id, order = p, orderDetail = orderDetail } });
					}
				}
				else
				{
					JObject jsonObj = JObject.Parse(strRespond);
					string strType = jsonObj["type"].ToString();
					string strOrderId = jsonObj["order_id"].ToString();
                    if (!string.IsNullOrEmpty(strOrderId))
                    {
                        TbOrder p = await _shopifyService.GetOrderInfo(strOrderId);
                        
						if (p == null)
						{
                            p = GetOrderInfoByType(strGmail, Type);
							if (p == null)
							{
								return Json(new { status = -1, data = new { rephase = new { msg = "There is no order information available." } } });
							}
						}
						string orderDetail = await _shopifyService.GetOrderInfoRequest(p.or_id);
						return Json(new { status = 4, data = new { orderId = strOrderId, order = p, orderDetail = orderDetail } });
					}
				}
				return Json(new { status = 0, data = new { msg = "" } });
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return Json(new { status = 0 });
			}
		}
		
        private async Task SendMailInfo(string strGmail)
        {
			if (!string.IsNullOrEmpty(strGmail))
			{
				await _emailService.SendMailInfo(strGmail);
			}
		}

        [AllowAnonymous]        
        [HttpPost("notification")]
        public async Task<IActionResult> ReceiveGmailNotification()
        {            
            using (var reader = new StreamReader(Request.Body))
            {
                var rawBody = await reader.ReadToEndAsync();
                JObject jsonObj = JObject.Parse(rawBody);
                if(jsonObj.Type == JTokenType.Null)
                {
                    return Ok();
                }

                if (jsonObj["historyId"] != null)
                {
                    var historyId = jsonObj["historyId"].ToString();
                    ulong lId = Convert.ToUInt64(historyId);
                    if (!Global.lstHistoryIds.Contains(lId))
                    {
                        Global.lstHistoryIds.Add(lId);
                        Console.WriteLine(Global.lstHistoryIds.Count);
                    }
                }
                Console.WriteLine($"Raw Request Body: {rawBody}");
            }
            return Ok();
        }

        private async Task UpdateEmail(string strUserEmail)
        {
            while(true)
            {
                if(Global.lstHistoryIds.Count == 0)
                {
                    Thread.Sleep(5 * 100);
                    continue;
                }
                try
                {
                    ulong strMsgId = Global.lstHistoryIds.First();
                    ulong responseId = await _emailService.UpdateEmail(strMsgId, strUserEmail);
                    if(responseId != 0)
                    {
                        if(!Global.lstHistoryIds.Contains(responseId))
                            Global.lstHistoryIds.Add(responseId);
                    }
                    Global.lstHistoryIds.Remove(strMsgId);
                }catch(Exception ex)
                {
                    _logger.LogError("in Update Email " + ex.Message);                    
                }
            }
        }
    }
}