using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Flows;
using GoogleLogin.Models;
using Google.Apis.Auth.OAuth2;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

using ShopifyService = GoogleLogin.Services.ShopifyService;
using System.Web;
using System;
using System.Diagnostics;
using System.Security.Policy;
using Humanizer;
using System.Runtime.InteropServices.JavaScript;
using Twilio.TwiML.Messaging;
using Microsoft.IdentityModel.Tokens;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class SettingController : Controller
    {
        private readonly IServiceScopeFactory    _serviceScopeFactory;
        private readonly ILogger<HomeController> _logger;
        private SignInManager<AppUser> signInManager;
        private UserManager<AppUser> userManager;
        private readonly EMailService _emailService;
        private readonly IConfiguration _configuration;
        public static readonly string[] Scopes = {"email", "profile", "https://www.googleapis.com/auth/gmail.modify"};

        public SettingController(SignInManager<AppUser> signinMgr, IServiceScopeFactory serviceScopeFactory, UserManager<AppUser> userMgr, EMailService service, ShopifyService shopifyService, ModelService smsService, ILogger<HomeController> logger, IConfiguration configuration, LLMService llmService)
        {
            signInManager = signinMgr;
            _serviceScopeFactory = serviceScopeFactory;
            userManager = userMgr;
            _logger = logger;
            _emailService = service;
            _configuration = configuration;
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
            string access_token = HttpContext.Session.GetString("AccessToken") ?? string.Empty;
            if(string.IsNullOrEmpty(access_token))
				return Redirect("/account/Login");

            return View();
        }

        [HttpGet]
        public IActionResult MailManage()
        {
            string access_token = HttpContext.Session.GetString("AccessToken") ?? string.Empty;
            if (string.IsNullOrEmpty(access_token))
                return Redirect("/account/Login");

            return View("View_MailManage");
        }

        [HttpPost]
        public IActionResult SaveMail(string strMailName, string strClientId, string strClientSecret)
        {
            
            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                TbMailAccount pMailAccount = _dbContext.TbMailAccount.Where(e => e.mail == strMailName ).FirstOrDefault();
                if (pMailAccount == null)
                {
                    TbMailAccount mailAccount = new TbMailAccount
                    {
                        mail = strMailName,
                        clientId = strClientId,
                        clientSecret = strClientSecret,
                        accessToken = string.Empty,
                        refreshToken = string.Empty,
                        authCode    = string.Empty,
                        redirecUri = string.Empty,
                    };

                    _dbContext.TbMailAccount.Add(mailAccount);
                    _dbContext.SaveChanges();
                    return Json(new { status = 201 });
                }
            }

            return Json(new { status = -201 });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateMail(string strMailIdx)
        {
            if (string.IsNullOrWhiteSpace(strMailIdx))
            {
                return Json(new { status = -201, message = "Update failed" });
            }

            int ret = await _emailService.GetAccessTokenFromMailIdx(strMailIdx);

            if (ret == 1)
            {
                return Json(new { status = 201, message = "Update Success" });
            }
            return Json(new { status = -201, message = "Update failed" });
        }

        [HttpPost]
        public IActionResult DeleteMail(string strMailIdx)
        {
            if (string.IsNullOrWhiteSpace(strMailIdx))
            {
                return Json(new { status = -201, message = "Invalid mail index" }); 
            }

            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                if (!int.TryParse(strMailIdx, out int mailIdx))
                {
                    return Json(new { status = -201, message = "Mail index must be a valid number" }); 
                }

                var pMailAccount = _dbContext.TbMailAccount.FirstOrDefault(e => e.id == mailIdx); 

                if (pMailAccount == null)
                {
                    return Json(new { status = -201, message = "Record not found" }); 
                }

                _dbContext.TbMailAccount.Remove(pMailAccount);
                _dbContext.SaveChanges();

                return Json(new { status = 201, message = "Record deleted successfully" });
            }
        }

        [HttpPost]
        public IActionResult GetMailList()
        {
            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                List<TbMailAccount> mailList = _dbContext.TbMailAccount.Where(e=>e.mail != "").ToList();

                ViewBag.mailList = mailList;
                return PartialView("View_MailList");
            }
        }

        [HttpPost]
        public IActionResult RegisterNewMail()
        {
            Console.WriteLine(_configuration["clientId"]);
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _configuration["clientId"],
                        ClientSecret = _configuration["clientSecret"]
                    },
                    Scopes = Scopes,
                    Prompt = "select_account consent",
                });

            string redirectUri = "https://localhost:7150/setting/OAuth2Callback";
            string authorizationUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build().ToString();
            return Json( new { status = 201, authorizationUrl = authorizationUrl });
        }

        [HttpGet]
        public async Task<IActionResult> OAuth2Callback(string code, string error)
        {
            Console.WriteLine(code);
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest("Error during Google sign-in: " + error);
            }

            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("No authorization code received.");
            }

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = _configuration["clientId"],
                    ClientSecret = _configuration["clientSecret"]
                },
                Scopes = Scopes
            });

            string redirectUri = "https://localhost:7150/setting/OAuth2Callback";

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                userId: "user-id", // Can be any identifier for the user (e.g., a session ID)
                code: HttpUtility.UrlDecode(code),
                redirectUri: redirectUri,
                taskCancellationToken: CancellationToken.None
            );

            Console.WriteLine(tokenResponse);
            Console.WriteLine($"Access Token: {tokenResponse.AccessToken}");
            Console.WriteLine($"Refresh Token: {tokenResponse.RefreshToken}");
            Console.WriteLine($"Token Expiry: {tokenResponse.ExpiresInSeconds}");
            
            string strMailName = await _emailService.GetGmailNameAsync(tokenResponse.AccessToken);
            Console.WriteLine(strMailName);

            if ( strMailName.IsNullOrEmpty() ) 
                return RedirectToAction("MailManage", "setting");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                var _item = _dbContext.TbMailAccount
                    .Where(item => item.mail.Contains(strMailName))
                    .FirstOrDefault();

                if (_item == null)
                {
                    _dbContext.Add(new TbMailAccount
                    {
                        mail = strMailName,
                        clientId = "",
                        clientSecret = "",
                        accessToken = tokenResponse.AccessToken,
                        refreshToken = tokenResponse.RefreshToken,
                        authCode = "",
                        redirecUri = "",
                    });
                }
                else
                {
                    _item.accessToken = tokenResponse.AccessToken;
                    _item.refreshToken = tokenResponse.RefreshToken;
                }
                _dbContext.SaveChanges();
            }

            return RedirectToAction("MailManage", "setting");
        }

        [HttpGet]
        public IActionResult UserManage()
        {
            string access_token = HttpContext.Session.GetString("AccessToken") ?? string.Empty;
            if (string.IsNullOrEmpty(access_token))
                return Redirect("/account/Login");

            return View("View_UserManage");
        }
    }
}