using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LivestockManager.Application.Common;
using LivestockManager.Domain;
using LivestockManager.Domain.Abstractions;
using LivestockManager.Infrastructure.Identity;
using LivestockManager.Infrastructure.Persistence;
using LivestockManager.Infrastructure.Security;
using LivestockManager.Infrastructure.Services;
using LivestockManager.Infrastructure.Services.Pdf;
using LivestockManager.Infrastructure.Services.Sequencing;
using LivestockManager.Infrastructure.Services.Storage;

namespace LivestockManager.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("LivestockManagerDb"),
                sql =>
                {
                    sql.CommandTimeout(90);
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IDateTime, DateTimeProvider>();
        services.AddScoped<ISequenceGenerator, EfSequenceGenerator>();
        services.AddScoped<IPdfGenerator, FormattedPdfWriter>();

        services.AddScoped<DocumentNumberGenerator>();
        services.AddScoped<IProtectedDocumentStorage, ProtectedDocumentStorage>();

        services.AddScoped<IUserCapabilityService, UserCapabilityService>();

        return services;
    }
}
