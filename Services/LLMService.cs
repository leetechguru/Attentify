
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Google.Cloud.PubSub.V1;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoogleLogin.Services
{
    public class LLMService
    {
        ILogger<LLMService> logger;
        public LLMService(ILogger<LLMService> _logger)
        {
            this.logger = _logger;
        }

        public async Task<string> GetResponseLLM(string strBody)
        {
            try
            {
                var client = new AnthropicClient("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"); // for client
                string strTxt = $"The following text is an order, cancellation, or refund email encoded in Base64 from a Shopify customer. " +
                    $"Please check if the order ID field exists and is correct. If the email is correct, then output the necessary string formatted as JSON. " +
                    $"The JSON string should include order_id, type (either cancel or refund), status (1 if correct, otherwise 0), and msg " +
                    $"(a message requesting the order ID if the email is incorrect; if the email is correct, msg should be null). I need only the JSON output: {strBody}";

                var messages = new List<Message>()
                {
                    //new Message(RoleType.User, $"This is a Gmail body string encoded in Base64. Please provide just the reply content. I don't need your words like 'Based on the decoded email content, here is the key reply content extracted:' It's indeed unncessary. {strBody}") 
                    new Message(RoleType.User, strTxt)
                };

                var parameters = new MessageParameters()
                {
                    Messages = messages,
                    MaxTokens = 2048,
                    Model = AnthropicModels.Claude35Sonnet,
                    Stream = false,
                    Temperature = 1.0m,
                };
                var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);
                logger.LogInformation($"LLMService/GetResponseLLM {strBody}");
                logger.LogInformation($"LLMService: ${finalResult.Message}");
                Console.WriteLine(finalResult.Message.ToString());
                return finalResult.Message.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError("llmservice/getresponseasync: " + ex.ToString());
                Console.WriteLine("llmservice/getresponseasync" + ex.ToString());
            }
            return "";
        }

        public static async Task<string> GetResponseAsync(string strBody, string strUserName, EMailService _emailService = null)
        {
            try
            {
                string strOcrText = await ExtractTextOcr(_emailService, strBody);
                if(!string.IsNullOrEmpty(strOcrText))
                {
                    strBody = $"{strBody}\n{strOcrText}";
                }

				var client = new AnthropicClient("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"); // for client
                string strTxt = $"The following text is an order, cancellation, or refund email encoded in Base64 from a Shopify customer. " +
                    $"Please check if the order ID field exists and is correct. If the email is correct, then output the necessary string formatted as JSON. " +
                    $"The JSON string should include order_id, type (either cancel or refund), status (1 if correct, otherwise 0), and msg " +
                    $"(a message requesting the order ID if the email is incorrect; if the email is correct, msg should be null). I need only the JSON output: {strBody}";
                    
                var messages = new List<Message>()
                {
                    //new Message(RoleType.User, $"This is a Gmail body string encoded in Base64. Please provide just the reply content. I don't need your words like 'Based on the decoded email content, here is the key reply content extracted:' It's indeed unncessary. {strBody}") 
                    new Message(RoleType.User, strTxt) 
                };
            
                var parameters = new MessageParameters()
                {
                    Messages = messages,                
                    MaxTokens = 2048,
                    Model = AnthropicModels.Claude35Sonnet,
                    Stream = false,
                    Temperature = 1.0m,
                };
                var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);
            
                Console.WriteLine(finalResult.Message.ToString());
                return finalResult.Message.ToString();
            }catch(Exception ex)
            {
                Console.WriteLine("llmservice/getresponseasync" + ex.ToString());
            }
            return "";
        }

        public static async Task<string> GetResponseAsyncOnlyText(string strBody)
        {
            try
            {
                var client = new AnthropicClient("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"); // for client
                string strTxt = $"The following text is an order, cancellation, or refund email encoded in Base64 from a Shopify customer. " +
                    $"Please check if the order ID field exists and is correct. If the email is correct, then output the necessary string formatted as JSON. " +
                    $"The JSON string should include order_id, type (either cancel or refund), status (1 if correct, otherwise 0), and msg " +
                    $"(a message requesting the order ID if the email is incorrect; if the email is correct, msg should be null). I need only the JSON output: {strBody}";

                var messages = new List<Message>()
                {
                    //new Message(RoleType.User, $"This is a Gmail body string encoded in Base64. Please provide just the reply content. I don't need your words like 'Based on the decoded email content, here is the key reply content extracted:' It's indeed unncessary. {strBody}") 
                    new Message(RoleType.User, strTxt)
                };

                var parameters = new MessageParameters()
                {
                    Messages = messages,
                    MaxTokens = 2048,
                    Model = AnthropicModels.Claude35Sonnet,
                    Stream = false,
                    Temperature = 1.0m,
                };
                var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);

                Console.WriteLine(finalResult.Message.ToString());
                return finalResult.Message.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("llmservice/getresponseasync" + ex.ToString());
            }
            return "";
        }

        public static async Task<string> GetResponseAsync(string strBody)
        {
            try
            {
                var client = new AnthropicClient("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"); // for client
                string strTxt = $"The following text is an order, cancellation, or refund SMS from a Shopify customer. " +
                    $"Please check if the order ID field exists and is correct. If the SMS is correct, then output the necessary string formatted as JSON. " +
                    $"The JSON string should include order_id, type (either cancel or refund), status (1 if correct, otherwise 0), and msg " +
                    $"(a message requesting the order ID if the SMS is incorrect; if the SMS is correct, msg should be null). I need only the JSON output: {strBody}";
               
                var messages = new List<Message>()
                {                    
                    new Message(RoleType.User, strTxt)
                };

                var parameters = new MessageParameters()
                {
                    Messages = messages,
                    MaxTokens = 2048,
                    Model = AnthropicModels.Claude35Sonnet,
                    Stream = false,
                    Temperature = 1.0m,
                };
                var finalResult = await client.Messages.GetClaudeMessageAsync(parameters);
                
                Console.WriteLine(finalResult.Message.ToString());
                return finalResult.Message.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return "";
        }


        private static async Task<string> ExtractTextOcr(EMailService emailservice, string strBody)
        {
            string _strBody = emailservice.GetMailBodyAsHtml(strBody);
			var imgMatch = Regex.Match(_strBody, @"<img[^>]*src=""data:image\/[a-zA-Z]+;base64,([^""]+)""");

			if (imgMatch.Success)
			{
				var base64ImageData = Convert.FromBase64String(imgMatch.Groups[1].Value);
                var strExtractText = OcrService.ExtractTextFromImage(base64ImageData);
                return strExtractText;
            }
            else
            {
				var imgUrlMatches = Regex.Matches(_strBody, @"<img[^>]*src=""(http[s]?:\/\/[^\s""]+)""");
                List<string> lstUrls = imgUrlMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                string strResult = "";
                foreach(var imgUrlMatch in lstUrls)
                {
					HttpClient HttpClient = new HttpClient();
					var imageBytes = await HttpClient.GetByteArrayAsync(imgUrlMatch);

                    string strExtrace = OcrService.ExtractTextFromImage(imageBytes);
                    if (string.IsNullOrWhiteSpace(strExtrace)) continue;
                    strResult = $"{OcrService.ExtractTextFromImage(imageBytes)}\n{strResult}";
				}
                return strResult;
			}
		}
    }

}
