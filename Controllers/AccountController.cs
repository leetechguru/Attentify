using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ShopifyService = GoogleLogin.Services.ShopifyService;

namespace GoogleLogin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private UserManager<AppUser> userManager;
        private SignInManager<AppUser> signInManager;
        private readonly EMailService _emailService;
        private readonly ShopifyService _shopifyService;
        private readonly AppIdentityDbContext _dbContext;
        private readonly ModelService _modelService;
        private readonly string _phoneNumber;
        public AccountController(UserManager<AppUser> userMgr, SignInManager<AppUser> signinMgr, EMailService context, AppIdentityDbContext dbContext, ShopifyService shopifyService, ModelService modelService, IConfiguration configuration)
        {
            userManager = userMgr;
            signInManager = signinMgr;
            _emailService = context;
            _dbContext = dbContext;
            _shopifyService = shopifyService;
            _modelService = modelService;
            _phoneNumber = configuration["Twilio:PhoneNumber"];
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            Login login = new Login();
            login.ReturnUrl = returnUrl;
            return View(login);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login login)
        {
            if (ModelState.IsValid)
            {
                AppUser appUser = await userManager.FindByEmailAsync(login.Email);
                if (appUser != null)
                {
                    await signInManager.SignOutAsync();
                    Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(appUser, login.Password, login.Remember, false);
                    if (result.Succeeded)
                        return Redirect(login.ReturnUrl ?? "/");

                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToAction("LoginTwoStep", new { appUser.Email, login.ReturnUrl });
                    }

                    bool emailStatus = await userManager.IsEmailConfirmedAsync(appUser);
                    if (emailStatus == false)
                    {
                        ModelState.AddModelError(nameof(login.Email), "Email is unconfirmed, please confirm it first");
                    }

                    if (result.IsLockedOut)
                        ModelState.AddModelError("", "Your account is locked out. Kindly wait for 10 minutes and try again");
                }
                ModelState.AddModelError(nameof(login.Email), "Login Failed: Invalid Email or password");
            }
            return View(login);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            string redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            ExternalLoginInfo info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
            var access_token = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
            
            if (!string.IsNullOrEmpty(access_token))
            {
                new Thread(async () =>
                {
                    int nCnt = 500;
                    //while (true)
                    {
                        List<string> lstLabelIds = await _emailService.GetMailList(access_token, info.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "", nCnt);
                        await _emailService.SubscribeToPushNotifications(access_token);
                    }
                }).Start();
                new Thread(async () => {
                    await _shopifyService.OrderRequest();
                }).Start();
                new Thread(async () => {
                    await _shopifyService.CustomersRequest();
                }).Start();
                new Thread(async () =>
                {
                    try
                    {
                        var user = await userManager.GetUserAsync(User);
                        string strPhone = _phoneNumber;
                        if (user != null && string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            user.PhoneNumber = strPhone;
                        }
                        else
                        {
                            strPhone = _phoneNumber;
                        }

                        await _modelService.GetMessages(strPhone);
                        await _modelService.SendSmsCountInfo(strPhone);
                    }catch(Exception ex)
                    {
                        Console.WriteLine("in account/googleResponse thread" + ex.ToString());
                    }
                }).Start();
                HttpContext.Session.SetString("AccessToken", access_token);
                
            }

            if (result.Succeeded)
                return Redirect("/home/index");
            else
            {
                AppUser user = new AppUser
                {
                    Email = info.Principal.FindFirst(ClaimTypes.Email)?.Value,
                    UserName = info.Principal.FindFirst(ClaimTypes.Name)?.Value
                };

                IdentityResult identResult = await userManager.CreateAsync(user);
                if (identResult.Succeeded)
                {
                    identResult = await userManager.AddLoginAsync(user, info);
                    if (identResult.Succeeded)
                    {
                        await signInManager.SignInAsync(user, false);
                        return Redirect("/home/index");
                    }
                }
                return AccessDenied();
            }
        }        

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            if (!ModelState.IsValid)
                return View(email);

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToAction(nameof(ForgotPasswordConfirmation));

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var link = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            EmailHelper emailHelper = new EmailHelper();
            bool emailResponse = emailHelper.SendEmailPasswordReset(user.Email, link);

            if (emailResponse)
                return RedirectToAction("ForgotPasswordConfirmation");
            else
            {
                // log email failed 
            }
            return View(email);
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            var model = new ResetPassword { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPassword resetPassword)
        {
            if (!ModelState.IsValid)
                return View(resetPassword);

            var user = await userManager.FindByEmailAsync(resetPassword.Email);
            if (user == null)
                RedirectToAction("ResetPasswordConfirmation");

            var resetPassResult = await userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
            if (!resetPassResult.Succeeded)
            {
                foreach (var error in resetPassResult.Errors)
                    ModelState.AddModelError(error.Code, error.Description);
                return View();
            }

            return RedirectToAction("ResetPasswordConfirmation");
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
