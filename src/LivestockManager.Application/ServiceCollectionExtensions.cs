using Microsoft.Extensions.DependencyInjection;

namespace LivestockManager.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<Services.Livestock.ILivestockService, Services.Livestock.LivestockService>();
        services.AddScoped<Services.Sales.ISaleService, Services.Sales.SaleService>();
        services.AddScoped<Services.Invoices.IInvoiceService, Services.Invoices.InvoiceService>();
        services.AddScoped<Services.Payments.IPaymentService, Services.Payments.PaymentService>();
        services.AddScoped<Services.Reports.IReportService, Services.Reports.ReportService>();
        services.AddScoped<Services.Customers.ICustomerService, Services.Customers.CustomerService>();
        services.AddScoped<Services.Companies.ICompanyService, Services.Companies.CompanyService>();
        services.AddScoped<Services.Farms.IFarmService, Services.Farms.FarmService>();

        return services;
    }
}
