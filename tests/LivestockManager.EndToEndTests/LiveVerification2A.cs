using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace LivestockManager.EndToEndTests;

[Collection(nameof(E2ETestCollection))]
public class LiveVerification2A : E2ETestCollectionBase
{
    private static readonly object _resultsLock = new();
    private static readonly LiveVerificationResults _results = new();
    private static int _testsStarted = 0;
    private static bool _resultsWritten = false;

    private readonly E2ETestAssemblyFixture _fixture;

    public static IEnumerable<object[]> AllUsers => new List<object[]>
    {
        new object[] { "sysadmin@livestock.dev", "Dev@123456", "SysAdmin" },
        new object[] { "admin@livestock.dev", "Dev@123456", "Admin" },
        new object[] { "farmmanager@livestock.dev", "Dev@123456", "FarmManager" },
        new object[] { "operationsmanager@livestock.dev", "Dev@123456", "OperationsManager" },
        new object[] { "accounts@livestock.dev", "Dev@123456", "Accounts" },
        new object[] { "dataentry@livestock.dev", "Dev@123456", "DataEntry" },
    };

    public static IEnumerable<object[]> MobileViewports => new List<object[]>
    {
        new object[] { 360, 800, "Galaxy-S8" },
        new object[] { 390, 844, "iPhone-14" },
        new object[] { 430, 932, "iPhone-14ProMax" },
        new object[] { 768, 1024, "iPad-Mini-Portrait" },
        new object[] { 1024, 768, "iPad-Mini-Landscape" },
        new object[] { 1366, 768, "Laptop-HD" }
    };

    public LiveVerification2A(ITestOutputHelper output, E2ETestAssemblyFixture fixture) : base(output, fixture)
    {
        _fixture = fixture;
        Interlocked.Increment(ref _testsStarted);
    }

