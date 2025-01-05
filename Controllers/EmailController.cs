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
        private const int PerPageCnt = 20;
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
    }
}
