using System.Text;
using System.Text.Json.Serialization;
using ATG.Platform.API.Hubs;
using ATG.Platform.API.Json;
using ATG.Platform.API.Middleware;
using ATG.Platform.API.Services;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Infrastructure;
using ATG.Platform.Infrastructure.Jobs;
using ATG.Platform.Infrastructure.Seeds;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression();
builder.Services.AddHealthChecks();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        o.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeJsonConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();

var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);
if (hangfireEnabled)
{
    var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire")
        ?? builder.Configuration.GetConnectionString("Default");
    builder.Services.AddHangfire(c => c
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(hangfireConnection)));
    builder.Services.AddHangfireServer();
}

var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "sub",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (string.IsNullOrEmpty(ctx.Token) && ctx.Request.Cookies.ContainsKey("accessToken"))
                    ctx.Token = ctx.Request.Cookies["accessToken"];

                var accessToken = ctx.Request.Query["access_token"];
                if (string.IsNullOrEmpty(ctx.Token) && !string.IsNullOrEmpty(accessToken))
                    ctx.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();
var seedMasterData = app.Configuration.GetValue("DataSeeding:Enabled", isDevelopment);

await DatabaseSeeder.SeedAsync(app.Services);
if (seedMasterData)
{
    await HoDataSeeder.SeedAsync(app.Services);
    await BmgmcDataSeeder.SeedAsync(app.Services);
    await StationDataSeeder.SeedAsync(app.Services);
    await TaskDataSeeder.SeedAsync(app.Services);
    await DcsDataSeeder.SeedAsync(app.Services);
    await ItAutomationSeeder.SeedAsync(app.Services);
    await HrBusinessTripWorkflowSeeder.SeedAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDevOnlyFilter(isDevelopment)]
    });
    RecurringJob.AddOrUpdate<MarketingBackgroundJobs>(
        "marketing-portal-reminders",
        j => j.CheckPortalApprovalDelaysAsync(),
        Cron.Daily(8));
    RecurringJob.AddOrUpdate<MarketingBackgroundJobs>(
        "marketing-deadline-warnings",
        j => j.CheckMarketingDeadlinesAsync(),
        Cron.Daily(9));
    RecurringJob.AddOrUpdate<NotificationBackgroundJobs>(
        "approval-reminders",
        j => j.CheckPendingApprovalRemindersAsync(),
        Cron.Daily(4));
    RecurringJob.AddOrUpdate<ItAutomationBackgroundJobs>(
        "it-automation-expiry-warnings",
        j => j.CheckExpiryWarningsAsync(),
        Cron.Daily(7));
}

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

file sealed class HangfireDevOnlyFilter(bool allow) : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => allow;
}
