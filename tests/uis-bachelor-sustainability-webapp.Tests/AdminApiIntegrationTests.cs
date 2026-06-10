using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace uis_bachelor_sustainability_webapp.Tests;

public class AdminApiIntegrationTests
{
    [Fact]
    public async Task AdminList_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/admin/clothingbrands");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithBootstrapCredentials_AllowsAccessToAdminList()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var login = await client.PostAsJsonAsync("/admin/login", new
        {
            username = TestAppFactory.BootstrapUser,
            password = TestAppFactory.BootstrapPassword,
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var adminList = await client.GetAsync("/admin/clothingbrands");
        Assert.Equal(HttpStatusCode.OK, adminList.StatusCode);
    }

    [Fact]
    public async Task AdminWrite_WithoutCsrfToken_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var login = await client.PostAsJsonAsync("/admin/login", new
        {
            username = TestAppFactory.BootstrapUser,
            password = TestAppFactory.BootstrapPassword,
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var createResponse = await client.PostAsJsonAsync("/admin/clothingbrands", new
        {
            brandName = "NoTokenBrand",
            description = "Missing CSRF should fail",
            category = "Test",
            evidenceSources = Array.Empty<object>(),
            criteriaItems = Array.Empty<object>(),
            certifications = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task AdminWrite_WithCsrfToken_CreatesBrand()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var login = await client.PostAsJsonAsync("/admin/login", new
        {
            username = TestAppFactory.BootstrapUser,
            password = TestAppFactory.BootstrapPassword,
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var csrfResponse = await client.GetAsync("/admin/csrf-token");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenPayload>();
        Assert.NotNull(csrfPayload);
        Assert.False(string.IsNullOrWhiteSpace(csrfPayload!.Token));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/clothingbrands")
        {
            Content = JsonContent.Create(new
            {
                brandName = "TokenBrand",
                description = "Created through integration test",
                category = "Test",
                evidenceSources = Array.Empty<object>(),
                criteriaItems = Array.Empty<object>(),
                certifications = Array.Empty<object>(),
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfPayload.Token);

        var createResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    private sealed record CsrfTokenPayload(string Token);
}

internal sealed class TestAppFactory : WebApplicationFactory<Program>
{
    public const string BootstrapUser = "testadmin";
    public const string BootstrapPassword = "test-password-1234";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=ignored;Username=ignored;Password=ignored",
                ["ADMIN_BOOTSTRAP_USER"] = BootstrapUser,
                ["ADMIN_BOOTSTRAP_PASSWORD"] = BootstrapPassword,
            };

            configBuilder.AddInMemoryCollection(values);
        });
    }
}
