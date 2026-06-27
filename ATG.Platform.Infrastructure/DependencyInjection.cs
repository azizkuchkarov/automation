using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using ATG.Platform.Infrastructure.Data;
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

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Default"))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.Configure<LdapOptions>(config.GetSection(LdapOptions.SectionName));
        services.PostConfigure<LdapOptions>(ApplyLdapEnvironmentOverrides);
        services.Configure<MinioOptions>(config.GetSection(MinioOptions.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IHelpDeskService, HelpDeskService>();
        services.AddScoped<IDcsService, DcsService>();
        services.AddScoped<IProcurementRequestService, ProcurementRequestService>();
        services.AddScoped<IMarketingService, MarketingService>();
        services.AddScoped<IIncomingLetterService, IncomingLetterService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ILdapService, LdapService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddScoped<MarketingBackgroundJobs>();

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
}
