using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GoogleLogin.Services;
using GoogleLogin.Models;
using WebSocketSharp;
using ShopifySharp;

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
        private readonly CompanyService             _companyService;
        private readonly MemberService              _memberService;

        public CompanyController(
            SignInManager<AppUser>  signinMgr,
            UserManager<AppUser>    userMgr,
            IServiceScopeFactory    serviceScopeFactory,
            EMailService            service,
            ILogger<HomeController> logger,
            IConfiguration          configuration,
            CompanyService          companyService,
            MemberService           memberSerivce)
        {
            _serviceScopeFactory    =   serviceScopeFactory;
            _signInManager          =   signinMgr;
            _userManager            =   userMgr;
            _logger                 =   logger;
            _emailService           =   service;
            _configuration          =   configuration;
            _companyService         =   companyService;
            _memberService          =   memberSerivce;
        }

        public IActionResult Index()
        {
            ViewBag.menu    = "setting";
            ViewBag.subMenu = "company";
            return View("View_Companies");
        }

        [HttpPost("/getCompanies")]
        public IActionResult getCompanies()
        {
            using (var scope = _serviceScopeFactory.CreateScope()) 
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                string strUserId = _userManager.GetUserId(HttpContext.User) ?? "";

                var companyList = (from company in _dbContext.TbCompanies
                                         join member in _dbContext.TbMembers
                                         on company.id equals member.companyIdx  // Assuming UserId is the common field
                                         where member.userIdx == strUserId
                                         select company).ToList();

                ViewBag.companyList = companyList;
                return PartialView("View_CompanyList");
            }
        }

        [HttpPost("/addCompany")]
        public IActionResult addCompany(string companyName, string companySite)
        {
            string userIdx = _userManager.GetUserId(HttpContext.User) ?? string.Empty;
            if (userIdx.IsNullOrEmpty())
            {
                return Json(new { status = -201, message = "Adding failed" });
            }

            if ( companyName.IsNullOrEmpty() )
            {
                return Json(new { status = -201, message = "Company name is required!" });
            }


            if (_companyService.getCompany(companyName) != null )
            {
                return Json(new { status = -201, message = "Company name already exists." });
            }

            long companyIdx = _companyService.addCompany(companyName, companySite);

            if (companyIdx > 0)
            {
                _memberService.addMember(userIdx, companyIdx, 0);
                return Json(new { status = 201, message = "Adding success" });
            } else
            {
                return Json(new { status = -201, message = "Adding failed" });
            }
        }

        [HttpPost("/editCompany")]
        public IActionResult editCompany()
        {
            return Json(new { status = -201, message = "Updating failed" });
        }

        [HttpPost("/deleteCompany")]
        public IActionResult deleteCompany(long companyIdx)
        {
            var _one = _companyService.getCompanyByIdx(companyIdx);

            if (_one == null)
            {
                return Json(new { status = -201, message = "Deleting is failed" });
            }

            int nRet = _companyService.deleteCompany(_one);

            if (nRet > 0)
            {
                _memberService.deleteMemeberByCompanyId(companyIdx);
                return Json(new { status = 201, message = "Deleting Success" });
            } else
            {
                return Json(new { status = -201, message = "Deleting is failed" });
            }
        }
    }
}