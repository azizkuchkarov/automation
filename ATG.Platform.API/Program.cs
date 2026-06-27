using System.Text;
using System.Text.Json.Serialization;
using ATG.Platform.API.Json;
using ATG.Platform.API.Middleware;
using ATG.Platform.Infrastructure;
using ATG.Platform.Infrastructure.Jobs;
using ATG.Platform.Infrastructure.Seeds;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);
if (hangfireEnabled)
{
    builder.Services.AddHangfire(c => c
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
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

await DatabaseSeeder.SeedAsync(app.Services);
await HoDataSeeder.SeedAsync(app.Services);
await BmgmcDataSeeder.SeedAsync(app.Services);
await StationDataSeeder.SeedAsync(app.Services);
await TaskDataSeeder.SeedAsync(app.Services);
await DcsDataSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
}

app.MapControllers();

app.Run();

file sealed class HangfireDevOnlyFilter(bool allow) : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => allow;
}
