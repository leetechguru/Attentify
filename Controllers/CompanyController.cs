using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GoogleLogin.Services;
using GoogleLogin.Models;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly IServiceScopeFactory       _serviceScopeFactory;
        private readonly ILogger<HomeController>    _logger;
        private SignInManager<AppUser>              _signInManager;
        private UserManager<AppUser>                _userManager;
        private readonly EMailService               _emailService;
        private readonly IConfiguration             _configuration;

        public CompanyController(
            SignInManager<AppUser>  signinMgr,
            UserManager<AppUser>    userMgr,
            IServiceScopeFactory    serviceScopeFactory,
            EMailService            service,
            ILogger<HomeController> logger,
            IConfiguration          configuration)
        {
            _serviceScopeFactory    =   serviceScopeFactory;
            _signInManager          =   signinMgr;
            _userManager            =   userMgr;
            _logger                 =   logger;
            _emailService           =   service;
            _configuration          =   configuration;
        }

        public IActionResult Index()
        {
            ViewBag.menu = "setting";
            ViewBag.subMenu = "company";
            return View("View_Companies");
        }

        [HttpPost("/getCompanies")]
        public IActionResult getCompanies()
        {
            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                string strUserId = _userManager.GetUserId(HttpContext.User) ?? "";
                List<TbShopifyToken> shopifyList =
                    _dbContext.TbTokens.Where(e => e.UserId == strUserId)
                                .ToList();

                ViewBag.shopifyList = shopifyList;
                return PartialView("View_shopifyList");
            }
        }

        [HttpPost("/editCompany")]
        public IActionResult editCompany()
        {
            return Json(new { status = 201, message = "Updated the user info" });
        }

        [HttpPost("/deleteCompany")]
        public IActionResult deleteCompany(string strUserIdx)
        {
            if (string.IsNullOrWhiteSpace(strUserIdx))
            {
                return Json(new { status = -201, message = "Invalid shopify index" });
            }

            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                if (!int.TryParse(strUserIdx, out int shopifyIdx))
                {
                    return Json(new { status = -201, message = "Shopify index must be a valid number" });
                }

                var pShopify = _dbContext.TbTokens.FirstOrDefault(e => e.idx == shopifyIdx);

                if (pShopify == null)
                {
                    return Json(new { status = -201, message = "Record not found" });
                }

                _dbContext.TbTokens.Remove(pShopify);
                _dbContext.SaveChanges();

                return Json(new { status = 201, message = "Record deleted successfully" });
            }
        }
    }
}