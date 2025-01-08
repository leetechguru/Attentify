using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

using ShopifyService = GoogleLogin.Services.ShopifyService;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class SettingController : Controller
    {
        private readonly IServiceScopeFactory    _serviceScopeFactory;
        private readonly ILogger<HomeController> _logger;
        private SignInManager<AppUser> signInManager;
        private UserManager<AppUser> userManager;

        public SettingController(SignInManager<AppUser> signinMgr, IServiceScopeFactory serviceScopeFactory, UserManager<AppUser> userMgr, EMailService service, ShopifyService shopifyService, ModelService smsService, ILogger<HomeController> logger, IConfiguration _configuration, LLMService llmService)
        {
            signInManager = signinMgr;
            _serviceScopeFactory = serviceScopeFactory;
            userManager = userMgr;
            _logger = logger;
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
                    };

                    _dbContext.TbMailAccount.Add(mailAccount);
                    _dbContext.SaveChanges();
                    return Json(new { status = 201 });
                }
            }

            return Json(new { status = -201 });
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