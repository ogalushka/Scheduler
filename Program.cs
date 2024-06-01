using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Scheduler.Db;
using Scheduler.Db.Models;
using Scheduler.Scraper;
using Scheduler.Service;
using Scheduler.Settings;
using Scheduler.Tracking;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
/*
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
*/
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
{
    o.Cookie.SameSite = SameSiteMode.None;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromDays(365);
    o.Cookie.MaxAge = o.ExpireTimeSpan;
    o.SlidingExpiration = true;
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    googleOptions.Events.OnCreatingTicket = ctx => {
        var authService = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>();
        return authService.HandleGoogleAuthOnCreateTicket(ctx);
    };
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
    facebookOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    facebookOptions.Events.OnCreatingTicket = ctx =>
    {
        var authService = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>();
        return authService.HandleFacebookAuthOnCreateTicket(ctx);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.SetupDb();
builder.Services.SetupTracking();
builder.Services.AddTransient<ScheduleUpdaterService>();
builder.Services.AddTransient<AuthService>();
builder.Services.AddHostedService<ScheduleScraperV2>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        })
);

var mongoSettings = builder.Configuration.GetSection(nameof(MongoSettings))?.Get<MongoSettings>();
if (mongoSettings == null)
{
    throw new Exception("Can't load mongo db from app settings");
}

builder.Services.SetupRepository<string, ScheduleEntity>(mongoSettings.ScheduleCollection);
builder.Services.SetupRepository<Guid, UserEntity>(mongoSettings.UserCollection);
builder.Services.SetupRepository<string, ScheduleHistory>(mongoSettings.HistoryCollection);

try
{
var app = builder.Build();
app.UseCors();
app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    var apiPath = builder.Configuration.GetValue<string>("BaseApiPath") ?? "/";
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers = new List<OpenApiServer> { 
                new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}{apiPath}" },
                new OpenApiServer { Url = $"https://{httpReq.Host.Value}{apiPath}" } 
            };
        });
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
        });

    });
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

    app.Run();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
    throw e;
}
