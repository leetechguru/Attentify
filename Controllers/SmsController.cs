using Google.Apis.Gmail.v1;
using Google.Cloud.PubSub.V1;
using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Newtonsoft.Json.Linq;
using ShopifySharp;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Reflection.PortableExecutable;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;

namespace GoogleLogin.Controllers
{

    //[Authorize]
    public class SmsController : Controller
    {
        
        private Microsoft.AspNetCore.Identity.UserManager<AppUser> userManager;
        private readonly ILogger<SmsController> _logger;
        private readonly ModelService _modelService;
        private readonly string _phoneNumber;
        private readonly GoogleLogin.Services.ShopifyService _shopifyService;
        public SmsController(UserManager<AppUser> _userManager, ILogger<SmsController> logger, IConfiguration configuration, ModelService modelService, GoogleLogin.Services.ShopifyService shopifyService)
        {            
            userManager = _userManager;
            _logger = logger;
            _phoneNumber = configuration["Twilio:PhoneNumber"];
            _modelService = modelService;
            _shopifyService = shopifyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string phone)
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
        public async Task<IActionResult> InitializeSms(string phoneNumber)
        {
            AppUser? user = await userManager.GetUserAsync(HttpContext.User);
			string? myPhone = _phoneNumber;
			if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
				myPhone = user.PhoneNumber;

			ViewBag.phone = phoneNumber;            
            ViewBag.myPhoneNumber = myPhone;
            ViewBag.smsList = await _modelService.GetSms(myPhone, phoneNumber);

            await _modelService.SendSmsCountInfo(myPhone);
            return PartialView("Sms");
            //return Ok(new { phoneNumber });
        }

        [HttpPost]
        public async Task<IActionResult> Response(string phone)
        {
			AppUser? user = await userManager.GetUserAsync(HttpContext.User);
            
            string? myPhone = _phoneNumber;
            if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                myPhone = user.PhoneNumber;

			string strBody = await _modelService.GetLastSms(phone, myPhone);
            if (string.IsNullOrWhiteSpace(strBody))
            {
                return Json(new { status = 0 });
            }

            string strRespond = await LLMService.GetResponseAsync(strBody);
            JObject jsonObj = JObject.Parse(strRespond);
            int status = (int)jsonObj["status"];
            if (status == 0)    //mail not contain order info.
            {
                string strMail = jsonObj["msg"].ToString();
                if (string.IsNullOrEmpty(strMail))
                {
                    strMail = "Hello! \n Could you please send me the correct message containing the order information?";
                    return Json(new { status = 1, data = new { rephase = new { msg = strMail } } });
                }
                return Json(new { status = 1, data = new { rephase = strRespond } });
            }
            else //mail contain order info.
            {
                string strMail = "Hello! \n I will consider your request. May I process your order request for you?";
                return Json(new { status = 1, data = new { rephase = new { msg = strMail } } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendSms(string sms, string phone)
        {
            AppUser? user = await userManager.GetUserAsync(HttpContext.User);
			string? myPhone = _phoneNumber;
			if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
				myPhone = user.PhoneNumber;

            await _modelService.SendSms(sms, phone, myPhone);
            return await InitializeSms(phone);
        }

        [HttpPost]
        public async Task<IActionResult> ParseAI(string phone)
        {
            try
            {
				AppUser? user = await userManager.GetUserAsync(HttpContext.User);
				string? myPhone = _phoneNumber;
				if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
					myPhone = user.PhoneNumber;

				string strBody = await _modelService.GetLastSms(phone, myPhone);
                if (string.IsNullOrWhiteSpace(strBody))
                {
                    return Json(new { status = -1, data = new { rephase = new { msg = "There is no request in the message." } } });
				}
                string strRespond = await LLMService.GetResponseAsync(strBody);
                JObject jsonObj = JObject.Parse(strRespond);
                int status = (int)jsonObj["status"];

                if (status == 0)
                {
                    string strMail = jsonObj["msg"].ToString();
                    TbOrder p = _shopifyService.GetOrderInfoByPhone(phone);
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
                    string strType = jsonObj["type"].ToString();
                    string strOrderId = jsonObj["order_id"].ToString();
                    if (!string.IsNullOrEmpty(strOrderId))
                    {                        
                        TbOrder p = _shopifyService.GetOrderInfo(strOrderId);
                        if (p == null)
                        {                            
                            p = _shopifyService.GetOrderInfoByPhone(phone);
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
        /// <summary>
        /// SMS Twilio webhook endpoint
        /// </summary>
        /// <param name="from"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost("/smsreceive")]
        public async Task<IActionResult> ReceiveSms([FromForm] string from, [FromForm] string body, [FromForm] string messageSid)
        {
            Console.WriteLine($"Received SMS from {from}: {body}");
			_logger.LogInformation($"Received SMS from {from}: {body}");
			AppUser? user = await userManager.GetUserAsync(HttpContext.User);
            //if (user == null) return NoContent();

			string? myPhone = _phoneNumber;
			if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
				myPhone = user.PhoneNumber;

			TbSms p = new TbSms
            {
                sm_id = messageSid,
                sm_from = from,
                sm_body = body,
                sm_to = myPhone,
                sm_date = DateTime.Now,
                sm_read = 0
            };
            await _modelService.SaveSms(p);

            await _modelService.SendSmsCountInfo(myPhone);
            Thread.Sleep(10);
            await _modelService.SendNewSmsInfo(p);
            return NoContent();            
        }
    }    
}
