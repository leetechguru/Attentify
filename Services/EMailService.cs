using Anthropic.SDK.Messaging;
using Azure.Core;
using Google.Api;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Cloud.PubSub.V1;
using GoogleLogin.Models;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using ShopifySharp;
using ShopifySharp.GraphQL;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Drawing.Printing;
using System.Net.Mail;
using System.Text;
using Tesseract;
using Twilio.TwiML.Messaging;
using Twilio.TwiML.Voice;
using static Google.Api.ResourceDescriptor.Types;
using MessagePart = Google.Apis.Gmail.v1.Data.MessagePart;
using Task = System.Threading.Tasks.Task;

namespace GoogleLogin.Services
{
    public class EMailService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IHubContext<DataWebsocket> _hubContext;
        private readonly ILogger<EMailService> _logger;
        private GmailService? _service = null;

		public EMailService(IServiceScopeFactory serviceScopeFactory, IHubContext<DataWebsocket> dataWebsocket, ILogger<EMailService> logger)
		{
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = dataWebsocket;
            _logger = logger;
        }
        
        public async Task<List<string>> GetMailInfo(GmailService service, string msgId, string strMyGmail)
        {
            var emailInfoRequest = service.Users.Messages.Get("me", msgId);
            var emailInfoResponse = await emailInfoRequest.ExecuteAsync();
            List<string> lstReturn = new List<string>();
            if (emailInfoResponse != null)
            {
                try
                {
                    string? body = "";
                    var _date = DateTime.Now;

                    var dateHeader = emailInfoResponse.Payload.Headers.FirstOrDefault(h => h.Name == "Date")?.Value;
                    if (DateTime.TryParse(dateHeader, out var emailDate))
                    {
                        _date = emailDate;
                    }

                    string _from = emailInfoResponse.Payload.Headers.Where(obj => obj.Name == "From").FirstOrDefault()?.Value ?? "";
                    string _to = emailInfoResponse.Payload.Headers.Where(obj => obj.Name == "To").FirstOrDefault()?.Value ?? "";
                    string? _subject = emailInfoResponse.Payload.Headers.Where(obj => obj.Name == "Subject").FirstOrDefault()?.Value;
                    _subject = string.IsNullOrEmpty(_subject) ? "No Subject" : _subject;

                    string? _inReplyTo = emailInfoResponse.Payload.Headers.Where(obj => obj.Name == "In-Reply-To").FirstOrDefault()?.Value;
                    string? _threadId = emailInfoResponse.ThreadId;
                    if (emailInfoResponse.LabelIds == null) return lstReturn;
                    bool isRead = !emailInfoResponse.LabelIds.Contains("UNREAD");
                    bool isInInbox = emailInfoResponse.LabelIds.Contains("INBOX");
                    bool isArchived = !isInInbox;
                    lstReturn = emailInfoResponse.LabelIds.ToList();

                    //Console.WriteLine(string.Join(',', emailInfoResponse.LabelIds));
                    if (_from != null)
                    {
                        if (emailInfoResponse.Payload.MimeType == "text/html")
                        {
                            body = emailInfoResponse.Payload.Body.Data;
                        }
                        else if (emailInfoResponse.Payload.MimeType == "multipart/alternative" || emailInfoResponse.Payload.MimeType == "multipart/mixed"
                            || emailInfoResponse.Payload.MimeType == "multipart/related")
                        {
                            body = emailInfoResponse.Payload.Parts.Where(o => o.MimeType == "text/html").FirstOrDefault()?.Body.Data;
                            if (body == null)
                            {
                                foreach (var part in emailInfoResponse.Payload.Parts)
                                {
                                    if (part.MimeType == "multipart/alternative")
                                    {
                                        //var _data = "";
                                        //foreach(var _part in part.Parts)
                                        //{
                                        //    if(_part.MimeType == "text/html" || _part.MimeType == "text/plain")
                                        //    {
                                        //        _data += GetMailBodyAsHtml(_part.Body.Data);
                                        //    }
                                        //}
                                        //if (!string.IsNullOrEmpty(_data))
                                        //{
                                        //    body = GetMailBodyAsHtml(_data);
                                        //}
                                    }
                                    else if (part.MimeType.StartsWith("image/"))
                                    {
                                        var _data = part.Body.Data;
                                    }
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(_to) || string.IsNullOrEmpty(_from)) return lstReturn;
                    TbEmail p = new TbEmail
                    {
                        em_id = msgId,
                        em_from = _from,
                        em_to = _to,
                        em_replay = _inReplyTo,
                        em_subject = _subject,
                        em_state = isArchived ? 3 : 0,
                        em_body = body,
                        em_threadId = _threadId,
                        em_level = 0,
                        em_date = _date,
                        em_read = isRead ? 1 : 0,
                    };

                    _ = Task.Run(async () => 
                    {
                        await WriteOne(p);
                        //await SendMailInfo(strMyGmail);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"EmailService {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }
            }
            return lstReturn;
        }

        private GmailService MakeGmailService(string access_token = "")
        {
            if (string.IsNullOrEmpty(access_token))
            {
                if (string.IsNullOrEmpty(Global.access_token)) return _service;
                access_token = Global.access_token;                
            }  

            var credential = GoogleCredential.FromAccessToken(access_token).CreateScoped(new string[] {
                "https://www.googleapis.com/auth/gmail.readonly",
                "https://www.googleapis.com/auth/gmail.modify"
            });

            if(_service == null)
            {
                _service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GoogleLogin"
                });

            }
            return _service;
        }
        public async Task<List<string>> GetMailList(string accessToken, string strUser, int nCnt = 10)
        {
            Global.access_token = accessToken;

            GmailService service = MakeGmailService(accessToken);
            long nWholeInboxCnt = await GetWholeMessageCnt(service);
            var request = service.Users.Messages.List("me");

            request.MaxResults = Math.Min(nWholeInboxCnt, nCnt);
            var messagesResponse = await request.ExecuteAsync();
            List<string> lstUsers = await GetCustomerMails();

            List<string> lstLabelIds = new List<string>();
            if (messagesResponse.Messages != null && messagesResponse.Messages.Count > 0)
            {
                foreach (var msg in messagesResponse.Messages)
                {
                    try
                    {
                        var lstItems = await GetMailInfo(service, msg.Id, strUser);
                        foreach(var item in lstItems)
                        {
                            if(!lstLabelIds.Contains(item) && item != "READ" && item != "UNREAD")
                                lstLabelIds.Add(item);
                        }
                    }
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
					}
				}
            }
            Global.lstLabelIds = lstLabelIds;
            return lstLabelIds;
        }
        
        private byte[] FromBase64ForUrlString(string base64Url)
        {
            string padded = base64Url.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }

        public string GetMailBodyAsHtml(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return "";
            byte[] byteArray = FromBase64ForUrlString(base64String);
            return System.Text.Encoding.UTF8.GetString(byteArray);
        }
        
        private async Task<long> GetWholeMessageCnt(GmailService _service)
        {
            if (_service == null) return 0;
            var request = _service.Users.Messages.List("me");
            request.LabelIds = "INBOX";  
            request.MaxResults = 1;

            ListMessagesResponse response = await request.ExecuteAsync();

            return response.ResultSizeEstimate ?? 0;
        }
       
        private async Task WriteOne(TbEmail p)
        {
            {
                using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
                {
                    var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    try
                    {
                        var _one = _dbContext.TbEmails.Where(e => e.em_id == p.em_id).FirstOrDefault();
                        if (_one == null)
                        {
                            _dbContext.Add(p);
                        }
                        else
                        {
                            _one.em_state = p.em_state;
                            _one.em_read = p.em_read;
                            _one.em_level = p.em_level;
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        private async Task<List<string>> GetCustomerMails()
        {
            using (var scope = _serviceScopeFactory.CreateScope())  // Create a new scope
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                try
                {
                    return _dbContext.TbShopifyUsers.Select(e => e.UserId).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
            return new List<string>();
        }

        public async Task<MailInfo> GetMailCount(string strGmail)
        {

            MailInfo pInfo = new MailInfo();
            
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    var result = dbContext.TbEmails
                        .Where(e => EF.Functions.Like(e.em_to, $"%{strGmail}%"))
                        .AsEnumerable() // Forces client-side evaluation after this point
                        .GroupBy(e => 1)
                        .Select(g => new MailInfo
                        {
                            nCntWhole = g.Count(),
                            nCntRead = g.Count(e => e.em_read == 1),
                            nCntUnread = g.Count(e => e.em_read == 0),
                            nCntOnTime = g.Count(e => e.em_state == 1 || (e.em_state == 0 && e.em_date.HasValue && (DateTime.Now - e.em_date.Value).Days < 7)),
                            nCntLate = g.Count(e => e.em_state == 0 && e.em_date.HasValue && (DateTime.Now - e.em_date.Value).Days >= 7 && (DateTime.Now - e.em_date.Value).Days < 30),
                            nCntDanger = g.Count(e => e.em_state == 0 && e.em_date.HasValue && (DateTime.Now - e.em_date.Value).Days > 30),
                            nCntArchived = g.Count(e => e.em_state == 3)
                        })
                        .FirstOrDefault();
                    var nCntReply = dbContext.TbEmails.Where(e => EF.Functions.Like(e.em_from, $"%{strGmail}")).AsEnumerable().Count();

                    result.nCntReply = nCntReply;
                    return result ?? pInfo;
                }
            }catch(Exception ex)
            {
                Console.WriteLine("emailservice/getmaincount(string) " + ex.ToString());
            }
            return pInfo;
        }

		public async Task<int> GetMailListPerPageCnt(string strGmail, int nPageNo, int nPerCnt, int em_state, string strSearch)
		{
            int nCnt = 0;
			try
			{
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    var query = dbContext.TbEmails.AsQueryable();

                    if (em_state != -1)
                    {
                        query = query.Where(e => e.em_state == em_state);
                    }

                    if (!string.IsNullOrEmpty(strSearch))
                    {
                        query = query.Where(e => (!string.IsNullOrEmpty(e.em_subject) && e.em_subject.Contains(strSearch)) ||
                                                  e.em_from.Contains(strSearch) || e.em_to.Contains(strSearch));
                    }

                    if (!string.IsNullOrEmpty(strGmail))
                    {
                        query = query.Where(e => e.em_to.Contains(strGmail) || e.em_from.Contains(strGmail));
                    }

                    // Execute the count query
                    nCnt = await query.CountAsync();

                    return nCnt;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
			return nCnt;
		}

        public int GetMailListPerUserCount(string strEmail, int nPerPage, int nType = 1)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    var rawEmails = dbContext.TbEmails.Where(e => e.em_to.Contains(strEmail)).ToList();
                    if (nType == 2)
                    {
                        rawEmails = dbContext.TbEmails.Where(e => e.em_state == 3 && e.em_to.Contains(strEmail)).ToList();
                    }
                    else if (nType == 1)
                    {
                        rawEmails = dbContext.TbEmails.Where(e => e.em_state == 0 && e.em_to.Contains(strEmail)).ToList();
                    }

                    var nCnt = rawEmails
                        .GroupBy(e => e.em_from)
                        .Select(g => new EmailDto
                        {
                            em_from = g.Key,
                            em_date = g.Max(e => e.em_date)
                        }).Count();

                    return nCnt / nPerPage + 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 0;
        }

        public List<TbEmail> GetMailListPerUser(string strEmail, int nPageIndex, int nPerPage, int nType = 1)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    var rawEmails = dbContext.TbEmails.Where(e => e.em_to.Contains(strEmail)).ToList();
                    if(nType == 2)
                    {
                        rawEmails = dbContext.TbEmails.Where(e => e.em_state == 3 && e.em_to.Contains(strEmail)).ToList();
                    }else if(nType == 1)
                    {
						rawEmails = dbContext.TbEmails.Where(e => e.em_state == 0 && e.em_to.Contains(strEmail)).ToList();
					}

                    var lstFrom = rawEmails
                        .GroupBy(e => e.em_from)
                        .Select(g => new EmailDto
                        {
                            em_from = g.Key,
                            em_date = g.Max(e => e.em_date)
                        })
                        .OrderByDescending(e => e.em_date)
                        .Skip(nPageIndex * nPerPage)
                        .Take(nPerPage).ToList();

                    List<TbEmail> lstResult = new List<TbEmail>();
                    foreach(var item in lstFrom)
                    {
                        var result = dbContext.TbEmails.Where(e => e.em_from == item.em_from && e.em_date == item.em_date).FirstOrDefault();
                        if (result == null) continue;
                        lstResult.Add(result);
                    }
                    return lstResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null;
        }

        public async Task<List<TbEmail>> GetMailListPerPage(string strGmail, int nPageNo, int nPerCnt, int em_state, string strSearch)
        {
            List<TbEmail> lstResult = new List<TbEmail>();
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    var query = dbContext.TbEmails.AsQueryable();

                    if (em_state != -1)
                    {
                        query = query.Where(e => e.em_state == em_state);
                    }

                    if (!string.IsNullOrEmpty(strSearch))
                    {
                        query = query.Where(e => (!string.IsNullOrEmpty(e.em_subject) && e.em_subject.Contains(strSearch)) ||
                                                  e.em_from.Contains(strSearch) || e.em_to.Contains(strSearch));
                    }

                    if (!string.IsNullOrEmpty(strGmail))
                    {
                        query = query.Where(e => e.em_to.Contains(strGmail) || e.em_from.Contains(strGmail));
                    }

                    lstResult = await query
                        .OrderByDescending(e => e.em_date) // Order by `EmDate` in descending order
                        .Skip((nPageNo - 1) * nPerCnt)    // Skip records based on the page number
                        .Take(nPerCnt)                    // Take only the number of records per page
                        .ToListAsync();

                    return lstResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return lstResult;            
        }

		public async Task<bool> MakeReadState(long em_idx, int em_read)
		{
			try
			{
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    var _one = dbContext.TbEmails.Where(e => e.em_idx == em_idx).FirstOrDefault();
                    if(_one != null) 
                    {
                        _one.em_read = em_read;
                        await dbContext.SaveChangesAsync();
                    }
                }
                return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
			return false;
		}

        public async Task<bool> ChangeStates(long[] em_idxs, int em_state)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                    List<TbEmail> lstOnes = await dbContext.TbEmails.Where(e => em_idxs.Contains(e.em_idx)).ToListAsync();

                    foreach(TbEmail e in lstOnes)
                    {
                        if(e == null)continue;
                        e.em_state = em_state;
                    }
                    await dbContext.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return false;
        }

		public async Task<bool> ChangeStates(string strGmail, int em_state, string strMyGmail)
		{
			try
			{
				using (var scope = _serviceScopeFactory.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
					List<TbEmail> lstOnes = dbContext.TbEmails.Where(e => (e.em_to.Contains(strGmail) && e.em_from.Contains(strMyGmail)) 
                    || (e.em_to.Contains(strMyGmail) && e.em_from.Contains(strGmail))).ToList();

					foreach (TbEmail e in lstOnes)
					{
						if (e == null) continue;
						e.em_state = em_state;
					}
					await dbContext.SaveChangesAsync();
					return true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
			return false;
		}
		public async Task<bool> ChangeState(long em_idx, int em_state)
		{
			try
			{
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                    TbEmail? _one = await dbContext.TbEmails.Where(e => e.em_idx == em_idx).FirstOrDefaultAsync();

                    if(_one != null)
                    {
                        _one.em_state = em_state;

                        await dbContext.SaveChangesAsync();
                    }
                    return true;
                }
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
			return false;
		}

        public async Task<bool> ChangeState(List<string> lstIds, int em_state)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    List<TbEmail?> lst = dbContext.TbEmails.Where(e => lstIds.Contains(e.em_id)).ToList();
                    foreach(var _one in lst)
                    {
                        _one.em_state = em_state;
                    }
                    await dbContext.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return false;
        }

        public async Task<bool> MakeAllRead()
		{
			try
			{
				using (var scope = _serviceScopeFactory.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

					dbContext.TbEmails.Where(e => e.em_read != 1)
					  .ToList()
					  .ForEach(e => e.em_read = 1);

					await dbContext.SaveChangesAsync();
				}
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
			return false;
		}

		public async Task<TbEmail?> GetMailInfo(long em_idx)
        {
            TbEmail? p = null;
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    p = await dbContext.TbEmails.Where(e => e.em_idx == em_idx).FirstOrDefaultAsync();

                    return p;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return p;
        }

		public TbEmail? GetMailInfo_(string strGMail, string strMyGMail)
		{
			TbEmail? p = null;
			try
			{
				using (var scope = _serviceScopeFactory.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

					p = dbContext.TbEmails.Where(e => e.em_from.Contains(strGMail) && e.em_to.Contains(strMyGMail) && !string.IsNullOrEmpty(e.em_body)).OrderByDescending(e => e.em_date).FirstOrDefault();

					return p;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
			}
			return p;
		}
		public async Task<CustomerInfo> GetCustomerInfo(string em_id)
        {
            CustomerInfo obj = new CustomerInfo(); 
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    TbEmail p = dbContext.TbEmails.Where(e => e.em_id == em_id).FirstOrDefault();
                    if (p == null) return null;
                    obj.strEmail = p.em_from;
                    obj.strSubject = string.IsNullOrEmpty(p.em_subject) ? $"New customer message " : p.em_subject;
                    obj.strSubject = $"{obj.strSubject} on {p.em_date?.ToString("MMM dd, hh:mm")}";
                    
                    TbShopifyUser _user = dbContext.TbShopifyUsers.Where(e => !string.IsNullOrEmpty(p.em_from) && p.em_from.Contains(e.UserId)).FirstOrDefault();
                    if (_user == null) return obj;
                    obj.strName = _user.UserName;
                    obj.strPhone = _user.phone;
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return obj;
        }

        private string GetEmailBody(string strBody)
        {            
            return @"
                <html>
                    <head>
                        <style>
                            body { font-family: Arial, sans-serif; }
                            .header { background-color: #4CAF50; color: white; text-align: center; padding: 20px; }
                            .body { padding: 20px; font-size: 16px; color: #333333; }
                            .footer { background-color: #f1f1f1; text-align: center; padding: 10px; font-size: 16px; color: #888888; }
                        </style>
                    </head>
                    <body>
                        <div class='email-container'>
                            <div class='header'><h1>Welcome to Our Attentify Service!</h1></div>
                            <div class='body'>                            
                                <p>" + strBody + @"</p>
                            </div>
                            <div class='footer'>
                                <p>Best regards, The Attnetify Team</p>                            
                            </div>
                        </div>
                    </body>
                </html>";
        }
        public async Task<bool> SendEmailAsync(string toUserEmail, string fromUserEmail, string accessToken, string subject, string body)
        {
            GmailService service = MakeGmailService(accessToken);
            
            var toAddress = MailboxAddress.Parse(toUserEmail);
            var fromAddress = MailboxAddress.Parse(fromUserEmail);
            var mimeMessage = new MimeKit.MimeMessage();
            mimeMessage.To.Add(toAddress);
            mimeMessage.From.Add(fromAddress);
            mimeMessage.Subject = subject;
            var bodyBuilder = new BodyBuilder { HtmlBody = GetEmailBody(body) };

           
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            var rawMessage = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mimeMessage.ToString()));
            rawMessage = rawMessage.Replace("+", "-").Replace("/", "_").Replace("=", "");

            var message = new Google.Apis.Gmail.v1.Data.Message
            {
                Raw = rawMessage,                
            };

            try
            {
                var result = await service.Users.Messages.Send(message, "me").ExecuteAsync();
                Console.WriteLine("Email sent successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
        //this function is read from Database
        public async Task<List<EmailExt>> GetGmailList(string strGmail, string strMyGmail)
        {
            List<EmailExt> lstReturn = new List<EmailExt>();
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    List<TbEmail> lst = dbContext.TbEmails.Where(e => ((e.em_from.Contains(strGmail) && e.em_to.Contains(strMyGmail)) || (e.em_to.Contains(strGmail) && e.em_from.Contains(strMyGmail))) && /*e.em_state == 0 && */!string.IsNullOrEmpty(e.em_body)).OrderBy(e => e.em_date).ToList();
                    
                    foreach(var email in lst)
                    {
                        var emailType = email.em_to.Contains(strMyGmail) ? 0 : 1;

                        string body = GetMailBodyAsHtml(email.em_body);
                        body = $"{body} <style>::-webkit-scrollbar {{\r\n    width: 7px;\r\n    height: 7px;\r\n}}\r\n\r\n::-webkit-scrollbar-button {{\r\n    display: none;\r\n}}\r\n\r\n::-webkit-scrollbar-track-piece {{\r\n    background: rgba(161,164,167,0.38) !important;\r\n}}\r\n\r\n::-webkit-scrollbar-thumb {{\r\n    height: 20px;\r\n    background-color: #a1a4a7;\r\n    border-radius: 4px;\r\n}}\r\n\r\n::-webkit-scrollbar-corner {{\r\n    background-color: #A1A4A7;\r\n}}\r\n\r\n::-webkit-resizer {{\r\n    background-color: #344055;\r\n}}</style>";
                        lstReturn.Add(
                            new EmailExt
                            {
                                em_id = email.em_id,
                                em_subject = email.em_subject,
                                em_from = email.em_from,
                                em_to = email.em_to,
                                em_date = email.em_date.Value,
                                em_body = body,
                                strLabel = new List<string>(),
                                nType = emailType,
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return lstReturn;
        }

        //this function is fetch from GMail using API service.
        public async Task<List<EmailExt>> GetGmailList(string strGmail, string accessToken, string strMyGmail)
        {
            GmailService service = MakeGmailService(accessToken);

            var query = $"from:{strGmail} OR to:{strGmail}";
			var request = service.Users.Messages.List("me");
			request.Q = query;
			request.MaxResults = 50;
			//request.LabelIds = new string[] { "INBOX", "SENT" };

			var response = await request.ExecuteAsync();
			var emails = new List<EmailExt>();

            if (response.Messages != null)
            {
				foreach (var message in response.Messages)
				{
					var email = await service.Users.Messages.Get("me", message.Id).ExecuteAsync();

					var subject = email.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(No Subject)";
					var from = email.Payload.Headers.FirstOrDefault(h => h.Name == "From")?.Value;
					var to = email.Payload.Headers.FirstOrDefault(h => h.Name == "To")?.Value;
					var dateHeader = email.Payload.Headers.FirstOrDefault(h => h.Name == "Date")?.Value;
					DateTime.TryParse(dateHeader, out var date);

					string body = GetEmailBody(email.Payload);
                    body = $"{body} <style>::-webkit-scrollbar {{\r\n    width: 7px;\r\n    height: 7px;\r\n}}\r\n\r\n::-webkit-scrollbar-button {{\r\n    display: none;\r\n}}\r\n\r\n::-webkit-scrollbar-track-piece {{\r\n    background: rgba(161,164,167,0.38) !important;\r\n}}\r\n\r\n::-webkit-scrollbar-thumb {{\r\n    height: 20px;\r\n    background-color: #a1a4a7;\r\n    border-radius: 4px;\r\n}}\r\n\r\n::-webkit-scrollbar-corner {{\r\n    background-color: #A1A4A7;\r\n}}\r\n\r\n::-webkit-resizer {{\r\n    background-color: #344055;\r\n}}</style>";

                    var labels = email.LabelIds;

                    var emailType = to.Contains(strMyGmail) ? 0 : 1;

                    emails.Add(
                        new EmailExt
                        {
                            em_id = email.Id,
                            em_subject = subject,
                            em_from = from,
                            em_to = to,
                            em_date = date,
                            em_body = body,
                            strLabel = labels.ToList(),
                            nType = emailType,
                        }
                    );
                }
			}
			return emails.OrderBy(e => e.em_date).ToList();
		}

		private string GetEmailBody(MessagePart Payload)
		{
			if (Payload == null) return string.Empty;

            string body = "";
			//if (!string.IsNullOrEmpty(payload.Body?.Data))
			//{
			//	return GetMailBodyAsHtml(payload.Body.Data);
			//}

			//if (payload.Parts != null && payload.Parts.Any())
			//{
			//	foreach (var part in payload.Parts)
			//	{
			//		if (part.MimeType == "text/plain" || part.MimeType == "text/html")
			//		{
			//			return GetMailBodyAsHtml(part.Body.Data);
			//		}
			//	}
			//}
			if (Payload.MimeType == "text/html")
			{
				body = Payload.Body.Data;
			}
			else if (Payload.MimeType == "multipart/alternative" || Payload.MimeType == "multipart/mixed"
				|| Payload.MimeType == "multipart/related")
			{
				body = Payload.Parts.Where(o => o.MimeType == "text/html").FirstOrDefault()?.Body.Data;
				if (body == null)
				{
					foreach (var part in Payload.Parts)
					{
						if (part.MimeType == "multipart/alternative")
						{
							
						}
						else if (part.MimeType.StartsWith("image/"))
						{
							var _data = part.Body.Data;
						}
					}
				}
			}
            if(string.IsNullOrEmpty(body))
            {
			    return string.Empty;
            }
            return GetMailBodyAsHtml(body);
		}

		public async Task SubscribeToPushNotifications(string accessToken)
        {
            GmailService service = MakeGmailService(accessToken);
            
            var request = service.Users.Watch(new WatchRequest()
            {
#if DEBUG

                TopicName = "projects/extended-bongo-444622-u5/topics/pubsub",


#else
                TopicName = "projects/plasma-galaxy-444920-h0/topics/attentify",//"projects/grand-proton-441717-c4/topics/attentify",
#endif

                LabelFilterAction = "INCLUDE",
                LabelIds = Global.lstLabelIds,//new List<string> { "INBOX", "UNREAD", "READ", "CATEGORY_SOCIAL", "CATEGORY_UPDATES", "CATEGORY_PROMOTIONS", "SENT"},
            }, "me");;
            
            try
            {
                _logger.LogInformation(string.Join(',', Global.lstLabelIds));
                var response = request.Execute();
                _logger.LogInformation("Watch request successful, resource id: " + response);
                Console.WriteLine("Watch request successful, resource id: " + response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error: " + ex.Message);
                Console.WriteLine("Error: " + ex.Message);
            }            
        }

		public async Task SendMailInfo(string strGmail)
		{
            if (!string.IsNullOrEmpty(strGmail))
            {
                MailInfo pMailInfo = await GetMailCount(strGmail);
                var objPacket = new
                {
                    MailInfo = pMailInfo,
                    type = "mail"
                };                
                string strJson = System.Text.Json.JsonSerializer.Serialize(objPacket);
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", "", strJson);
            }
        }

        private void RemoveEmail(string em_id, string userId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

                    TbEmail p = dbContext.TbEmails.Where(e => e.em_id == em_id).FirstOrDefault();
                    //if(p != null)
                    //{
                    //    dbContext.TbEmails.Remove(p);
                    //    this.SendMailInfo(userId);
                    //}
                    //else
                    {
                        GmailService _p = MakeGmailService();
                        GetMailInfo(_p, em_id, userId);
                    }

                }
            }catch(Exception ex)
            {
                _logger.LogError("emailservice/removeEmail " + ex.Message);
            }            
        }

        public async Task<ulong> UpdateEmail(ulong msgId, string strUserEmail)
        {
            GmailService _service = MakeGmailService();
            if (_service == null) return 0;
            try
            {
                //var request = _service.Users.Messages.Get("me", $"{msgId}");

                //var messagesResponse = await request.ExecuteAsync();
                var profile = _service.Users.GetProfile("me").Execute();
                var currentHistoryId = profile.HistoryId;


                UInt64 lMsgId = msgId;
                var historyRequest = _service.Users.History.List("me");
                historyRequest.StartHistoryId = lMsgId > currentHistoryId ? currentHistoryId : lMsgId;
                var historyResponse = historyRequest.Execute();
                if (historyResponse != null)
                {
                    if (historyResponse.History == null && historyResponse.HistoryId != null)
                    {
                        return historyResponse.HistoryId.Value;
                    }
                }
                if (historyResponse.History == null || !historyResponse.History.Any())
                {
                    Console.WriteLine("No history records found.");
                    return 0;
                }
                
                foreach (var history in historyResponse.History)
                {
                    if (history.MessagesAdded != null)
                    {
                        foreach (var message in history.MessagesAdded)
                        {
                            await GetMailInfo(_service, message.Message.Id, strUserEmail);
                            _logger.LogInformation($"{message.Message.Id} message was added");
                            Console.WriteLine($"New message ID: {message.Message.Id}");
                        }
                    }

                    if (history.MessagesDeleted != null)
                    {
                        foreach (var message in history.MessagesDeleted)
                        {
                            var msg_id = message.Message.Id;
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                                TbEmail p = dbContext.TbEmails.Where(p => p.em_id == msg_id).FirstOrDefault();
                                if (p != null)
                                {
                                    dbContext.TbEmails.Remove(p);
                                    await dbContext.SaveChangesAsync();
                                    _logger.LogInformation($"{msg_id} message was deleted");
                                    await SendMailInfo(strUserEmail);
                                }
                            }
                        }
                    }

                    if (history.LabelsAdded != null)
                    {
                        foreach (var labelChange in history.LabelsAdded)
                        {
                            string msg_id = labelChange.Message.Id;
                            List<string> labelIds = labelChange.LabelIds.ToList();
                            bool isRead = labelIds.Contains("READ");
                            bool isInInbox = labelIds.Contains("INBOX");
                            bool isArchived = !isInInbox;

                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                                TbEmail p = dbContext.TbEmails.Where(p => p.em_id == msg_id).FirstOrDefault();
                                if (p != null)
                                {
                                    p.em_state = isArchived ? 3 : 0;
                                    p.em_read = isRead ? 1 : 0;

                                    await dbContext.SaveChangesAsync();
                                    _logger.LogInformation($"{msg_id} message was added");
                                    //await SendMailInfo(strUserEmail);
                                }
                            }
                            Console.WriteLine($"Labels added from message ID {labelChange.Message.Id}: {string.Join(", ", labelChange.LabelIds)}");
                        }
                    }

                    if (history.LabelsRemoved != null)
                    {
                        foreach (var labelChange in history.LabelsRemoved)
                        {
                            string msg_id = labelChange.Message.Id;
                            List<string> labelIds = labelChange.LabelIds.ToList();
                            bool isRead = !labelIds.Contains("READ");
                            bool isInInbox = !labelIds.Contains("INBOX");
                            bool isArchived = !isInInbox;

                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
                                TbEmail p = dbContext.TbEmails.Where(p => p.em_id == msg_id).FirstOrDefault();
                                if (p != null)
                                {
                                    p.em_state = isArchived ? 3 : 0;
                                    p.em_read = isRead ? 1 : 0;

                                    await dbContext.SaveChangesAsync();
                                    _logger.LogInformation($"{msg_id} message was changed");
                                    //await SendMailInfo(strUserEmail);
                                }
                            }
                            Console.WriteLine($"Labels removed from message ID {labelChange.Message.Id}: {string.Join(", ", labelChange.LabelIds)}");
                        }
                    }

                    //if (history.MessagesAdded == null && history.MessagesDeleted == null && history.LabelsAdded == null && history.LabelsRemoved == null)
                    //{
                    //    foreach (var _m in history.Messages)
                    //    {
                    //        RemoveEmail(_m.Id, strUserEmail);
                    //    }
                    //}
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"in EmailService {ex.Message}");
                Console.WriteLine(ex.Message);
            }
            return 0;
        }
	}
    public class EmailDto
    {
        public string em_from { get; set; }
        public DateTime? em_date { get; set; }
    }
    public class TbEmailsExt
    {
        //on time - 1 week - 1
        //too late - 1 month - 2
        //danger - 1 year - 3
        public string em_from { set; get; }
        public string em_date_ex { set; get; }
        public string em_id { set; get; }
        public string em_subject { set; get; }
        public int? em_read { set; get; }
        public int? em_state { set; get; }
        public int em_status { set; get; }
        public string? em_body { set; get; }
        public TbEmailsExt(TbEmail p)
        {
            em_from = p.em_from;
            em_date_ex = p.em_date?.ToString("MMM dd, hh:mm");
            em_id = p.em_id;
            em_subject = string.IsNullOrEmpty(p.em_subject) ? $"New customer EMail on {em_date_ex}" : p.em_subject;
            em_read = p.em_read;
            em_state = p.em_read;

            if(em_read == 1)
            {
                em_status = 1;
            }
            else
            {
                TimeSpan tp = (DateTime.Now - p.em_date.Value);
                if(tp.Days < 7)
                {
                    em_status = 1;
                }else if(tp.Days >= 7 && tp.Days < 30)
                {
                    em_status = 2;
                }
                else
                {
                    em_status = 3;
                }
            }
        }

        public TbEmailsExt(TbSms p)
        {
            em_from = p.sm_from;
            em_date_ex = p.sm_date?.ToString("MMM dd, hh:mm");
            em_id = p.sm_id;
            em_subject = string.IsNullOrEmpty(p.sm_body) ? $"New customer messages on {em_date_ex}" : p.sm_body;
            em_read = p.sm_read;
            em_state = p.sm_state;

            if (em_read == 1)
            {
                em_status = 1;
            }
            else
            {
                TimeSpan tp = (DateTime.Now - p.sm_date.Value);
                if (tp.Days < 7)
                {
                    em_status = 1;
                }
                else if (tp.Days >= 7 && tp.Days < 30)
                {
                    em_status = 2;
                }
                else
                {
                    em_status = 3;
                }
            }
        }
    }

    public class CustomerInfo
    {
        public string strEmail { set; get; }
        public string strPhone { set; get; }
        public string strName { set; get; }
        public string strSubject { set; get; }

        public CustomerInfo()
        {
            strEmail = "";
            strPhone = "";
            strName = "";
            strSubject = "";
        }
    }

    public class EmailExt
    {
        public string em_id { set; get; }
        public string em_subject { set; get; }
        public string em_from {set;get;}
        public string em_to { set; get; }
        public DateTime em_date { set; get; }
        public string em_body { set; get; }
        public List<string> strLabel { set; get; }
        public int nType { set; get; } //0 - to me, 1 - from me
    }
}