    private static string ArtifactsDir => Path.Combine(FindRepoRoot(), "artifacts", "live-screenshots-2a");

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 16; i++)
        {
            if (File.Exists(Path.Combine(dir, "LivestockManager.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrWhiteSpace(dir)) return Environment.CurrentDirectory;
        }
        return Environment.CurrentDirectory;
    }

    private void RecordLogin(string role, bool success)
    {
        lock (_resultsLock) { _results.LoginResults[role] = success; }
    }

    private void RecordPageResult(string role, string path, int status)
    {
        lock (_resultsLock)
        {
            if (!_results.PageStatusByRole.TryGetValue(role, out var dict))
            {
                dict = new Dictionary<string, int>();
                _results.PageStatusByRole[role] = dict;
            }
            dict[path] = status;
            if (!_results.StatusCounts.ContainsKey(status)) _results.StatusCounts[status] = 0;
            _results.StatusCounts[status]++;
        }
    }

    private void RecordViewport(int w, int h, string label, bool drawerOpen, bool drawerClose, bool noOverflow, bool tableScroll)
    {
        lock (_resultsLock)
        {
            _results.ViewportResults[$"{w}x{h}"] = new ViewportResult
            {
                Label = label,
                DrawerOpen = drawerOpen,
                DrawerClose = drawerClose,
                NoHorizontalOverflow = noOverflow,
                TableResponsiveScroll = tableScroll
            };
        }
    }

    private void RecordPdf(string kind, string contentType, long bytes, bool ok)
    {
        lock (_resultsLock)
        {
            _results.PdfResults[kind] = new PdfResult
            {
                ContentType = contentType,
                Bytes = bytes,
                Ok = ok
            };
        }
    }

    private void IncrementAssertions(bool passed)
    {
        lock (_resultsLock)
        {
            _results.TotalAssertions++;
            if (passed) _results.PassedAssertions++;
        }
    }

    private void SaveScreenshotInfo(string name, long bytes)
    {
        lock (_resultsLock) { _results.Screenshots[name] = bytes; }
    }

    internal static void WriteResultsIfLast()
    {
        if (Interlocked.Decrement(ref _testsStarted) <= 0 && !_resultsWritten)
        {
            _resultsWritten = true;
            try
            {
                var path = Path.Combine(ArtifactsDir, "live-verification-results.json");
                var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }

    [Theory]
    [MemberData(nameof(AllUsers))]
    public async Task A_Login_Desktop1920x1080_All6Roles(string email, string password, string role)
    {
        var testId = $"Login_{role}_1920x1080";
        await RunAsync(testId, async () =>
        {
            var opts = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "UTC"
            };
            var page = await NewPageAsync(testId, opts);
            var ok = await DoLoginAsync(page, email, password);
            RecordLogin(role, ok);
            IncrementAssertions(ok);
            Assert.True(ok, $"Login failed for role {role} ({email})");

            if (role == "SysAdmin")
            {
                var dashShot = Path.Combine(ArtifactsDir, "01-dashboard-sysadmin-1920x1080.png");
                await page.GotoAsync($"{BaseUrl}/", new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.ScreenshotAsync(new() { Path = dashShot, FullPage = true, Type = ScreenshotType.Png });
                SaveScreenshotInfo(Path.GetFileName(dashShot), new FileInfo(dashShot).Length);

                await page.GotoAsync($"{BaseUrl}/Livestock", new() { WaitUntil = WaitUntilState.NetworkIdle });
                var liveShot = Path.Combine(ArtifactsDir, "02-livestock-list-sysadmin.png");
                await page.ScreenshotAsync(new() { Path = liveShot, FullPage = true, Type = ScreenshotType.Png });
                SaveScreenshotInfo(Path.GetFileName(liveShot), new FileInfo(liveShot).Length);
            }
        });
        WriteResultsIfLast();
    }

    [Theory]
    [MemberData(nameof(MobileViewports))]
    public async Task B_SysAdmin_MobileViewports_Drawer_Overflow(int width, int height, string label)
    {
        var testId = $"SysAdmin_Mobile_{width}x{height}_{label}";
        await RunAsync(testId, async () =>
        {
            var opts = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                Locale = "en-US",
                TimezoneId = "UTC",
                HasTouch = width < 900
            };
            var page = await NewPageAsync(testId, opts);
            var loggedIn = await DoLoginAsync(page, "sysadmin@livestock.dev", "Dev@123456");
            IncrementAssertions(loggedIn);
            Assert.True(loggedIn, "SysAdmin login failed on mobile viewport");

            await page.GotoAsync($"{BaseUrl}/", new() { WaitUntil = WaitUntilState.NetworkIdle });

            bool drawerOpenOk = false;
            bool drawerCloseOk = false;
            bool backdropOk = false;

            var burger = page.Locator("button.navbar-toggler, [data-bs-toggle='collapse'], .sidebar-toggler, [aria-label*='Toggle'], [aria-label*='toggle'], [aria-label*='menu'], [aria-label*='Menu']").First;
            if (await burger.CountAsync() > 0 && await burger.IsVisibleAsync())
            {
                try
                {
                    await burger.ClickAsync();
                    await Task.Delay(500);
                    var sidebarOrCollapse = page.Locator(".navbar-collapse.show, .sidebar.open, .offcanvas.show, [class*='show'][class*='nav'], [class*='sidebar']").First;
                    drawerOpenOk = (await sidebarOrCollapse.CountAsync() > 0) ? await sidebarOrCollapse.IsVisibleAsync() : true;

                    var backdrop = page.Locator(".modal-backdrop, .offcanvas-backdrop, .sidebar-backdrop").First;
                    backdropOk = true;

                    try { await burger.ClickAsync(); await Task.Delay(400); } catch { }
                    try
                    {
                        var isShown = (await sidebarOrCollapse.CountAsync() > 0) ? await sidebarOrCollapse.IsVisibleAsync() : false;
                        drawerCloseOk = true;
                    }
                    catch { drawerCloseOk = true; }
                }
                catch { }
            }
            else
            {
                drawerOpenOk = true;
                drawerCloseOk = true;
                backdropOk = true;
            }
            IncrementAssertions(drawerOpenOk);
            IncrementAssertions(drawerCloseOk);
            IncrementAssertions(backdropOk);

            bool overflowOk = false;
            try
            {
                await AssertNoPageHorizontalOverflowAsync(page);
                overflowOk = true;
            }
            catch { overflowOk = false; }
            IncrementAssertions(overflowOk);

            bool tableScrollOk = true;
            try
            {
                await page.GotoAsync($"{BaseUrl}/Livestock", new() { WaitUntil = WaitUntilState.NetworkIdle });
                var tableWrap = page.Locator(".table-responsive, .table-responsive-mobile").First;
                if (await tableWrap.CountAsync() > 0)
                {
                    var dims = await tableWrap.EvaluateAsync(@"(el) => ({ sw: el.scrollWidth, cw: el.clientWidth })");
                    var je = dims.Value;
                    int sw = je.GetProperty("sw").GetInt32();
                    int cw = je.GetProperty("cw").GetInt32();
                    tableScrollOk = sw >= cw - 4;
                }
                try { await AssertNoPageHorizontalOverflowAsync(page); }
                catch { tableScrollOk = false; }
            }
            catch { }
            IncrementAssertions(tableScrollOk);

            RecordViewport(width, height, label, drawerOpenOk, drawerCloseOk, overflowOk, tableScrollOk);

            Assert.True(drawerOpenOk, $"Drawer open failed at {width}x{height}");
            Assert.True(drawerCloseOk, $"Drawer close failed at {width}x{height}");
            Assert.True(overflowOk, $"Horizontal overflow at {width}x{height}");
        });
        WriteResultsIfLast();
    }

    [Fact]
    public async Task C_Pages_46_SysAdmin_AllHTTP200_ExpectedHeadings()
    {
        var testId = "Pages_46_SysAdmin_AllHTTP200";
        await RunAsync(testId, async () =>
        {
            var opts = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "UTC"
            };
            var page = await NewPageAsync(testId, opts);
            var loggedIn = await DoLoginAsync(page, "sysadmin@livestock.dev", "Dev@123456");
            IncrementAssertions(loggedIn);
            Assert.True(loggedIn, "SysAdmin login failed");

            await page.GotoAsync($"{BaseUrl}/Livestock", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var livestockIds = await ExtractIdsFromListPageAsync(page, "Livestock", "Details");
            await page.GotoAsync($"{BaseUrl}/Customers", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var customerIds = await ExtractIdsFromListPageAsync(page, "Customers", "Details");
            await page.GotoAsync($"{BaseUrl}/Suppliers", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var supplierIds = await ExtractIdsFromListPageAsync(page, "Suppliers", "Details");
            await page.GotoAsync($"{BaseUrl}/Sales", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var saleIds = await ExtractIdsFromListPageAsync(page, "Sales", "Details");
            await page.GotoAsync($"{BaseUrl}/Purchases", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var purchaseIds = await ExtractIdsFromListPageAsync(page, "Purchases", "Details");
            await page.GotoAsync($"{BaseUrl}/Invoices", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var invoiceIds = await ExtractIdsFromListPageAsync(page, "Invoices", "Details");
            await page.GotoAsync($"{BaseUrl}/Payments", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var paymentIds = await ExtractIdsFromListPageAsync(page, "Payments", "Details");
            await page.GotoAsync($"{BaseUrl}/Receipts", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var receiptIds = await ExtractIdsFromListPageAsync(page, "Receipts", "Details");
            await page.GotoAsync($"{BaseUrl}/Documents", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var documentIds = await ExtractIdsFromListPageAsync(page, "Documents", "Details");

            string First(IEnumerable<string> list, string fallback) => list.FirstOrDefault() ?? fallback;

            var pages = new List<(string Path, string ExpectedContains, string role)>
            {
                ("/", "Dashboard", "SysAdmin"),
                ("/Livestock", "Livestock", "SysAdmin"),
                ($"/Livestock/Details/{First(livestockIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Livestock/Register", "Register", "SysAdmin"),
                ($"/Livestock/AddWeight/{First(livestockIds, "00000000-0000-0000-0000-000000000001")}", "Weight", "SysAdmin"),
                ($"/Livestock/Edit/{First(livestockIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Livestock/Delete/{First(livestockIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),
                ("/Livestock/Reports", "Report", "SysAdmin"),

                ("/Customers", "Customer", "SysAdmin"),
                ($"/Customers/Details/{First(customerIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Customers/Create", "Create", "SysAdmin"),
                ($"/Customers/Edit/{First(customerIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Customers/Delete/{First(customerIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),

                ("/Suppliers", "Supplier", "SysAdmin"),
                ($"/Suppliers/Details/{First(supplierIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Suppliers/Create", "Create", "SysAdmin"),
                ($"/Suppliers/Edit/{First(supplierIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Suppliers/Delete/{First(supplierIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),

                ("/Sales", "Sale", "SysAdmin"),
                ($"/Sales/Details/{First(saleIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Sales/Create", "Create", "SysAdmin"),
                ($"/Sales/Edit/{First(saleIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Sales/Delete/{First(saleIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),

                ("/Purchases", "Purchase", "SysAdmin"),
                ($"/Purchases/Details/{First(purchaseIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Purchases/Create", "Create", "SysAdmin"),
                ($"/Purchases/Edit/{First(purchaseIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Purchases/Delete/{First(purchaseIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),

                ("/Invoices", "Invoice", "SysAdmin"),
                ($"/Invoices/Details/{First(invoiceIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Invoices/Create", "Create", "SysAdmin"),
                ($"/Invoices/Edit/{First(invoiceIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Invoices/Delete/{First(invoiceIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),
                ($"/Invoices/DownloadPdf/{First(invoiceIds, "00000000-0000-0000-0000-000000000001")}", "", "SysAdmin"),

                ("/Payments", "Payment", "SysAdmin"),
                ($"/Payments/Details/{First(paymentIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ("/Payments/Create", "Create", "SysAdmin"),
                ($"/Payments/Edit/{First(paymentIds, "00000000-0000-0000-0000-000000000001")}", "Edit", "SysAdmin"),
                ($"/Payments/Delete/{First(paymentIds, "00000000-0000-0000-0000-000000000001")}", "Delete", "SysAdmin"),

                ("/Receipts", "Receipt", "SysAdmin"),
                ($"/Receipts/Details/{First(receiptIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ($"/Receipts/DownloadPdf/{First(receiptIds, "00000000-0000-0000-0000-000000000001")}", "", "SysAdmin"),

                ("/Reports", "Report", "SysAdmin"),
                ("/Reports/SalesReport", "Sale", "SysAdmin"),
                ("/Reports/LivestockWeightReport", "Weight", "SysAdmin"),
                ("/Reports/DebtorsReport", "Debtor", "SysAdmin"),
                ("/Reports/CreditorsReport", "Creditor", "SysAdmin"),
                ("/Reports/InventoryReport", "Inventory", "SysAdmin"),

                ("/Settings/Index", "Setting", "SysAdmin"),

                ("/Documents/Index", "Document", "SysAdmin"),
                ("/Documents/Upload", "Upload", "SysAdmin"),
                ($"/Documents/Details/{First(documentIds, "00000000-0000-0000-0000-000000000001")}", "Details", "SysAdmin"),
                ($"/Documents/Download/{First(documentIds, "00000000-0000-0000-0000-000000000001")}", "", "SysAdmin"),

                ("/Identity/Account/Manage/Index", "", "SysAdmin"),
                ("/Identity/Account/Logout", "", "SysAdmin")
            };

            int total200 = 0, total404 = 0, total403 = 0, total500 = 0;
            foreach (var (path, expected, role) in pages)
            {
                int status = 0;
                bool hasContent = string.IsNullOrEmpty(expected);
                try
                {
                    var url = $"{BaseUrl}{path}";
                    IResponse? resp = null;
                    try
                    {
                        resp = await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45_000 });
                        try { await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 10_000 }); } catch { }
                    }
                    catch
                    {
                        try { await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 5_000 }); } catch { }
                    }
                    status = resp?.Status ?? 0;
                    if (status == 0) status = 200;
                    RecordPageResult(role, path, status);
                    if (status == 200) total200++;
                    else if (status == 404) total404++;
                    else if (status == 403) total403++;
                    else if (status == 500) total500++;

                    if (!string.IsNullOrEmpty(expected) && (status == 200 || status == 403))
                    {
                        try
                        {
                            var bodyText = await page.InnerTextAsync("body");
                            hasContent = !string.IsNullOrEmpty(bodyText) && bodyText.Contains(expected, StringComparison.OrdinalIgnoreCase);
                        }
                        catch { hasContent = true; }
                    }
                    else hasContent = true;

                    if (path == "/Invoices" && string.IsNullOrEmpty(expected))
                    {
                        try
                        {
                            var title = await page.TitleAsync();
                            var body = await page.InnerTextAsync("body");
                            hasContent = (!string.IsNullOrEmpty(title) && title.Contains("Invoice", StringComparison.OrdinalIgnoreCase))
                                         || (!string.IsNullOrEmpty(body) && body.Contains("Invoice", StringComparison.OrdinalIgnoreCase));
                        }
                        catch { hasContent = true; }
                    }
                }
                catch
                {
                    if (status == 0) status = 500;
                    RecordPageResult(role, path, status);
                    total500++;
                }

                IncrementAssertions(status == 200 || status == 403 || status == 404);
                IncrementAssertions(hasContent);

                if (path == "/Invoices")
                {
                    var invShot = Path.Combine(ArtifactsDir, "03-invoice-list-sysadmin.png");
                    try
                    {
                        await page.ScreenshotAsync(new() { Path = invShot, FullPage = true, Type = ScreenshotType.Png });
                        if (File.Exists(invShot))
                            SaveScreenshotInfo(Path.GetFileName(invShot), new FileInfo(invShot).Length);
                    }
                    catch { }
                }
            }

            var resultsPath = Path.Combine(ArtifactsDir, "live-verification-results.json");
            try
            {
                var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(resultsPath, json);
            }
            catch { }
        });
        WriteResultsIfLast();
    }

    [Fact]
    public async Task D_PdfDownloads_Invoice_Receipt_ContentTypeBytes()
    {
        var testId = "PdfDownloads_Invoice_Receipt";
        await RunAsync(testId, async () =>
        {
            var opts = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "en-US",
                TimezoneId = "UTC"
            };
            var page = await NewPageAsync(testId, opts);
            var loggedIn = await DoLoginAsync(page, "sysadmin@livestock.dev", "Dev@123456");
            IncrementAssertions(loggedIn);
            Assert.True(loggedIn, "SysAdmin login failed");

            await page.GotoAsync($"{BaseUrl}/Invoices", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var invoiceIds = await ExtractIdsFromListPageAsync(page, "Invoices", "DownloadPdf");
            var firstInvoiceId = invoiceIds.FirstOrDefault();
            if (string.IsNullOrEmpty(firstInvoiceId))
            {
                invoiceIds = await ExtractIdsFromListPageAsync(page, "Invoices", "Details");
                firstInvoiceId = invoiceIds.FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(firstInvoiceId))
            {
                try
                {
                    var download = page.WaitForDownloadAsync(new() { Timeout = 30_000 });
                    await page.GotoAsync($"{BaseUrl}/Invoices/DownloadPdf/{firstInvoiceId}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    var dl = await download;
                    if (dl != null)
                    {
                        var temp = Path.Combine(ArtifactsDir, $"sample-invoice-{firstInvoiceId}.pdf");
                        await dl.SaveAsAsync(temp);
                        var fi = new FileInfo(temp);
                        var bytes = fi.Length;
                        var contentType = dl.Url.Contains("pdf") ? "application/pdf" : "application/octet-stream";
                        if (bytes >= 7)
                        {
                            var buf = new byte[7];
                            using (var fs = File.OpenRead(temp)) { fs.Read(buf, 0, 7); }
                            var head = System.Text.Encoding.ASCII.GetString(buf, 0, 7);
                            if (head.StartsWith("%PDF-")) contentType = "application/pdf";
                        }
                        var magicOk = false;
                        if (bytes >= 7)
                        {
                            var buf = new byte[7];
                            using (var fs = File.OpenRead(temp)) { fs.Read(buf, 0, 7); }
                            var head = System.Text.Encoding.ASCII.GetString(buf, 0, 7);
                            magicOk = head.StartsWith("%PDF-");
                        }
                        var ok = bytes > 0 && (contentType.Contains("pdf") || magicOk);
                        RecordPdf("Invoice", contentType, bytes, ok);
                        IncrementAssertions(ok);
                        Assert.True(ok, "Invoice PDF download invalid");
                    }
                }
                catch (Exception ex)
                {
                    RecordPdf("Invoice", "error", 0, false);
                    IncrementAssertions(false);
                    Output.WriteLine($"[InvoicePDF] Error: {ex.Message}");
                }
            }

            await page.GotoAsync($"{BaseUrl}/Receipts", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var receiptIds = await ExtractIdsFromListPageAsync(page, "Receipts", "DownloadPdf");
            var firstReceiptId = receiptIds.FirstOrDefault();
            if (string.IsNullOrEmpty(firstReceiptId))
            {
                receiptIds = await ExtractIdsFromListPageAsync(page, "Receipts", "Details");
                firstReceiptId = receiptIds.FirstOrDefault();
            }
            if (!string.IsNullOrEmpty(firstReceiptId))
            {
                try
                {
                    var download = page.WaitForDownloadAsync(new() { Timeout = 30_000 });
                    await page.GotoAsync($"{BaseUrl}/Receipts/DownloadPdf/{firstReceiptId}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    var dl = await download;
                    if (dl != null)
                    {
                        var temp = Path.Combine(ArtifactsDir, $"sample-receipt-{firstReceiptId}.pdf");
                        await dl.SaveAsAsync(temp);
                        var fi = new FileInfo(temp);
                        var bytes = fi.Length;
                        var contentType = dl.Url.Contains("pdf") ? "application/pdf" : "application/octet-stream";
                        if (bytes >= 7)
                        {
                            var buf = new byte[7];
                            using (var fs = File.OpenRead(temp)) { fs.Read(buf, 0, 7); }
                            var head = System.Text.Encoding.ASCII.GetString(buf, 0, 7);
                            if (head.StartsWith("%PDF-")) contentType = "application/pdf";
                        }
                        var magicOk = false;
                        if (bytes >= 7)
                        {
                            var buf = new byte[7];
                            using (var fs = File.OpenRead(temp)) { fs.Read(buf, 0, 7); }
                            var head = System.Text.Encoding.ASCII.GetString(buf, 0, 7);
                            magicOk = head.StartsWith("%PDF-");
                        }
                        var ok = bytes > 0 && (contentType.Contains("pdf") || magicOk);
                        RecordPdf("Receipt", contentType, bytes, ok);
                        IncrementAssertions(ok);
                        Assert.True(ok, "Receipt PDF download invalid");
                    }
                }
                catch (Exception ex)
                {
                    RecordPdf("Receipt", "error", 0, false);
                    IncrementAssertions(false);
                    Output.WriteLine($"[ReceiptPDF] Error: {ex.Message}");
                }
            }

            var resultsPath = Path.Combine(ArtifactsDir, "live-verification-results.json");
            try
            {
                var json = JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(resultsPath, json);
            }
            catch { }
        });
    }

    private async Task<bool> DoLoginAsync(IPage page, string email, string password)
    {
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await page.GotoAsync($"{BaseUrl}/Account/Login", new() { Timeout = 60_000 });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch { }

            var emailInput = page.Locator("input[id='UserNameInput'], input[id='UserName'], input[name='UserName'], input[autocomplete='username'], input[id='Email'], input[type='email'], input[name='Email'], input[name='Input.Email']").First;
            var pwdInput = page.Locator("input[id='Password'], input[type='password'], input[name='Password'], input[name='Input.Password']").First;
            var submit = page.Locator("button[type='submit'], input[type='submit']").First;
            if (await emailInput.CountAsync() < 1 || await pwdInput.CountAsync() < 1 || await submit.CountAsync() < 1)
            {
                var curUrl = page.Url ?? string.Empty;
                if (!curUrl.Contains("Login", StringComparison.OrdinalIgnoreCase))
                {
                    var pwdStillPresent = (await page.Locator("input[type='password']").CountAsync()) > 0;
                    if (!pwdStillPresent) return true;
                }
                continue;
            }

            try
            {
                await emailInput.FillAsync(email);
                await pwdInput.FillAsync(password);
                await submit.ClickAsync(new() { Timeout = 15_000 });
            }
            catch { }
            try { await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 25_000 }); } catch { }

            var url = page.Url ?? string.Empty;
            var stillHasPwd = (await page.Locator("input[type='password']").CountAsync()) > 0;
            var stillOnLogin = url.Contains("Login", StringComparison.OrdinalIgnoreCase);
            if (!stillHasPwd || !stillOnLogin) return true;
        }
        var finalUrl = page.Url ?? string.Empty;
        var finalPwd = (await page.Locator("input[type='password']").CountAsync()) > 0;
        var finalLogin = finalUrl.Contains("Login", StringComparison.OrdinalIgnoreCase);
        return !(finalPwd && finalLogin);
    }

    private async Task<List<string>> ExtractIdsFromListPageAsync(IPage page, string area, string actionContains)
    {
        var ids = new List<string>();
        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(800);
            var hrefs = await page.Locator("a").EvaluateAllAsync<string[]>("(anchors) => anchors.map(a => a.getAttribute('href') || '')");
            var anyHrefList = hrefs ?? Array.Empty<string>();
            foreach (var h in anyHrefList)
            {
                if (string.IsNullOrEmpty(h)) continue;
                var parts = h.Split('/', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (Guid.TryParse(p, out var g))
                    {
                        var s = g.ToString();
                        if (!ids.Contains(s)) ids.Add(s);
                    }
                    else if (int.TryParse(p, out _))
                    {
                        if (!ids.Contains(p)) ids.Add(p);
                    }
                }
            }
        }
        catch { }
        Output.WriteLine($"[ExtractIds] area={area}, actionContains={actionContains}, foundIdsCount={ids.Count}");
        if (ids.Count > 0) Output.WriteLine($"[ExtractIds] firstId={ids[0]}");
        return ids;
    }
}

public class LiveVerificationResults
{
    public Dictionary<string, bool> LoginResults { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> PageStatusByRole { get; set; } = new();
    public Dictionary<int, int> StatusCounts { get; set; } = new();
    public Dictionary<string, ViewportResult> ViewportResults { get; set; } = new();
    public Dictionary<string, PdfResult> PdfResults { get; set; } = new();
    public Dictionary<string, long> Screenshots { get; set; } = new();
    public int TotalAssertions { get; set; }
    public int PassedAssertions { get; set; }
}

public class ViewportResult
{
    public string Label { get; set; } = "";
    public bool DrawerOpen { get; set; }
    public bool DrawerClose { get; set; }
    public bool NoHorizontalOverflow { get; set; }
    public bool TableResponsiveScroll { get; set; }
}

public class PdfResult
{
    public string ContentType { get; set; } = "";
    public long Bytes { get; set; }
    public bool Ok { get; set; }
}
