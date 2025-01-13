using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopifyService = GoogleLogin.Services.ShopifyService;
using GoogleLogin.Services;
using GoogleLogin.Models;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class SettingController : Controller
    {
        private readonly IServiceScopeFactory       _serviceScopeFactory;
        private readonly ILogger<HomeController>    _logger;
        private SignInManager<AppUser>              _signInManager;
        private UserManager<AppUser>                _userManager;
        private readonly EMailService               _emailService;
        private readonly IConfiguration             _configuration;
        public static readonly string[]             Scopes = {"email", "profile", "https://www.googleapis.com/auth/gmail.modify"};

        public SettingController(
            SignInManager<AppUser> signinMgr, 
            IServiceScopeFactory serviceScopeFactory, 
            UserManager<AppUser> userMgr, 
            EMailService service, 
            ShopifyService shopifyService, 
            ModelService smsService, 
            ILogger<HomeController> logger,
            IConfiguration configuration, 
            LLMService llmService)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _signInManager = signinMgr;
            _userManager = userMgr;
            _logger = logger;
            _emailService = service;
            _configuration = configuration;
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
                List<TbMailAccount> mailList = _dbContext.TbMailAccount.Where(e=>e.mail != "" && e.userId == _userManager.GetUserId(HttpContext.User)).ToList();

                ViewBag.mailList = mailList;
                return PartialView("View_MailList");
            }
        }

        [HttpPost]
        public IActionResult RegisterNewMail()
        {
            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId        = _configuration["clientId"],
                        ClientSecret    = _configuration["clientSecret"]
                    },
                    Scopes = Scopes,
                    Prompt = "select_account consent",
                });

            var request = HttpContext.Request;
            var hostUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            string redirectUri = $"{hostUrl}/OAuth2Callback";

            string authorizationUrl = flow.CreateAuthorizationCodeRequest(redirectUri).Build().ToString();
            HttpContext.Session.SetString("RedirectUri", $"{hostUrl}/setting/mailmanage");
            return Json( new { status = 201, authorizationUrl = authorizationUrl });
        }

        [HttpGet]
        public IActionResult ShopifyManage()
        {
            string access_token = HttpContext.Session.GetString("AccessToken") ?? string.Empty;
            if (string.IsNullOrEmpty(access_token))
                return Redirect("/account/Login");

            return View("View_ShopifyManage");
        }

        [HttpPost]
        public IActionResult GetShopifyList()
        {
            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                List<TbShopifyToken> shopifyList = _dbContext.TbTokens.ToList();

                ViewBag.shopifyList = shopifyList;
                return PartialView("View_shopifyList");
            }
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