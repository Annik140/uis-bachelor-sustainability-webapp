using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace uis_bachelor_sustainability_webapp.Tests;

public class AdminApiSecurityAndAuthTests
{
    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/admin/login", new
        {
            username = TestAppFactory.BootstrapUser,
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingCredentials_ReturnsUnauthorized()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/admin/login", new
        {
            username = "",
            password = ""
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_AfterLogin_ReturnsAuthenticatedUser()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var response = await client.GetAsync("/admin/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SessionPayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.Authenticated);
        Assert.Equal(TestAppFactory.BootstrapUser, payload.Username);
    }

    [Fact]
    public async Task CsrfToken_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/admin/csrf-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_AfterLogin_InvalidatesSession()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/logout");
        logoutRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var sessionResponse = await client.GetAsync("/admin/session");
        Assert.Equal(HttpStatusCode.Unauthorized, sessionResponse.StatusCode);
    }

    [Fact]
    public async Task PublicBrands_WithoutAuth_ReturnsOk()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/brands");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicBrandById_NotFound_ReturnsNotFound()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/brands/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicBrandById_WithExistingBrand_ReturnsOk()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        var brandId = await CreateBrandAsync(client, csrfToken, "Detail Brand");

        var response = await client.GetAsync($"/brands/{brandId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<BrandPayload>();
        Assert.NotNull(payload);
        Assert.Equal(brandId, payload!.Id);
        Assert.Equal("Detail Brand", payload.BrandName);
    }

    [Fact]
    public async Task PublicBrands_WithSearchQuery_FiltersResults()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);

        await CreateBrandAsync(client, csrfToken, "NordicWear Alpha");
        await CreateBrandAsync(client, csrfToken, "UrbanThread Beta");

