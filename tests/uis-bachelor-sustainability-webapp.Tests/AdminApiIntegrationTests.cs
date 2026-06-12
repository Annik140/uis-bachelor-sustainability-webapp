using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

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

        var login = await TestHttpHelpers.LoginAsync(client);

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

        var login = await TestHttpHelpers.LoginAsync(client);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var createResponse = await client.PostAsJsonAsync("/admin/clothingbrands", new
        {
            brandName = "NoTokenBrand",
            description = "Missing CSRF should fail",
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

        var login = await TestHttpHelpers.LoginAsync(client);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var csrfToken = await TestHttpHelpers.GetCsrfTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/clothingbrands")
        {
            Content = JsonContent.Create(new
            {
                brandName = "TokenBrand",
                description = "Created through integration test",
                evidenceSources = Array.Empty<object>(),
                criteriaItems = Array.Empty<object>(),
                certifications = Array.Empty<object>(),
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var createResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }
}
