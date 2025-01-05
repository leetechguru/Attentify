using GoogleLogin.Models;
using GoogleLogin.CustomPolicy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GoogleLogin.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Google.Api;
using Twilio.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(builder.Configuration["ConnectionStrings:GoogleAuthInAspNetCoreMVCContextConnection"]));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{    
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -";
}).AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();


builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AspManager", policy =>
    {
        policy.RequireRole("Manager");
        policy.RequireClaim("Coding-Skill", "ASP.NET Core MVC");
    });
});

builder.Services.AddTransient<IAuthorizationHandler, AllowUsersHandler>();
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AllowTom", policy =>
    {
        policy.AddRequirements(new AllowUserPolicy("tom"));
    });
});

builder.Services.AddAuthentication()
    .AddCookie()
    .AddGoogle(opts =>
    {
#if DEBUG
        //opts.ClientId = "849361092514-782nq1ipi5irv59rjj00src6ar9scco5.apps.googleusercontent.com";//for me(test)        
        //opts.ClientSecret = "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";  //for me(test)
        opts.ClientId = "554411087297-k1a42bhgrutgbq5inss1qoj79tltd2on.apps.googleusercontent.com";//"903521853687-3gprhtno2qglf85ji87r01su2es6omno.apps.googleusercontent.com"; //for client                       
        opts.ClientSecret = "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";//"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";  //for me(test)   
#else
        opts.ClientId = "554411087297-k1a42bhgrutgbq5inss1qoj79tltd2on.apps.googleusercontent.com";//"903521853687-3gprhtno2qglf85ji87r01su2es6omno.apps.googleusercontent.com"; //for client                       
        opts.ClientSecret = "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";//"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";  //for client                           
#endif
        opts.SignInScheme = IdentityConstants.ExternalScheme;
        opts.Scope.Add("email");
        opts.Scope.Add("https://www.googleapis.com/auth/gmail.readonly");
        opts.Scope.Add("https://www.googleapis.com/auth/gmail.modify");
        opts.Scope.Add("https://www.googleapis.com/auth/gmail.send");
        opts.Scope.Add("https://www.googleapis.com/auth/pubsub");
        opts.Scope.Add("profile");
        opts.SaveTokens = true;
    });


builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EMailService>();
builder.Services.AddScoped<ModelService>();
builder.Services.AddScoped<LLMService>();
builder.Services.AddScoped<GoogleLogin.Services.ShopifyService>();

builder.Services.AddSingleton(new TwilioRestClient(
            builder.Configuration["Twilio:AccountSid"],
            builder.Configuration["Twilio:AuthToken"]
        ));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);    
    options.Cookie.IsEssential = true;
});
builder.Services.AddSignalR(); 

var app = builder.Build();
var loggerFactory = app.Services.GetService<ILoggerFactory>();
loggerFactory.AddFile(builder.Configuration["Logging:LogFilePath"]?.ToString());

//using (var scope = app.Services.CreateScope())
//{
//    var gmailWatchService = scope.ServiceProvider.GetRequiredService<GMailWatchService>();
//    var topicName = "projects/YOUR_PROJECT_ID/topics/YOUR_TOPIC_NAME";
//    var response = gmailWatchService.StartWatch("me", topicName);    
//}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");    
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();
app.UseSession();

app.MapHub<DataWebsocket>("/ws");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=account}/{action=login}");

app.Run();
