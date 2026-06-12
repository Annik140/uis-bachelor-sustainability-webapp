using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using uis_bachelor_sustainability_webapp.Data;

namespace uis_bachelor_sustainability_webapp.Tests;

public class AdminApiFailureInjectionTests
{
    [Fact]
    public async Task CreateBrand_WhenDbCommitThrows_Returns500()
    {
        await using var factory = CreateFailureFactory();
        using var client = CreateClient(factory);

        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        SetFailureMode(factory, TestFailureMode.DbCommit);

        using var request = BuildJsonRequest(HttpMethod.Post, "/admin/clothingbrands", csrfToken, BuildValidBrandPayload("Failure Create"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Failed to create brand", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBrand_WhenDbCommitThrows_Returns500()
    {
        await using var factory = CreateFailureFactory();
        using var client = CreateClient(factory);

        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);

        SetFailureMode(factory, TestFailureMode.None);
        var brandId = await CreateBrandAsync(client, csrfToken, "Original Update Target");

        SetFailureMode(factory, TestFailureMode.DbCommit);
        using var request = BuildJsonRequest(HttpMethod.Put, $"/admin/clothingbrands/{brandId}", csrfToken, BuildValidBrandPayload("Updated Failure"));
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Failed to update brand", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteBrand_WhenDbCommitThrows_Returns500()
    {
        await using var factory = CreateFailureFactory();
        using var client = CreateClient(factory);

        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);

        SetFailureMode(factory, TestFailureMode.None);
        var brandId = await CreateBrandAsync(client, csrfToken, "Original Delete Target");

        SetFailureMode(factory, TestFailureMode.DbCommit);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/admin/clothingbrands/{brandId}");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Failed to delete brand", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadLogo_WhenFileIoThrows_Returns507()
    {
        await using var factory = CreateFailureFactory();
        using var client = CreateClient(factory);

        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        SetFailureMode(factory, TestFailureMode.UploadIo);

        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(fileContent, "file", "logo.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/admin/upload-logo")
        {
            Content = multipart
        };
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InsufficientStorage, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Failed to save logo", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteUploadLogo_WhenFileIoThrows_ReturnsNoContent()
    {
        await using var factory = CreateFailureFactory();
        using var client = CreateClient(factory);

        await LoginAsync(client);
        var csrfToken = await GetCsrfTokenAsync(client);
        SetFailureMode(factory, TestFailureMode.DeleteIo);

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/admin/upload-logo?logoPath=%2Fbrand-logos%2Ffailure.png");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static WebApplicationFactory<Program> CreateFailureFactory()
    {
        return new TestAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<TestFailureSwitch>();
                services.AddScoped<IDbCommitter, FailingDbCommitter>();
                services.AddSingleton<ILogoFileOperations, FailingLogoFileOperations>();
            });
        });
    }

    private static void SetFailureMode(WebApplicationFactory<Program> factory, TestFailureMode mode)
    {
        var failureSwitch = factory.Services.GetRequiredService<TestFailureSwitch>();
        failureSwitch.Mode = mode;
    }

    private static async Task LoginAsync(HttpClient client)
    {
        var login = await TestHttpHelpers.LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        return await TestHttpHelpers.GetCsrfTokenAsync(client);
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

        var payload = await response.Content.ReadFromJsonAsync<CreatedBrandPayload>();
        Assert.NotNull(payload);
        return payload!.Id;
    }

    private sealed record CreatedBrandPayload(int Id);
}

internal enum TestFailureMode
{
    None,
    DbCommit,
    UploadIo,
    DeleteIo,
}

internal sealed class TestFailureSwitch
{
    public TestFailureMode Mode { get; set; } = TestFailureMode.None;
}

internal sealed class FailingDbCommitter(TestFailureSwitch failureSwitch) : IDbCommitter
{
    public Task CommitAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (failureSwitch.Mode == TestFailureMode.DbCommit)
        {
            throw new DbUpdateException("Simulated database commit failure for test.");
        }

        return db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class FailingLogoFileOperations(TestFailureSwitch failureSwitch) : ILogoFileOperations
{
    public void EnsureDirectory(string path)
    {
        if (failureSwitch.Mode == TestFailureMode.UploadIo)
        {
            throw new IOException("Simulated directory creation failure for test.");
        }

        Directory.CreateDirectory(path);
    }

    public Stream CreateWriteStream(string fullPath)
    {
        if (failureSwitch.Mode == TestFailureMode.UploadIo)
        {
            throw new IOException("Simulated file creation failure for test.");
        }

        return File.Create(fullPath);
    }

    public bool FileExists(string fullPath)
    {
        if (failureSwitch.Mode == TestFailureMode.DeleteIo)
        {
            return true;
        }

        return File.Exists(fullPath);
    }

    public void DeleteFile(string fullPath)
    {
        if (failureSwitch.Mode == TestFailureMode.DeleteIo)
        {
            throw new IOException("Simulated file delete failure for test.");
        }

        File.Delete(fullPath);
    }
}
