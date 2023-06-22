using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Scheduler.Db;
using Scheduler.Db.Models;
using Scheduler.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//"Connection": "mongodb://localhost:27017",

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddSwaggerGen();
builder.Services.SetupDb();

var mongoSettings = builder.Configuration.GetSection(nameof(MongoSettings))?.Get<MongoSettings>();
if (mongoSettings == null)
{
    throw new Exception("Can't load mongo db from app settings");
}

builder.Services.SetupRepository<string, ScheduleEntity>(mongoSettings.ScheduleCollection);
builder.Services.SetupRepository<Guid, UserEntity>(mongoSettings.UserCollection);

var app = builder.Build();
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
