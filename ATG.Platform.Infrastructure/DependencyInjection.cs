using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Eimzo;
using ATG.Platform.Infrastructure.Jobs;
using ATG.Platform.Infrastructure.Services;
using ATG.Platform.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATG.Platform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        services.AddMemoryCache();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                    config.GetConnectionString("Default"),
                    npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.Configure<LdapOptions>(config.GetSection(LdapOptions.SectionName));
        services.PostConfigure<LdapOptions>(ApplyLdapEnvironmentOverrides);
        services.Configure<MinioOptions>(config.GetSection(MinioOptions.SectionName));
        services.Configure<NotificationOptions>(config.GetSection(NotificationOptions.SectionName));
        services.Configure<EimzoOptions>(config.GetSection(EimzoOptions.SectionName));
        services.PostConfigure<EimzoOptions>(ApplyEimzoEnvironmentOverrides);
        services.Configure<HrLeaveOptions>(config.GetSection(HrLeaveOptions.SectionName));
        services.PostConfigure<HrLeaveOptions>(ApplyHrLeaveEnvironmentOverrides);

        services.AddHttpClient<IEimzoServerClient, EimzoServerClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EimzoOptions>>().Value;
            client.BaseAddress = new Uri(opts.ServerBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IHelpDeskService, HelpDeskService>();
        services.AddScoped<IDcsService, DcsService>();
        services.AddScoped<IProcurementRequestService, ProcurementRequestService>();
        services.AddScoped<IMarketingService, MarketingService>();
        services.AddScoped<MarketingRfqChannelService>();
        services.AddScoped<IMarketingRfqChannelService>(sp => sp.GetRequiredService<MarketingRfqChannelService>());
        services.AddScoped<IIncomingLetterService, IncomingLetterService>();
        services.AddScoped<IOutgoingLetterService, OutgoingLetterService>();
        services.AddScoped<IMemoService, MemoService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IHrLeaveRequestService, HrLeaveRequestService>();
        services.AddScoped<IHrBusinessTripRequestService, HrBusinessTripRequestService>();
        services.AddScoped<IHrBusinessTripWorkflowService, HrBusinessTripWorkflowService>();
        services.AddScoped<IItAutomationService, ItAutomationService>();
        services.AddScoped<IPlatformHomeService, PlatformHomeService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ILdapService, LdapService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddScoped<MarketingBackgroundJobs>();
        services.AddScoped<NotificationBackgroundJobs>();
        services.AddScoped<ItAutomationBackgroundJobs>();

        return services;
    }

    private static void ApplyLdapEnvironmentOverrides(LdapOptions options)
    {
        SetIfPresent("LDAP_ENABLED", v => options.Enabled = v is "1" or "true" or "True");
        SetIfPresent("LDAP_SERVER", v => options.Server = v);
        SetIfPresent("LDAP_PORT", v => { if (int.TryParse(v, out var port)) options.Port = port; });
        SetIfPresent("LDAP_USE_SSL", v => options.UseSsl = v is "1" or "true" or "True");
        SetIfPresent("LDAP_BASE_DN", v => options.BaseDn = v);
        SetIfPresent("LDAP_BIND_DN", v => options.BindDn = v);
        SetIfPresent("LDAP_BIND_PASSWORD", v => options.BindPassword = v);
        SetIfPresent("LDAP_DOMAIN", v => options.Domain = v);
        SetIfPresent("LDAP_NETBIOS_NAME", v => options.NetBiosName = v);

        void SetIfPresent(string key, Action<string> apply)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value is not null)
                apply(value);
        }
    }

    private static void ApplyEimzoEnvironmentOverrides(EimzoOptions options)
    {
        SetIfPresent("EIMZO_ENABLED", v => options.Enabled = v is "1" or "true" or "True");
        SetIfPresent("EIMZO_SERVER_BASE_URL", v => options.ServerBaseUrl = v);
        SetIfPresent("EIMZO_SITE_HOST", v => options.SiteHost = v);
        SetIfPresent("EIMZO_FRONTEND_API_KEY", v => options.FrontendApiKey = v);
        SetIfPresent("EIMZO_CLIENT_IP", v => options.ClientIp = v);
        SetIfPresent("EIMZO_ALLOW_UNSIGNED_PKCS7", v => options.AllowUnsignedPkcs7 = v is "1" or "true" or "True");

        static void SetIfPresent(string key, Action<string> apply)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value is not null)
                apply(value);
        }
    }

    private static void ApplyHrLeaveEnvironmentOverrides(HrLeaveOptions options)
    {
        SetIfPresent("HR_LEAVE_SHORT_HO_CHAIN", v => options.ShortHoApprovalChain = v is "1" or "true" or "True");
        SetIfPresent("HR_LEAVE_HO_GD_EMAIL", v => options.HoGeneralDirectorEmail = v);
        SetIfPresent("HR_LEAVE_PUBLIC_APP_URL", v => options.PublicAppBaseUrl = v.TrimEnd('/'));

        static void SetIfPresent(string key, Action<string> apply)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value is not null)
                apply(value);
        }
    }
}
