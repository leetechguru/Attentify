using Azure.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopifySharp;
using ShopifySharp.GraphQL;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using ShopifyService = GoogleLogin.Services.ShopifyService;

namespace GoogleLogin.Controllers
{
	public class ShopifyController : Controller
	{
		private readonly string                             _clientId;
		private readonly string                             _clientSecret;
		private readonly string                             _domain;
        private readonly UserManager<AppUser>               _userManager;
		private readonly ShopifyService                     _shopifyService;
        private readonly EMailService                       _emailService;
        private readonly ILogger<ShopifyController>         _logger;
        private const int PerPageCnt = 10;

        public ShopifyController(
            UserManager<AppUser>        userMgr, 
            ShopifyService              shopifyService, 
            EMailService                emailService,
            IConfiguration              configuration, 
            ILogger<ShopifyController>  logger)
        {
            _clientId               = configuration["Shopify:clientId"]  ?? "";
            _clientSecret           = configuration["Shopify:ApiSecret"] ?? "";
            _domain                 = configuration["Domain"] ?? "";
#if DEBUG
            _clientId               = configuration["shopify_test:clientId"] ?? "";
            _clientSecret           = configuration["shopify_test:ApiSecret"] ?? "";
            _domain                 = configuration["Domain"] ?? "";
#endif
            _userManager            = userMgr;
            _shopifyService         = shopifyService;
            _emailService           = emailService;
            _logger                 = logger;
        }

		[HttpGet]
		public async Task<IActionResult> Orders(string store, int nPageNo = 0)
		{
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
#if DEBUG
                user = new AppUser();
				user.Email = "sherman@zahavas.com";
#else
                return RedirectToAction("Login");
#endif
            }

            ViewBag.scripts = new List<string>(){"/js/orders.js"};
            ViewBag.Store = store;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Order(string store, int nPageNo = 0)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
#if DEBUG
                user = new AppUser();
                user.Email = "sherman@zahavas.com";
#else
                return PartialView("Order");
#endif
            }

            if (nPageNo <= 0)
            {
                nPageNo = 1;
            }
            int nRecCnt = await _shopifyService.GetOrdersPerPageCnt(store, nPageNo, PerPageCnt);
            int nPageCnt = nRecCnt / PerPageCnt + 1;
            if (nPageNo >= nPageCnt)
            {
                nPageNo = nPageCnt;
            }

            List<TbOrder> lstOrders = await _shopifyService.GetOrders(store, nPageNo, PerPageCnt);

            ViewBag.orders      = lstOrders;
            ViewBag.PageNo      = nPageNo;
            ViewBag.AllCnt      = nRecCnt;
            ViewBag.PageCnt     = nPageCnt;
            ViewBag.PagePerCnt  = PerPageCnt;
            ViewBag.Store       = store;

