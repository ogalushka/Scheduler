using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Scheduler.Db;
using Scheduler.Db.Models;
using Scheduler.Settings;
using Scheduler.Tracking;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o => {
    o.Cookie.SameSite = SameSiteMode.None;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromDays(365);
    o.Cookie.MaxAge = o.ExpireTimeSpan;
    o.SlidingExpiration = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen();

builder.Services.SetupDb();
builder.Services.SetupTracking();

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
