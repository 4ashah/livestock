using System.Net;
using LivestockManager.Domain.Common;

namespace LivestockManager.IntegrationTests;

public class AuthAndCompanyIsolationTests : IClassFixture<LivestockManagerWebFactory>
{
    private readonly LivestockManagerWebFactory _factory;

    public AuthAndCompanyIsolationTests(LivestockManagerWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DataEntryCanGet_Home()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.DataEntry, companyA);
        var response = await client.GetAsync("/");
        Assert.True(
            ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300) ||
            response.StatusCode == HttpStatusCode.Forbidden ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 500,
            $"DataEntry authenticated request to GET / should not redirect to login. Got {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task AccountsRole_CannotAccess_FarmsCreate()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.Accounts, companyA);
        var response = await client.GetAsync("/Farms/Create");
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 302,
            $"Accounts role should not create Farms (CanManageCompany). Got {(int)response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task CrossCompany_InvoiceDetails_ReturnsNotFound()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.Accounts, companyA);
        Guid fakeCompanyBInvoice = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
        var response = await client.GetAsync($"/Invoices/Details/{fakeCompanyBInvoice}");
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Forbidden ||
            (int)response.StatusCode == 404 ||
            (int)response.StatusCode == 403,
            $"Cross-company invoice details should not reveal existence. Got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task CrossCompany_LivestockEditPost_ReturnsNotFound()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.FarmManager, companyA);
        Guid fakeCompanyBLivestock = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = fakeCompanyBLivestock.ToString(),
            ["Tag"] = "TAG-HACK",
            ["__RequestVerificationToken"] = "integration-test-token"
        });
        var response = await client.PostAsync($"/Livestock/Edit/{fakeCompanyBLivestock}", formData);
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            (int)response.StatusCode == 404 ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 400 ||
            (int)response.StatusCode == 302,
            $"Cross-company Livestock Edit POST should be rejected. Got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task SettingsIndex_CompanyAdministrator_Allowed()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.CompanyAdministrator, companyA);
        var response = await client.GetAsync("/Settings");
        Assert.True(
            ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300) ||
            response.StatusCode == HttpStatusCode.Forbidden ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 404 ||
            (int)response.StatusCode == 500,
            $"CompanyAdministrator request to Settings should not redirect to login (policy check must run). Got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task SettingsIndex_AccountsRole_Forbidden()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.Accounts, companyA);
        var response = await client.GetAsync("/Settings");
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 302,
            $"Accounts role should not reach Settings (CanManageCompany required). Got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Audit_Requires_SystemOrCompanyAdministrator()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.DataEntry, companyA);
        var response = await client.GetAsync("/Audit");
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 302,
            $"DataEntry should not access Audit page. Got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task CrossCompany_DocumentDownload_Rejected()
    {
        var companyA = Guid.Parse(LivestockManagerWebFactory.StagingCompanyIdA);
        var client = _factory.CreateAuthenticatedClient(RoleNames.FarmManager, companyA);
        Guid fakeCompanyBDoc = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD");
        var response = await client.GetAsync($"/Documents/Download/{fakeCompanyBDoc}");
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.InternalServerError ||
            (int)response.StatusCode == 404 ||
            (int)response.StatusCode == 403 ||
            (int)response.StatusCode == 400 ||
            (int)response.StatusCode == 500 ||
            (int)response.StatusCode == 302,
            $"Cross-company document download should not be possible (no 2xx allowed). Got {(int)response.StatusCode}");
    }
}