            return PartialView("Order");
        }

        [HttpPost("shopify/auth")]
        public IActionResult Authenticate(string shop)
        {
            ShopifyAuthHelper pHelper = new ShopifyAuthHelper(_clientId, _clientSecret);
            if (string.IsNullOrEmpty(shop))
            {
                shop = "punkcaseca.myshopify.com";
            }
            //var authUrl = pHelper.BuildAuthorizationUrl(shop, $"https://{_domain}/shopify/install");
            var authUrl = "https://admin.shopify.com/oauth/install_custom_app?client_id=80dc7ad1a7ce6a47857f7f85479d3c23&no_redirect=true&signature=eyJleHBpcmVzX2F0IjoxNzM3MzkxNTI2LCJwZXJtYW5lbnRfZG9tYWluIjoicHVua2Nhc2UubXlzaG9waWZ5LmNvbSIsImNsaWVudF9pZCI6IjgwZGM3YWQxYTdjZTZhNDc4NTdmN2Y4NTQ3OWQzYzIzIiwicHVycG9zZSI6ImN1c3RvbV9hcHAiLCJtZXJjaGFudF9vcmdhbml6YXRpb25faWQiOjEwNjkzN30%3D--0d6519ffa58c4afd91448f237ac16cf458eb37f8";
            //var authUrl = "https://admin.shopify.com/oauth/install_custom_app?client_id=6616d5ec22eae6be7aad9309f17365cf&no_redirect=true&signature=eyJleHBpcmVzX2F0IjoxNzMyODM5NDA1LCJwZXJtYW5lbnRfZG9tYWluIjoicHVua2Nhc2VjYS5teXNob3BpZnkuY29tIiwiY2xpZW50X2lkIjoiNjYxNmQ1ZWMyMmVhZTZiZTdhYWQ5MzA5ZjE3MzY1Y2YiLCJwdXJwb3NlIjoiY3VzdG9tX2FwcCIsIm1lcmNoYW50X29yZ2FuaXphdGlvbl9pZCI6MTA2OTM3fQ%3D%3D--8a24ef4a46224af218252c17c0af411d6cb9556f";
            return Json(new { status = 201, authorizationUrl = authUrl });
        }

        [HttpGet("shopify/callback")]
        public async Task<IActionResult> Callback(string shop, string code, string hmac)
        {
            var authHelper = new ShopifyAuthHelper(_clientId, _clientSecret);
            var accessToken = await authHelper.ExchangeCodeForAccessToken(shop, code);

            string userId = _userManager.GetUserId(HttpContext.User) ?? "";
          
            await _shopifyService.SaveAccessToken(userId, shop, accessToken);
            await _shopifyService.RegisterHookEntry(shop, accessToken);
            return RedirectToAction("shopifymanage", "setting");
        }

        [HttpGet("shopify/install")]
        public IActionResult Install(string host, string hmac, string shop, string state)
        {
            if (!VerifyHmac(hmac, Request.Query, _clientSecret))
            {
                return BadRequest("Invalid HMAC signature");
            }
            try
            {
                string decodeHost = DecodeHost(host);
            
                if (string.IsNullOrEmpty(decodeHost) || string.IsNullOrEmpty(shop))
                {
                    return BadRequest("Required parameters missing");
                }
                var authHelper = new ShopifyAuthHelper(_clientId, _clientSecret);
                string strRedirectUrl = authHelper.BuildAuthorizationUrl(decodeHost, $"https://{_domain}/shopify/callback");
                return Redirect(strRedirectUrl);
            }
            catch(Exception ex)
            {
                Console.WriteLine("shopify/install" + ex.Message);
            }

            return BadRequest("Failed to retrieve access token");
        }

        private string DecodeHost(string base64Host)
        {
            try
            {
                // Add padding if necessary
                if (base64Host.Length % 4 != 0)
                {
                    base64Host = base64Host.PadRight(base64Host.Length + (4 - base64Host.Length % 4), '=');
                }

                byte[] data = Convert.FromBase64String(base64Host);
                return Encoding.UTF8.GetString(data);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error decoding Base64 host: {ex.Message}");
                return null;
            }
        }

        private bool VerifyHmac(string hmac, IQueryCollection query, string sharedSecret)
        {
            // Extract query parameters except 'hmac'
            var sortedParams = query
                .Where(kvp => kvp.Key != "hmac")
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}={kvp.Value}");

            var data = string.Join("&", sortedParams);

            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret)))
            {
                var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                var computedHmac = BitConverter.ToString(hash).Replace("-", "").ToLower();

                return hmac == computedHmac;
            }
        }

        public async Task<IActionResult> RefreshOrder(string strStore)
        {
            string strToken = await _shopifyService.GetAccessTokenByStore(strStore);
            if(string.IsNullOrEmpty(strToken))
            {
                return await Order(strStore);
            }

            await _shopifyService.OrderRequest(strStore, strToken);
            return await Order(strStore);
        }

        /********************************webhook****************************************/
        [HttpPost("shopify/order_create")]
        public async Task<IActionResult> OrderCreate()
        {
            try
            {
                string requestBody;
                using (var reader = new StreamReader(Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                string hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"];

                if (!VerifyWebhook(requestBody, hmacHeader, _clientSecret))
                {
                    return Unauthorized("Invalid HMAC signature");
                }

                Console.WriteLine($"Webhook received: {requestBody}");
                _logger.LogInformation($"Webhook create request : {requestBody}");
                await _shopifyService.SaveNewOrder(requestBody);
                return Ok();
            }
            catch (Exception ex)
            {
                // Log or handle errors
                Console.WriteLine($"Error processing webhook: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpPost("shopify/order_cancelled")]
        public async Task<IActionResult> OrderCancelled()
        {
            try
            {
                string requestBody;
                using (var reader = new StreamReader(Request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                string hmacHeader = Request.Headers["X-Shopify-Hmac-Sha256"];

                if (!VerifyWebhook(requestBody, hmacHeader, _clientSecret))
                {
                    return Unauthorized("Invalid HMAC signature");
                }

                Console.WriteLine($"Webhook received: {requestBody}");
                _logger.LogInformation($"Webhook cancelled request : {requestBody}");
                await _shopifyService.SaveNewOrder(requestBody);
                return Ok();
            }
            catch (Exception ex)
            {
                // Log or handle errors
                Console.WriteLine($"Error processing webhook: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        private bool VerifyWebhook(string requestBody, string hmacHeader, string sharedSecret)
        {
            var encoding = new System.Text.UTF8Encoding();
            var key = encoding.GetBytes(sharedSecret);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(key))
            {
                var hash = hmac.ComputeHash(encoding.GetBytes(requestBody));
                var calculatedHmac = Convert.ToBase64String(hash);

                return calculatedHmac == hmacHeader;
            }
        }
    }
}
