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
        services.AddScoped<Services.Customers.ICustomerBalanceService, Services.Customers.CustomerBalanceService>();
        services.AddScoped<Services.Expenses.IExpenseService, Services.Expenses.ExpenseService>();
        services.AddScoped<Services.Receipts.IReceiptService, Services.Receipts.ReceiptService>();
        services.AddScoped<Services.Suppliers.ISupplierService, Services.Suppliers.SupplierService>();
        services.AddScoped<Services.Purchases.IPurchaseService, Services.Purchases.PurchaseService>();
        services.AddScoped<Services.LivestockLosses.ILivestockLossService, Services.LivestockLosses.LivestockLossService>();
        services.AddScoped<Services.Companies.ICompanyService, Services.Companies.CompanyService>();
        services.AddScoped<Services.Farms.IFarmService, Services.Farms.FarmService>();

        services.AddSingleton<Services.StockAddition.IStockAdditionIdempotencyCache, Services.StockAddition.StockAdditionIdempotencyCache>();
        services.AddScoped<Services.StockAddition.IStockAdditionService, Services.StockAddition.StockAdditionService>();

        return services;
    }
}