        var response = await client.GetAsync("/brands?q=nordicwear");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<BrandPayload>>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Items);
        Assert.Equal("NordicWear Alpha", payload.Items[0].BrandName);
    }

    [Fact]
    public async Task PublicBrands_WithAlphabeticalSort_ReturnsSorted()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);

        await CreateBrandAsync(client, csrfToken, "Zulu Wear");
        await CreateBrandAsync(client, csrfToken, "Alpha Wear");

        var response = await client.GetAsync("/brands?sort=alphabeticalAsc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<BrandPayload>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Items.Count >= 2);
        var names = payload.Items.Select(item => item.BrandName).ToList();
        Assert.True(names.IndexOf("Alpha Wear") < names.IndexOf("Zulu Wear"));
    }

    [Fact]
    public async Task PublicBrands_WithPageSizeAboveLimit_ClampsTo100()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/brands?page=1&pageSize=999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<BrandPayload>>();
        Assert.NotNull(payload);
        Assert.Equal(100, payload!.PageSize);
    }

    [Fact]
    public async Task CreateBrand_WithInvalidInput_ReturnsValidationProblem()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var request = BuildJsonRequest(HttpMethod.Post, "/admin/clothingbrands", csrfToken, new
        {
            brandName = "",
            logoPath = "not-a-logo-path",
            description = new string('x', 1101),
            evidenceSources = new[]
            {
                new { sourceTitle = "Bad URL", sourceUrl = "javascript:alert(1)" }
            },
            criteriaItems = new[]
            {
                new { category = "Material", name = "Fiber traceability", numericValue = 120, weight = 50 }
            },
            certifications = new[]
            {
                new { name = "" }
            }
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBrand_WithoutCsrfToken_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        var brandId = await CreateBrandAsync(client, csrfToken, "Needs Update");

        var response = await client.PutAsJsonAsync($"/admin/clothingbrands/{brandId}", BuildValidBrandPayload("Updated Name"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBrand_WithoutCsrfToken_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        var brandId = await CreateBrandAsync(client, csrfToken, "Needs Delete");

        var response = await client.DeleteAsync($"/admin/clothingbrands/{brandId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdminUser_WithoutCsrfToken_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var response = await client.PostAsJsonAsync("/admin/users", new
        {
            username = "csrf-missing-admin",
            password = "very-strong-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBrand_WithCsrfToken_UpdatesData()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        var brandId = await CreateBrandAsync(client, csrfToken, "Original Name");

        using var updateRequest = BuildJsonRequest(HttpMethod.Put, $"/admin/clothingbrands/{brandId}", csrfToken, BuildValidBrandPayload("Updated Brand Name"));
        var updateResponse = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var fetchResponse = await client.GetAsync($"/admin/clothingbrands/{brandId}");
        Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);
        var brand = await fetchResponse.Content.ReadFromJsonAsync<BrandPayload>();
        Assert.NotNull(brand);
        Assert.Equal("Updated Brand Name", brand!.BrandName);
    }

    [Fact]
    public async Task DeleteBrand_WithCsrfToken_RemovesBrand()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        var brandId = await CreateBrandAsync(client, csrfToken, "Delete Candidate");

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/admin/clothingbrands/{brandId}");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/admin/clothingbrands/{brandId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task UploadLogo_WithoutMultipartFormData_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = JsonContent.Create(new { test = "not multipart" })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Expected multipart/form-data", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadLogo_WithoutCsrfToken_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(fileContent, "file", "logo.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = multipart
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadLogo_WithMissingFile_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("value"), "notFile");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = multipart
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("No file was uploaded", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadLogo_WithUnsupportedExtension_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-binary"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        multipart.Add(fileContent, "file", "logo.exe");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = multipart
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unsupported image format", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadLogo_WithOversizedFile_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[(3 * 1024 * 1024) + 1]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(fileContent, "file", "big.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = multipart
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("3MB or smaller", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadLogo_WithValidFile_ReturnsLogoPath_AndCanBeDeleted()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(fileContent, "file", "logo.png");

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = multipart
        };
        uploadRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var uploadResponse = await client.SendAsync(uploadRequest);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var uploadPayload = await uploadResponse.Content.ReadFromJsonAsync<UploadLogoPayload>();
        Assert.NotNull(uploadPayload);
        Assert.False(string.IsNullOrWhiteSpace(uploadPayload!.LogoPath));
        Assert.StartsWith("/brand-logos/", uploadPayload.LogoPath, StringComparison.Ordinal);

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/admin/upload-logo?logoPath={Uri.EscapeDataString(uploadPayload.LogoPath)}");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUploadLogo_WithMissingPath_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/admin/upload-logo");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUploadLogo_WithPathTraversalLikeValue_ReturnsNoContent()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        var maliciousPath = "../outside.png";
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/admin/upload-logo?logoPath={Uri.EscapeDataString(maliciousPath)}");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdminUser_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/admin/users", new
        {
            username = "newadmin",
            password = "very-strong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdminUser_WithShortPassword_ReturnsBadRequest()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var request = BuildJsonRequest(HttpMethod.Post, "/admin/users", csrfToken, new
        {
            username = "newadmin",
            password = "short"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdminUser_WithDuplicateUsername_ReturnsConflict()
    {
        await using var factory = new TestAppFactory();
        using var client = CreateClient(factory);
        await LoginAsync(client);

        var csrfToken = await GetCsrfTokenAsync(client);

        using var createFirst = BuildJsonRequest(HttpMethod.Post, "/admin/users", csrfToken, new
        {
            username = "another-admin",
            password = "very-strong-password"
        });
        var firstResponse = await client.SendAsync(createFirst);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var createDuplicate = BuildJsonRequest(HttpMethod.Post, "/admin/users", csrfToken, new
        {
            username = "another-admin",
            password = "different-strong-password"
        });
        var duplicateResponse = await client.SendAsync(createDuplicate);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    private static HttpClient CreateClient(TestAppFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var login = await client.PostAsJsonAsync("/admin/login", new
        {
            username = TestAppFactory.BootstrapUser,
            password = TestAppFactory.BootstrapPassword,
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var csrfResponse = await client.GetAsync("/admin/csrf-token");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);

        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenPayload>();
        Assert.NotNull(csrfPayload);
        Assert.False(string.IsNullOrWhiteSpace(csrfPayload!.Token));

        return csrfPayload.Token;
    }

    private static HttpRequestMessage BuildJsonRequest(HttpMethod method, string url, string csrfToken, object payload)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return request;
    }

    private static object BuildValidBrandPayload(string brandName)
    {
        return new
        {
            brandName,
            description = "Integration test brand",
            evidenceSources = new[]
            {
                new
                {
                    sourceTitle = "Test Source",
                    sourceUrl = "https://example.com/report"
                }
            },
            criteriaItems = new[]
            {
                new
                {
                    category = "Material",
                    name = "Fiber traceability",
                    numericValue = 80m,
                    unit = "%",
                    weight = 1m
                }
            },
            certifications = new[]
            {
                new { name = "GOTS" }
            }
        };
    }

    private static async Task<int> CreateBrandAsync(HttpClient client, string csrfToken, string brandName)
    {
        using var request = BuildJsonRequest(HttpMethod.Post, "/admin/clothingbrands", csrfToken, BuildValidBrandPayload(brandName));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        Assert.True(document.RootElement.TryGetProperty("id", out var idElement));
        return idElement.GetInt32();
    }

    private sealed record CsrfTokenPayload(string Token);

    private sealed record UploadLogoPayload(string LogoPath);

    private sealed record SessionPayload(bool Authenticated, string Username);

    private sealed record BrandPayload(int Id, string BrandName);

    private sealed record PagedPayload<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
