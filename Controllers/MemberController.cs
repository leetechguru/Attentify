
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GoogleLogin.Services;
using GoogleLogin.Models;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly IServiceScopeFactory       _serviceScopeFactory;
        private readonly ILogger<HomeController>    _logger;
        private SignInManager<AppUser>              _signInManager;
        private UserManager<AppUser>                _userManager;
        private readonly EMailService               _emailService;
        private readonly IConfiguration             _configuration;

        public MemberController(
            SignInManager<AppUser>  signinMgr,
            UserManager<AppUser>    userMgr,
            IServiceScopeFactory    serviceScopeFactory,
            EMailService            emailService,
            ILogger<HomeController> logger,
            IConfiguration configuration)
        {
            _serviceScopeFactory    =   serviceScopeFactory;
            _signInManager          =   signinMgr;
            _userManager            =   userMgr;
            _logger                 =   logger;
            _emailService           =   emailService;
            _configuration          =   configuration;
        }

        public IActionResult index()
        {
            ViewBag.menu = "setting";
            ViewBag.subMenu = "member";
            return View("View_Members");
        }

        [HttpPost("/getMemebers")]
        public IActionResult getMembers()
        {
            using (var scope = _serviceScopeFactory.CreateScope())  
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

        [HttpPost("/editMember")]
        public IActionResult editMember()
        {
            return Json(new { status = 201, message = "Updated the user info" });
        }

        [HttpPost("/deleteMemeber")]
        public IActionResult deleteMemeber(string strUserIdx)
        {
            if (string.IsNullOrWhiteSpace(strUserIdx))
            {
                return Json(new { status = -201, message = "Invalid shopify index" });
            }

            using (var scope = _serviceScopeFactory.CreateScope()) 
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