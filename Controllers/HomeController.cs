using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopifyService = GoogleLogin.Services.ShopifyService;
using Google.Apis.Auth.OAuth2.Flows;
using System.Web;
using WebSocketSharp;
using System.Security.Claims;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController>    _logger;
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
        private const int nCntPerPage = 20;
        
        public HomeController(
            SignInManager<AppUser>      signinMgr, 
            UserManager<AppUser>        userMgr,
            EMailService                emailService, 
            EMailTokenService           emailTokenService,
            ShopifyService              shopifyService,
            SmsService                  smsService, 
            ILogger<HomeController>     logger, 
            IConfiguration              configuration, 
            IServiceScopeFactory        serviceScopeFactory,
            LLMService                  llmService)
        {
            _signInManager  =       signinMgr;
            _userManager    =       userMgr;
            _emailService   =       emailService;
            _emailTokenService    = emailTokenService;
            _shopifyService =       shopifyService;
            _logger         =       logger;
            _smsService     =       smsService;
            _configuration  =       configuration;
            _serviceScopeFactory =  serviceScopeFactory;
            _phoneNumber    =       configuration["Twilio:PhoneNumber"] ?? "";
            _llmService     =       llmService;
        }
       
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(HttpContext.User);
            return View();
        }

        [HttpGet("/OAuth2Callback")]
        public async Task<IActionResult> OAuth2Callback(string code, string error)
        {
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
                    ClientId        = _configuration["clientId"],
                    ClientSecret    = _configuration["clientSecret"]
                },
                Scopes = Scopes
            });

            string redirectUri = $"{HttpContext.Session.GetString("HostUrl")}/OAuth2Callback";

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                userId: "user-id", // Can be any identifier for the user (e.g., a session ID)
                code: HttpUtility.UrlDecode(code),
                redirectUri: redirectUri,
                taskCancellationToken: CancellationToken.None
            );

            string strMailName = await _emailTokenService.GetGmailNameAsync(tokenResponse.AccessToken);

            if (strMailName.IsNullOrEmpty())
                return RedirectToAction("MailManage", "setting");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                var _item = _dbContext.TbMailAccount
                    .Where(item => item.mail.Contains(strMailName) && item.userId == _userManager.GetUserId(HttpContext.User))
                    .FirstOrDefault();

                if (_item == null)
                {
                    _dbContext.Add(new TbMailAccount
                    {
                        mail            = strMailName,
                        accessToken     = tokenResponse.AccessToken,
                        refreshToken    = tokenResponse.RefreshToken,
                        userId          = _userManager.GetUserId(HttpContext.User) ?? ""
                    });
                }
                else
                {
                    _item.accessToken  = tokenResponse.AccessToken;
                    _item.refreshToken = tokenResponse.RefreshToken;
                }
                _dbContext.SaveChanges();
            }

            return Redirect(HttpContext.Session.GetString("RedirectUri") ?? "");
        }
    }
}