using Azure.Core.Serialization;
using GoogleLogin.Models;
using GoogleLogin.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace GoogleLogin.Controllers
{

    //[Authorize]
    public class CallController : Controller
    {
        private readonly UserManager<AppUser>   _userManager;
        private readonly ILogger<SmsController> _logger;
        private readonly LLMService             _llmService;
        private readonly string                 _phoneNumber;
        private readonly ShopifyService         _shopifyService;
        private readonly SmsService _smsService;
        private readonly IConfiguration         _configuration;
        private string                 blandUrl;
        public CallController(
            UserManager<AppUser> userManager,
            LLMService llmService,
            SmsService smsService,
            ILogger<SmsController> logger,
            IConfiguration configuration,
            ShopifyService shopifyService)
        {
            _userManager    =   userManager;
            _configuration  = configuration;
            _llmService     =   llmService;
            _smsService = smsService;
            _logger         =   logger;
            _phoneNumber    =   "+18888179263";
            _shopifyService =   shopifyService;
            blandUrl = $"https://api.bland.ai/v1/inbound/{_phoneNumber}";
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("View_BlandManage");
        }

        [HttpPost("/configurebland")] 
        public async Task<IActionResult> configureBland(string prompt)
        {
            Console.WriteLine($"Prompt is :{prompt}");
            prompt = "Goal: Receiving call from customers. They want to refund(cancel, know or else) their order. Confirm the order id and what they want with their order.\r\n\r\nCall Flow:\r\n1. Introduce yourself and say greeting.\r\n2. Ask customer what he/she wants with his/her order?\r\n3. Confirm order id.\r\n4. Greeting.\r\n\r\nBackground:\r\nMy name is Sherman. I am an assistant of attentify customer service. I receive a call from customer who they want something with their order.\r\nI confirm the order id and what they are looking for with.\r\n\r\nHere’s an example dialogue:\r\n\r\nYou: Thank you for calling attentify customer service? My name is Sherman, how can I assist you today?\r\nPerson: Hi Sherman, I need some help with an order I recently placed.\r\nYou: Sure, I'd be happy to help. Could I get your name and order number to pull up your account?\r\nPerson: It's John Smith, order #12345\r\nYou: Thanks for that information. What do you need with your order?\r\nPerson: I want to refund the order.(I want to know the status of the order or I want to cancel my order)\r\nYou: Got it. I will process it. You should receive a confirmation email shortly.\r\nPerson: Thank you, I really appreciate you taking care of this so quickly!\r\nYou: You're very welcome! Thank you for being a valued attentify customer. Please reach back out if you have any other issues with your order.\r\n    Have a great rest of your day!\r\nPerson: You too, bye!";
            

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, blandUrl);
            request.Headers.Add("Authorization", "org_7df5d48b85099174c6241f40331d4a22f05c15c24d0ff240f2850c36f2db0ce5308ea74110d10eea928369");
            //org_dffbc34bfffb310f4f2fae9c053b6e663803512b77de2d03849589597b5a98a313499da5f9011c6fd36b69
            request.Headers.Add("encrypted_key", "a8c06ebc-da85-4ea7-b5cd-ae5d9aee6275");

            var payload = new
            {
                prompt = prompt,
                pathway_id = string.Empty,
                voice = "josh",
                background_track = "office",//string.Empty,
                first_sentence = string.Empty,
                wait_for_greeting = true,
                interruption_threshold = 123,
                model = "enhanced",
                tools = Array.Empty<Object>(),
                language = "en-US",
                timezone = "America/Los_Angeles",
                //transfer_phone_number = string.Empty,
                //transfer_list = new {},
                //dynamic_data = new[] { "" },
                //keywords = Array.Empty<string>(),
                max_duration = 123,
                webhook = "https://9844-107-172-242-4.ngrok-free.app/receivebland",
                //analysis_schema = new {},
                //metadata = new { },
                //summary_prompt = string.Empty,
                //analysis_prompt = "",
                record = true
            };
            string jsonContent = JsonConvert.SerializeObject(payload);

            var content = new StringContent(jsonContent);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;

            var response = await client.SendAsync(request);
            Console.WriteLine(response.ToJson());
            if (response.IsSuccessStatusCode)
            {
             

                return Json(new { status = 201, message = "Update Ok" });
            }
            else
            {
                return Json(new { status = 201, message = "Update fail" });
            }
            
        }

        [HttpPost("/deletebland")]
        public async Task<IActionResult> deleteBland(string prompt)
        {
            Console.WriteLine($"Prompt is :{prompt}");

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{blandUrl}/delete");
            request.Headers.Add("Authorization", "org_7df5d48b85099174c6241f40331d4a22f05c15c24d0ff240f2850c36f2db0ce5308ea74110d10eea928369");
            request.Headers.Add("encrypted_key", "a8c06ebc-da85-4ea7-b5cd-ae5d9aee6275");

            var payload = new
            {
            };
            string jsonContent = JsonConvert.SerializeObject(payload);

            var content = new StringContent(jsonContent);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { status = 201, message = "delete Ok" });
            }
            else
            {
                return Json(new { status = 201, message = "delete fail" });
            }
        }

        [HttpPost("/insertbland")]
        public async Task<IActionResult> insertBland(string prompt)
        {
            Console.WriteLine($"Prompt is :insert");

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.bland.ai/v1/inbound/insert");
            request.Headers.Add("Authorization", "org_7df5d48b85099174c6241f40331d4a22f05c15c24d0ff240f2850c36f2db0ce5308ea74110d10eea928369");
            request.Headers.Add("encrypted_key", "a8c06ebc-da85-4ea7-b5cd-ae5d9aee6275");

            var payload = new
            {
                numbers = new[] { "+18888179263" }
            };
            string jsonContent = JsonConvert.SerializeObject(payload);

            var content = new StringContent(jsonContent);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { status = 201, message = "Insert Ok" });
            }
            else
            {
                return Json(new { status = 201, message = "Insert fail" });
            }
        }

        [HttpPost("/receivebland")]
        public async Task<IActionResult> ReceiveBland()
        {
            using (var reader = new StreamReader(Request.Body))
            {
                string requestBody = await reader.ReadToEndAsync();

                JObject jsonObject = JObject.Parse(requestBody);

                string callId = jsonObject["call_id"]?.ToString() ?? string.Empty;
                string from = jsonObject["from"]?.ToString() ?? string.Empty;
                string to = jsonObject["to"]?.ToString() ?? string.Empty;

                List<TbSms> extractedData = new List<TbSms>();
                JArray transcripts = (JArray)jsonObject["transcripts"];

                if ( transcripts != null )
                {
                    foreach( var item in transcripts)
                    {
                        var user = item["user"]?.ToString() ?? string.Empty;

                        TbSms p = new TbSms
                        {
                            sm_id = callId,
                            sm_from = user == "assistant" ? to : from,
                            sm_to = user == "assistant" ? from : to,
                            sm_body = item.ToString(),
                            sm_date = DateTime.Now,
                            sm_read = 0
                        };

                        await _smsService.SaveSms(p);
                        Console.WriteLine("Save Sms");
                    }
                }
            }
            return Ok();
        }
    }    
}
